namespace AppHost.Extension;

public static class RedisContainer
{
    extension<TBuilder>(TBuilder builder)
        where TBuilder : IDistributedApplicationBuilder
    {
        public IResourceBuilder<RedisResource> AddRedisContainer()
        {
            var redis = builder.AddRedis(
                "myAppRedis",
                password: builder.AddParameterFromConfiguration(
                    "Z-PARAM-REDIS-PASSWORD",
                    "api_config:redis:password",
                    true),
                port: int.TryParse(builder.Configuration["api_config:redis:port"], out var port)
                    ? port
                    : throw new InvalidOperationException(nameof(port))
            );

            // Remove TLS certificate as Aspire is used only for development
#pragma warning disable ASPIRECERTIFICATES001
            redis.WithoutHttpsCertificate();
#pragma warning restore ASPIRECERTIFICATES001

            return redis;
        }
    }
}