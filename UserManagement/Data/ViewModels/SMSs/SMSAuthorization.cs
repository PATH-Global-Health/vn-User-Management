using System;
using System.Collections.Generic;
using System.Text;

namespace Data.ViewModels.SMSs
{
    public class SMSAuthorization
    {
        public bool Active { get; set; }
        public string Brandname { get; set; }
        public string ApiKey { get; set; }
        public string SecretKey { get; set; }
    }
}
