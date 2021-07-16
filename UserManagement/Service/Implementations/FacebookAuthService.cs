using Data.ViewModels.FacebookAuths;
using Newtonsoft.Json;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Service.Implementations
{

    public class FacebookAuthService : IFacebookAuthService
    {

        private const string TokenValidationUrl = "https://graph.facebook.com/debug_token?access_token={0}&input_token={1}|{2}";
        private const string UserInfoUrl = "https://graph.facebook.com/me?access_token={0}&fields=name,email,picture,birthday";
        private readonly FacebookAuthSettings _facebookAuthSettings;
        private readonly IHttpClientFactory _httpClientFactory;
        public FacebookAuthService(FacebookAuthSettings facebookAuthSettings, IHttpClientFactory httpClientFactory)
        {
            _facebookAuthSettings = facebookAuthSettings;
            _httpClientFactory = httpClientFactory;
        }
        public async Task<FacebookUserInfoResult> GetUserInfoResult(string accessToken)
        {
            var formattedUrl = string.Format(UserInfoUrl, accessToken);
            var result = await _httpClientFactory.CreateClient().GetAsync(formattedUrl);
            if (result.IsSuccessStatusCode)
            {
                var responseAsString = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<FacebookUserInfoResult>(responseAsString);
            }
            else
            {
                return null;
            }
        }

        public async Task<FacebookTokenValidationResult> ValidateAccessTokenAsync(string accessToken)
        {
            var formattedUrl = string.Format(TokenValidationUrl, accessToken, _facebookAuthSettings.AppId, _facebookAuthSettings.AppSecret);
            var result = await _httpClientFactory.CreateClient().GetAsync(formattedUrl);
            result.EnsureSuccessStatusCode();
            var responseAsString = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<FacebookTokenValidationResult>(responseAsString);
        }
    }
}
