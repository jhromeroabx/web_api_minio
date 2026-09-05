using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using web_api_users.Controllers;
using web_api_users.Domain.Interfaces;
using Xunit;

namespace web_api_users.Tests.Controllers;

public class ObjectControllerTests
{
    private readonly Mock<IObjectService> _objectServiceMock = new();
    private readonly Mock<ILogger<ObjectController>> _loggerMock = new();

    [Fact]
    public async Task UploadObject_ReturnsBadRequest_WhenFileIsMissing()
    {
        var controller = new ObjectController(_objectServiceMock.Object, _loggerMock.Object);

        var result = await controller.UploadObject("bucket", "file", "image/jpeg", null!);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadObject_ReturnsOk_WhenServiceSucceeds()
    {
        var file = CreateFormFile("payload", "image/jpeg");
        _objectServiceMock
            .Setup(service => service.UploadObjectAsync("bucket", "photo.jpg", "image/jpeg", file))
            .ReturnsAsync((true, "Imagen subida exitosamente"));

        var controller = new ObjectController(_objectServiceMock.Object, _loggerMock.Object);

        var result = await controller.UploadObject("bucket", "photo.jpg", "image/jpeg", file);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value!;
        Assert.True((bool)GetAnonymousProperty(value, "success")!);
        Assert.Equal("Imagen subida exitosamente", GetAnonymousProperty(value, "message"));
        Assert.Equal("photo.jpg", GetAnonymousProperty(value, "objectName"));
        Assert.Equal("bucket", GetAnonymousProperty(value, "bucket"));
    }

    [Fact]
    public async Task UploadObject_ReturnsInternalServerError_WhenServiceFails()
    {
        var file = CreateFormFile("payload", "application/pdf");
        _objectServiceMock
            .Setup(service => service.UploadObjectAsync("bucket", "doc.pdf", "application/pdf", file))
            .ReturnsAsync((false, "Error al subir objeto"));

        var controller = new ObjectController(_objectServiceMock.Object, _loggerMock.Object);

        var result = await controller.UploadObject("bucket", "doc.pdf", "application/pdf", file);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetObjectMINio_ReturnsFile_WhenServiceSucceeds()
    {
        var data = new byte[] { 1, 2, 3 };
        _objectServiceMock
            .Setup(service => service.GetObjectAsync("bucket", "file.txt"))
            .ReturnsAsync((true, data, "text/plain", "ok"));

        var controller = new ObjectController(_objectServiceMock.Object, _loggerMock.Object);

        var result = await controller.GetObjectMINio("bucket", "file.txt");

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(data, fileResult.FileContents);
        Assert.Equal("text/plain", fileResult.ContentType);
        Assert.Equal("file.txt", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task GetObjectMINio_ReturnsNotFound_WhenServiceFails()
    {
        _objectServiceMock
            .Setup(service => service.GetObjectAsync("bucket", "file.txt"))
            .ReturnsAsync((false, null!, null!, "No existe"));

        var controller = new ObjectController(_objectServiceMock.Object, _loggerMock.Object);

        var result = await controller.GetObjectMINio("bucket", "file.txt");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteObjectMINio_ReturnsOk_WhenServiceSucceeds()
    {
        _objectServiceMock
            .Setup(service => service.DeleteObjectAsync("bucket", "file.txt"))
            .ReturnsAsync((true, "Objeto eliminado"));

        var controller = new ObjectController(_objectServiceMock.Object, _loggerMock.Object);

        var result = await controller.DeleteObjectMINio("bucket", "file.txt");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.True((bool)GetAnonymousProperty(okResult.Value!, "success")!);
        Assert.Equal("Objeto eliminado", GetAnonymousProperty(okResult.Value!, "message"));
    }

    [Fact]
    public async Task DeleteObjectMINio_ReturnsInternalServerError_WhenServiceFails()
    {
        _objectServiceMock
            .Setup(service => service.DeleteObjectAsync("bucket", "file.txt"))
            .ReturnsAsync((false, "Error al eliminar"));

        var controller = new ObjectController(_objectServiceMock.Object, _loggerMock.Object);

        var result = await controller.DeleteObjectMINio("bucket", "file.txt");

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
    }

    private static IFormFile CreateFormFile(string content, string contentType)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);

        return new FormFile(stream, 0, bytes.Length, "file", "test.bin")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static object? GetAnonymousProperty(object source, string propertyName)
    {
        return source.GetType().GetProperty(propertyName)?.GetValue(source);
    }
}