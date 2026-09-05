using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace web_api_users.Tests.Services;

public class ObjectStorageTests
{
    private static FormFile File(byte[] bytes) => new(new MemoryStream(bytes), 0, bytes.Length, "file", "file.bin");

    [Fact]
    public async Task Upload_RejectsEmptyFileWithoutStorageCalls()
    {
        using var fixture = new MinioFixture();
        var result = await fixture.Objects.UploadObjectAsync("docs", "file", "text/plain", File([]));
        Assert.False(result.Success);
        Assert.Equal("Archivo vacío.", result.Message);
        Assert.Empty(fixture.Requests);
    }

    [Theory]
    [InlineData("text/plain", "Archivo", true)]
    [InlineData("application/pdf", "PDF", false)]
    [InlineData("audio/mpeg", "Audio", true)]
    [InlineData("image/jpeg", "Imagen", true)]
    public async Task Upload_PreservesBytesAndContentType(string contentType, string kind, bool bucketExists)
    {
        using var fixture = new MinioFixture { BucketExists = bucketExists };
        byte[] bytes = [1, 2, 3, 4];
        var result = await fixture.Objects.UploadObjectAsync("docs", "file.bin", contentType, File(bytes));
        Assert.True(result.Success, result.Message);
        Assert.Equal($"¡{kind} subido exitosamente!", result.Message);
        var upload = Assert.Single(fixture.Requests, r => r.Method == HttpMethod.Put && r.Path == "/docs/file.bin");
        Assert.Equal(bytes, upload.Body);
        Assert.Equal(contentType, upload.ContentType);
        Assert.Equal(bucketExists ? 0 : 1, fixture.Requests.Count(r => r.Method == HttpMethod.Put && r.Path == "/docs"));
    }

    [Theory]
    [InlineData(1200, 600, 1200, 600)]
    [InlineData(1600, 400, 1200, 300)]
    [InlineData(400, 1200, 200, 600)]
    public async Task Upload_ResizesOnlyOversizedImages(int width, int height, int expectedWidth, int expectedHeight)
    {
        using var fixture = new MinioFixture();
        using var source = new Image<Rgb24>(width, height);
        using var stream = new MemoryStream();
        source.SaveAsJpeg(stream);
        var original = stream.ToArray();
        var result = await fixture.Objects.UploadObjectAsync("docs", "photo.jpg", "image/jpeg", File(original));
        Assert.True(result.Success, result.Message);
        var upload = Assert.Single(fixture.Requests, r => r.Method == HttpMethod.Put);
        using var image = Image.Load(upload.Body);
        Assert.Equal(expectedWidth, image.Width);
        Assert.Equal(expectedHeight, image.Height);
        if (width == expectedWidth && height == expectedHeight) Assert.Equal(original, upload.Body);
    }

    [Fact]
    public async Task Upload_ReturnsFailureWhenBucketCreationIsDenied()
    {
        using var fixture = new MinioFixture { BucketExists = false, FailRequest = r => r.Method == HttpMethod.Put };
        var result = await fixture.Objects.UploadObjectAsync("docs", "file.bin", "text/plain", File([1]));
        Assert.False(result.Success);
        Assert.StartsWith("Error al crear bucket 'docs':", result.Message);
        Assert.DoesNotContain(fixture.Requests, r => r.Path == "/docs/file.bin");
    }

    [Fact]
    public async Task Upload_ReturnsFailureWhenStorageRejectsObject()
    {
        using var fixture = new MinioFixture { FailRequest = r => r.Method == HttpMethod.Put };
        var result = await fixture.Objects.UploadObjectAsync("docs", "file.bin", "text/plain", File([1]));
        Assert.False(result.Success);
        Assert.StartsWith("Error al subir objeto:", result.Message);
    }

    [Fact]
    public async Task Download_ReturnsStoredBytesAndContentType()
    {
        using var fixture = new MinioFixture();
        var result = await fixture.Objects.GetObjectAsync("docs", "file.txt");
        Assert.True(result.Success, result.Message);
        Assert.Equal(fixture.Download, result.Data);
        Assert.Equal("text/plain", result.ContentType);
        Assert.Equal("Objeto recuperado correctamente.", result.Message);
    }

    [Fact]
    public async Task Download_ReportsMissingObject()
    {
        using var fixture = new MinioFixture { ObjectExists = false };
        var result = await fixture.Objects.GetObjectAsync("docs", "file.txt");
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Null(result.ContentType);
        Assert.Equal("El objeto 'file.txt' no existe en el bucket 'docs'.", result.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_ChecksBucketBeforeDeleting(bool exists)
    {
        using var fixture = new MinioFixture { BucketExists = exists };
        var result = await fixture.Objects.DeleteObjectAsync("docs", "file.txt");
        Assert.Equal(exists, result.Success);
        Assert.Equal(exists ? "Objeto 'file.txt' eliminado correctamente." : "El bucket 'docs' no existe.", result.Message);
        Assert.Equal(exists ? 1 : 0, fixture.Requests.Count(r => r.Method == HttpMethod.Delete && r.Path == "/docs/file.txt"));
    }

    [Fact]
    public async Task Download_DoesNotReadFromMissingBucket()
    {
        using var fixture = new MinioFixture { BucketExists = false };
        var result = await fixture.Objects.GetObjectAsync("docs", "file.txt");
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("El bucket 'docs' no existe.", result.Message);
        Assert.DoesNotContain(fixture.Requests, r => r.Method == HttpMethod.Get);
    }

    [Fact]
    public async Task DownloadAndDelete_ReportStorageErrors()
    {
        using var fixture = new MinioFixture { FailRequest = _ => true };
        var download = await fixture.Objects.GetObjectAsync("docs", "file.txt");
        Assert.False(download.Success);
        Assert.Null(download.Data);
        Assert.StartsWith("Error al obtener el objeto:", download.Message);
        var delete = await fixture.Objects.DeleteObjectAsync("docs", "file.txt");
        Assert.False(delete.Success);
        Assert.StartsWith("Error al eliminar el objeto 'file.txt':", delete.Message);
    }
}
