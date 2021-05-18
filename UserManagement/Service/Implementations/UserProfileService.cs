using System;
using AutoMapper;
using Bogus;
using Data.DataAccess;
using Data.MongoCollections;
using Data.ViewModels;
using MongoDB.Driver;
using Service.Interfaces;

namespace Service.Implementations
{
    public class UserProfileService:IUserProfileService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;

        public UserProfileService(ApplicationDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public ResultModel Create(UserProfileCreateModel model)
        {
            var result = new ResultModel();
            try
            {
                var profile = _mapper.Map<UserProfileCreateModel, UserProfile>(model);
                var faker = new Faker();
                var code = faker.Random.Replace("######");
                while(_dbContext.UserProfiles.Find(i=>i.Code == code).Any() || code == "000000")
                {
                    code = faker.Random.Replace("######");
                }

                profile.Code = code;
                _dbContext.UserProfiles.InsertOne(profile);
                result.Succeed = true;
                result.Data = code;

            }
            catch(Exception e)
            {
                result.ErrorMessage = e.Message;
            }

            return result;
        }

        public ResultModel Delete(string key)
        {
            var result = new ResultModel();
            try
            {
                var isGuid = Guid.TryParse(key, out var guid);
                if (isGuid)
                {
                    key = Guid.Parse(key).ToString();
                    _dbContext.UserProfiles.FindOneAndDelete(i => i.Id == key);
                }
                else
                {
                    _dbContext.UserProfiles.FindOneAndDelete(i => i.Code == key);
                }

                result.Succeed = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.Message;
            }
            return result;
        }

        public PagingModel Search(string name, string phoneNumber, string email, DateTime? dateOfBirth, bool hasYearOfBirthOnly, int pageSize, int pageIndex)
        {
            var result = new PagingModel();
            var filters = Builders<UserProfile>.Filter.Empty;

            if (!string.IsNullOrEmpty(name))
            {
                filters &= Builders<UserProfile>.Filter.Where(i => i.FullName.Contains(name,StringComparison.CurrentCultureIgnoreCase));
            }

            if (!string.IsNullOrEmpty(phoneNumber))
            {
                filters &= Builders<UserProfile>.Filter.Eq(i => i.PhoneNumber, phoneNumber);
            }

            if (!string.IsNullOrEmpty(email))
            {
                filters &= Builders<UserProfile>.Filter.Eq(i => i.Email, email);
            }

            if (dateOfBirth.HasValue)
            {
                if (hasYearOfBirthOnly)
                {
                    filters &= Builders<UserProfile>.Filter.Eq(i=>i.DateOfBirth.Year,dateOfBirth.Value.Year);
                }
                else
                {
                    filters &= Builders<UserProfile>.Filter.Eq(i => i.DateOfBirth.Year, dateOfBirth.Value.Year)
                             & Builders<UserProfile>.Filter.Eq(i => i.DateOfBirth.Month, dateOfBirth.Value.Month)
                             & Builders<UserProfile>.Filter.Eq(i => i.DateOfBirth.Day, dateOfBirth.Value.Day);
                }
            }

            var profilesCursor = _dbContext.UserProfiles.Find(filters);
            result.TotalPages = (int)Math.Ceiling((double)profilesCursor.CountDocuments() / pageSize);
            result.Data = profilesCursor.Skip(pageSize * pageIndex).Limit(pageSize).ToList();

            return result;
        }
    }
}
