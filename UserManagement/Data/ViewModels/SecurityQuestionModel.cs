using System;
using System.Collections.Generic;
using System.Text;

namespace Data.ViewModels
{
    public class CreateSecurityQuestionModel
    {
        public string Question { get; set; }
    }
    public class UpdateSecurityQuestionModel
    {
        public string Id { get; set; }
        public string Question { get; set; }
    }
}
