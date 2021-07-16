using Data.ViewModels.GoogleAuths;
using Flurl.Http;
using Google.Apis.Auth;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Service.Implementations
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly GoogleAuthSettings _googleAuthSettings;

        public GoogleAuthService(GoogleAuthSettings googleAuthSettings)
        {
            _googleAuthSettings = googleAuthSettings;
        }

        public async Task<GoogleJsonWebSignature.Payload> GetUserInfoResult(string accessToken)
        {
            GoogleJsonWebSignature.ValidationSettings settings = new GoogleJsonWebSignature.ValidationSettings();
            settings.Audience = new List<string>() { _googleAuthSettings.AppId };
            GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(accessToken, settings);
            return payload;
        }
    }
}
