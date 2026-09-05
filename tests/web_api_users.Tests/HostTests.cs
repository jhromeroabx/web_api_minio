using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace web_api_users.Tests;

public class HostTests
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Host_StartsAndProtectsEveryStorageEndpoint(string environment)
    {
        using var host = Program.CreateHostBuilder([])
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureWebHost(web => web.UseEnvironment(environment)
                .UseSetting(WebHostDefaults.ApplicationKey, typeof(Startup).Assembly.FullName)
                .UseServer(new InMemoryServer()))
            .ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Wso2is:Authority"] = "https://identity.example.test",
                ["minio:endpoint"] = "localhost:9000",
                ["minio:accessKey"] = "test-access",
                ["minio:secretKey"] = "test-secret"
            }))
            .Build();
        await host.StartAsync();
        try
        {
            var endpoints = host.Services.GetRequiredService<EndpointDataSource>().Endpoints;
            var expected = new Dictionary<string, string>
            {
                ["api/Bucket/CreateBucketMINio"] = "MinioBucketCreate",
                ["api/Bucket/ListBucketsMINio"] = "MinioBucketList",
                ["api/Bucket/ListObjectsMINio"] = "MinioBucketListObject",
                ["api/Bucket/DeleteBucketMINio"] = "MinioBucketDelete",
                ["api/Object/UploadObject"] = "MinioObjectUpload",
                ["api/Object/GetObjectMINio"] = "MinioObjectDownload",
                ["api/Object/DeleteObjectMINio"] = "MinioObjectDelete"
            };
            Assert.Equal(expected.Count, endpoints.Count);
            foreach (var endpoint in endpoints)
            {
                var route = Assert.IsType<RouteEndpoint>(endpoint);
                Assert.Equal(expected[route.RoutePattern.RawText!], route.Metadata.GetMetadata<IAuthorizeData>()!.Policy);
            }
        }
        finally
        {
            await host.StopAsync();
        }
    }

    // Build the actual middleware pipeline without opening a TCP port.
    private sealed class InMemoryServer : IServer
    {
        public IFeatureCollection Features { get; } = new FeatureCollection();
        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public void Dispose() { }
    }
}
