using AutoMapper;
using Data.DataAccess;
using Data.MongoCollections;
using Data.ViewModels;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Service.Implementations
{
    public class ApiModuleService : IApiModuleService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMapper _mapper;

        public ApiModuleService(ApplicationDbContext dbContext, IHttpClientFactory httpClientFactory, IMapper mapper)
        {
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
            _mapper = mapper;
        }

        public async Task<ResultModel> Create(string apiHost, string moduleName, string upstreamName)
        {
            var result = new ResultModel();

            #region Validate apiHost
            if (string.IsNullOrEmpty(apiHost))
            {
                result.ErrorMessage = "Invalid host name"; return result;
            }

            apiHost = apiHost.Trim();
            if (apiHost.Contains(" "))
            {
                result.ErrorMessage = "Host cannot have white spaces"; return result;
            }
            var lastChar = apiHost.ElementAt(apiHost.Length - 1);
            if (lastChar == '/')
            {
                apiHost = apiHost.Remove(apiHost.Length - 1);
            }
            var existedModule = _dbContext.ApiModules.Find(i => i.NormalizedHostName == apiHost.ToUpper()).FirstOrDefault();
            if (existedModule != null)
            {
                result.ErrorMessage = "Host is existed as module name : " + existedModule.NormalizedModuleName; return result;
            }
            #endregion

            #region Validate moduleName
            if (string.IsNullOrEmpty(moduleName))
            {
                result.ErrorMessage = "Invalid module name"; return result;
            }

            moduleName = moduleName.Trim();
            if (moduleName.Contains(" "))
            {
                result.ErrorMessage = "Module name cannot have white spaces"; return result;
            }
            existedModule = _dbContext.ApiModules.Find(i => i.NormalizedModuleName == moduleName.ToUpper()).FirstOrDefault();
            if (existedModule != null)
            {
                result.ErrorMessage = "Module name is existed"; return result;
            }
            #endregion

            #region Validate upstreamName
            if (string.IsNullOrWhiteSpace(upstreamName))
            {
                result.ErrorMessage = "Invalid upstream name"; return result;
            }

            upstreamName = upstreamName.Trim();
            existedModule = _dbContext.ApiModules.Find(i => i.NormalizedUpstreamName == upstreamName.ToUpper()).FirstOrDefault();
            if (existedModule != null)
            {
                result.ErrorMessage = "upstreamName is existed at module : " + existedModule.ModuleName; return result;
            }
            #endregion

            var httpClient = _httpClientFactory.CreateClient();
            try
            {
                var swaggerDocument = await GetSwaggerDocument(apiHost);
                if (swaggerDocument != null)
                {
                    var newModule = new ApiModule
                    {
                        HostName = apiHost,
                        NormalizedHostName = apiHost.ToUpper().Trim(),
                        ModuleName = moduleName,
                        NormalizedModuleName = moduleName.ToUpper().Trim(),
                        UpstreamName = upstreamName,
                        NormalizedUpstreamName = upstreamName.ToUpper().Trim(),
                        Paths = swaggerDocument.Paths.Select(path => new ApiPath
                        {
                            Path = path.Key,
                            NormalizedPath = path.Key.ToUpper().Trim(),
                            Method = GetMethodName(path.Value),
                            NormalizedMethod = GetMethodName(path.Value).ToUpper().Trim()
                        }).ToList()
                    };
                    _dbContext.ApiModules.InsertOne(newModule);

                    result.Succeed = true;
                    result.Data = newModule.Id;
                }
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.Message;
            }
            finally
            {
                httpClient.Dispose();
            }

            return result;

            #region Inner functions
            async Task<SwaggerDocument> GetSwaggerDocument(string host)
            {
                var responseMessage = await httpClient.GetAsync(apiHost + "/swagger/v1/swagger.json");
                if (responseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var swaggerDocumentJson = await responseMessage.Content.ReadAsStringAsync();
                    var swaggerDocument = JsonConvert.DeserializeObject<SwaggerDocument>(swaggerDocumentJson);

                    return swaggerDocument;
                }
                else
                {
                    result.ErrorMessage = "Status Code : " + responseMessage.StatusCode + " on host : " + apiHost;
                    return null;
                }
            }

            string GetMethodName(PathItem value)
            {
                if (value.Get != null) return "Get";
                if (value.Post != null) return "Post";
                if (value.Put != null) return "Put";
                if (value.Delete != null) return "Delete";
                if (value.Options != null) return "Options";
                if (value.Head != null) return "Head";
                if (value.Patch != null) return "Patch";
                return "";
            }
            #endregion
        }

        public PagingModel GetAll(int pageSize, int pageIndex)
        {
            var result = new PagingModel();

            var modulesFluent = _dbContext.ApiModules.Find(i => !i.IsDeleted).SortByDescending(i => i.DateCreated);

            var totalPages = (int)Math.Ceiling((double)modulesFluent.CountDocuments() / pageSize);
            var modules = modulesFluent.Skip(pageSize * pageIndex).Limit(pageSize).ToList();

            result.TotalPages = totalPages;
            result.Data = _mapper.Map<List<ApiModule>, List<ApiModuleModel>>(modules);

            return result;
        }

        public ApiModuleDetailModel GetDetail(string id)
        {
            var module = _dbContext.ApiModules.Find(i => i.Id == id).FirstOrDefault();
            if (module == null || module.IsDeleted) return null;

            return _mapper.Map<ApiModule, ApiModuleDetailModel>(module);
        }

        public async Task<ResultModel> GetSwaggerDocument(string swaggerHost, string serverUrl, bool removeApiPathPrefix)
        {
            var result = new ResultModel();
            try
            {
                #region Validate swaggerHost
                if (string.IsNullOrEmpty(swaggerHost))
                {
                    result.ErrorMessage = "Invalid host name"; return result;
                }

                swaggerHost = swaggerHost.Trim();
                if (swaggerHost.Contains(" "))
                {
                    result.ErrorMessage = "Host cannot have white spaces"; return result;
                }
                var lastChar = swaggerHost.ElementAt(swaggerHost.Length - 1);
                if (lastChar == '/')
                {
                    swaggerHost = swaggerHost.Remove(swaggerHost.Length - 1);
                }
                #endregion

                using (var httpClient = _httpClientFactory.CreateClient())
                {
                    var responseMessage = await httpClient.GetAsync(swaggerHost + "/swagger/v1/swagger.json");

                    if (responseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var swaggerDocumentJson = await responseMessage.Content.ReadAsStringAsync();
                        var swaggerDocument = JsonConvert.DeserializeObject<dynamic>(swaggerDocumentJson);

                        //Set serverUrl
                        swaggerDocument.servers[0].url = serverUrl;


                        #region  Modify api Path 
                        if (removeApiPathPrefix)
                        {
                            //define variables
                            var rawPathsObject = swaggerDocument.paths as JObject;
                            var paths = rawPathsObject.ToObject<IDictionary<string, dynamic>>();
                            var modifiedPaths = new Dictionary<string, dynamic>();

                            //Modifying
                            foreach (var item in paths)
                            {
                                modifiedPaths.Add(item.Key.Replace("/api", ""), item.Value);
                            }

                            //Set to swagger document object
                            rawPathsObject = JObject.FromObject(modifiedPaths);
                            swaggerDocument.paths = rawPathsObject;
                        }
                        #endregion

                        swaggerDocumentJson = JsonConvert.SerializeObject(swaggerDocument);

                        result.Succeed = true;
                        result.Data = swaggerDocumentJson;
                    }
                    else
                    {
                        result.ErrorMessage = "Status Code : " + responseMessage.StatusCode + " on host : " + swaggerHost;
                        return null;
                    }
                }

            }
            catch (Exception e)
            {
                result.ErrorMessage = e.Message;
            }

            return result;
        }
    }
}
