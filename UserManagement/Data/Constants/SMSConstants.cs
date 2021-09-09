using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Constants
{
    public class SMSConstants
    {
        public const string BaseURL = "http://rest.esms.vn/MainService.svc/json/";

        public const string SUCCESS = "100";
        public const string UNDEFINED = "99";
        public const string FAILED_LOGIN = "101";
        public const string LOCKED_LOGIN = "102";
        public const string OUT_OF_MONEY = "103";
        public const string INCORRECT_BRANDNAME = "104";
    }
}
