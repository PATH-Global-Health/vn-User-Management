using Data.ViewModels.GoogleAuths;
using Google.Apis.Auth;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IGoogleAuthService
    {
        Task<GoogleJsonWebSignature.Payload> GetUserInfoResult(string accessToken);
    }
}
