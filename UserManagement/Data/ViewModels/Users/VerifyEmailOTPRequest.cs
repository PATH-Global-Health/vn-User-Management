using System;
using System.Collections.Generic;
using System.Text;

namespace Data.ViewModels.Users
{
    public class VerifyEmailOTPRequest
    {
        public string Email { get; set; }
        public string OTP { get; set; }
    }
}
