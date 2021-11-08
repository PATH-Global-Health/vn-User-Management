using System;
using System.Collections.Generic;
using System.Text;

namespace Data.ViewModels.Users
{
    public class SendOTPRequest
    {
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
    }
}
