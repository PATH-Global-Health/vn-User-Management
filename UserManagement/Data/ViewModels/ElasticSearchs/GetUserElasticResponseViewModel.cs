using System;
using System.Collections.Generic;
using System.Text;

namespace Data.ViewModels.ElasticSearchs
{
    public class GetUserElasticResponseViewModel
    {
        public string Username { get; set; }
        public List<string> Roles { get; set; }
        public string Full_name { get; set; }
        public string Email { get; set; }
        public bool Enabled { get; set; }
    }
}
