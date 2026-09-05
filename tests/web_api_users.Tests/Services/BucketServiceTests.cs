using System.Net.Http;
using Xunit;

namespace web_api_users.Tests.Services;

public class BucketServiceTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreateBucket_CreatesOnlyWhenMissing(bool exists)
    {
        using var fixture = new MinioFixture { BucketExists = exists };
        var result = await fixture.Buckets.CreateBucket("docs");
        Assert.Equal(!exists, result.Success);
        Assert.Equal(exists ? "No se creo el bucket, YA EXISTE: docs" : "Se creo el bucket: docs", result.Message);
        Assert.Equal(exists ? 0 : 1, fixture.Requests.Count(r => r.Method == HttpMethod.Put && r.Path == "/docs"));
    }

    [Fact]
    public async Task ListBuckets_MapsNameAndCreationDate()
    {
        using var fixture = new MinioFixture();
        var bucket = Assert.Single(await fixture.Buckets.ListBuckets());
        Assert.Equal("docs", bucket.Name);
        Assert.Equal(new DateTime(2025, 1, 2), bucket.CreationDate.ToUniversalTime().Date);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteBucket_DeletesOnlyWhenPresent(bool exists)
    {
        using var fixture = new MinioFixture { BucketExists = exists };
        var result = await fixture.Buckets.DeleteBucket("docs");
        Assert.Equal(exists, result.Success);
        Assert.Equal(exists ? "Se borró el bucket 'docs'" : "El bucket no existe", result.Message);
        Assert.Equal(exists ? 1 : 0, fixture.Requests.Count(r => r.Method == HttpMethod.Delete));
    }

    [Fact]
    public async Task ListObjects_MapsObjectMetadata()
    {
        using var fixture = new MinioFixture();
        var result = await fixture.Buckets.ListObjects("docs");
        Assert.True(result.Success, result.Message);
        var item = Assert.Single(result.Objects);
        Assert.Equal("a.txt", item.Key);
        Assert.Equal(new DateTime(2025, 1, 2), item.LastModifiedDateTime!.Value.ToUniversalTime().Date);
    }

    [Fact]
    public async Task ListObjects_ReportsEmptyBucketErrorFromSdk()
    {
        using var fixture = new MinioFixture { ListingXml = "<ListBucketResult><Name>docs</Name><Prefix></Prefix><KeyCount>0</KeyCount><MaxKeys>1000</MaxKeys><IsTruncated>false</IsTruncated></ListBucketResult>" };
        var result = await fixture.Buckets.ListObjects("docs");
        Assert.False(result.Success);
        Assert.Null(result.Objects);
        Assert.Contains("Bucket docs is empty", result.Message);
    }

    [Fact]
    public async Task ListObjects_ReturnsFailureForMissingBucket()
    {
        using var fixture = new MinioFixture { BucketExists = false };
        var result = await fixture.Buckets.ListObjects("docs");
        Assert.False(result.Success);
        Assert.Null(result.Objects);
        Assert.Equal("El bucket no existe", result.Message);
        Assert.DoesNotContain(fixture.Requests, r => r.Method == HttpMethod.Get);
    }

    [Fact]
    public async Task StorageErrors_AreReportedByAllOperations()
    {
        using var fixture = new MinioFixture { FailRequest = _ => true };
        var create = await fixture.Buckets.CreateBucket("docs");
        Assert.False(create.Success);
        Assert.StartsWith("No se creo el bucket:", create.Message);
        Assert.Null(await fixture.Buckets.ListBuckets());
        var delete = await fixture.Buckets.DeleteBucket("docs");
        Assert.False(delete.Success);
        Assert.StartsWith("Error al eliminar bucket:", delete.Message);
        var list = await fixture.Buckets.ListObjects("docs");
        Assert.False(list.Success);
        Assert.Null(list.Objects);
        Assert.StartsWith("Error al listar objetcts:", list.Message);
    }

    [Fact]
    public async Task ListObjects_ReportsObservableErrors()
    {
        using var fixture = new MinioFixture { FailRequest = r => r.Method == HttpMethod.Get };
        var result = await fixture.Buckets.ListObjects("docs");
        Assert.False(result.Success);
        Assert.Null(result.Objects);
        Assert.StartsWith("Error al listar objetcts:", result.Message);
    }
}
