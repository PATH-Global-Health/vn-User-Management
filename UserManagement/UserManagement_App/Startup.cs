using AutoMapper;
using Data.DataAccess;
using Data.ViewModels;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Service.RabbitMQ;
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
            services.AddAutoMapper();
            services.AddLogging();
            services.AddHttpClient();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ApplicationDbContext mongoDbContext)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                mongoDbContext.CreateCollectionsIfNotExists();
                mongoDbContext.SeedData();
            }
            app.UseCors("AllowAll");
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseOpenApi();
            app.UseSwaggerUi3();
        }
    }
}
