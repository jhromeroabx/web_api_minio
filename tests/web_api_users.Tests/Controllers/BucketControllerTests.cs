using Microsoft.AspNetCore.Mvc;
using Moq;
using web_api_users.Application.Dtos;
using web_api_users.Controllers;
using web_api_users.Controllers.ParamsDTO;
using web_api_users.Domain.Interfaces;
using Xunit;

namespace web_api_users.Tests.Controllers;

public class BucketControllerTests
{
    private readonly Mock<IBucketService> _bucketServiceMock = new();

    [Fact]
    public async Task CreateBucketMINio_ReturnsOk_WhenServiceSucceeds()
    {
        _bucketServiceMock
            .Setup(service => service.CreateBucket("docs"))
            .ReturnsAsync((true, "Se creo el bucket: docs"));

        var controller = new BucketController(_bucketServiceMock.Object);

        var result = await controller.CreateBucketMINio("docs");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Se creo el bucket: docs", okResult.Value);
    }

    [Fact]
    public async Task CreateBucketMINio_ReturnsConflict_WhenServiceFails()
    {
        _bucketServiceMock
            .Setup(service => service.CreateBucket("docs"))
            .ReturnsAsync((false, "No se creo el bucket"));

        var controller = new BucketController(_bucketServiceMock.Object);

        var result = await controller.CreateBucketMINio("docs");

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("No se creo el bucket", conflictResult.Value);
    }

    [Fact]
    public async Task ListBucketsMINio_ReturnsBucketsFromService()
    {
        var buckets = new List<BucketInfo>
        {
            new() { Name = "docs" },
            new() { Name = "images" }
        };

        _bucketServiceMock
            .Setup(service => service.ListBuckets())
            .ReturnsAsync(buckets);

        var controller = new BucketController(_bucketServiceMock.Object);

        var result = await controller.ListBucketsMINio();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(buckets, okResult.Value);
    }

    [Fact]
    public async Task ListObjectsMINio_ReturnsOk_WhenServiceSucceeds()
    {
        var objects = new List<ObjectInfo>
        {
            new() { Key = "a.txt" }
        };

        _bucketServiceMock
            .Setup(service => service.ListObjects("docs"))
            .ReturnsAsync((true, objects, string.Empty));

        var controller = new BucketController(_bucketServiceMock.Object);

        var result = await controller.ListObjectsMINio("docs");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(objects, okResult.Value);
    }

    [Fact]
    public async Task ListObjectsMINio_ReturnsConflict_WhenServiceFails()
    {
        _bucketServiceMock
            .Setup(service => service.ListObjects("docs"))
            .ReturnsAsync((false, null!, "El bucket no existe"));

        var controller = new BucketController(_bucketServiceMock.Object);

        var result = await controller.ListObjectsMINio("docs");

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("El bucket no existe", conflictResult.Value);
    }

    [Fact]
    public async Task DeleteBucketMINio_ReturnsOk_WhenServiceSucceeds()
    {
        _bucketServiceMock
            .Setup(service => service.DeleteBucket("docs"))
            .ReturnsAsync((true, "Bucket eliminado"));

        var controller = new BucketController(_bucketServiceMock.Object);

        var result = await controller.DeleteBucketMINio("docs");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Bucket eliminado", okResult.Value);
    }

    [Fact]
    public async Task DeleteBucketMINio_ReturnsConflict_WhenServiceFails()
    {
        _bucketServiceMock
            .Setup(service => service.DeleteBucket("docs"))
            .ReturnsAsync((false, "Error al eliminar"));

        var controller = new BucketController(_bucketServiceMock.Object);

        var result = await controller.DeleteBucketMINio("docs");

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Error al eliminar", conflictResult.Value);
    }
}