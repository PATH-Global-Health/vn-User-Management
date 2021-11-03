using System.Collections.Generic;

namespace Data.ViewModels
{
    public class RoleModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class RoleUpdateModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class RoleCreateModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class AddUsersToRoleModel
    {
        public List<string> UserIds { get; set; }
    }

    public class AddRolesToGroupModel
    {
        public List<string> RoleIds { get; set; }
    }
}
