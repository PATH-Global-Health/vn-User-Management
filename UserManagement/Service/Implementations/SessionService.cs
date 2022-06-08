using AutoMapper;
using Data.Constants;
using Data.DataAccess;
using Data.MongoCollections;
using Data.ViewModels;
using LazyCache;
using Microsoft.Extensions.Caching.Distributed;
using MongoDB.Driver;
using Newtonsoft.Json;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Implementations
{
    public class SessionService : ISessionService
    {
        private readonly IMapper _mapper;
        private readonly ApplicationDbContext _dbContext;

        public SessionService(IMapper mapper, ApplicationDbContext dbContext)
        {
            _mapper = mapper;
            _dbContext = dbContext;
        }

        public async Task<List<SessionStatisticModel>> Statistic(GetStatisticRequest request)
        {
            var filters = Builders<Session>.Filter.Gte(x => x.DateCreated, request.FromDate);
            filters &= Builders<Session>.Filter.Lte(x => x.DateCreated, request.ToDate);
            var data = await _dbContext.Sessions.Find(filters).ToListAsync();

            var results = data
                .GroupBy(x => x.AppName)
                .Select(app => new SessionStatisticModel
                {
                    AppName = app.Key,
                    UsersNumber = app.Select(y => y.UserId).Distinct().Count(),
                    ServiceStatistics = app
                        .GroupBy(sg => sg.ServiceName)
                        .Select(service => new SessionStatisticModel.ServiceNameSessionStatistic
                        {
                            ServiceName = service.Key,
                            UsersNumber = service.Select(y => y.UserId).Distinct().Count(),
                        }).ToList(),
                })
                .ToList();
            return results;
        }

        public async Task<string> Create(CreateSessionRequest request, string userId)
        {
            var now = DateTime.UtcNow;
            var existedFilters = Builders<Session>.Filter.Eq(x => x.UserId, userId);
            existedFilters &= Builders<Session>.Filter.Eq(x => x.AppName, request.AppName);
            existedFilters &= Builders<Session>.Filter.Eq(x => x.ServiceName, request.ServiceName);
            existedFilters &= Builders<Session>.Filter.Lte(x => x.DateCreated, now);
            existedFilters &= Builders<Session>.Filter.Eq(x => x.DateEnded, null);
            var existedItem = await _dbContext.Sessions.Find(existedFilters).FirstOrDefaultAsync();
            if (existedItem == null)
            {
                var session = new Session
                {
                    AppName = request.AppName,
                    DateCreated = now,
                    ServiceName = request.ServiceName,
                    UserId = userId,
                };
                await _dbContext.Sessions.InsertOneAsync(session);
                return session.Id;
            }
            return existedItem.Id;
        }

        public async Task End(string id)
        {
            var now = DateTime.UtcNow;
            var item = await _dbContext.Sessions.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (item == null)
            {
                throw new Exception("Session does not exist");
            }
            item.DateEnded = now;
            await _dbContext.Sessions.ReplaceOneAsync(x => x.Id == item.Id, item);
        }
    }
}
