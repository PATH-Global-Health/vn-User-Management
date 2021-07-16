using System;
using System.Collections.Generic;
using System.Text;

namespace Data.ViewModels.GoogleAuths
{
    public class GoogleUserInfoViewModel
    {
        public string emailAddress { get; set; }
        public int messagesTotal { get; set; }
        public int threadsTotal { get; set; }
        public string historyId { get; set; }
    }
}
