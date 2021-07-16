using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.ViewModels.FacebookAuths
{
    public class DataValidatation
    {
        public string app_id { get; set; }
        public string type { get; set; }
        public string application { get; set; }
        public int data_access_expires_at { get; set; }
        public int expires_at { get; set; }
        public bool is_valid { get; set; }
        public List<string> scopes { get; set; }
        public string user_id { get; set; }
    }

    public class FacebookTokenValidationResult
    {
        public DataValidatation data { get; set; }
    }
}
