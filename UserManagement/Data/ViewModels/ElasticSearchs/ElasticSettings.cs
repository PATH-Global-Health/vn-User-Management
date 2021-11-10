using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.ViewModels.ElasticSearchs
{
    public class ElasticSettings
    {
        public string KibanaUrl { get; set; }
        public string Url { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string DefaultRole { get; set; }
    }
}
