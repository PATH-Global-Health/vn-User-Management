using Data.MongoCollections;
using Data.ViewModels;
using System.Collections.Generic;

namespace Service.Interfaces
{
    public interface IProvincialService
    {
        void EnsureDataPopulated();
        List<ProvincialInformation> GetAll();
        List<int> GetProvincials(string userId);
        ResultModel AddProvincialInfo(string userId, List<string> provinceIds);
        ResultModel RemoveProvincialInfo(string userId, string provinceId);
    }
}
