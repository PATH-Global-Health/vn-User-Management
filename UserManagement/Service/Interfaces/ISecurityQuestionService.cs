using Data.ViewModels;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface ISecurityQuestionService
    {
        Task<ResultModel> GetAll();
        Task<ResultModel> Get(string id);
        Task<ResultModel> Create(CreateSecurityQuestionModel model);
        Task<ResultModel> Update(UpdateSecurityQuestionModel model);
        Task<ResultModel> Delete(string id);
    }
}
