using Data.Enums;
using System;

namespace Data.ViewModels
{
    public class ResourcePermissionModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public string Url { get; set; }
        public string NormalizedUrl { get; set; }
        public string Method { get; set; }
        public string NormalizedMethod { get; set; }

        public PermissionType PermissionType { get; set; }
    }

    public class ResourcePermissionCreateModel
    {
        public string Name { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// Format sample : /api/{path1}/{path2}
        /// </summary>
        public string Url { get; set; }
        public string Method { get; set; }
        public PermissionType PermissionType { get; set; }
    }

    public class UiPermissionModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public string Code { get; set; }
        public PermissionType Type { get; set; }
    }

    public class UiPermissionCreateModel
    {
        public string Name { get; set; }
        public string Code { get; set; } = Guid.NewGuid().ToString();
        public string Description { get; set; }

        public PermissionType PermissionType { get; set; }
    }

    public class AddResourcePermissionModel
    {
        public ResourcePermissionCreateModel Permission { get; set; }
        public HolderType HolderType { get; set; }
        public string HolderId { get; set; }
    }

    public class AddUiPermissionModel
    {
        public UiPermissionCreateModel Permission { get; set; }
        public HolderType HolderType { get; set; }
        public string HolderId { get; set; }
    }

    public class ResourcePermissionValidationModel
    {
        /// <summary>
        /// Sample Format : /api/{path1}/{path2}/...
        /// </summary>
        public string ApiPath { get; set; }
        public string Method { get; set; }
    }
}
