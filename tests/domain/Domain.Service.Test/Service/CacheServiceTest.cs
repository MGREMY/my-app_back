using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Domain.Service.Service;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Xunit;

namespace Domain.Service.Test.Service;

public class CacheServiceTest
{
    private readonly Mock<IDistributedCache> _cacheMock;

    public CacheServiceTest()
    {
        _cacheMock = new Mock<IDistributedCache>();
    }

    [Fact]
    public async Task Should_ReturnObject_ForObject()
    {
        // Arrange
        _cacheMock
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(0, JsonSerializerOptions.Default)));
        var req = string.Empty;

        // Act
        var service = new CacheService(_cacheMock.Object);
        var result = await service.GetAsync<int>(
            req,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, result);
    }
}