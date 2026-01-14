using Domain.Model;
using Domain.Service.Contract.Dto.AuthDto.AuthSyncUserDto;
using Domain.Service.Resource;
using Domain.Service.Service;
using Domain.Service.Service.AuthService;
using Microsoft.Extensions.Localization;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;

namespace Domain.Service.Test.Service.AuthService;

public class AuthSyncUserTest
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IStringLocalizer<SharedResource>> _stringLocalizerMock;
    private readonly Mock<AppDbContext> _dbContextMock;

    public AuthSyncUserTest()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _stringLocalizerMock = new Mock<IStringLocalizer<SharedResource>>();
        _dbContextMock = new Mock<AppDbContext>();
    }

    [Fact]
    public async Task Should_ReturnTask_WhenSynced()
    {
        // Arrange
        _cacheServiceMock
            .Setup(x => x.GetAsync<string[]>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([string.Empty]);
        _dbContextMock
            .Setup(x => x.Users)
            .ReturnsDbSet([]);
        var req = new AuthSyncUserServiceRequest(string.Empty, string.Empty, string.Empty);

        // Act
        var service = new AuthSyncUserService(
            _dbContextMock.Object,
            _stringLocalizerMock.Object,
            _cacheServiceMock.Object);
        var result = service.ExecuteAsync(
            req,
            TestContext.Current.CancellationToken);
        await result;

        // Assert
        Assert.True(result.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Should_ReturnTask_WhenNotSynced()
    {
        // Arrange
        _cacheServiceMock
            .Setup(x => x.GetAsync<string[]>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _dbContextMock
            .Setup(x => x.Users)
            .ReturnsDbSet([]);
        var req = new AuthSyncUserServiceRequest(string.Empty, string.Empty, string.Empty);

        // Act
        var service = new AuthSyncUserService(
            _dbContextMock.Object,
            _stringLocalizerMock.Object,
            _cacheServiceMock.Object);
        var result = service.ExecuteAsync(
            req,
            TestContext.Current.CancellationToken);
        await result;

        // Assert
        Assert.True(result.IsCompletedSuccessfully);
    }
}