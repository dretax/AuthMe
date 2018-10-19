namespace AuthMe
{
    internal class CharacterWaiter : UnityEngine.MonoBehaviour
    {
        private void Update()
        {
            if (PlayerClient.GetLocalPlayer() != null && PlayerClient.GetLocalPlayer().controllable != null)
            {
                Character player = PlayerClient.GetLocalPlayer().controllable.GetComponent<Character>();
                if (player != null)
                {
                    // If we are connected to a server, disable the current behaviour and activate the other one containing the RPCs.
                    this.enabled = false;
                    AuthMe.Instance.StartMyRPCs();
                }
            }
        }
    }
}