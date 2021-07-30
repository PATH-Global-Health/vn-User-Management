using Data.ViewModels.ProfileAPIs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IScheduleManagementAPIService
    {
        Task<bool> CreateProfile(string token, CreateProfileRequest request);
    }
}
