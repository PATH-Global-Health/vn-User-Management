using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Service.Helper
{
    public class StringHelper
    {
        public static bool IsPhoneNumber(string number)
        {
            return Regex.Match(number, @"(84|0[3|5|7|8|9])+([0-9]{8})\b").Success;
        }
        public static bool IsValidEmail(string email)
        {
            if (email.Trim().EndsWith("."))
            {
                return false; // suggested by @TK-421
            }
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
