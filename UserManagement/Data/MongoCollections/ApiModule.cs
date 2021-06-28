using System;
using System.Collections.Generic;
using System.Text;

namespace Data.MongoCollections
{
    public class ApiModule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string HostName { get; set; }
        public string NormalizedHostName { get; set; }
        public string ModuleName { get; set; }
        public string NormalizedModuleName { get; set; }
        public string UpstreamName { get; set; }
        public string NormalizedUpstreamName { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public DateTime DateUpdated { get; set; } = DateTime.Now;
        public string Description { get; set; }

        public List<ApiPath> Paths { get; set; } = new List<ApiPath>();

        public bool IsDeleted { get; set; }
    }

    public class ApiPath
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Path { get; set; }
        public string NormalizedPath { get; set; }
        public string Method { get; set; }
        public string NormalizedMethod { get; set; }
        public string Description { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public DateTime DateUpdated { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; }
    }
}
