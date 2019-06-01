namespace AuthMeServer
{
    public class Credential
    {
        private readonly string _username;
        private string _hashedpassword;

        public Credential(string username, string password)
        {
            _username = username;
            _hashedpassword = password;
        }

        public string Username
        {
            get { return _username; }
        }

        public string HashedPassword
        {
            get { return _hashedpassword; }
            set { _hashedpassword = value; }
        }
    }
}