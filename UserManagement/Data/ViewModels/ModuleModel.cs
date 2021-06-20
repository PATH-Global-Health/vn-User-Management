using System;
using System.Collections.Generic;

namespace Data.ViewModels
{
    public class ApiModuleModel
    {
        public string Id { get; set; }
        public string HostName { get; set; }
        public string ModuleName { get; set; }
        public string UpstreamName { get; set; }
        public string Description { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class ApiModuleDetailModel
    {
        public string Id { get; set; }
        public string HostName { get; set; }
        public string ModuleName { get; set; }
        public string UpstreamName { get; set; }
        public string Description { get; set; }
        public DateTime DateCreated { get; set; }
        public List<ApiPathModel> Paths { get; set; } = new List<ApiPathModel>();
    }

    public class ApiPathModel
    {
        public string Id { get; set; }
        public string Path { get; set; }
        public string Method { get; set; }
        public string Description { get; set; }
    }

    public class ModuleCreateModel
    {
        public string Host { get; set; }
        public string ModuleName { get; set; }
        public string UpstreamName { get; set; }
    }

    public class SwaggerDocument
    {
        public IDictionary<string, PathItem> Paths;
    }

    public class PathItem
    {
        public Operation Get;

        public Operation Put;

        public Operation Post;

        public Operation Delete;

        public Operation Options;

        public Operation Head;

        public Operation Patch;

    }

    public class Operation
    {
        public IList<string> Tags;

        public string Summary;

        public string Description;

        public string OperationId;

        public IList<string> Consumes;

        public IList<string> Produces;

        public IList<string> Schemes;

        public bool? Deprecated;

        public IList<IDictionary<string, IEnumerable<string>>> Security;

        public Dictionary<string, object> VendorExtensions = new Dictionary<string, object>();
    }
}
