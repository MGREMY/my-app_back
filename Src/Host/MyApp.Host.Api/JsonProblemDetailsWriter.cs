using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace MyApp.Host.Api;

/// <summary>
///     Fallback problem writer: the API only speaks JSON, so a problem is written as JSON even when the request does not
///     accept it (<c>Accept: application/xml</c>). The default writer declines such requests, and the framework then
///     falls back to plain text (status code pages) or to a problem without <c>instance</c> and <c>traceId</c>.
///     Registered after the default writer, it only handles the requests the default one declines, with the same output.
/// </summary>
internal sealed class JsonProblemDetailsWriter : IProblemDetailsWriter
{
    private readonly IOptions<ProblemDetailsOptions> _options;

    public JsonProblemDetailsWriter(IOptions<ProblemDetailsOptions> options)
    {
        _options = options;
    }

    public bool CanWrite(ProblemDetailsContext context)
    {
        return true;
    }

    public ValueTask WriteAsync(ProblemDetailsContext context)
    {
        var httpContext = context.HttpContext;
        var problem = context.ProblemDetails;

        problem.Status ??= httpContext.Response.StatusCode;
        // Applies the default type and title of the status to the problem, as the default writer does
        _ = TypedResults.Problem(problem);
        problem.Extensions.TryAdd("traceId", Activity.Current?.Id ?? httpContext.TraceIdentifier);
        _options.Value.CustomizeProblemDetails?.Invoke(context);

        return new ValueTask(httpContext.Response.WriteAsJsonAsync(
            problem, options: null, contentType: "application/problem+json", httpContext.RequestAborted));
    }
}
