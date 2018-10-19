using UnityEngine;

namespace AuthMe
{
    public class AuthChecker : MonoBehaviour
    {
        private bool _Freezing = false;
        
        private void Start()
        {
            _Freezing = true;
            Invoke(nameof(LoginAlert), 10f);
            if (_Freezing)
            {
                Rust.Notice.Popup("", "Type /authme login username password to login.");
            }
        }

        private void LoginAlert()
        {
            if (_Freezing)
            {
                Rust.Notice.Popup("", "Type /authme login username password to login.", 7f);
            }
            Invoke(nameof(LoginAlert), 10f);
        }

        private void FixedUpdate()
        {
            if (!_Freezing) return;
            if (PlayerClient.GetLocalPlayer() != null && PlayerClient.GetLocalPlayer().controllable != null)
            {
                Character player = PlayerClient.GetLocalPlayer().controllable.GetComponent<Character>();
                if (player != null)
                {
                    player.lockMovement = true;
                    player.lockLook = true;
                }
            }
        }
        
        [RPC]
        public void DestroyFreeze()
        {
            _Freezing = false;
            this.enabled = false;
            if (PlayerClient.GetLocalPlayer() != null && PlayerClient.GetLocalPlayer().controllable != null)
            {
                Character player = PlayerClient.GetLocalPlayer().controllable.GetComponent<Character>();
                if (player != null)
                {
                    // Thanks Jakkee
                    player.lockMovement = false;
                    player.lockLook = false;
                }
            }
        }

        private void OnDestroy()
        {
            _Freezing = false;
            if (PlayerClient.GetLocalPlayer() != null && PlayerClient.GetLocalPlayer().controllable != null)
            {
                Character player = PlayerClient.GetLocalPlayer().controllable.GetComponent<Character>();
                if (player != null)
                {
                    player.lockMovement = false;
                    player.lockLook = false;
                }
            }
        }
    }
}