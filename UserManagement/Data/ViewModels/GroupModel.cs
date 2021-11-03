using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Data.ViewModels
{
    public class GroupOverviewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

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
        [Required]
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
