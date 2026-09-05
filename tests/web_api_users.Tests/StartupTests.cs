using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using web_api_users.Application.Interfaces;
using web_api_users.Domain.Interfaces;
using web_api_users.Infrastructure.Services;
using Xunit;

namespace web_api_users.Tests;

public class StartupTests
{
    private static ServiceProvider Services(string? corsKey = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["Wso2is:Authority"] = "https://identity.example.test",
            ["Wso2is:OidcMetadata"] = "https://identity.example.test/.well-known/openid-configuration",
            ["minio:endpoint"] = "localhost:9000",
            ["minio:accessKey"] = "test-access",
            ["minio:secretKey"] = "test-secret"
        };
        if (corsKey != null) values[$"Cors:{corsKey}:0"] = "https://app.example.test";
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        new Startup(config).ConfigureServices(services);
        return services.BuildServiceProvider();
    }

    [Theory]
    [InlineData("MinioBucketList", "minio-bucket-list.read")]
    [InlineData("MinioBucketCreate", "minio-bucket-create.write")]
    [InlineData("MinioBucketListObject", "minio-bucket-list-objects.read")]
    [InlineData("MinioBucketDelete", "minio-bucket-delete.write")]
    [InlineData("MinioObjectUpload", "minio-object-upload.write")]
    [InlineData("MinioObjectDownload", "minio-object-download.read")]
    [InlineData("MinioObjectDelete", "minio-object-delete.write")]
    public async Task Policies_RequireExactScope(string policy, string scope)
    {
        using var services = Services();
        var authorization = services.GetRequiredService<IAuthorizationService>();
        foreach (var (claimType, value, allowed) in new[]
        {
            ("scope", scope, true), ("scope", $"other {scope} another", true),
            ("scope", scope + ".invalid", false), ("scope", "unrelated", false),
            ("role", scope, false), ("scope", "", false)
        })
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(claimType, value)], "test"));
            Assert.Equal(allowed, (await authorization.AuthorizeAsync(user, null, policy)).Succeeded);
        }
        Assert.False((await authorization.AuthorizeAsync(new ClaimsPrincipal(), null, policy)).Succeeded);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("AllowedOrigins")]
    [InlineData("AllowedHosts")]
    public void Cors_UsesConfiguredOriginsOrFallback(string? key)
    {
        using var services = Services(key);
        var policy = services.GetRequiredService<IOptions<CorsOptions>>().Value.GetPolicy("AllowAll")!;
        Assert.True(policy.AllowAnyHeader);
        Assert.True(policy.AllowAnyMethod);
        Assert.Equal(key == null, policy.AllowAnyOrigin);
        if (key != null) Assert.Equal("https://app.example.test", Assert.Single(policy.Origins));
    }

    [Fact]
    public void Authentication_UsesConfiguredAuthorityAndValidatesTokens()
    {
        using var services = Services();
        var options = services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
        Assert.Equal("https://identity.example.test", options.Authority);
        Assert.Equal("https://identity.example.test/.well-known/openid-configuration", options.MetadataAddress);
        Assert.True(options.RequireHttpsMetadata);
        Assert.True(options.TokenValidationParameters.ValidateIssuer);
        Assert.True(options.TokenValidationParameters.ValidateLifetime);
        Assert.True(options.TokenValidationParameters.ValidateIssuerSigningKey);
        Assert.False(options.TokenValidationParameters.ValidateAudience);
    }

    [Fact]
    public void Swagger_DescribesOAuthAndAllScopes()
    {
        using var services = Services();
        var options = services.GetRequiredService<IOptions<SwaggerGenOptions>>().Value.SwaggerGeneratorOptions;
        Assert.Equal("web_api_minio", options.SwaggerDocs["v1"].Title);
        var flow = options.SecuritySchemes["oauth2"].Flows.ClientCredentials;
        Assert.Equal("https://identity.example.test/token", flow.TokenUrl.ToString());
        Assert.Equal(7, flow.Scopes.Count);
        Assert.Equal(flow.Scopes.Keys.Order(), Assert.Single(Assert.Single(options.SecurityRequirements).Values).Order());
    }

    [Fact]
    public void DependencyInjection_ResolvesScopedStorageServices()
    {
        using var services = Services();
        using var scope = services.CreateScope();
        var manager = Assert.IsType<FileManager>(scope.ServiceProvider.GetRequiredService<IFileManager>());
        Assert.Same(manager.GetMinio(), manager.GetMinio());
        Assert.Same(manager, scope.ServiceProvider.GetRequiredService<IFileManager>());
        Assert.IsType<BucketService>(scope.ServiceProvider.GetRequiredService<IBucketService>());
        Assert.IsType<ObjectService>(scope.ServiceProvider.GetRequiredService<IObjectService>());
        using var otherScope = services.CreateScope();
        Assert.NotSame(manager, otherScope.ServiceProvider.GetRequiredService<IFileManager>());
    }
}
