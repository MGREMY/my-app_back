using Domain.Model.Model;

namespace Domain.Service.Test.Data;

public static partial class TestData
{
#pragma warning disable format // @formatter:off
    public static IEnumerable<User> Users(int count = 50) =>
        Enumerable
            .Range(0, count)
            .Select(i => new User
            {
                Id = Guid.NewGuid(),
                AuthId = $"auth{i}",
                CreatedAtUtc = new DateTime(Random.Shared.NextInt64(DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks)),
                HasBeenModified = i % 3 == 0,
                UpdatedAtUtc = i % 3 == 0 ? new DateTime(Random.Shared.NextInt64(DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks)) : null,
                IsDeleted = false,
                DeletedAtUtc = null,
            });
}