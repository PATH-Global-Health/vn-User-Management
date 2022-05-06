using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UserManagement_App.Extensions
{
    public static class ExternalEnv
    {
        public static string? APP_CONNECTION_STRING = Environment.GetEnvironmentVariable("APP_CONNECTION_STRING");
        public static string? APP_DB_NAME = Environment.GetEnvironmentVariable("APP_DB_NAME");
    }
}
