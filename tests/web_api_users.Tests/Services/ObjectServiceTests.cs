using Microsoft.Extensions.Logging;
using Moq;
using web_api_users.Application.Interfaces;
using web_api_users.Infrastructure.Services;
using Xunit;

namespace web_api_users.Tests.Services;

public class ObjectServiceTests
{
    [Fact]
    public async Task UploadObjectAsync_ReturnsFailure_WhenFileIsNull()
    {
        var fileManagerMock = new Mock<IFileManager>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<ObjectService>>();
        var service = new ObjectService(fileManagerMock.Object, loggerMock.Object);

        var result = await service.UploadObjectAsync("bucket", "file", "image/jpeg", null!);

        Assert.False(result.Success);
        Assert.Equal("Archivo vacío.", result.Message);
    }
}