using Domain.Model;
using Domain.Service.Contract.Dto.PaginationDto;
using Domain.Service.Contract.Dto.UserDto;
using Domain.Service.Service.UserService;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;

namespace Domain.Service.Test.Service.UserService;

public class UserGetServiceTest
{
    private readonly Mock<AppDbContext> _dbContextMock;

    public UserGetServiceTest()
    {
        _dbContextMock = new Mock<AppDbContext>();
    }

    [Theory]
    [ClassData<PaginationRequestGenerator>]
    public async Task Should_ReturnCorrectCount_ForPagination(int pageNumber, int pageSize)
    {
        // Arrange
        _dbContextMock.Setup(x => x.Users).ReturnsDbSet(TestData.Users());

        // Act
        var service = new UserGetService(_dbContextMock.Object);
        var result = await service.ExecuteAsync(
            new PaginationServiceRequest(pageNumber, pageSize),
            TestContext.Current.CancellationToken);

        // Assert
        var expected = _dbContextMock.Object.Users
            .Skip(pageSize * (pageNumber - 1))
            .Take(pageSize);
        Assert.Multiple(() =>
        {
            Assert.IsType<PaginationServiceResponse<MinimalUserServiceResponse>>(result);
            Assert.Equal(expected.Count(), result.Data.Count());
        });
    }
}