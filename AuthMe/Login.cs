using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AuthMe
{
    public class Login : MonoBehaviour
    {
        public GUIStyle _BackgroundStyle;
        public GUIStyle _ButtonsStyle;
        public Texture2D _BackgroundTexture;
        public Texture2D _ButtonsTexture;
        public const string BytesDark = "/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAIBAQIBAQICAgICAgICAwUDAwMDAwYEBAMFBwYHBwcGBwcICQsJCAgKCAcHCg0KCgsMDAwMBwkODw0MDgsMDAz/2wBDAQICAgMDAwYDAwYMCAcIDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAz/wAARCAAtAC8DASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwD8c6KKKACiiigAooooAKKKKACiiigAooooAKKKKACiiigAooooA//Z";
        public const string BytesButtons = "/9j/4AAQSkZJRgABAgEASABIAAD/7QUCUGhvdG9zaG9wIDMuMAA4QklNA+0AAAAAABAASAAAAAIAAgBIAAAAAgACOEJJTQQNAAAAAAAEAAAAeDhCSU0EGQAAAAAABAAAAB44QklNA/MAAAAAAAkAAAAAAAAAAAEAOEJJTQQKAAAAAAABAAA4QklNJxAAAAAAAAoAAQAAAAAAAAACOEJJTQP1AAAAAABIAC9mZgABAGxmZgAGAAAAAAABAC9mZgABAKGZmgAGAAAAAAABADIAAAABAFoAAAAGAAAAAAABADUAAAABAC0AAAAGAAAAAAABOEJJTQP4AAAAAABwAAD/////////////////////////////A+gAAAAA/////////////////////////////wPoAAAAAP////////////////////////////8D6AAAAAD/////////////////////////////A+gAADhCSU0EAAAAAAAAAgABOEJJTQQCAAAAAAAEAAAAADhCSU0ECAAAAAAAEAAAAAEAAAJAAAACQAAAAAA4QklNBB4AAAAAAAQAAAAAOEJJTQQaAAAAAAB5AAAABgAAAAAAAAAAAAAABgAAABwAAAAMAFMAaQBuACAAdADtAHQAdQBsAG8ALQAyAAAAAQAAAAAAAAAAAAAAAAAAAAAAAAABAAAAAAAAAAAAAAAcAAAABgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA4QklNBBEAAAAAAAEBADhCSU0EFAAAAAAABAAAAAI4QklNBAwAAAAAAjwAAAABAAAAHAAAAAYAAABUAAAB+AAAAiAAGAAB/9j/4AAQSkZJRgABAgEASABIAAD/7gAOQWRvYmUAZIAAAAAB/9sAhAAMCAgICQgMCQkMEQsKCxEVDwwMDxUYExMVExMYEQwMDAwMDBEMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMAQ0LCw0ODRAODhAUDg4OFBQODg4OFBEMDAwMDBERDAwMDAwMEQwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAz/wAARCAAGABwDASIAAhEBAxEB/90ABAAC/8QBPwAAAQUBAQEBAQEAAAAAAAAAAwABAgQFBgcICQoLAQABBQEBAQEBAQAAAAAAAAABAAIDBAUGBwgJCgsQAAEEAQMCBAIFBwYIBQMMMwEAAhEDBCESMQVBUWETInGBMgYUkaGxQiMkFVLBYjM0coLRQwclklPw4fFjczUWorKDJkSTVGRFwqN0NhfSVeJl8rOEw9N14/NGJ5SkhbSVxNTk9KW1xdXl9VZmdoaWprbG1ub2N0dXZ3eHl6e3x9fn9xEAAgIBAgQEAwQFBgcHBgU1AQACEQMhMRIEQVFhcSITBTKBkRShsUIjwVLR8DMkYuFygpJDUxVjczTxJQYWorKDByY1wtJEk1SjF2RFVTZ0ZeLys4TD03Xj80aUpIW0lcTU5PSltcXV5fVWZnaGlqa2xtbm9ic3R1dnd4eXp7fH/9oADAMBAAIRAxEAPwDiElgpJJd5JYKSSn//2ThCSU0EIQAAAAAAVQAAAAEBAAAADwBBAGQAbwBiAGUAIABQAGgAbwB0AG8AcwBoAG8AcAAAABMAQQBkAG8AYgBlACAAUABoAG8AdABvAHMAaABvAHAAIAA2AC4AMAAAAAEAOEJJTQQGAAAAAAAHAAgAAAABAQD/7gAOQWRvYmUAZEAAAAAB/9sAhAABAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAgICAgICAgICAgIDAwMDAwMDAwMDAQEBAQEBAQEBAQECAgECAgMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwP/wAARCAAGABwDAREAAhEBAxEB/90ABAAE/8QBogAAAAYCAwEAAAAAAAAAAAAABwgGBQQJAwoCAQALAQAABgMBAQEAAAAAAAAAAAAGBQQDBwIIAQkACgsQAAIBAwQBAwMCAwMDAgYJdQECAwQRBRIGIQcTIgAIMRRBMiMVCVFCFmEkMxdScYEYYpElQ6Gx8CY0cgoZwdE1J+FTNoLxkqJEVHNFRjdHYyhVVlcassLS4vJkg3SThGWjs8PT4yk4ZvN1Kjk6SElKWFlaZ2hpanZ3eHl6hYaHiImKlJWWl5iZmqSlpqeoqaq0tba3uLm6xMXGx8jJytTV1tfY2drk5ebn6Onq9PX29/j5+hEAAgEDAgQEAwUEBAQGBgVtAQIDEQQhEgUxBgAiE0FRBzJhFHEIQoEjkRVSoWIWMwmxJMHRQ3LwF+GCNCWSUxhjRPGisiY1GVQ2RWQnCnODk0Z0wtLi8lVldVY3hIWjs8PT4/MpGpSktMTU5PSVpbXF1eX1KEdXZjh2hpamtsbW5vZnd4eXp7fH1+f3SFhoeIiYqLjI2Oj4OUlZaXmJmam5ydnp+So6SlpqeoqaqrrK2ur6/9oADAMBAAIRAxEAPwDVs906Ude9+691737r3Xvfuvdf/9k=";
        public Rect UserTextFieldArea;
        public Rect PassTextFieldArea;
        public Rect LoginButtonArea;
        public Rect RegisterButtonArea;
        public string UserName = "UserName";
        public string Password = "Password";
        public bool SelectFieldUser = false;
        public bool SelectFieldPass = false;
        public bool UnderLogin;
        public bool UnderRegister;
        public bool AllLoaded = false;
        
        private void Start()
        {
            _BackgroundStyle = new GUIStyle();
            _ButtonsStyle = new GUIStyle();

            UserTextFieldArea = new Rect(60, 60, 130, 20);
            PassTextFieldArea = new Rect(60, 90, 130, 20);
            LoginButtonArea = new Rect(60, 120, 60, 20);
            RegisterButtonArea = new Rect(130, 120, 60, 20);

            UnderLogin = false;
            UnderRegister = false;

            byte[] bytes1 = Convert.FromBase64String(BytesDark);
            _BackgroundTexture = new Texture2D(60, 16, TextureFormat.RGB24, false);
            _BackgroundTexture.filterMode = FilterMode.Trilinear;
            _BackgroundTexture.LoadImage(bytes1);
            _BackgroundStyle.alignment = TextAnchor.MiddleCenter;
            _BackgroundStyle.normal.textColor = Color.white;
            _BackgroundStyle.hover.textColor = Color.red;
            _BackgroundStyle.active.textColor = Color.green;
            _BackgroundStyle.normal.background = (Texture2D)_BackgroundTexture;
            _BackgroundStyle.hover.background = (Texture2D)_BackgroundTexture;
            _BackgroundStyle.active.background = (Texture2D)_BackgroundTexture;
            _BackgroundStyle.fontSize = 768 * 2 / 150;

            byte[] bytes2 = Convert.FromBase64String(BytesButtons);
            _ButtonsTexture = new Texture2D(60, 16, TextureFormat.RGB24, false);
            _ButtonsTexture.filterMode = FilterMode.Trilinear;
            _ButtonsTexture.LoadImage(bytes2);
            _ButtonsStyle.alignment = TextAnchor.MiddleCenter;
            _ButtonsStyle.normal.textColor = Color.white;
            _ButtonsStyle.hover.textColor = Color.red;
            _ButtonsStyle.active.textColor = Color.green;
            _ButtonsStyle.normal.background = (Texture2D)_ButtonsTexture;
            _ButtonsStyle.hover.background = (Texture2D)_ButtonsTexture;
            _ButtonsStyle.active.background = (Texture2D)_ButtonsTexture;
            _ButtonsStyle.fontSize = 768 * 2 / 150;

            AllLoaded = true;
        }
        
        private void Update()
        {
            if (AllLoaded)
            {
                if (UserTextFieldArea.Contains(Event.current.mousePosition))
                {
                    if (Event.current.type == EventType.MouseDown || Input.GetButtonDown("Fire1"))
                    {
                        SelectFieldUser = true;
                    }
                }
                else if (PassTextFieldArea.Contains(Event.current.mousePosition))
                {
                    if (Event.current.type == EventType.MouseDown || Input.GetButtonDown("Fire1"))
                    {
                        SelectFieldPass = true;
                    }
                }
                else if (LoginButtonArea.Contains(Event.current.mousePosition))
                {
                    if (Event.current.type == EventType.MouseDown || Input.GetButtonDown("Fire1"))
                    {
                        TryLogin();
                    }
                }
                else if (RegisterButtonArea.Contains(Event.current.mousePosition))
                {
                    if (Event.current.type == EventType.MouseDown || Input.GetButtonDown("Fire1"))
                    {
                        TryRegister();
                    }
                }
            }
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(50, 50, 150, 100), String.Empty, _BackgroundStyle);
            GUI.Label(LoginButtonArea, "Login", _ButtonsStyle);
            GUI.Label(RegisterButtonArea, "Register", _ButtonsStyle);

            GUI.SetNextControlName("UserNameTextField");
            UserName = GUI.TextField(new Rect(60, 60, 130, 20), UserName,20);
            GUI.SetNextControlName("PasswordTextField");
            Password = GUI.TextField(new Rect(60, 90, 130, 20), Password,20);

            if (SelectFieldUser)
            {
                GUI.FocusControl("UserNameTextField");
                SelectFieldUser = false;
            }
            if (SelectFieldPass)
            {
                GUI.FocusControl("PasswordTextField");
                SelectFieldPass = false;
            }
            /*
            if (GUI.Button(LoginButtonArea, "Login", _ButtonsStyle))
            {
            }
            if (GUI.Button(RegisterButtonArea, "Register", _ButtonsStyle))
            {
            }
            */
        }

        private void TryLogin()
        {
            if (!UnderLogin)
            {
                UnderLogin = true;
                if (UserName == "UserName" || string.IsNullOrEmpty(UserName))
                {
                    Rust.Notice.Popup("", "You must enter a valid User Name :(", 10);
                    UnderLogin = false;
                }
                else
                {
                    if (Password == "Password" || string.IsNullOrEmpty(Password))
                    {
                        Rust.Notice.Popup("", "You must enter a valid Password :(", 10);
                        UnderLogin = false;
                    }
                    else
                    {
                        bool b = Regex.IsMatch(UserName, @"^[a-zA-Z0-9_&@%!+<>]+$");
                        bool b2 = Regex.IsMatch(Password, @"^[a-zA-Z0-9_&@%!+<>]+$");

                        if (!b || !b2)
                        {
                            Rust.Notice.Popup("", "Sorry, no special characters or space! Only: a-zA-Z0-9_&@%!+<>", 10);
                            UnderLogin = false;
                            return;
                        }
                        
                        if (UserName.Length > 25 || Password.Length > 25)
                        {
                            Rust.Notice.Popup("", "Sorry username and password length must be below 25.", 10);
                            UnderLogin = false;
                            return;
                        }
                        
                        string request = AuthMe.Instance.SendMessageToServer("AuthMeLogin-" + UserName + "-" + Password);
                        if (request == "Approved")
                        {
                            Rust.Notice.Popup("", "You logged in successfully.", 10);
                            this.enabled = false;
                        }
                        else if (request == "DisApproved")
                        {
                            UnderLogin = false;
                            Rust.Notice.Popup("", "Login failed.", 10);
                        }
                    }
                }
            }
            else
            {
                Rust.Notice.Popup("", "Wait a bit, your request is still being processed.", 10);
            }
        }

        private void TryRegister()
        {
            if (!UnderRegister)
            {
                UnderRegister = true;
                if (UserName == "UserName" || string.IsNullOrEmpty(UserName))
                {
                    Rust.Notice.Popup("", "You must enter a valid User Name :(", 10);
                    UnderRegister = false;
                }
                else
                {
                    if (Password == "Password" || string.IsNullOrEmpty(Password))
                    {
                        Rust.Notice.Popup("", "You must enter a valid Password :(", 10);
                        UnderRegister = false;
                    }
                    else
                    {
                        bool b = Regex.IsMatch(UserName, @"^[a-zA-Z0-9_&@%!+<>]+$");
                        bool b2 = Regex.IsMatch(Password, @"^[a-zA-Z0-9_&@%!+<>]+$");

                        if (!b || !b2)
                        {
                            Rust.Notice.Popup("", "Sorry, no special characters or space! Only: a-zA-Z0-9_&@%!+<>", 10);
                            UnderRegister = false;
                            return;
                        }

                        if (UserName.Length > 25 || Password.Length > 25)
                        {
                            Rust.Notice.Popup("", "Sorry username and password length must be below 25.", 10);
                            UnderRegister = false;
                            return;
                        }
                        
                        string request = AuthMe.Instance.SendMessageToServer("AuthMeRegister-" + UserName + "-" + Password);
                        if (request == "ValidRegistration")
                        {
                            Rust.Notice.Popup("", "Your have registered successfully.", 10);
                            this.enabled = false;
                        }
                        else if (request == "InvalidRegistration")
                        {
                            Rust.Notice.Popup("",
                                "There is something wrong. Check the chat for the issue.",
                                10);
                            UnderRegister = false;
                        }
                    }
                }
            }
            else
            {
                Rust.Notice.Popup("", "Wait a bit, your request is still being processed.", 10);
            }
        }
    }
}