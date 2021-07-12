using Data.ViewModels;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IApiModuleService
    {
        PagingModel GetAll(int pageSize, int pageIndex);
        ApiModuleDetailModel GetDetail(string id);
        Task<ResultModel> Create(string apiHost, string moduleName, string upstreamName);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="swaggerHost"></param>
        /// <param name="serverUrl"></param>
        /// <param name="removeApiPathPrefix">Remove the prefix '/api' in the api path</param>
        /// <returns></returns>
        Task<ResultModel> GetSwaggerDocument(string swaggerHost, string serverUrl, bool removeApiPathPrefix);
    }
}
