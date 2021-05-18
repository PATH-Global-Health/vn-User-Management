using Data.DataAccess;
using Data.MongoCollections;
using Data.ViewModels;
using MongoDB.Driver;
using MoreLinq;
using Newtonsoft.Json;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Service.Implementations
{
    public class ProvincialService : IProvincialService
    {
        private readonly ApplicationDbContext _dbContext;

        public ProvincialService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void EnsureDataPopulated()
        {
            var hasData = _dbContext.ProvincialInformation.Find(i => true).ToList().Any();
            if (!hasData)
            {
                var districtsJsonString = ReadJsonFile("CommonResources/districts.json");
                var districtsJson = JsonConvert.DeserializeObject<List<ProvincialImportModel>>(districtsJsonString);
                var districts = districtsJson.Select(i => new ProvincialInformation
                {
                    Name = i.Name,
                    Value = i.Value
                });

                _dbContext.ProvincialInformation.InsertMany(districts);
            }
        }

        private string ReadJsonFile(string name)
        {
            var result = "";
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), name);
                result = File.ReadAllText(filePath).Trim();

            }
            catch (Exception e)
            {
                result = e.InnerException != null ? e.InnerException.Message : e.Message;
            }

            return result;
        }

        public List<ProvincialInformation> GetAll()
        {
            var provinces = _dbContext.ProvincialInformation.Find(i => true).ToList();
            return provinces;
        }

        public List<int> GetProvincials(string userId)
        {
            var results = new List<int>();

            var user = _dbContext.Users.Find(i => i.Id == userId).FirstOrDefault();
            if (user != null && user.ProvincialInformation.Any())
            {
                var provinces = user.ProvincialInformation.Select(id => _dbContext.ProvincialInformation.Find(i => i.Id == id).First());
                results = provinces.Select(i => i.Value).ToList();
            }

            return results;
        }

        public ResultModel AddProvincialInfo(string userId, List<string> provinceIds)
        {
            var result = new ResultModel();
            try
            {
                var user = _dbContext.Users.Find(i => i.Id == userId).FirstOrDefault();
                if (user == null)
                {
                    result.ErrorMessage = "Invalid User";
                    return result;
                }

                user.ProvincialInformation.AddRange(provinceIds);
                user.ProvincialInformation = user.ProvincialInformation.DistinctBy(i => i).ToList();

                _dbContext.Users.FindOneAndReplace(i => i.Id == user.Id, user);

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.Message + "\n" + e.InnerException != null ? e.InnerException.Message : "";
            }

            return result;
        }

        public ResultModel RemoveProvincialInfo(string userId, string provinceId)
        {
            var result = new ResultModel();
            try
            {
                var user = _dbContext.Users.Find(i => i.Id == userId).FirstOrDefault();
                if (user == null)
                {
                    result.ErrorMessage = "Invalid User";
                    return result;
                }

                user.ProvincialInformation.Remove(provinceId);

                _dbContext.Users.FindOneAndReplace(i => i.Id == user.Id, user);

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.Message + "\n" + e.InnerException != null ? e.InnerException.Message : "";
            }

            return result;
        }
    }
}
