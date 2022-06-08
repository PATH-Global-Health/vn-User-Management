using Data.MongoCollections;
using Data.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface ISessionService
    {
        Task<List<SessionStatisticModel>> Statistic(GetStatisticRequest request);
        Task<string> Create(CreateSessionRequest request, string userId);
        Task End(string id);
    }
}
