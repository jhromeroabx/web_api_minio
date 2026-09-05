using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Minio;
using Moq;
using web_api_users.Application.Interfaces;
using web_api_users.Infrastructure.Services;

namespace web_api_users.Tests.Services;

// Exercise the real MinIO SDK against an in-memory HTTP transport, without a server or credentials.
internal sealed class MinioFixture : HttpMessageHandler
{
    public bool BucketExists { get; set; } = true;
    public bool ObjectExists { get; set; } = true;
    public Func<HttpRequestMessage, bool>? FailRequest { get; set; }
    public string? ListingXml { get; set; }
    public byte[] Download { get; set; } = Encoding.UTF8.GetBytes("stored document");
    public List<(HttpMethod Method, string Path, byte[] Body, string? ContentType)> Requests { get; } = new();
    private readonly HttpClient http;
    public IFileManager FileManager { get; }
    public ObjectService Objects => new(FileManager, NullLogger<ObjectService>.Instance);
    public BucketService Buckets => new(FileManager);

    public MinioFixture()
    {
        http = new HttpClient(this, disposeHandler: false);
        var client = new MinioClient().WithEndpoint("localhost:9000")
            .WithCredentials("test-access", "test-secret").WithRegion("us-east-1")
            .WithHttpClient(http).Build();
        var manager = new Mock<IFileManager>();
        manager.Setup(x => x.GetMinio()).Returns(client);
        FileManager = manager.Object;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath.TrimEnd('/');
        if (path.Length == 0) path = "/";
        var body = request.Content == null ? Array.Empty<byte>() : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        Requests.Add((request.Method, path, body, request.Content?.Headers.ContentType?.MediaType));
        if (FailRequest?.Invoke(request) == true)
            return Xml(HttpStatusCode.Forbidden, "<Error><Code>AccessDenied</Code><Message>Access denied</Message></Error>");
        if (!BucketExists && request.Method == HttpMethod.Head)
            return Xml(HttpStatusCode.NotFound, "<Error><Code>NoSuchBucket</Code><Message>Missing bucket</Message></Error>");
        if (!ObjectExists && path.Count(c => c == '/') > 1)
            return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new ByteArrayContent([]) };
        if (request.Method == HttpMethod.Get && path == "/")
            return Xml(HttpStatusCode.OK, ListingXml ?? "<ListAllMyBucketsResult><Buckets><Bucket><Name>docs</Name><CreationDate>2025-01-02T00:00:00Z</CreationDate></Bucket></Buckets></ListAllMyBucketsResult>");
        if (request.Method == HttpMethod.Get && path.Trim('/').IndexOf('/') < 0)
            return Xml(HttpStatusCode.OK, ListingXml ?? "<ListBucketResult><Name>docs</Name><IsTruncated>false</IsTruncated><Contents><Key>a.txt</Key><LastModified>2025-01-02T00:00:00Z</LastModified><ETag>abc</ETag><Size>15</Size></Contents></ListBucketResult>");
        var response = new HttpResponseMessage(request.Method == HttpMethod.Delete ? HttpStatusCode.NoContent : HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(request.Method == HttpMethod.Get ? Download : Array.Empty<byte>())
        };
        response.Headers.TryAddWithoutValidation("ETag", "\"abc\"");
        response.Content.Headers.ContentType = new("text/plain");
        response.Content.Headers.LastModified = DateTimeOffset.Parse("2025-01-02T00:00:00Z");
        if (request.Method == HttpMethod.Head) response.Content.Headers.ContentLength = Download.Length;
        return response;
    }

    private static HttpResponseMessage Xml(HttpStatusCode status, string xml) => new(status)
    {
        Content = new StringContent(xml.Replace("<ListAllMyBucketsResult>", "<ListAllMyBucketsResult xmlns=\"http://s3.amazonaws.com/doc/2006-03-01/\">")
            .Replace("<ListBucketResult>", "<ListBucketResult xmlns=\"http://s3.amazonaws.com/doc/2006-03-01/\">"), Encoding.UTF8, "application/xml")
    };

    protected override void Dispose(bool disposing)
    {
        if (disposing) http.Dispose();
        base.Dispose(disposing);
    }
}
