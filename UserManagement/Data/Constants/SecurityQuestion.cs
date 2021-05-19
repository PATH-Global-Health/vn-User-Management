using Data.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Constants
{
    public static class SecurityQuestion
    {
        private static readonly List<SecurityQuestionModel> securityQuestions = new List<SecurityQuestionModel>() {
            new SecurityQuestionModel("D169D7CF-9C24-4F3C-84F7-E91010E35E24", "Tên người quản lý đầu tiên của bạn là gì?"),
            new SecurityQuestionModel("F8CF7FB9-DD62-40A4-92A9-3F016AD1C4D2", "Ba mẹ của bạn gặp nhau ở thành phố nào?"),
            new SecurityQuestionModel("A38248AD-A868-47E1-86C2-251159C09B17", "Thú nuôi đầu tiên của bạn tên gì?"),
            new SecurityQuestionModel("4870E347-386C-49DE-B662-7DCB8758D5BB", "Người bạn thân của bạn ở thời thanh thiếu niên tên gì?"),
            new SecurityQuestionModel("B5834AC2-F506-4ECD-BB76-042D7F72D5CC", "Món ăn đầu tiên bạn học nấu là gì?"),
        };

        public static List<SecurityQuestionModel> SecurityQuestions
        {
            get { return securityQuestions; }
        }
    }
}
