namespace Domain.Service.Contract;

public class DomainException : Exception
{
    public int StatusCode { get; set; }

    public DomainException(string message, int statusCode, Exception? e = null) : base(message, e)
    {
        StatusCode = statusCode;
    }
}