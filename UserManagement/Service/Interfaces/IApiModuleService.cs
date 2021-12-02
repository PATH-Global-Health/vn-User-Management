using Data.ViewModels;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IApiModuleService
    {
        PagingModel GetAll(int pageSize, int pageIndex);
        ApiModuleDetailModel GetDetail(string id);
        Task<ResultModel> Create(string apiHost, string replacementHost, string moduleName, bool doPathReplacement);
        Task<ResultModel> Update(ModuleUpdateModel request);
        Task<string> GetSwaggerDocument(string moduleId);
        Task<ResultModel> Delete(string id);
    }
}
