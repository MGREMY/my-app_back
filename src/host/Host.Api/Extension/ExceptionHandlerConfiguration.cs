using System.Net.Mime;
using System.Text.Json;
using Domain.Service.Contract;
using FastEndpoints;
using Microsoft.AspNetCore.Diagnostics;

namespace Host.Api.Extension;

// ReSharper disable once ClassNeverInstantiated.Global
internal class ExceptionHandler;

/// <summary>
/// extensions for global exception handling
/// </summary>
public static partial class ExceptionHandlerConfiguration
{
    [LoggerMessage(3, LogLevel.Error, "[{@exceptionType}] at [{@route}] due to [{@reason}]")]
    public static partial void LogStructuredException(this ILogger l, Exception ex, string? exceptionType,
        string? route, string? reason);

    [LoggerMessage(4, LogLevel.Error, """
                                      =================================
                                      {route}
                                      TYPE: {exceptionType}
                                      REASON: {reason}
                                      ---------------------------------
                                      {stackTrace}
                                      """)]
    public static partial void LogUnStructuredException(this ILogger l, string? exceptionType, string? route,
        string? reason, string? stackTrace);

    /// <summary>
    /// registers the default global exception handler which will log the exceptions on the server and return a user-friendly JSON response to the client
    /// when unhandled exceptions occur.
    /// TIP: when using this exception handler, you may want to turn off the asp.net core exception middleware logging to avoid duplication like so:
    /// <code>
    /// "Logging": { "LogLevel": { "Default": "Warning", "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware": "None" } }
    /// </code>
    /// </summary>
    /// <param name="app">the application builder</param>
    /// <param name="logger">an optional logger instance</param>
    /// <param name="logStructuredException">set to true if you'd like to log the error in a structured manner</param>
    /// <param name="useGenericReason">set to true if you don't want to expose the actual exception reason in the JSON response sent to the client</param>
    public static IApplicationBuilder UseDomainExceptionHandler(
        this IApplicationBuilder app,
        ILogger? logger = null,
        bool logStructuredException = false,
        bool useGenericReason = false)
    {
        app.UseExceptionHandler(errApp =>
        {
            errApp.Run(async ctx =>
            {
                var exHandlerFeature = ctx.Features.Get<IExceptionHandlerFeature>();

                if (exHandlerFeature is not null)
                {
                    if (exHandlerFeature.Error is DomainException domainException)
                    {
                        ctx.Response.StatusCode = domainException.StatusCode;
                        await WriteToResponse(new ErrorResponse
                        {
                            StatusCode = domainException.StatusCode,
                            Message = domainException.Message,
                        });
                    }
                    else
                    {
                        logger ??= ctx.Resolve<ILogger<ExceptionHandler>>();
                        var reason = exHandlerFeature.Error.Message;

                        if (logStructuredException)
                        {
                            logger.LogStructuredException(
                                exHandlerFeature.Error,
                                exHandlerFeature.Error.GetType().Name,
                                exHandlerFeature.Endpoint?.DisplayName?.Split(" => ")[0],
                                reason);
                        }
                        else
                        {
                            //this branch is only meant for unstructured textual logging
                            logger.LogUnStructuredException(
                                exHandlerFeature.Error.GetType().Name,
                                exHandlerFeature.Endpoint?.DisplayName?.Split(" => ")[0],
                                reason,
                                exHandlerFeature.Error.StackTrace);
                        }

                        ctx.Response.StatusCode = 500;
                        await WriteToResponse(new InternalErrorResponse
                        {
                            Status = "Internal Server Error!",
                            Code = ctx.Response.StatusCode,
                            Reason = useGenericReason ? "An unexpected error has occurred." : reason,
                            Note = "See application log for stack trace."
                        });
                    }

                    Task WriteToResponse<TValue>(TValue value)
                    {
                        return ctx.Response.WriteAsJsonAsync(
                            value,
                            JsonSerializerOptions.Web,
                            MediaTypeNames.Application.ProblemJson,
                            ctx.RequestAborted);
                    }
                }
            });
        });

        return app;
    }
}