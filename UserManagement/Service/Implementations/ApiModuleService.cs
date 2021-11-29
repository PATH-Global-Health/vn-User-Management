using AutoMapper;
using Data.DataAccess;
using Data.MongoCollections;
using Data.ViewModels;
using MongoDB.Driver;
using MoreLinq;
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

        public async Task<ResultModel> Create(string apiHost, string replacementHost, string moduleName, bool doPathReplacement)
        {
            var result = new ResultModel();

            #region Validate apiHost
            if (string.IsNullOrEmpty(apiHost))
            {
                result.ErrorMessage = "Invalid host name"; return result;
            }
            else
            {
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
            }


            #endregion

            #region Validate replacementHost
            if (string.IsNullOrEmpty(replacementHost))
            {
                result.ErrorMessage = "Invalid host name"; return result;
            }
            else
            {
                replacementHost = replacementHost.Trim();
                if (replacementHost.Contains(" "))
                {
                    result.ErrorMessage = "Host cannot have white spaces"; return result;
                }
                var lastChar = replacementHost.ElementAt(replacementHost.Length - 1);
                if (lastChar == '/')
                {
                    replacementHost = replacementHost.Remove(replacementHost.Length - 1);
                }
            }
            #endregion

            #region Validate moduleName
            if (string.IsNullOrEmpty(moduleName))
            {
                result.ErrorMessage = "Invalid module name"; return result;
            }
            else
            {
                moduleName = moduleName.Trim();
                if (moduleName.Contains(" "))
                {
                    result.ErrorMessage = "Module name cannot have white spaces"; return result;
                }
                var existedModule = _dbContext.ApiModules.Find(i => i.NormalizedModuleName == moduleName.ToUpper() && i.NormalizedHostName == replacementHost.ToUpper()).FirstOrDefault();
                if (existedModule != null)
                {
                    result.ErrorMessage = "Module name is existed"; return result;
                }
            }
            #endregion

            var httpClient = _httpClientFactory.CreateClient();
            var session = _dbContext.StartSession(); session.StartTransaction();
            try
            {
                #region Create module
                ResultModel modifySwaggerDocumentResult = await ModifySwaggerDocument(apiHost, $"{replacementHost}/api", doPathReplacement);
                var swaggerDocument = JsonConvert.DeserializeObject<SwaggerDocument>((string)modifySwaggerDocumentResult.Data);
                ApiModule newModule = null;
                if (swaggerDocument != null)
                {
                    newModule = new ApiModule
                    {
                        HostName = replacementHost,
                        NormalizedHostName = replacementHost.ToUpper().Trim(),
                        ModuleName = moduleName,
                        NormalizedModuleName = moduleName.ToUpper().Trim(),
                        Paths = swaggerDocument.Paths.Select(path => new ApiPath
                        {
                            Path = path.Key,
                            NormalizedPath = path.Key.ToUpper().Trim(),
                            Method = GetMethodName(path.Value),
                            NormalizedMethod = GetMethodName(path.Value).ToUpper().Trim()
                        }).ToList(),
                        RawSwaggerDocument = (string)modifySwaggerDocumentResult.Data
                    };
                    _dbContext.ApiModules.InsertOne(session, newModule);
                }
                else
                {
                    await session.AbortTransactionAsync();
                    result.ErrorMessage = "Error on getting swagger document";
                    return result;
                }
                #endregion

                #region Create Resource Permissions
                newModule.Paths.AsParallel().ForEach(path =>
                {
                    var permission = new ResourcePermission
                    {
                        Method = path.Method,
                        NormalizedMethod = path.NormalizedMethod,
                        Url = $"/api/{path.Path}",
                        PermissionType = Data.Enums.PermissionType.Allow,
                        IsAuthorizedAPI = true
                    };
                    permission.Url = permission.Url.Replace("//", "/");
                    permission.Url = $"{replacementHost}{permission.Url}";
                    permission.NormalizedUrl = permission.Url.Trim().ToUpper();
                    _dbContext.ResourcePermissions.InsertOne(session, permission);
                    path.PermissionIds.Add(permission.Id);
                });

                await _dbContext.ApiModules.ReplaceOneAsync(session, i => i.Id == newModule.Id, newModule);
                #endregion

                await session.CommitTransactionAsync();
                result.Succeed = true;
                result.Data = newModule.Id;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.Message;
                await session.AbortTransactionAsync();
            }
            finally
            {
                httpClient.Dispose();
                session.Dispose();
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

        private async Task<ResultModel> ModifySwaggerDocument(string swaggerHost, string serverUrl, bool removeApiPathPrefix)
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

        public async Task<string> GetSwaggerDocument(string moduleId)
        {
            var module = await _dbContext.ApiModules.Find(i => i.Id == moduleId).FirstOrDefaultAsync();
            if (module != null)
            {
                return module.RawSwaggerDocument;
            }
            return null;
        }

        public async Task<ResultModel> Update(string moduleId, string apiHost, string replacementHost, string moduleName, string upstreamName, bool doPathReplacement)
        {
            var result = new ResultModel();

            var module = await _dbContext.ApiModules.Find(i => i.Id == moduleId).FirstOrDefaultAsync();
            if (module == null)
            {
                result.ErrorMessage = "Module is not existed";
                return result;
            }

            #region Validate apiHost
            if (string.IsNullOrEmpty(apiHost))
            {
                result.ErrorMessage = "Invalid host name"; return result;
            }
            else
            {
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
            }


            #endregion

            #region Validate replacementHost
            if (string.IsNullOrEmpty(replacementHost))
            {
                result.ErrorMessage = "Invalid host name"; return result;
            }
            else
            {
                replacementHost = replacementHost.Trim();
                if (replacementHost.Contains(" "))
                {
                    result.ErrorMessage = "Host cannot have white spaces"; return result;
                }
                var lastChar = replacementHost.ElementAt(replacementHost.Length - 1);
                if (lastChar == '/')
                {
                    replacementHost = replacementHost.Remove(replacementHost.Length - 1);
                }
                var existedModule = _dbContext.ApiModules.Find(i => i.NormalizedHostName == replacementHost.ToUpper()).FirstOrDefault();
                if (existedModule != null)
                {
                    result.ErrorMessage = "Host is existed as module name : " + existedModule.NormalizedModuleName; return result;
                }
            }


            #endregion

            #region Validate moduleName
            if (string.IsNullOrEmpty(moduleName))
            {
                result.ErrorMessage = "Invalid module name"; return result;
            }
            else
            {
                moduleName = moduleName.Trim();
                if (moduleName.Contains(" "))
                {
                    result.ErrorMessage = "Module name cannot have white spaces"; return result;
                }
                var existedModule = await _dbContext.ApiModules.Find(i => i.NormalizedModuleName == moduleName.ToUpper() && i.Id != moduleId).FirstOrDefaultAsync();
                if (existedModule != null)
                {
                    result.ErrorMessage = "Module name is existed"; return result;
                }
            }


            #endregion

            var session = _dbContext.StartSession(); session.StartTransaction();
            try
            {
                ResultModel modifySwaggerDocumentResult = await ModifySwaggerDocument(apiHost, $"{replacementHost}/api/{upstreamName}", doPathReplacement);
                var swaggerDocument = JsonConvert.DeserializeObject<SwaggerDocument>((string)modifySwaggerDocumentResult.Data);
                if (swaggerDocument != null)
                {
                    #region Update paths
                    var newPaths = new List<ApiPath>();
                    foreach (var apiPath in swaggerDocument.Paths)
                    {
                        var normalizedPath = apiPath.Key.ToUpper().Trim();
                        var normalizedMethod = GetMethodName(apiPath.Value).ToUpper().Trim();

                        var existedPath = module.Paths.Find(p =>
                        {
                            return p.NormalizedPath == normalizedPath && p.NormalizedMethod == normalizedMethod;
                        });
                        if (existedPath == null)
                        {
                            newPaths.Add(new ApiPath
                            {
                                Path = apiPath.Key,
                                NormalizedPath = normalizedPath,
                                Method = GetMethodName(apiPath.Value),
                                NormalizedMethod = normalizedMethod
                            });
                        }
                    }

                    //Find deletedPaths : Except by NormalizedPath and NormalizedMethod
                    var deletedPaths = module.Paths.ExceptBy(swaggerDocument.Paths.Select(p => new ApiPath
                    {
                        NormalizedPath = p.Key.ToUpper().Trim(),
                        NormalizedMethod = GetMethodName(p.Value).ToUpper().Trim()
                    }), element =>
                    {
                        return new { element.NormalizedPath, element.NormalizedMethod };
                    });

                    //Add new api paths and add new permissions
                    if (newPaths.Any())
                    {
                        #region Add new api paths
                        module.Paths.AddRange(newPaths);
                        #endregion

                        #region Add new resource permissions
                        var newPermissions = newPaths.Select(path =>
                        {
                            var permission = new ResourcePermission
                            {
                                Method = path.Method,
                                NormalizedMethod = path.NormalizedMethod,
                                Url = $"{replacementHost}/api/{moduleName}/{upstreamName}/{path.Path}",
                                PermissionType = Data.Enums.PermissionType.Allow
                            };
                            permission.Url = permission.Url.Replace("//", "/");
                            permission.Url = $"{replacementHost}{permission.Url}";
                            permission.NormalizedUrl = permission.Url.Trim().ToUpper();

                            path.PermissionIds.Add(permission.Id);

                            return permission;
                        });

                        await _dbContext.ResourcePermissions.InsertManyAsync(session, newPermissions);
                        #endregion
                    }

                    //Remove api paths and remove permissions
                    if (deletedPaths.Any())
                    {
                        foreach (var apiPath in deletedPaths)
                        {
                            #region Remove in module
                            module.Paths.Remove(apiPath);
                            #endregion

                            #region Remove permission
                            foreach (var permissionId in apiPath.PermissionIds)
                            {
                                await _dbContext.ResourcePermissions.DeleteOneAsync(i => i.Id == permissionId);

                                #region Create filter tasks
                                var userFilters = Builders<UserInformation>.Filter.AnyEq(i => i.ResourcePermissionIds, permissionId);
                                var userTask = _dbContext.Users.Find(userFilters).ToListAsync();

                                var groupFilters = Builders<Group>.Filter.AnyEq(i => i.ResourcePermissionIds, permissionId);
                                var groupTask = _dbContext.Groups.Find(groupFilters).ToListAsync();

                                var roleFilters = Builders<Role>.Filter.AnyEq(i => i.ResourcePermissionIds, permissionId);
                                var roleTask = _dbContext.Roles.Find(roleFilters).ToListAsync();
                                #endregion

                                await Task.WhenAll(userTask, groupTask, roleTask);

                                #region Remove permission from holders
                                userTask.Result.AsParallel().ForEach(user =>
                                {
                                    user.ResourcePermissionIds.RemoveAll(id => id == permissionId);
                                    _dbContext.Users.ReplaceOne(session, i => i.Id == user.Id, user);
                                });

                                groupTask.Result.AsParallel().ForEach(group =>
                                {
                                    group.ResourcePermissionIds.RemoveAll(id => id == permissionId);
                                    _dbContext.Groups.ReplaceOne(session, i => i.Id == group.Id, group);
                                });

                                roleTask.Result.AsParallel().ForEach(role =>
                                {
                                    role.ResourcePermissionIds.RemoveAll(id => id == permissionId);
                                    _dbContext.Roles.ReplaceOne(session, i => i.Id == role.Id, role);
                                });
                                #endregion

                            }
                            #endregion
                        }
                    }
                    #endregion

                    #region Update common properties
                    module.HostName = replacementHost;
                    module.NormalizedHostName = module.HostName.Trim().ToUpper();
                    module.UpstreamName = upstreamName;
                    module.NormalizedUpstreamName = module.UpstreamName.Trim().ToUpper();
                    module.ModuleName = moduleName;
                    module.NormalizedModuleName = module.ModuleName.Trim().ToUpper();
                    module.RawSwaggerDocument = (string)modifySwaggerDocumentResult.Data;
                    module.DateUpdated = DateTime.Now;
                    #endregion

                    await _dbContext.ApiModules.ReplaceOneAsync(session, i => i.Id == module.Id, module);

                    await session.CommitTransactionAsync();
                    result.Succeed = true;
                }
                else
                {
                    await session.AbortTransactionAsync();
                    result.ErrorMessage = "Error on getting swagger document";
                    return result;
                }
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.Message;
                await session.AbortTransactionAsync();
            }
            finally
            {
                session.Dispose();
            }

            return result;
        }

        private string GetMethodName(PathItem value)
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
    }
}
