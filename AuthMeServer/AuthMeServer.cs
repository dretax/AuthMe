using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Fougerite;
using Fougerite.Concurrent;
using Fougerite.Events;
using Fougerite.Permissions;
using UnityEngine;

namespace AuthMeServer
{
    public class AuthMeServer : Fougerite.Module
    {
        private static AuthMeServer _instance;
        internal bool FoundRB = false;
        internal readonly ConcurrentDictionary<ulong, PrivilegeStorage> WaitingUsers = new ConcurrentDictionary<ulong, PrivilegeStorage>();
        internal readonly ConcurrentList<ulong> SpawnedUsers = new ConcurrentList<ulong>();
        internal readonly ConcurrentDictionary<ulong, Credential> Credentials = new ConcurrentDictionary<ulong, Credential>();
        
        public IniParser Auths;
        public IniParser Settings;
        public static string AuthLogPath;
        public readonly List<string> RestrictedCommands = new List<string>();
        public readonly List<string> RestrictedConsoleCommands = new List<string>();
        public const string red = "[color #FF0000]";
        public const string yellow = "[color yellow]";
        public const string green = "[color green]";
        public const string orange = "[color #ffa500]";
        public string YouNeedToBeLoggedIn = "You can't do this. You need to be logged in.";
        public string PleaseLoginOrRegister = "Please use /authme register or /authme login to authenticate";
        public string YouWillBeKicked = "Otherwise you will be KICKED!";
        public string CredsReset = "Your credentials are reset! Type /authme register username password";
        public string SocialSiteForHelp = "InsertYourSiteHere";
        public bool RestrictAllConsoleCommands = false;
        public int TimeToLogin = 120;
        public int HelpTextTime = 20;
        public bool RemovePermissionsUntilLogin = true;
        
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
            get { return new Version("1.3"); }
        }
        
        public override void Initialize()
        {
            _instance = this;
            if (!File.Exists(ModuleFolder + "\\Data.ini"))
            {
                File.Create(ModuleFolder + "\\Data.ini").Dispose();
            }

            AuthLogPath = ModuleFolder + "\\Logs";
            if (!Directory.Exists(AuthLogPath))
            {
                Directory.CreateDirectory(AuthLogPath);
            }

            AuthLogger.LogWriterInit();
            ReloadConfig();
            DataStore.GetInstance().Flush("AuthMeLogin");
            
            Auths = new IniParser(ModuleFolder + "\\Data.ini");

            Thread t = new Thread(() =>
            {
                foreach (string id in Auths.EnumSection("Login"))
                {
                    string userpw = Auths.GetSetting("Login", id);
                    try
                    {
                        ulong uid = ulong.Parse(id);
                        string[] spl = userpw.Split(new string[] {"---##---"}, StringSplitOptions.None);
                        Credentials.TryAdd(uid, new Credential(spl[0].ToLower(), spl[1]));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("[AuthMe] Invalid data at: " + id + " " + userpw + " Error: " + ex);
                    }
                }
            });
            t.IsBackground = true;
            t.Start();
            
            Hooks.OnCommand += OnCommand;
            Hooks.OnPlayerHurt += OnPlayerHurt;
            Hooks.OnPlayerDisconnected += OnPlayerDisconnected;
            Hooks.OnItemRemoved += OnItemRemoved;
            Hooks.OnChat += OnChat;
            Hooks.OnEntityHurt += OnEntityHurt;
            Hooks.OnPlayerConnected += OnPlayerConnected;
            Hooks.OnPlayerSpawned += OnPlayerSpawned;
            Hooks.OnEntityDeployedWithPlacer += OnEntityDeployedWithPlacer;
            Hooks.OnCrafting += OnCrafting;
            Hooks.OnResearch += OnResearch;
            Hooks.OnItemAdded += OnItemAdded;
            Hooks.OnConsoleReceivedWithCancel += OnConsoleReceived;
            Hooks.OnModulesLoaded += OnModulesLoaded;
            Hooks.OnBeltUse += OnBeltUse;
            Hooks.OnLootUse += OnLootUse;
            Hooks.OnItemPickup += OnItemPickup;
            Hooks.OnDoorUse += OnDoorUse;
        }

        public override void DeInitialize()
        {
            if (FoundRB)
            {
                RustBuster2016Server.API.OnRustBusterUserMessage -= OnRustBusterUserMessage;
            }
            
            Hooks.OnCommand -= OnCommand;
            Hooks.OnPlayerHurt -= OnPlayerHurt;
            Hooks.OnPlayerDisconnected -= OnPlayerDisconnected;
            Hooks.OnItemRemoved -= OnItemRemoved;
            Hooks.OnChat -= OnChat;
            Hooks.OnEntityHurt -= OnEntityHurt;
            Hooks.OnPlayerConnected -= OnPlayerConnected;
            Hooks.OnPlayerSpawned -= OnPlayerSpawned;
            Hooks.OnEntityDeployedWithPlacer -= OnEntityDeployedWithPlacer;
            Hooks.OnCrafting -= OnCrafting;
            Hooks.OnResearch -= OnResearch;
            Hooks.OnItemAdded -= OnItemAdded;
            Hooks.OnConsoleReceivedWithCancel -= OnConsoleReceived;
            Hooks.OnBeltUse -= OnBeltUse;
            Hooks.OnLootUse -= OnLootUse;
            Hooks.OnItemPickup -= OnItemPickup;
            Hooks.OnDoorUse -= OnDoorUse;
        }

        /// <summary>
        /// API usage.
        /// Returns the instance.
        /// </summary>
        /// <returns></returns>
        public static AuthMeServer GetInstance()
        {
            return _instance;
        }

        /// <summary>
        /// API usage.
        /// Returns if the user has been logged in.
        /// </summary>
        /// <param name="steamid"></param>
        /// <returns></returns>
        public bool IsLoggedIn(ulong steamid)
        {
            return !WaitingUsers.ContainsKey(steamid) && !SpawnedUsers.Contains(steamid);
        }

        /// <summary>
        /// API usage.
        /// Returns if the user has been logged in.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public bool IsLoggedIn(Fougerite.Player player)
        {
            if (player != null)
            {
                return !WaitingUsers.ContainsKey(player.UID) && !SpawnedUsers.Contains(player.UID);
            }

            return false;
        }

        /// <summary>
        /// API usage.
        /// Returns the user credentials if exists.
        /// </summary>
        /// <param name="steamid"></param>
        /// <returns></returns>
        public Credential GetCredential(ulong steamid)
        {
            if (Credentials.ContainsKey(steamid))
            {
                return Credentials[steamid];
            }

            return null;
        }
        
        /// <summary>
        /// API usage.
        /// Returns the user credentials if exists.
        /// </summary>
        /// <param name="steamid"></param>
        /// <returns></returns>
        public Credential GetCredential(Fougerite.Player player)
        {
            Credential credential;
            Credentials.TryGetValue(player.UID, out credential);
            return credential;
        }

        public void ReloadConfig()
        {
            if (!File.Exists(ModuleFolder + "\\Settings.ini"))
            {
                File.Create(ModuleFolder + "\\Settings.ini").Dispose();
                Settings = new IniParser(ModuleFolder + "\\Settings.ini");
                Settings.AddSetting("Settings", "YouNeedToBeLoggedIn", YouNeedToBeLoggedIn);
                Settings.AddSetting("Settings", "CredsReset", CredsReset);
                Settings.AddSetting("Settings", "TimeToLogin", TimeToLogin.ToString());
                Settings.AddSetting("Settings", "SocialSiteForHelp", SocialSiteForHelp);
                Settings.AddSetting("Settings", "RestrictedCommands", "home,tpa,tpaccept,hg");
                Settings.AddSetting("Settings", "RestrictedConsoleCommands", "something.console,something.console2,*");
                Settings.AddSetting("Settings", "RemovePermissionsUntilLogin", "true");
                Settings.AddSetting("Settings", "PleaseLoginOrRegister", PleaseLoginOrRegister);
                Settings.AddSetting("Settings", "YouWillBeKicked", YouWillBeKicked);
                Settings.AddSetting("Settings", "HelpTextTime", HelpTextTime.ToString());
                Settings.Save();
            }

            try
            {

                Settings = new IniParser(ModuleFolder + "\\Settings.ini");

                YouNeedToBeLoggedIn = Settings.GetSetting("Settings", "YouNeedToBeLoggedIn");
                CredsReset = Settings.GetSetting("Settings", "CredsReset");
                TimeToLogin = int.Parse(Settings.GetSetting("Settings", "TimeToLogin"));
                HelpTextTime = int.Parse(Settings.GetSetting("Settings", "HelpTextTime"));
                SocialSiteForHelp = Settings.GetSetting("Settings", "SocialSiteForHelp");
                PleaseLoginOrRegister = Settings.GetSetting("Settings", "PleaseLoginOrRegister");
                YouWillBeKicked = Settings.GetSetting("Settings", "YouWillBeKicked");
                RemovePermissionsUntilLogin = Settings.GetBoolSetting("Settings", "RemovePermissionsUntilLogin");
                RestrictedCommands.Clear();
                RestrictedConsoleCommands.Clear();

                string data = Settings.GetSetting("Settings", "RestrictedCommands");
                foreach (string x in data.Split(','))
                {
                    if (string.IsNullOrEmpty(x))
                    {
                        continue;
                    }
                    RestrictedCommands.Add(x.ToLower());
                }

                string data2 = Settings.GetSetting("Settings", "RestrictedConsoleCommands");
                foreach (string x in data2.Split(','))
                {
                    if (string.IsNullOrEmpty(x))
                    {
                        continue;
                    }

                    string lower = x.ToLower();
                    if (lower == "*")
                    {
                        RestrictAllConsoleCommands = true;
                    }
                    else
                    {
                        RestrictedConsoleCommands.Add(x.ToLower());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("[AuthMe] Error Reading the config: " + ex);
            }
        }
        
        private AuthMeTE CreateParallelTimer(int timeoutDelay, Dictionary<string, object> args)
        {
            AuthMeTE timedEvent = new AuthMeTE(timeoutDelay);
            timedEvent.Args = args;
            timedEvent.OnFire += Callback;
            return timedEvent;
        }

        private void Callback(AuthMeTE e)
        {
            e.Kill();
            
            Dictionary<string, object> data = e.Args;
            Fougerite.Player player = (Fougerite.Player) data["Player"];
            
            if (data.ContainsKey("HelpText"))
            {
                if (player.IsOnline && WaitingUsers.ContainsKey(player.UID))
                {
                    Vector3 spawnPosition = (Vector3) data["OriginalPosition"];
                    if (spawnPosition.x != 0f && spawnPosition.y != 0f && spawnPosition.z != 0f)
                    {
                        player.TeleportTo(spawnPosition, false);
                    }
                    
                    player.MessageFrom("AuthMe", orange + PleaseLoginOrRegister);
                    player.MessageFrom("AuthMe", orange + YouWillBeKicked);
                    player.MessageFrom("AuthMe", yellow + "Get help with login at: " + SocialSiteForHelp);
                    
                    // Keep resetting the timer until player logged in or disconnected
                    CreateParallelTimer(HelpTextTime * 1000, data).Start();
                }
            }
            else
            {
                if (player.IsOnline && WaitingUsers.ContainsKey(player.UID))
                {
                    player.Disconnect();
                }
            }
        }
        
        private void OnModulesLoaded()
        {
            foreach (var x in Fougerite.PluginLoaders.PluginLoader.GetInstance().Plugins.Values)
            {
                if (x.Name == "RustBuster2016Server")
                {
                    FoundRB = true;
                    // Need to place this to another function to avoid exception lol
                    AddRBEvent();
                    break;
                }
            }
        }

        private void AddRBEvent()
        {
            RustBuster2016Server.API.OnRustBusterUserMessage += OnRustBusterUserMessage;
        }

        private void OnRustBusterUserMessage(RustBuster2016Server.API.RustBusterUserAPI user, RustBuster2016Server.Message msgc)
        {
            if (msgc.PluginSender == "AuthMe")
            {
                Fougerite.Player player = user.Player;
                
                string[] spl = msgc.MessageByClient.Split('-');
                if (spl.Length != 3)
                {
                    return;
                }
                
                string evt = spl[0];
                string username = spl[1];
                string password = spl[2];
                if (evt == "AuthMeLogin")
                {
                    if (!WaitingUsers.ContainsKey(player.UID))
                    {
                        player.MessageFrom("AuthMe", orange + "You are logged in already.");
                        msgc.ReturnMessage = "DisApproved";
                        return;
                    }

                    if (!Credentials.ContainsKey(player.UID))
                    {
                        player.MessageFrom("AuthMe", orange + "This steamid is not registered yet!");
                        player.MessageFrom("AuthMe", orange + "Type /authme register username password");
                        msgc.ReturnMessage = "DisApproved";
                        return;
                    }

                    Credential cred = Credentials[player.UID];
                    if (!string.Equals(cred.Username, username, StringComparison.CurrentCultureIgnoreCase))
                    {
                        player.MessageFrom("AuthMe", orange + "Invalid username!");
                        AuthLogger.Log(player.Name + " - " + player.SteamID + " - " + player.IP +
                                       " tried to login using: " + username);
                        msgc.ReturnMessage = "DisApproved";
                        return;
                    }
                    
                    if (username.Length > 25 || password.Length > 25)
                    {
                        player.MessageFrom("AuthMe", orange + "Sorry, username and password length must be below 25.");
                        msgc.ReturnMessage = "DisApproved";
                        return;
                    }

                    if (cred.HashedPassword != SHA1Hash(password))
                    {
                        AuthLogger.Log(player.Name + " - " + player.SteamID + " - " + player.IP +
                                       " tried to login using: " + username);
                        player.MessageFrom("AuthMe", red + "Invalid password! Seek help here: " + yellow + " " + SocialSiteForHelp);
                        msgc.ReturnMessage = "DisApproved";
                    }
                    else
                    {
                        PrivilegeStorage storage = WaitingUsers[player.UID];
                        if (storage.WasAdmin)
                        {
                            player.ForceAdminOff(false);
                            player.PlayerClient.netUser.SetAdmin(true);
                        }

                        if (storage.WasModerator)
                        {
                            player.ForceModeratorOff(false);
                        }

                        if (RemovePermissionsUntilLogin)
                        {
                            PermissionSystem.GetPermissionSystem().RemoveForceOffPermissions(player.UID);
                        }

                        WaitingUsers.TryRemove(player.UID);
                        DataStore.GetInstance().Remove("AuthMeLogin", player.UID);
                        
                        uLink.NetworkView.Get(player.PlayerClient.networkView).RPC("DestroyFreezeAuthMe", player.NetworkPlayer);

                        foreach (var x in RestrictedCommands)
                        {
                            player.UnRestrictCommand(x);
                        }

                        foreach (var x in RestrictedConsoleCommands)
                        {
                            player.UnRestrictConsoleCommand(x);
                        }

                        AuthLogger.Log(player.Name + " - " + player.SteamID + " - " + player.IP + " logged in using: " +
                                       username);
                        player.MessageFrom("AuthMe", green + "Successfully logged in!");
                        msgc.ReturnMessage = "Approved";
                    }
                }
                else if (evt == "AuthMeRegister")
                {
                    if (Credentials.ContainsKey(player.UID))
                    {
                        player.MessageFrom("AuthMe",
                            red + "This STEAMID is already protected using password authentication!");
                        msgc.ReturnMessage = "InvalidRegistration";
                        return;
                    }

                    if (username.ToLower() == "username" || password.ToLower() == "password")
                    {
                        player.MessageFrom("AuthMe", orange + "Type /authme register username password");
                        msgc.ReturnMessage = "InvalidRegistration";
                        return;
                    }

                    bool b = Regex.IsMatch(username, @"^[a-zA-Z0-9_&@%!+<>]+$");
                    bool b2 = Regex.IsMatch(password, @"^[a-zA-Z0-9_&@%!+<>]+$");

                    if (!b || !b2)
                    {
                        player.MessageFrom("AuthMe", orange + "Sorry, no special characters or space! Only: a-zA-Z0-9_&@%!+<>");
                        msgc.ReturnMessage = "InvalidRegistration";
                        return;
                    }

                    if (username.Length > 25 || password.Length > 25)
                    {
                        player.MessageFrom("AuthMe", orange + "Sorry, username and password length must be below 25.");
                        msgc.ReturnMessage = "InvalidRegistration";
                        return;
                    }

                    string hash = SHA1Hash(password);
                    Auths.AddSetting("Login", player.SteamID, username.ToLower() + "---##---" + hash);
                    Auths.Save();
                    Credentials.TryAdd(player.UID, new Credential(username.ToLower(), hash));
                    player.MessageFrom("AuthMe",
                        orange + "You have registered with: " + username + " - " + password +
                        " (Your console has this info now too.)");
                    player.SendConsoleMessage(orange + "You have registered with: " + username + " - " + password);

                    WaitingUsers.TryRemove(player.UID);
                    DataStore.GetInstance().Remove("AuthMeLogin", player.UID);
                    
                    uLink.NetworkView.Get(player.PlayerClient.networkView).RPC("DestroyFreezeAuthMe", player.NetworkPlayer);

                    foreach (var x in RestrictedCommands)
                    {
                        player.UnRestrictCommand(x);
                    }

                    foreach (var x in RestrictedConsoleCommands)
                    {
                        player.UnRestrictConsoleCommand(x);
                    }

                    AuthLogger.Log(player.Name + " - " + player.SteamID + " - " + player.IP +
                                   " registered an account: " + username);
                    msgc.ReturnMessage = "ValidRegistration";
                }
            }
        }
        
        private void OnDoorUse(Fougerite.Player player, DoorEvent de)
        {
            if (WaitingUsers.ContainsKey(player.UID))
            {
                player.MessageFrom("AuthMe", red + YouNeedToBeLoggedIn);
                de.Open = false;
            }
        }

        private void OnItemPickup(ItemPickupEvent itempickupevent)
        {
            if (WaitingUsers.ContainsKey(itempickupevent.Player.UID))
            {
                itempickupevent.Player.MessageFrom("AuthMe", red + YouNeedToBeLoggedIn);
                itempickupevent.Cancel();
            }
        }

        private void OnConsoleReceived(ref ConsoleSystem.Arg arg, bool external, ConsoleEvent ce)
        {
            Fougerite.Player adminplr = null;
            // Restrict all commands only if * was applied
            if (arg.argUser != null && RestrictAllConsoleCommands)
            {
                adminplr = Fougerite.Server.GetServer().FindPlayer(arg.argUser.userID);
                if (adminplr != null && WaitingUsers.ContainsKey(adminplr.UID))
                {
                    adminplr.MessageFrom("AuthMe", red + YouNeedToBeLoggedIn);
                    ce.Cancel();
                    return;
                }
            }
            
            if (arg.Class == "authme" && arg.Function == "resetuser")
            {
                if ((arg.argUser != null && arg.argUser.admin) || arg.argUser == null)
                {
                    string name = string.Join(" ", arg.Args);
                    if (string.IsNullOrEmpty(name))
                    {
                        arg.ReplyWith(green + "Specify a name!");
                        return;
                    }
                    
                    Fougerite.Player plr = Fougerite.Server.GetServer().FindPlayer(name);
                    if (plr != null)
                    {
                        if (Auths.GetSetting("Login", plr.SteamID) != null)
                        {
                            Auths.DeleteSetting("Login", plr.SteamID);
                            Auths.Save();
                        }

                        if (Credentials.ContainsKey(plr.UID))
                        {
                            Credentials.TryRemove(plr.UID);
                        }
                                    
                        arg.ReplyWith(green + "User: " + plr.Name + " reset! He can now register a new account for that steamid.");
                        plr.MessageFrom("AuthMe", green + CredsReset);

                        if (adminplr != null)
                        {
                            AuthLogger.Log("[USER RESET] " + adminplr.Name + " - " + adminplr.SteamID
                                           + " - " + adminplr.IP + " reset credetials for: " + plr.Name + " - " +
                                           plr.SteamID + " - " + plr.IP);
                        }
                        else
                        {
                            AuthLogger.Log("[USER RESET] Console reset credetials for: " + plr.Name + " - " +
                                           plr.SteamID + " - " + plr.IP);
                        }
                    }
                    else
                    {
                        arg.ReplyWith(green + "No player found!");
                    }
                }
            }
        }
        
        private void OnLootUse(LootStartEvent lootstartevent)
        {
            if (WaitingUsers.ContainsKey(lootstartevent.Player.UID))
            {
                lootstartevent.Player.MessageFrom("AuthMe", red + YouNeedToBeLoggedIn);
                lootstartevent.Cancel();
            }
        }
        
        private void OnBeltUse(BeltUseEvent beltuseevent)
        {
            if (WaitingUsers.ContainsKey(beltuseevent.Player.UID))
            {
                beltuseevent.Player.MessageFrom("AuthMe", red + YouNeedToBeLoggedIn);
                beltuseevent.Cancel();
            }
        }

        private void OnItemAdded(InventoryModEvent e)
        {
            if (e.Player != null)
            {
                if (WaitingUsers.ContainsKey(e.Player.UID))
                {
                    e.Player.MessageFrom("AuthMe", red + YouNeedToBeLoggedIn);
                    e.Cancel();
                }
            }
        }

        private void OnResearch(ResearchEvent re)
        {
            if (WaitingUsers.ContainsKey(re.Player.UID))
            {
                re.Player.MessageFrom("AuthMe", red + YouNeedToBeLoggedIn);
                re.Cancel();
            }
        }

        private void OnCrafting(CraftingEvent e)
        {
            if (WaitingUsers.ContainsKey(e.Player.UID))
            {
                e.Player.MessageFrom("AuthMe", red + YouNeedToBeLoggedIn);
                e.Cancel();
            }
        }

        private void OnEntityDeployedWithPlacer(Fougerite.Player player, Entity e, Fougerite.Player actualplacer)
        {
            if (WaitingUsers.ContainsKey(actualplacer.UID))
            {
                e.Destroy();
                actualplacer.MessageFrom("AuthMe", red + YouNeedToBeLoggedIn);
            }
        }

        private void OnPlayerSpawned(Fougerite.Player player, SpawnEvent se)
        {
            if (SpawnedUsers.Contains(player.UID))
            {
                SpawnedUsers.Remove(player.UID);
                DataStore.GetInstance().Add("AuthMeLogin", player.UID, true);
                
                PrivilegeStorage storage = new PrivilegeStorage(player.Admin, player.Moderator);
                if (player.Admin)
                {
                    player.ForceAdminOff(true);
                }

                if (player.Moderator)
                {
                    player.ForceModeratorOff(true);
                }

                if (RemovePermissionsUntilLogin)
                {
                    PermissionSystem.GetPermissionSystem().ForceOffPermissions(player.UID, true);
                }
                WaitingUsers[player.UID] = storage;

                foreach (string x in RestrictedCommands)
                {
                    player.RestrictCommand(x);
                }

                foreach (string x in RestrictedConsoleCommands)
                {
                    if (x == "*")
                        continue;
                    
                    player.RestrictConsoleCommand(x);
                }

                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    ["Player"] = player
                };
                CreateParallelTimer(TimeToLogin * 1000, data).Start();

                Dictionary<string, object> data2 = new Dictionary<string, object>
                {
                    ["Player"] = player,
                    ["HelpText"] = true,
                    ["OriginalPosition"] = se.Location
                };
                CreateParallelTimer(HelpTextTime * 1000, data2).Start();
                
                player.MessageFrom("AuthMe", orange + PleaseLoginOrRegister);
                player.MessageFrom("AuthMe", orange + YouWillBeKicked);
                player.MessageFrom("AuthMe", yellow + "Get help with login at: " + SocialSiteForHelp);
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
                if (WaitingUsers.ContainsKey(attacker.UID))
                {
                    attacker.MessageFrom("AuthMe", red + YouNeedToBeLoggedIn);
                    he.DamageAmount = 0f;
                }
            }
        }

        private void OnChat(Fougerite.Player player, ref ChatString text)
        {
            if (WaitingUsers.ContainsKey(player.UID))
            {
                text.NewText = string.Empty;
                player.MessageFrom("AuthMe", red + YouNeedToBeLoggedIn + "xd");
            }
        }

        private void OnItemRemoved(InventoryModEvent e)
        {
            if (e.Player != null)
            {
                if (WaitingUsers.ContainsKey(e.Player.UID))
                {
                    e.Cancel();
                    e.Player.MessageFrom("AuthMe", red + YouNeedToBeLoggedIn);
                }
            }
        }

        private void OnPlayerDisconnected(Fougerite.Player player)
        {
            if (WaitingUsers.ContainsKey(player.UID))
            {
                WaitingUsers.TryRemove(player.UID);
                DataStore.GetInstance().Remove("AuthMeLogin", player.UID);
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
                if (WaitingUsers.ContainsKey(player.UID))
                {
                    if (he.AttackerIsPlayer && he.Attacker != null)
                    {
                        Fougerite.Player attacker = (Fougerite.Player) he.Attacker;
                        attacker.MessageFrom("AuthMe", red + "This player haven't logged in yet! He will be kicked if he is AFK.");
                    }
                    he.DamageAmount = 0f;
                }
            }
            if (he.AttackerIsPlayer && he.Attacker != null)
            {
                Fougerite.Player attacker = (Fougerite.Player) he.Attacker;
                if (WaitingUsers.ContainsKey(attacker.UID))
                {
                    attacker.MessageFrom("AuthMe", red + YouNeedToBeLoggedIn);
                    he.DamageAmount = 0f;
                }
            }
        }

        private void OnCommand(Fougerite.Player player, string cmd, string[] args)
        {
            if (cmd != "authme")
                return;
            
            if (args.Length == 0)
            {
                player.MessageFrom("AuthMe", orange + "AuthMe V" + Version + " By " + Author);
                player.MessageFrom("AuthMe", yellow + "-- Passwords are stored using SHA1 hashes --");
                player.MessageFrom("AuthMe", yellow + "-- Always use a different password than your emails, etc. --");
                player.MessageFrom("AuthMe", orange + "/authme register username password");
                player.MessageFrom("AuthMe", orange + "/authme login username password");
                player.MessageFrom("AuthMe", orange + "/authme changepw username newpassword");
                if (player.Admin || player.Moderator || PermissionSystem.GetPermissionSystem().PlayerHasPermission(player, "authme.info"))
                {
                    player.MessageFrom("AuthMe", orange + "/authme resetuser ingamename");
                    player.MessageFrom("AuthMe", orange + "/authme reload");
                }
            }
            else if (args.Length == 1)
            {
                string subcmd = args[0];
                switch (subcmd)
                {
                    case "reload":
                    {
                        if (player.Admin || player.Moderator || PermissionSystem.GetPermissionSystem().PlayerHasPermission(player, "authme.reload"))
                        {
                            ReloadConfig();
                            player.MessageFrom("AuthMe", green + "Config reloaded!");
                        }

                        break;
                    }
                }
            }
            else if (args.Length == 2)
            {
                string subcmd = args[0];
                switch (subcmd)
                {
                    case "resetuser":
                    {
                        if (player.Admin || player.Moderator || PermissionSystem.GetPermissionSystem().PlayerHasPermission(player, "authme.resetuser"))
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
                                    Credentials.TryRemove(plr.UID);
                                }

                                player.MessageFrom("AuthMe",
                                    green + "User: " + plr.Name +
                                    " reset! He can now register a new account for that steamid.");
                                plr.MessageFrom("AuthMe", green + CredsReset);
                                AuthLogger.Log("[USER RESET] " + player.Name + " - " + player.SteamID
                                               + " - " + player.IP + " reset credetials for: " + plr.Name + " - " +
                                               plr.SteamID + " - " + plr.IP);
                            }
                        }

                        break;
                    }
                }
            }
            else if (args.Length == 3)
            {
                string subcmd = args[0];
                switch (subcmd)
                {
                    case "register":
                    {
                        if (Credentials.ContainsKey(player.UID))
                        {
                            player.MessageFrom("AuthMe",
                                red + "This STEAMID is already protected using password authentication!");
                            player.MessageFrom("AuthMe", "Seek help here: " + yellow + " " + SocialSiteForHelp);
                            return;
                        }

                        string username = args[1];
                        string password = args[2];

                        if (username.ToLower() == "username" || password.ToLower() == "password")
                        {
                            player.MessageFrom("AuthMe", orange + "Type /authme register username password");
                            return;
                        }

                        bool b = Regex.IsMatch(username, @"^[a-zA-Z0-9_&@%!+<>]+$");
                        bool b2 = Regex.IsMatch(password, @"^[a-zA-Z0-9_&@%!+<>]+$");

                        if (!b || !b2)
                        {
                            player.MessageFrom("AuthMe",
                                orange + "Sorry, no special characters or space! Only: a-zA-Z0-9_&@%!+<>");
                            return;
                        }

                        if (username.Length > 25 || password.Length > 25)
                        {
                            player.MessageFrom("AuthMe",
                                orange + "Sorry, username and password length must be below 25.");
                            return;
                        }

                        string hash = SHA1Hash(password);
                        Auths.AddSetting("Login", player.SteamID, username.ToLower() + "---##---" + hash);
                        Auths.Save();
                        Credentials.TryAdd(player.UID, new Credential(username.ToLower(), hash));
                        player.MessageFrom("AuthMe",
                            orange + "You have registered with: " + username + " - " + password +
                            " (Your console has this info now too.)");
                        player.SendConsoleMessage(orange + "You have registered with: " + username + " - " +
                                                  password);

                        WaitingUsers.TryRemove(player.UID);
                        DataStore.GetInstance().Remove("AuthMeLogin", player.UID);

                        if (FoundRB)
                        {
                            uLink.NetworkView.Get(player.PlayerClient.networkView).RPC("DestroyFreezeAuthMe", player.NetworkPlayer);
                        }

                        foreach (var x in RestrictedCommands)
                        {
                            player.UnRestrictCommand(x);
                        }

                        foreach (var x in RestrictedConsoleCommands)
                        {
                            player.UnRestrictConsoleCommand(x);
                        }

                        AuthLogger.Log(player.Name + " - " + player.SteamID + " - " + player.IP +
                                       " registered an account: " + username);
                        break;
                    }
                    case "login":
                    {
                        if (!WaitingUsers.ContainsKey(player.UID))
                        {
                            player.MessageFrom("AuthMe", orange + "You are logged in already.");
                            return;
                        }

                        string username2 = args[1];
                        string password2 = args[2];

                        if (username2.Length > 25 || password2.Length > 25)
                        {
                            player.MessageFrom("AuthMe",
                                orange + "Sorry, username and password length must be below 25.");
                            return;
                        }

                        if (!Credentials.ContainsKey(player.UID))
                        {
                            player.MessageFrom("AuthMe", orange + "This steamid is not registered yet!");
                            player.MessageFrom("AuthMe", orange + "Type /authme register username password");
                            return;
                        }

                        Credential cred = Credentials[player.UID];
                        if (cred.Username.ToLower() != username2.ToLower())
                        {
                            player.MessageFrom("AuthMe", orange + "Invalid username!");
                            AuthLogger.Log(player.Name + " - " + player.SteamID + " - " + player.IP +
                                           " tried to login using: " + username2);
                            return;
                        }

                        if (cred.HashedPassword != SHA1Hash(password2))
                        {
                            AuthLogger.Log(player.Name + " - " + player.SteamID + " - " + player.IP +
                                           " tried to login using: " + username2);
                            player.MessageFrom("AuthMe",
                                orange + "Invalid password! Seek admin for help on their social site.");
                            player.MessageFrom("AuthMe", "Seek help here: " + yellow + " " + SocialSiteForHelp);
                        }
                        else
                        {
                            PrivilegeStorage storage = WaitingUsers[player.UID];
                            if (storage.WasAdmin)
                            {
                                player.ForceAdminOff(false);
                                player.PlayerClient.netUser.SetAdmin(true);
                            }

                            if (storage.WasModerator)
                            {
                                player.ForceModeratorOff(false);
                            }

                            if (RemovePermissionsUntilLogin)
                            {
                                PermissionSystem.GetPermissionSystem().RemoveForceOffPermissions(player.UID);
                            }

                            WaitingUsers.TryRemove(player.UID);
                            DataStore.GetInstance().Remove("AuthMeLogin", player.UID);

                            if (FoundRB)
                            {
                                uLink.NetworkView.Get(player.PlayerClient.networkView).RPC("DestroyFreezeAuthMe", player.NetworkPlayer);
                            }

                            foreach (var x in RestrictedCommands)
                            {
                                player.UnRestrictCommand(x);
                            }

                            foreach (var x in RestrictedConsoleCommands)
                            {
                                player.UnRestrictConsoleCommand(x);
                            }

                            AuthLogger.Log(player.Name + " - " + player.SteamID + " - " + player.IP +
                                           " logged in using: " + username2);
                            player.MessageFrom("AuthMe", green + "Successfully logged in!");
                        }

                        break;
                    }
                    case "changepw":
                    {
                        if (WaitingUsers.ContainsKey(player.UID))
                        {
                            player.MessageFrom("AuthMe", orange + "Nice try. You need to be logged in to do that.");
                            return;
                        }

                        string username3 = args[1];
                        string password3 = args[2];
                        if (!Credentials.ContainsKey(player.UID))
                        {
                            player.MessageFrom("AuthMe", orange + "This steamid is not registered yet!");
                            return;
                        }

                        Credential cred2 = Credentials[player.UID];
                        if (!string.Equals(cred2.Username, username3, StringComparison.CurrentCultureIgnoreCase))
                        {
                            player.MessageFrom("AuthMe", orange + "Invalid username!");
                            return;
                        }

                        if (username3.Length > 25 || password3.Length > 25)
                        {
                            player.MessageFrom("AuthMe",
                                orange + "Sorry, username and password length must be below 25.");
                            return;
                        }

                        Credentials.TryRemove(player.UID);

                        string hash3 = SHA1Hash(password3);
                        Auths.SetSetting("Login", player.SteamID, username3.ToLower() + "---##---" + hash3);
                        Auths.Save();
                        Credentials.TryAdd(player.UID, new Credential(username3.ToLower(), hash3));
                        AuthLogger.Log(player.Name + " - " + player.SteamID + " - " + player.IP +
                                       " changed password using: " + username3);

                        player.MessageFrom("AuthMe", green + "Password successfully changed!");
                        break;
                    }
                    default:
                    {
                        player.MessageFrom("AuthMe", orange + "Invalid command. Type /authme for help.");
                        break;
                    }
                }
            }
        }

        private string SHA1Hash(string input)
        {
            byte[] hash = (new SHA1Managed()).ComputeHash(Encoding.UTF8.GetBytes(input));
            return string.Join("", hash.Select(b => b.ToString("x2")).ToArray());
        }
    }
}