using System;
using System.Collections.Generic;
using System.Text;

namespace Data.ViewModels
{
    public class EmailViewModel
    {
        public string To { get; set; }
        public string Subject { get; set; }
        public string Text { get; set; }
    }
    public class EmailSettings
    {
        public string ApiKey { get; set; }
        public string ApiBaseUri { get; set; }
        public string Domain { get; set; }
        public string From { get; set; }
    }
}
