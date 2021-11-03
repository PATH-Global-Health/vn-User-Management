using Data.ViewModels;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IApiModuleService
    {
        PagingModel GetAll(int pageSize, int pageIndex);
        ApiModuleDetailModel GetDetail(string id);
        Task<ResultModel> Create(string apiHost, string replacementHost, string moduleName, bool doPathReplacement);
        Task<ResultModel> Update(string moduleId, string apiHost, string replacementHost, string moduleName, string upstreamName, bool doPathReplacement);
        Task<string> GetSwaggerDocument(string moduleId);
    }
}
