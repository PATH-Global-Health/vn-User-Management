using System;
using System.Collections.Generic;
using System.Text;

namespace Data.ViewModels.ElasticSearchs
{
    public class CreateUserElasticRequest
    {
        public string password { get; set; }
        public List<string> roles { get; set; }
        public string full_name { get; set; }
        public bool enabled { get; set; } = true;
        public string email { get; set; }
    }
}
