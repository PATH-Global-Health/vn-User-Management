using Data.ViewModels;
using Microsoft.Extensions.Options;
using RestSharp;
using RestSharp.Authenticators;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Service.Implementations
{
    public class MailService : IMailService
    {
        private readonly EmailSettings _emailSettings;

        public MailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task<bool> SendEmail(EmailViewModel email)
        {
            RestClient client = new RestClient();
            client.BaseUrl = new Uri(_emailSettings.ApiBaseUri);
            client.Authenticator = new HttpBasicAuthenticator("api", _emailSettings.ApiKey);
            RestRequest request = new RestRequest();
            request.AddParameter("domain", _emailSettings.Domain, ParameterType.UrlSegment);
            request.Resource = $"{_emailSettings.Domain}/messages";
            request.AddParameter("from", $"USAID <{_emailSettings.From}>");
            request.AddParameter("to", email.To);
            request.AddParameter("subject", email.Subject);
            request.AddParameter("text", email.Text);
            request.Method = Method.POST;
            await client.ExecuteAsync(request);
            return true;
        }
    }
}
