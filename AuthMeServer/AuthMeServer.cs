using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Fougerite;
using Fougerite.Events;
using RustBuster2016Server;

namespace AuthMeServer
{
    public class AuthMeServer : Fougerite.Module
    {
        internal bool FoundRB = false;
        internal readonly List<ulong> WaitingUsers = new List<ulong>();
        internal readonly List<ulong> SpawnedUsers = new List<ulong>();
        public IniParser Auths;
        internal readonly Dictionary<ulong, Credential> Credentials = new Dictionary<ulong, Credential>();
        public const string red = "[color #FF0000]";
        public const string yellow = "[color yellow]";
        public const string green = "[color green]";
        public const string orange = "[color #ffa500]";
        
        // todo: Timer for new connections and kick for afk.
        // todo: gui
        
        public override string Name
        {
            get { return "AuthMeServer"; }
        }

        public override string Author
        {
            get { return "DreTaX"; }
        }

        public override string Description
        {
            get { return "AuthMeServer"; }
        }

        public override Version Version
        {
            get { return new Version("1.0"); }
        }
        
        public override void Initialize()
        {
            if (!File.Exists(ModuleFolder + "\\Data.ini"))
            {
                File.Create(ModuleFolder + "\\Data.ini").Dispose();
            }
            Auths = new IniParser(ModuleFolder + "\\Data.ini");
            foreach (string id in Auths.EnumSection("Login"))
            {
                string userpw = Auths.GetSetting("Login", id);
                try
                {
                    ulong uid = ulong.Parse(id);
                    string[] spl = userpw.Split(new string[] { "---##---" }, StringSplitOptions.None);
                    Credentials.Add(uid, new Credential(spl[0].ToLower(), spl[1]));
                }
                catch (Exception ex)
                {
                    Logger.LogError("[AuthMe] Invalid data at: " + id + " " + userpw + " Error: " + ex);
                }
            }
            
            Hooks.OnCommand += OnCommand;
            Hooks.OnPlayerHurt += OnPlayerHurt;
            Hooks.OnPlayerDisconnected += OnPlayerDisconnected;
            Hooks.OnItemRemoved += OnItemRemoved;
            Hooks.OnChat += OnChat;
            Hooks.OnEntityHurt += OnEntityHurt;
            Hooks.OnPlayerConnected += OnPlayerConnected;
            Hooks.OnPlayerSpawned += OnPlayerSpawned;
        }

        public override void DeInitialize()
        {
            Hooks.OnCommand -= OnCommand;
            Hooks.OnPlayerHurt -= OnPlayerHurt;
            Hooks.OnPlayerDisconnected -= OnPlayerDisconnected;
            Hooks.OnItemRemoved -= OnItemRemoved;
            Hooks.OnChat -= OnChat;
            Hooks.OnEntityHurt -= OnEntityHurt;
            Hooks.OnPlayerConnected -= OnPlayerConnected;
            Hooks.OnPlayerSpawned -= OnPlayerSpawned;
        }

        private void OnPlayerSpawned(Fougerite.Player player, SpawnEvent se)
        {
            if (SpawnedUsers.Contains(player.UID))
            {
                WaitingUsers.Add(player.UID);
                SpawnedUsers.Remove(player.UID);
            }
        }

        private void OnPlayerConnected(Fougerite.Player player)
        {
            if (!SpawnedUsers.Contains(player.UID))
            {
                SpawnedUsers.Add(player.UID);
            }
        }

        private void OnEntityHurt(HurtEvent he)
        {
            if (he.AttackerIsPlayer && he.Attacker != null)
            {
                Fougerite.Player attacker = (Fougerite.Player) he.Attacker;
                if (WaitingUsers.Contains(attacker.UID))
                {
                    attacker.MessageFrom("AuthMe", red + "You can't do this.");
                    he.DamageAmount = 0f;
                }
            }
        }

        private void OnChat(Fougerite.Player player, ref ChatString text)
        {
            if (WaitingUsers.Contains(player.UID))
            {
                text.NewText = string.Empty;
            }
        }

        private void OnItemRemoved(InventoryModEvent e)
        {
            if (e.Player != null)
            {
                if (WaitingUsers.Contains(e.Player.UID))
                {
                    e.Cancel();
                }
            }
        }

        private void OnPlayerDisconnected(Fougerite.Player player)
        {
            if (WaitingUsers.Contains(player.UID))
            {
                WaitingUsers.Remove(player.UID);
            }

            if (SpawnedUsers.Contains(player.UID))
            {
                SpawnedUsers.Remove(player.UID);
            }
        }

        private void OnPlayerHurt(HurtEvent he)
        {
            if (he.VictimIsPlayer && he.Victim != null)
            {
                Fougerite.Player player = (Fougerite.Player) he.Victim;
                if (WaitingUsers.Contains(player.UID))
                {
                    if (he.AttackerIsPlayer && he.Attacker != null)
                    {
                        Fougerite.Player attacker = (Fougerite.Player) he.Attacker;
                        attacker.MessageFrom("AuthMe", red + "This player haven't logged in yet! He will be kicked if he is AFK.");
                    }
                    he.DamageAmount = 0f;
                }
                else
                {
                    if (he.AttackerIsPlayer && he.Attacker != null)
                    {
                        Fougerite.Player attacker = (Fougerite.Player) he.Attacker;
                        if (WaitingUsers.Contains(attacker.UID))
                        {
                            attacker.MessageFrom("AuthMe", red + "You can't do this.");
                            he.DamageAmount = 0f;
                        }
                    }
                }
            }
        }

        private void OnCommand(Fougerite.Player player, string cmd, string[] args)
        {
            if (cmd == "authme")
            {
                if (args.Length == 0)
                {
                    player.MessageFrom("AuthMe", orange + "AuthMe V" + Version + " By " + Author);
                    player.MessageFrom("AuthMe", yellow + "-- Passwords are stored using SHA1 hashes --");
                    player.MessageFrom("AuthMe", yellow + "-- Always use a different password than your emails, etc. --");
                    player.MessageFrom("AuthMe", "/authme register username password");
                    player.MessageFrom("AuthMe", "/authme login username password");
                    if (player.Admin || player.Moderator)
                    {
                        player.MessageFrom("AuthMe", "/authme resetuser ingamename");
                    }
                }
                else if (args.Length == 2)
                {
                    string subcmd = args[0];
                    switch (subcmd)
                    {
                        case "resetuser":
                            if (player.Admin || player.Moderator)
                            {
                                Fougerite.Player plr = Fougerite.Server.GetServer().FindPlayer(args[1]);
                                if (plr != null)
                                {
                                    if (Auths.GetSetting("Login", plr.SteamID) != null)
                                    {
                                        Auths.DeleteSetting("Login", plr.SteamID);
                                        Auths.Save();
                                    }

                                    if (Credentials.ContainsKey(plr.UID))
                                    {
                                        Credentials.Remove(plr.UID);
                                    }
                                    
                                    plr.MessageFrom("AuthMe", "User reset! He can now register a new account for that steamid.");
                                }
                            }

                            break;
                    }
                }
                else if (args.Length == 3)
                {
                    string subcmd = args[0];
                    switch (subcmd)
                    {
                        case "register":
                            if (Credentials.ContainsKey(player.UID))
                            {
                                player.MessageFrom("AuthMe", "This STEAMID is already protected using password authentication!");
                                return;
                            }
                            string username = args[1];
                            string password = args[2];

                            if (username == "username" || password == "password")
                            {
                                player.MessageFrom("AuthMe", "Type /authme register username password");
                                return;
                            }
                            
                            bool b = Regex.IsMatch(username, @"^[a-zA-Z0-9_#&@%!+<>]+$");
                            bool b2 = Regex.IsMatch(password, @"^[a-zA-Z0-9_#&@%!+<>]+$");

                            if (!b || !b2)
                            {
                                player.MessageFrom("AuthMe", "Sorry, no special characters or space! Only: a-zA-Z0-9_#&@%!+<>");
                                return;
                            }
                            
                            string hash = SHA1Hash(password);
                            Auths.AddSetting("Login", player.SteamID, username.ToLower() + "---##---" + hash);
                            Auths.Save();
                            Credentials.Add(player.UID, new Credential(username.ToLower(), hash));
                            player.MessageFrom("AuthMe", orange + "You have registered with: " + username + " - " + password + " (Your console has this info now too.)");
                            player.SendConsoleMessage(orange + "You have registered with: " + username + " - " + password);
                            player.MessageFrom("AuthMe", "Please login using: /authme login username password");
                            break;
                        case "login":
                            if (!WaitingUsers.Contains(player.UID))
                            {
                                player.MessageFrom("AuthMe", "You are logged in already.");
                                return;
                            }
    
                            string username2 = args[1];
                            string password2 = args[2];
                            
                            if (!Credentials.ContainsKey(player.UID))
                            {
                                player.MessageFrom("AuthMe", "This steamid is not registered yet!");
                                player.MessageFrom("AuthMe", "Type /authme register username password");
                                return;
                            }

                            Credential cred = Credentials[player.UID];
                            if (cred.Username.ToLower() != username2.ToLower())
                            {
                                player.MessageFrom("AuthMe", "Invalid username!");
                                return;
                            }
    
                            if (cred.HashedPassword != SHA1Hash(password2))
                            {
                                player.MessageFrom("AuthMe", "Invalid password! Seek admin for help on their social site.");
                            }
                            else
                            {
                                WaitingUsers.Remove(player.UID);
                                uLink.NetworkView.Get(player.PlayerClient.networkView)
                                    .RPC("DestroyFreeze", player.NetworkPlayer);
                                player.MessageFrom("AuthMe", "Successfully logged in!");
                            }
                            break;
                        case "changepw":
                            if (WaitingUsers.Contains(player.UID))
                            {
                                player.MessageFrom("AuthMe", "Nice try. You need to be logged in to do that.");
                                return;
                            }
                            
                            string username3 = args[1];
                            string password3 = args[2];
                            if (!Credentials.ContainsKey(player.UID))
                            {
                                player.MessageFrom("AuthMe", "This steamid is not registered yet!");
                                return;
                            }

                            Credential cred2 = Credentials[player.UID];
                            if (cred2.Username.ToLower() != username3.ToLower())
                            {
                                player.MessageFrom("AuthMe", "Invalid username!");
                                return;
                            }

                            Credentials.Remove(player.UID);
                            
                            string hash3 = SHA1Hash(password3);
                            Auths.SetSetting("Login", player.SteamID, username3.ToLower() + "---##---" + hash3);
                            Auths.Save();
                            Credentials.Add(player.UID, new Credential(username3.ToLower(), hash3));
                            
                            player.MessageFrom("AuthMe", "Password successfully changed!");
                            break;
                        default:
                            player.MessageFrom("AuthMe", "Invalid command. Type /authme for help.");
                            break;
                    }
                }
            }
        }

        private string SHA1Hash(string input)
        {
            var hash = (new SHA1Managed()).ComputeHash(Encoding.UTF8.GetBytes(input));
            return string.Join("", hash.Select(b => b.ToString("x2")).ToArray());
        }
    }
}