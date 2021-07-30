using Data.Constants;
using Data.ViewModels;
using Data.ViewModels.ProfileAPIs;
using Flurl;
using Flurl.Http;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Service.Implementations
{
    public class ScheduleManagementAPIService : IScheduleManagementAPIService
    {
        private readonly string ProfilePath = "Profiles";
        #region Profile API
        public async Task<bool> CreateProfile(string token, CreateProfileRequest request)
        {
            var response = await MyConstants.ScheduleManagementAPIBase
                //.WithHeader(AuthorizationType, token)
                .WithOAuthBearerToken(token)
                .AppendPathSegment(ProfilePath)
                .PostJsonAsync(request);
            return response.StatusCode == 200;
        }
        #endregion
    }
}
