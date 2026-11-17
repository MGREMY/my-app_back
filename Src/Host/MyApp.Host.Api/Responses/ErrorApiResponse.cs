namespace MyApp.Host.Api.Responses;

public class ErrorApiResponse
{
    public int StatusCode { get; init; } = 400;
    public string Message { get; init; } = "One or more errors occurred!";
    public IDictionary<string, string[]> Errors { get; init; } = new Dictionary<string, string[]>();
}

public class InternalErrorApiResponse
{
    public int Code { get; init; } = 500;
    public string Status { get; init; } = "Internal Server Error!";
    public string Reason { get; init; } = "Something unexpected has happened";
    public string Note { get; init; } = "See application log for stack trace.";
}