using System;
using System.Collections.Generic;
using System.Text;

namespace Data.ViewModels.SMSs
{
    public class SendMessageViewModel : SMSAuthorization
    {
        public string Content { get; set; }
        public string Phone { get; set; }
        public string IsUnicode { get; set; }
        public string Brandname { get; set; }
        public int SmsType { get; set; }
        public string RequestId { get; set; }
        public string CallbackUrl { get; set; }
        public string Campaignid { get; set; }
    }
    public class SendMessageResponseViewModel
    {
        public string CodeResult { get; set; }
        public string CountRegenerate { get; set; }
        public string SMSID { get; set; }
    }
}
