using UnityEngine;

namespace AuthMe
{
    public class AuthChecker : MonoBehaviour
    {
        private bool _Freezing = false;

        private void FixedUpdate()
        {
            if (!_Freezing) return;
            if (PlayerClient.GetLocalPlayer() != null && PlayerClient.GetLocalPlayer().controllable != null)
            {
                Character player = PlayerClient.GetLocalPlayer().controllable.GetComponent<Character>();
                if (player != null)
                {
                    player.lockMovement = true;
                    //player.lockLook = true;
                }
            }
        }
        
        [RPC]
        public void DestroyFreezeAuthMe()
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
                    //player.lockLook = false;
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
                    //player.lockLook = false;
                }
            }
        }
    }
}