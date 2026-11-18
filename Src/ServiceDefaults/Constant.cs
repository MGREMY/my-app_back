namespace ServiceDefaults;

public static class ServiceDefaultConstant
{
    public const string HealthEndpointPath = "/health";
    public const string AliveEndpointPath = "/alive";

    /// <summary>Tag of the health checks run by the alive endpoint</summary>
    public const string LiveTag = "live";
}