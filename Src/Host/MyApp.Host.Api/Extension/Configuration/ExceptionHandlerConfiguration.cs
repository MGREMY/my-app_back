using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using MyApp.Host.Api.Helpers;
using MyApp.Services.Abstractions.Requests;

namespace MyApp.Host.Api.Extension.Configuration;

// ReSharper disable once ClassNeverInstantiated.Global
internal class ExceptionHandler;

/// <summary>
///     extensions for global exception handling
/// </summary>
public static partial class ExceptionHandlerConfiguration
{
    [LoggerMessage(3, LogLevel.Error, "[{ExceptionType}] at [{Route}] due to [{Reason}]")]
    public static partial void LogStructuredException(this ILogger l, Exception ex, string? exceptionType,
        string? route, string? reason);

    [LoggerMessage(4, LogLevel.Error, """
                                      =================================
                                      {Route}
                                      TYPE: {ExceptionType}
                                      REASON: {Reason}
                                      ---------------------------------
                                      {StackTrace}
                                      """)]
    public static partial void LogUnStructuredException(this ILogger l, string? exceptionType, string? route,
        string? reason, string? stackTrace);

    /// <summary>
    ///     registers the default global exception handler which will log the exceptions on the server and return a
    ///     user-friendly JSON response to the client
    ///     when unhandled exceptions occur.
    ///     TIP: when using this exception handler, you may want to turn off the asp.net core exception middleware logging to
    ///     avoid duplication like so:
    ///     <code>
    /// "Logging": { "LogLevel": { "Default": "Warning", "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware": "None" } }
    /// </code>
    /// </summary>
    /// <param name="app">the application builder</param>
    /// <param name="logger">an optional logger instance</param>
    /// <param name="logStructuredException">set to true if you'd like to log the error in a structured manner</param>
    public static WebApplication UseAppDomainExceptionHandler(
        this WebApplication app,
        ILogger? logger = null,
        bool logStructuredException = false)
    {
        app.UseExceptionHandler(errApp =>
        {
            errApp.Run(async ctx =>
            {
                var exHandlerFeature = ctx.Features.Get<IExceptionHandlerFeature>();

                if (exHandlerFeature is not null)
                    switch (exHandlerFeature.Error)
                    {
                        case ValidationException validationException:
                            await EndpointHelper.FormatValidationErrorResponse(validationException.Errors)
                                .ExecuteAsync(ctx);
                            break;
                        // Thrown by the generated pagination code: always a client error
                        case InvalidPaginationRequestException paginationException:
                            await TypedResults.Problem(
                                statusCode: StatusCodes.Status400BadRequest,
                                title: "Invalid pagination request",
                                detail: paginationException.Message
                            ).ExecuteAsync(ctx);
                            break;
                        default:
                        {
                            logger ??= ctx.RequestServices.GetRequiredService<ILogger<ExceptionHandler>>();
                            var reason = exHandlerFeature.Error.Message;

                            if (logStructuredException)
                                logger.LogStructuredException(
                                    exHandlerFeature.Error,
                                    exHandlerFeature.Error.GetType().Name,
                                    exHandlerFeature.Endpoint?.DisplayName?.Split(" => ")[0],
                                    reason);
                            else
                                //this branch is only meant for unstructured textual logging
                                logger.LogUnStructuredException(
                                    exHandlerFeature.Error.GetType().Name,
                                    exHandlerFeature.Endpoint?.DisplayName?.Split(" => ")[0],
                                    reason,
                                    exHandlerFeature.Error.StackTrace);

                            await TypedResults.InternalServerError().ExecuteAsync(ctx);
                            break;
                        }
                    }
            });
        });

        return app;
    }
}