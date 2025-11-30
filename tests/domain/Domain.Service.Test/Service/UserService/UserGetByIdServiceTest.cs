using Domain.Model;
using Domain.Model.Model;
using Domain.Service.Contract;
using Domain.Service.Contract.Dto.UserDto;
using Domain.Service.Contract.Dto.UserDto.UserGetByIdDto;
using Domain.Service.Resource;
using Domain.Service.Service.UserService;
using Microsoft.Extensions.Localization;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;

namespace Domain.Service.Test.Service.UserService;

public class UserGetByIdServiceTest
{
    private readonly Mock<AppDbContext> _dbContextMock;
    private readonly Mock<IStringLocalizer<SharedResource>> _stringLocalizerMock;

    public UserGetByIdServiceTest()
    {
        _dbContextMock = new Mock<AppDbContext>();
        _stringLocalizerMock = new Mock<IStringLocalizer<SharedResource>>();
    }

    [Fact]
    public async Task Should_ReturnUser_WhenFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _dbContextMock.Setup(x => x.Users).ReturnsDbSet([new User { Id = id }]);
        var req = new UserGetByIdServiceRequest(id);

        // Act
        var service = new UserGetByIdService(
            _dbContextMock.Object,
            _stringLocalizerMock.Object);
        var result = await service.ExecuteAsync(
            req,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.IsType<UserServiceResponse>(result);
            Assert.Equal(id, result.Id);
        });
    }

    [Fact]
    public async Task Should_ThrowException_WhenNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _dbContextMock
            .Setup(x => x.Users)
            .ReturnsDbSet([]);
        var req = new UserGetByIdServiceRequest(id);

        // Act
        var service = new UserGetByIdService(
            _dbContextMock.Object,
            _stringLocalizerMock.Object);

        // Assert
        await Assert.ThrowsAsync<DomainException>(() => service.ExecuteAsync(
            req,
            TestContext.Current.CancellationToken)
        );
    }
}