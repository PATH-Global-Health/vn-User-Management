using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.ViewModels
{
    public class SessionModel
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public Guid UserId { get; set; }
        public string ServiceName { get; set; }
        public string AppName { get; set; }
        [BsonDateTimeOptions]
        public DateTime DateCreated { get; set; } = DateTime.Now;
        [BsonDateTimeOptions]
        public DateTime? DateEnded { get; set; }
    }
    public class CreateSessionRequest
    {
        public string ServiceName { get; set; }
        public string AppName { get; set; }
    }
    public class GetStatisticRequest
    {
        public DateTime FromDate { get; set; } = DateTime.UtcNow.Date;
        public DateTime ToDate { get; set; } = DateTime.UtcNow.Date.AddDays(1);
    }
    public class SessionStatisticModel
    {
        public string AppName { get; set; }
        public int UsersNumber { get; set; }
        public List<ServiceNameSessionStatistic> ServiceStatistics { get; set; }
        public class ServiceNameSessionStatistic
        {
            public string ServiceName { get; set; }
            public int UsersNumber { get; set; }
        }
    }
}
