using Data.Constants;
using Data.ViewModels;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Service.Implementations
{
    public class SecurityQuestionService : ISecurityQuestionService
    {
        public ResultModel GetAll()
        {
            var result = new ResultModel();
            try
            {
                result.Data = SecurityQuestion.SecurityQuestions;
                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }
    }
}
