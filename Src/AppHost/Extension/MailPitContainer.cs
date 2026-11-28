namespace AppHost.Extension;

public static class MailPitContainer
{
    extension<TBuilder>(TBuilder builder)
        where TBuilder : IDistributedApplicationBuilder
    {
        public IResourceBuilder<MailPitContainerResource> AddMailPitContainer()
        {
            return builder.AddMailPit(
                "smtp",
                int.TryParse(builder.Configuration["api_config:mailPit:http_port"], out var httpPort)
                    ? httpPort
                    : throw new InvalidOperationException(nameof(httpPort)),
                int.TryParse(builder.Configuration["api_config:mailPit:smtp_port"], out var smtpPort)
                    ? smtpPort
                    : throw new InvalidOperationException(nameof(smtpPort))
            );
        }
    }
}