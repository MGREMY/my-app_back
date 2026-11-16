using System.Net;

namespace Domain.Service.Contract;

public sealed class DomainException : Exception
{
    public int StatusCode { get; }
    public Dictionary<string, string[]> Errors { get; }

    public DomainException(
        int statusCode,
        string message,
        Dictionary<string, string[]>? errors = null,
        Exception? e = null)
        : base(message, e)
    {
        StatusCode = statusCode;
        Errors = errors ?? [];
    }

    public DomainException(
        HttpStatusCode statusCode,
        string message,
        Dictionary<string, string[]>? errors = null,
        Exception? e = null)
        : base(message, e)
    {
        StatusCode = (int)statusCode;
        Errors = errors ?? [];
    }

    [Obsolete]
    public DomainException(string message, int statusCode, Exception? e = null) : base(message, e)
    {
        StatusCode = statusCode;
        Errors = [];
    }
}