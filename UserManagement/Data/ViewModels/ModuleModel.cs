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
        public IDictionary<string, PathItem> Paths { get; set; }
        public List<Server> Servers { get; set; }
    }

    public class Server
    {
        public string Url { get; set; }
    }

    public class PathItem
    {
        public Operation Get { get; set; }

        public Operation Put { get; set; }

        public Operation Post { get; set; }

        public Operation Delete { get; set; }

        public Operation Options { get; set; }

        public Operation Head { get; set; }

        public Operation Patch { get; set; }

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
