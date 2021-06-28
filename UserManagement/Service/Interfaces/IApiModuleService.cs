﻿using Data.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IApiModuleService
    {
        PagingModel GetAll(int pageSize, int pageIndex);
        ApiModuleDetailModel GetDetail(string id);
        Task<ResultModel> Create(string apiHost, string moduleName, string upstreamName);
    }
}