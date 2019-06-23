namespace AuthMeServer
{
    public class PrivilegeStorage
    {
        private readonly bool _admin;
        private readonly bool _moderator;
        
        public PrivilegeStorage(bool admin, bool moderator)
        {
            _admin = admin;
            _moderator = moderator;
        }

        public bool WasAdmin
        {
            get { return _admin; }
        }

        public bool WasModerator
        {
            get { return _moderator; }
        }
    }
}