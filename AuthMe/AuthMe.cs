using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RustBuster2016;
using RustBuster2016.API.Events;
using UnityEngine;

namespace AuthMe
{
    public class AuthMe : RustBuster2016.API.RustBusterPlugin
    {
        internal GameObject MainGameObject;
        internal AuthChecker Checker;
        private static AuthMe _inst;
        
        public override string Name
        {
            get { return "AuthMe"; }
        }

        public override string Author
        {
            get { return "DreTaX"; }
        }

        public override Version Version
        {
            get { return new Version("1.0"); }
        }

        internal static AuthMe Instance
        {
            get { return _inst; }
        }
        
        public override void Initialize()
        {
            _inst = this;
            MainGameObject = new GameObject();
            MainGameObject.AddComponent<CharacterWaiter>();
            UnityEngine.Object.DontDestroyOnLoad(MainGameObject);
        }

        public override void DeInitialize()
        {
            // Just incase.
            if (PlayerClient.GetLocalPlayer() != null && PlayerClient.GetLocalPlayer().controllable != null)
            {
                Character player = PlayerClient.GetLocalPlayer().controllable.GetComponent<Character>();
                if (player != null)
                {
                    player.lockMovement = false;
                    player.lockLook = false;
                }
            }
            if (MainGameObject != null)
            {
                UnityEngine.Object.Destroy(MainGameObject);
                MainGameObject = null;
            }
            if (Checker != null)
            {
                UnityEngine.Object.Destroy(Checker);
                Checker = null;
            }
        }
        
        internal void StartMyRPCs()
        {
            if (MainGameObject != null) {UnityEngine.Object.Destroy(MainGameObject);}
            // Add our Behaviour that is containing all the RPC methods to the player.
            Checker = PlayerClient.GetLocalPlayer().gameObject.AddComponent<AuthChecker>();
        }
    }
}