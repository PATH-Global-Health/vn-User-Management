using System;
using System.Collections.Generic;
using System.Text;

namespace Data.ViewModels
{
    public class SecurityQuestionModel
    {
        public SecurityQuestionModel(string id, string answer)
        {
            Id = id;
            Question = answer;
        }
        public string Id { get; set; }
        public string Question { get; set; }
    }
}
