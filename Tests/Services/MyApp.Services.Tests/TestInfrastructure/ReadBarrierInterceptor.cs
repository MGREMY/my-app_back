using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MyApp.Services.Tests.TestInfrastructure;

/// <summary>
/// Holds every <c>SELECT</c> matching <paramref name="marker"/> until <paramref name="participants"/> of them have been
/// executed, so concurrent calls all read the same state before any of them writes: a race that would otherwise depend
/// on scheduling becomes deterministic. Later matching reads go through without waiting.
/// </summary>
/// <param name="marker">Text identifying the held reads in the SQL of the command (a column name, for example).</param>
/// <param name="participants">Number of reads to hold before letting all of them go.</param>
internal sealed class ReadBarrierInterceptor(string marker, int participants) : DbCommandInterceptor
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    /// <summary>Number of matching reads executed so far.</summary>
    public int Reads => Volatile.Read(ref _reads);

    private readonly TaskCompletionSource _allRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _reads;

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        if (command.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            && command.CommandText.Contains(marker, StringComparison.Ordinal))
        {
            if (Interlocked.Increment(ref _reads) >= participants)
                _allRead.TrySetResult();

            await _allRead.Task.WaitAsync(Timeout, cancellationToken);
        }

        return result;
    }
}
