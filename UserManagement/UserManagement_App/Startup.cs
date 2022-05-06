using AutoMapper;
using Data.Constants;
using Data.DataAccess;
using Data.ViewModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Service.MappingProfiles;
using UserManagement_App.Extensions;

namespace UserManagement_App
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.ConfigSwagger();
            services.Configure<EmailSettings>(this.Configuration.GetSection("EmailSettings"));
            services.AddBusinessServices(Configuration);
            services.ConfigCors();
            services.ConfigJwt(Configuration["Jwt:Key"], Configuration["Jwt:Issuer"], null);
            services.AddMongoDbContext(Configuration["MongoDbSettings:ConnectionString"], Configuration["MongoDbSettings:DatabaseName"]);
            services.AddAutoMapper(typeof(UserInformationMappingProfile));
            services.AddLogging();
            services.AddHttpClient();
            services.AddLazyCache();
            services.ConfigRedis(Configuration);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ApplicationDbContext mongoDbContext,
        IDistributedCache _distributedCache
            )
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            mongoDbContext.CreateCollectionsIfNotExists();
            mongoDbContext.SeedData();

            app.UseCors("AllowAll");
            //app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseOpenApi();
            app.UseSwaggerUi3();

            //remove cache
            _distributedCache.Remove(CacheConstants.RESOURCE_PERMISSION);
            _distributedCache.Remove(CacheConstants.USER);
            _distributedCache.Remove(CacheConstants.ROLE);
            _distributedCache.Remove(CacheConstants.GROUP);
        }
    }
}
