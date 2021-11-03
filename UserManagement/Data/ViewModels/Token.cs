using System.Collections.Generic;

namespace Data.ViewModels
{
    public class Permission
    {
        public string Code { get; set; }
    }

    public class Token
    {
        public string Access_token { get; set; }
        public string Token_type { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public int Expires_in { get; set; }
        public List<Permission> ResourcePermissions { get; set; } = new List<Permission>();
        public List<Permission> UiPermissions { get; set; } = new List<Permission>();
    }
}
