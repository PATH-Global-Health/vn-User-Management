using System.Collections.Generic;

namespace Data.ViewModels
{
    public class GroupModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<UserInformationModel> Users { get; set; }
        public List<ResourcePermissionModel> ResourcePermissions { get; set; }
        public List<UiPermissionModel> UiPermissions { get; set; }
    }

    public class GroupCreateModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class GroupUpdateModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
