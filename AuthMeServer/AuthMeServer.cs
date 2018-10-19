using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Fougerite;
using Fougerite.Events;
using RustBuster2016Server;

namespace AuthMeServer
{
    public class AuthMeServer : Fougerite.Module
    {
        internal bool FoundRB = false;
        internal readonly List<ulong> WaitingUsers = new List<ulong>();
        public IniParser Auths;
        internal readonly Dictionary<string, string> Credentials = new Dictionary<string, string>();
        internal readonly Dictionary<ulong, string> IDWithUser = new Dictionary<ulong, string>();
        public const string red = "[color #FF0000]";
        public const string yellow = "[color yellow]";
        public const string green = "[color green]";
        public const string orange = "[color #ffa500]";
        // todo: Timer for new connections and kick for afk.
        // todo: moderator and admin, change password.
        // todo: Hook permission manager.
        
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
            foreach (var x in Auths.EnumSection("Login"))
            {
                string id = Auths.GetSetting("Login", x);
                try
                {
                    ulong uid = ulong.Parse(id);
                    string[] spl = x.Split(new string[] { "---##---" }, StringSplitOptions.None);
                    Credentials.Add(spl[0].ToLower(), spl[1]);
                    IDWithUser.Add(uid, spl[0].ToLower());
                }
                catch (Exception ex)
                {
                    Logger.LogError("[AuthMe] Invalid data at: " + x + " " + id + " Error: " + ex);
                }
            }
            
            Hooks.OnModulesLoaded += OnModulesLoaded;
            Hooks.OnCommand += OnCommand;
            Hooks.OnPlayerHurt += OnPlayerHurt;
            Hooks.OnPlayerDisconnected += OnPlayerDisconnected;
            Hooks.OnItemRemoved += OnItemRemoved;
        }

        public override void DeInitialize()
        {
            Hooks.OnModulesLoaded -= OnModulesLoaded;
            Hooks.OnCommand -= OnCommand;
            Hooks.OnPlayerHurt -= OnPlayerHurt;
            Hooks.OnPlayerDisconnected -= OnPlayerDisconnected;
            Hooks.OnItemRemoved -= OnItemRemoved;
            if (FoundRB)
            {
                RustBuster2016Server.API.OnRustBusterUserMessage -= OnRustBusterUserMessage;
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
                    player.MessageFrom("AuthMe", yellow + "-- Always use a different password than you emails, etc. --");
                    player.MessageFrom("AuthMe", "/authme register username password");
                    player.MessageFrom("AuthMe", "/authme login username password");
                }
                else if (args.Length == 3)
                {
                    string subcmd = args[0];
                    switch (subcmd)
                    {
                        case "register":
                            if (IDWithUser.ContainsKey(player.UID))
                            {
                                player.MessageFrom("AuthMe", "This STEAMID is already protected using password authentication!");
                                return;
                            }
                            string username = args[1];
                            string password = args[2];
                            string hash = SHA1Hash(password);
                            Auths.AddSetting("Login", username.ToLower() + "---##---" + hash, player.SteamID);
                            Auths.Save();
                            IDWithUser.Add(player.UID, username.ToLower());
                            Credentials.Add(username.ToLower(), hash);
                            player.MessageFrom("AuthMe", orange + "You have registered with: " + username + " - " + password + " (Your console has this info now too.)");
                            player.SendConsoleMessage(orange + "You have registered with: " + username + " - " + password);
                            player.MessageFrom("AuthMe", "Please login using: /authme login username password");
                            break;
                        case "login":
                            if (!WaitingUsers.Contains(player.UID))
                            {
                                player.MessageFrom("AuthMe", "You are either logged in already, or you don't have password protection.");
                                return;
                            }
    
                            string username2 = args[1];
                            string password2 = args[2];
                            if (!Credentials.ContainsKey(username2.ToLower()))
                            {
                                player.MessageFrom("AuthMe", "Invalid username!");
                                return;
                            }
                            // todo: Detect if user has different id.
    
                            if (Credentials[username2.ToLower()] != SHA1Hash(password2))
                            {
                                player.MessageFrom("AuthMe", "Invalid password!");
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
                            if (!WaitingUsers.Contains(player.UID))
                            {
                                player.MessageFrom("AuthMe", "Nice try. You need to be logged in to do that.");
                                return;
                            }
                            //todo: Make sure nobody else is able to change password for different accounts.
                            string username3 = args[1];
                            string password3 = args[2];
                            if (!Credentials.ContainsKey(username3.ToLower()))
                            {
                                player.MessageFrom("AuthMe", "Invalid username!");
                                return;
                            }
                            break;
                        default:
                            player.MessageFrom("AuthMe", "Invalid command. Type /authme for help.");
                            break;
                    }
                }
            }
        }

        private void OnModulesLoaded()
        {
            foreach (var x in Fougerite.ModuleManager.Modules)
            {
                if (x.Plugin.Name.ToLower().Contains("rustbuster"))
                {
                    FoundRB = true;
                    break;
                }
            }

            if (FoundRB)
            {
                RustBuster2016Server.API.OnRustBusterUserMessage += OnRustBusterUserMessage;
            }
        }

        private void OnRustBusterUserMessage(API.RustBusterUserAPI user, Message msgc)
        {
            if (msgc.PluginSender == "AuthMe")
            {
                string[] spl = msgc.MessageByClient.Split('-');
                string name = spl[0];
                switch (name)
                {
                    case "AuthMeR":
                        ulong id = ulong.Parse(user.SteamID);
                        msgc.ReturnMessage = IDWithUser.ContainsKey(id) ? "has" : "no";
                        WaitingUsers.Add(id);
                        break;
                    case "AuthMeL":
                        if (Credentials.ContainsKey(spl[1].ToLower()))
                        {
                            if (Credentials[spl[1].ToLower()] == spl[2])
                            {
                                msgc.ReturnMessage = "yes";
                                return;
                            }
                        }

                        msgc.ReturnMessage = "no";
                        break;
                }
            }
        }
        
        public string SHA1Hash(string input)
        {
            var hash = (new SHA1Managed()).ComputeHash(Encoding.UTF8.GetBytes(input));
            return string.Join("", hash.Select(b => b.ToString("x2")).ToArray());
        }
    }
}