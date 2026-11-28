namespace MyApp.Host.Gateway;

public sealed class GatewayOption
{
    public const string SectionKey = "Gateway";

    /// <summary>
    /// Address of each YARP cluster, by cluster id (<c>Gateway:Clusters:back</c>...).
    /// </summary>
    public Dictionary<string, string> Clusters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public GatewayHeaderOption ForwardedHeaders { get; set; } = new();
}

public sealed class GatewayHeaderOption
{
    /// <summary>
    /// Networks of the proxies in front of the Gateway (load balancer), CIDR Loopback is always trusted.
    /// </summary>
    public string[] KnownNetworks { get; set; } = [];
}