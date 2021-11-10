using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Text;

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
    }
}
