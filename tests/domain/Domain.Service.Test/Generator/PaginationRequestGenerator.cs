using System.Collections;

namespace Domain.Service.Test.Generator;

// ReSharper disable once ClassNeverInstantiated.Global
public class PaginationRequestGenerator : IEnumerable<object[]>
{
    private readonly IEnumerable<object[]> _data = Enumerable
        .Range(1, 3)
        .SelectMany<int, object[]>(i =>
        [
            [i, 5],
            [i, 10],
            [i, 15],
            [i, 20],
            [i, 25],
            [i, 30],
        ]);

    public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}