using System;
using System.Collections.Generic;
using System.Text;

namespace Data.ViewModels.Users
{
    public class ForgotPasswordModel
    {
        public string Email { get; set; }
        public string Username { get; set; }
    }

    public class SetUserPasswordModel
    {
        public string Password { get; set; }
        public string Token { get; set; }
    }
}
