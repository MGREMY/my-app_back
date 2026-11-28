using Yarp.ReverseProxy.Configuration;

namespace MyApp.Host.Gateway.ReverseProxy;

/// <summary>
/// Replace the placeholder address of every destination by the <c>Gateway:Clusters:{clusterId}</c>.
/// </summary>
public sealed class ClusterAddressConfigFilter : IProxyConfigFilter
{
    private readonly IReadOnlyDictionary<string, string> _clusters;

    public ClusterAddressConfigFilter(IReadOnlyDictionary<string, string> clusters)
    {
        _clusters = clusters;
    }

    public ValueTask<ClusterConfig> ConfigureClusterAsync(ClusterConfig cluster, CancellationToken cancel)
    {
        if (!_clusters.TryGetValue(cluster.ClusterId, out var address))
            throw new InvalidOperationException(
                $"Missing configuration: {GatewayOption.SectionKey}:Clusters:{cluster.ClusterId}");

        var destination = (cluster.Destinations ?? new Dictionary<string, DestinationConfig>())
            .ToDictionary(
                keySelector: x => x.Key,
                elementSelector: x => x.Value with { Address = address },
                comparer: StringComparer.OrdinalIgnoreCase);

        return ValueTask.FromResult(cluster with { Destinations = destination });
    }

    public ValueTask<RouteConfig> ConfigureRouteAsync(
        RouteConfig route,
        ClusterConfig? cluster,
        CancellationToken cancel)
        => ValueTask.FromResult(route);
}