using Domain.Model;
using Domain.Service.Contract.Dto.AuthDto.AuthSyncUserDto;
using Domain.Service.Service;
using Domain.Service.Service.AuthService;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;

namespace Domain.Service.Test.Service.AuthService;

public class AuthSyncUserTest
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<AppDbContext> _dbContextMock;

    public AuthSyncUserTest()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _dbContextMock = new Mock<AppDbContext>();
    }

    [Fact]
    public async Task Should_ReturnTask_ForAnyUser()
    {
        // Arrange
        _cacheServiceMock
            .Setup(x => x.GetAsync<string[]>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _dbContextMock
            .Setup(x => x.Users)
            .ReturnsDbSet([]);

        // Act
        var service = new AuthSyncUserService(
            _dbContextMock.Object,
            _cacheServiceMock.Object);
        var result = service.ExecuteAsync(
            new AuthSyncUserServiceRequest(string.Empty),
            TestContext.Current.CancellationToken);
        await result;

        // Assert
        Assert.True(result.IsCompletedSuccessfully);
    }
}