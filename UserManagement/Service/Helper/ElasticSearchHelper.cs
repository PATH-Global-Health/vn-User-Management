using Data.ViewModels.ElasticSearchs;
using Flurl.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Helper
{
    public static class ElasticSearchHelper
    {
        public static IFlurlRequest GetBaseUrlRequest(string url, string adminUsername, string adminPassword)
        {
            var tokenBytes = Encoding.UTF8.GetBytes($"{adminUsername}:{adminPassword}");
            var token = Convert.ToBase64String(tokenBytes);
            return url
            .WithHeader("Authorization", $"Basic {token}")
            .WithHeader("kbn-xsrf", "kibana")
            .AppendPathSegment(@$"api/console/proxy")
            ;
        }
        public static async Task<GetUserElasticResponseViewModel> GetUserRequestAsync(string url, string adminUsername, string adminPassword, string username)
        {
            var getUserResponse = await GetBaseUrlRequest(url, adminUsername, adminPassword)
                     .SetQueryParams(new
                     {
                         path = @$"_security/user/{username}",
                         method = "GET"
                     })
                    .PostAsync();
            var elasticUserRaw = await getUserResponse.GetJsonAsync<ExpandoObject>();
            var elasticUserString = JsonConvert.SerializeObject(elasticUserRaw.FirstOrDefault().Value);
            var elasticUser = JsonConvert.DeserializeObject<GetUserElasticResponseViewModel>(elasticUserString);
            return elasticUser;
        }
        public static async Task<bool> IndexUserRequestAsync(string url, string adminUsername, string adminPassword, string username, CreateUserElasticRequest user)
        {
            var response = await GetBaseUrlRequest(url, adminUsername, adminPassword)
               .SetQueryParams(new
               {
                   path = @$"/_security/user/{username}",
                   method = "POST"
               })
              .PostJsonAsync(user);
            return response.StatusCode == 200;
        }
    }
}
