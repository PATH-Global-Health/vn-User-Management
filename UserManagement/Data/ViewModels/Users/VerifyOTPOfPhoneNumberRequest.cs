using System;
using System.Collections.Generic;
using System.Text;

namespace Data.ViewModels.Users
{
    public class VerifyOTPOfPhoneNumberRequest
    {
        public string PhoneNumber { get; set; }
        public string OTP { get; set; }
    }
}
