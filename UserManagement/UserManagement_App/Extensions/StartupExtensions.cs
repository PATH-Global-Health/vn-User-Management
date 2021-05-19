using Data.DataAccess;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using NSwag;
using NSwag.Generation.Processors.Security;
using Service.Implementations;
using Service.Interfaces;
using Service.RabbitMQ;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace UserManagement_App.Extensions
{
    public static class StartupExtensions
    {
        public static void AddBusinessServices(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IGroupService, GroupService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IPermissionsService, PermissionsService>();
            services.AddSingleton<IHostedService, Consumer>();
            services.AddScoped<IUserProfileService, UserProfileService>();
            services.AddScoped<IProvincialService, ProvincialService>();
            services.AddScoped<ISecurityQuestionService, SecurityQuestionService>();
        }

        public static void ConfigSwagger(this IServiceCollection services)
        {
            services.AddOpenApiDocument(document =>
            {
                document.Title = "User Management";
                document.Version = "3.1";
                document.AddSecurity("JWT", Enumerable.Empty<string>(), new OpenApiSecurityScheme
                {
                    Type = OpenApiSecuritySchemeType.ApiKey,
                    Name = "Authorization",
                    In = OpenApiSecurityApiKeyLocation.Header,
                    Description = "Type into the textbox: Bearer {your JWT token}."
                });

                document.OperationProcessors.Add(
                    new AspNetCoreOperationSecurityScopeProcessor("JWT"));
                document.AllowReferencesWithProperties = true;
            });
        }

        public static void ConfigCors(this IServiceCollection services)
        {
            services.AddCors(options => options.AddPolicy("AllowAll", builder => builder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));
        }

        public static void ConfigJwt(this IServiceCollection services, string key, string issuer, string audience)
        {
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(jwtconfig =>
                {
                    jwtconfig.SaveToken = true;
                    jwtconfig.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = false,
                        RequireSignedTokens = true,
                        ValidIssuer = issuer,
                        ValidAudience = string.IsNullOrEmpty(audience) ? issuer : audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    };

                    jwtconfig.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine("OnAuthenticationFailed: " +
                                context.Exception.Message);
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            Console.WriteLine("OnTokenValidated: " +
                                context.SecurityToken);
                            return Task.CompletedTask;
                        },
                    };
                });
        }

        public static void AddMongoDbContext(this IServiceCollection services, string connectionString, string dbName)
        {
            services.AddSingleton<IMongoClient>(s => new MongoClient(connectionString));
            services.AddScoped(s => new ApplicationDbContext(s.GetRequiredService<IMongoClient>(), dbName));
        }
    }
}
