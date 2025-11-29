using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Domain.Service.Service;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Xunit;

namespace Domain.Service.Test.Service;

[ExcludeFromCodeCoverage]
internal class TestObject : IEquatable<TestObject>
{
    public int Id { get; set; }

    public bool Equals(TestObject? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((TestObject)obj);
    }

    public override int GetHashCode()
    {
        return Id;
    }
}

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
        TestObject testObject = new TestObject { Id = 0 };
        const string key = nameof(key);
        _cacheMock.Setup(x => x.GetAsync(key, TestContext.Current.CancellationToken))
            .ReturnsAsync(() => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                testObject,
                JsonSerializerOptions.Default)));

        // Act
        var service = new CacheService(_cacheMock.Object);
        var result = await service.GetAsync<TestObject>(key, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(testObject, result);
    }
}