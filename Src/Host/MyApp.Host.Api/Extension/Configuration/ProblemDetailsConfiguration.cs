namespace MyApp.Host.Api.Extension.Configuration;

public static class ProblemDetailsConfiguration
{
    extension<TBuilder>(TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        public TBuilder AddAppProblemDetails()
        {
            builder.Services.AddProblemDetails(options =>
            {
                // traceId is added by the writers; instance identifies the occurrence (RFC 9457)
                options.CustomizeProblemDetails = ctx =>
                    ctx.ProblemDetails.Instance ??= ctx.HttpContext.Request.Path.Value;
            });
            // After the default writer: only used for the requests it declines (Accept without JSON)
            builder.Services.AddSingleton<IProblemDetailsWriter, JsonProblemDetailsWriter>();

            return builder;
        }
    }

    extension(WebApplication app)
    {
        /// <summary>
        ///     Gives a problem details body to every error response sent without one (401/403 from authentication,
        ///     unknown routes, bare status codes). Must run before authentication to cover its challenges.
        /// </summary>
        public WebApplication UseAppProblemDetails()
        {
            app.UseStatusCodePages();

            return app;
        }
    }
}