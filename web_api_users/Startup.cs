using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using web_api_users.Application.Dtos;
using web_api_users.Application.Interfaces;
using web_api_users.Domain.Interfaces;
using web_api_users.Infrastructure.Services;

namespace web_api_users
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "web_api_minio", Version = "v1" });
                c.EnableAnnotations();
            });

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", // Cambiar el nombre de la política
                    builder =>
                    {
                        builder.AllowAnyOrigin()    // Permitir cualquier origen
                               .AllowAnyHeader()    // Permitir cualquier header
                               .AllowAnyMethod();   // Permitir cualquier método (GET, POST, etc.)
                    });
            });

            // Registro de configuración de MinIO
            services.Configure<CredentialsMINio>(Configuration.GetSection("minio"));

            // contenedor de dependencias..
            services.AddScoped<IFileManager, FileManager>();
            services.AddScoped<IBucketService, BucketService>();
            services.AddScoped<IObjectService, ObjectService>();
            services.AddLogging();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                //SI FALLA EL SWAGGER SACARLO DEL IF
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "web_api_users v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors("AllowAll"); // Usar la política que definiste

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}