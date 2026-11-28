using MyApp.Services;

namespace MyApp.Host.Api;

/// <summary>
///     Configuration schema of the API, bound from the <see cref="SectionKey" /> section.
/// </summary>
public sealed class ApiOption
{
    public const string SectionKey = "Api";

    public ApiForwardedHeaderOption ForwardedHeaders { get; set; } = new();
    public ApiDatabaseOption Database { get; set; } = new();
    public ApiCacheOption Cache { get; set; } = new();
    public ApiAuthOption Auth { get; set; } = new();
    public ApiCorsOption Cors { get; set; } = new();
    public ServiceOption Services { get; set; } = new();
}

public sealed class ApiForwardedHeaderOption
{
    /// <summary>
    /// Networks of the trusted proxies (the Gateway), CIRD Loopback is always truster.
    /// </summary>
    public string[] KnownNetworks { get; set; } = [];
}

public sealed class ApiDatabaseOption
{
    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class ApiCacheOption
{
    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class ApiAuthOption
{
    public string Domain { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}

public sealed class ApiCorsOption
{
    public string[] Origins { get; set; } = [];
}