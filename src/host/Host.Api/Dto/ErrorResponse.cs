using FluentValidation.Results;

namespace Host.Api.Dto;

public class ErrorResponse
{
    public int StatusCode { get; init; } = 400;
    public string Message { get; init; } = "One or more errors occurred!";
    public IDictionary<string, string[]> Errors { get; init; } = new Dictionary<string, string[]>();
}

public class InternalErrorResponse
{
    public int Code { get; init; } = 500;
    public string Status { get; init; } = "Internal Server Error!";
    public string Reason { get; init; } = "Something unexpected has happened";
    public string Note { get; init; } = "See application log for stack trace.";
}