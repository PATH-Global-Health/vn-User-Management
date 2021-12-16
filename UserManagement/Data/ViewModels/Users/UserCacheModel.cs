using System;
using System.Collections.Generic;
using System.Text;

namespace Data.ViewModels.Users
{
    public class UserCacheModel
    {
        public string Id { get; set; }
        public string NormalizedUsername { get; set; }
        public string HashedCredential { get; set; }
        public List<string> RoleIds { get; set; } = new List<string>();
        public List<string> GroupIds { get; set; } = new List<string>();
        public List<string> ResourcePermissionIds { get; set; } = new List<string>();
        public bool? IsDisabled { get; set; } = false;
    }
}
