using Data.DataAccess;
using Data.MongoCollections;
using Data.ViewModels;
using MongoDB.Bson;
using MongoDB.Driver;
using Service.Interfaces;
using System;
using System.Threading.Tasks;

namespace Service.Implementations
{
    public class SecurityQuestionService : ISecurityQuestionService
    {
        private readonly ApplicationDbContext _dbContext;

        public SecurityQuestionService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ResultModel> GetAll()
        {
            var result = new ResultModel();
            try
            {
                var securityQuestions = await _dbContext.SecurityQuestions.Find(_ => true).ToListAsync();
                result.Data = securityQuestions;
                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        public async Task<ResultModel> Create(CreateSecurityQuestionModel model)
        {
            var result = new ResultModel();
            try
            {
                var securityQuestion = await _dbContext.SecurityQuestions.Find(i => i.Question.ToLower() == model.Question.ToLower()).FirstOrDefaultAsync();
                if (securityQuestion != null)
                {
                    result.ErrorMessage = "SecurityQuestion was existed";
                    return result;
                }
                securityQuestion = new SecurityQuestion
                {
                    Question = model.Question,
                };
                await _dbContext.SecurityQuestions.InsertOneAsync(securityQuestion);
                result.Succeed = true;
                result.Data = securityQuestion.Id;
                return result;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        public async Task<ResultModel> Delete(string id)
        {

            var result = new ResultModel();
            try
            {
                var securityQuestion = await _dbContext.SecurityQuestions.FindOneAndDeleteAsync(i => i.Id == id);
                if (securityQuestion == null)
                {
                    result.ErrorMessage = "SecurityQuestion isn't existed";
                    return result;
                }
                result.Succeed = true;
                result.Data = securityQuestion.Id;
                return result;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        public async Task<ResultModel> Get(string id)
        {
            var result = new ResultModel();
            try
            {
                var securityQuestion = await _dbContext.SecurityQuestions.Find(i => i.Id == id).FirstOrDefaultAsync();
                if (securityQuestion == null)
                {
                    result.ErrorMessage = "SecurityQuestion isn't existed";
                    return result;
                }
                result.Succeed = true;
                result.Data = securityQuestion;
                return result;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }

        public async Task<ResultModel> Update(UpdateSecurityQuestionModel model)
        {
            var result = new ResultModel();
            try
            {
                var securityQuestion = await _dbContext.SecurityQuestions.FindOneAndReplaceAsync(i => i.Id == model.Id, new SecurityQuestion()
                {
                    Id = model.Id,
                    Question = model.Question,
                });
                if (securityQuestion == null)
                {
                    result.ErrorMessage = "SecurityQuestion isn't existed";
                    return result;
                }
                result.Succeed = true;
                result.Data = securityQuestion.Id;
                return result;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
            }
            return result;
        }
    }
}