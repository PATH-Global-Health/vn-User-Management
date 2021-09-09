using Data.Constants;
using Data.ViewModels.SMSs;
using Flurl;
using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Implementations
{
    public interface ISMSService
    {
        Task<SendMessageResponseViewModel> SendOTP(string OTP, string phoneNumber);
    }
    public class SMSService : ISMSService
    {
        public readonly SMSAuthorization _smsAuthorization;
        public SMSService(SMSAuthorization smsAuthorization)
        {
            _smsAuthorization = smsAuthorization;
        }


        public async Task<SendMessageResponseViewModel> SendOTP(string OTP, string phoneNumber)
        {
            await CheckBalance();
            var sendBody = new SendMessageViewModel
            {
                ApiKey = _smsAuthorization.ApiKey,
                SecretKey = _smsAuthorization.SecretKey,
                Brandname = _smsAuthorization.Brandname,
                SmsType = 2,
                Phone = phoneNumber,
                Content = $"{OTP} la ma xac minh dang ky Baotrixemay cua ban",
            };
            var response = await SMSConstants.BaseURL
              .AppendPathSegment("SendMultipleMessage_V4_post_json")
              .PostJsonAsync(sendBody);
            return await response.GetJsonAsync<SendMessageResponseViewModel>();
        }
        private async Task<SMSBalanceViewModel> GetBalance()
        {
            var response = await SMSConstants.BaseURL
                .AppendPathSegment("GetBalance_json")
                .PostJsonAsync(_smsAuthorization);
            return await response.GetJsonAsync<SMSBalanceViewModel>();
        }
        private async Task CheckBalance()
        {
            var balance = await GetBalance();
            switch (balance.CodeResponse)
            {
                case SMSConstants.OUT_OF_MONEY:
                    throw new Exception(nameof(SMSConstants.OUT_OF_MONEY));
                case SMSConstants.FAILED_LOGIN:
                    throw new Exception(nameof(SMSConstants.FAILED_LOGIN));
                default:
                    break;
            }
        }
    }
}
