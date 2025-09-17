using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.Authority = Configuration["Wso2is:Authority"];
                options.RequireHttpsMetadata = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = Configuration["Wso2is:Authority"], 

                    ValidateAudience = false,

                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
                    {
                        var client = new HttpClient();
                        var keySet = client.GetStringAsync(Configuration["Wso2is:Jwks"]).Result;
                        return new JsonWebKeySet(keySet).GetSigningKeys();
                    }
                };
            });

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "web_api_minio", Version = "v1" });
                c.EnableAnnotations();

                c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Description = "OAuth2 Password Grant",
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Flows = new OpenApiOAuthFlows
                    {
                        Password = new OpenApiOAuthFlow
                        {
                            TokenUrl = new Uri(Configuration["Wso2is:Authority"]),
                            Scopes = new Dictionary<string, string>
                            {
                                {"minio-bucket-create.write", "scope for bucket creation in minio" },
                                {"minio-bucket-list.read", "scope to list minio buckets" },
                                {"minio-bucket-list-objects.read", "Scope to list minio objects" },
                                {"minio-bucket-delete.write", "Scope for delete minio bucket" },
                                {"minio-object-upload.write", "Scope for upload objects in minio" },
                                {"minio-object-download.read", "Scope for download objects in minio" },
                                {"minio-object-delete.write", "Scope for delete objects in minio" }
                                
                            }
                        }
                    }
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "oauth2"
                            }
                        },
                        new[] { 
                            "minio-bucket-create.write",
                            "minio-bucket-list.read",
                            "minio-bucket-list-objects.read",
                            "minio-bucket-delete.write",
                            "minio-object-upload.write",
                            "minio-object-download.read",
                            "minio-object-delete.write"
                        }
                    }
                });
            });

            services.AddAuthorization(options =>
            {
                
                options.AddPolicy("MinioBucketList", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(c =>
                            c.Type == "scope" &&
                            c.Value.Split(' ').Contains("minio-bucket-list.read"))));

                options.AddPolicy("MinioBucketCreate", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(c =>
                            c.Type == "scope" &&
                            c.Value.Split(' ').Contains("minio-bucket-create.write"))));

                options.AddPolicy("MinioBucketListObject", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(c =>
                            c.Type == "scope" &&
                            c.Value.Split(' ').Contains("minio-bucket-list-objects.read"))));

                options.AddPolicy("MinioBucketDelete", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(c =>
                            c.Type == "scope" &&
                            c.Value.Split(' ').Contains("minio-bucket-delete.write"))));

                options.AddPolicy("MinioObjectUpload", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(c =>
                            c.Type == "scope" &&
                            c.Value.Split(' ').Contains("minio-object-upload.write"))));

                options.AddPolicy("MinioObjectDownload", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(c =>
                            c.Type == "scope" &&
                            c.Value.Split(' ').Contains("minio-object-download.read"))));

                options.AddPolicy("MinioObjectDelete", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(c =>
                            c.Type == "scope" &&
                            c.Value.Split(' ').Contains("minio-object-delete.write"))));

                
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
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "web_api_minio v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseAuthentication();
            app.UseCors("AllowAll"); // Usar la política que definiste
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}