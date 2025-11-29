namespace Domain.Service.Contract;

public class DomainException : Exception
{
    public int StatusCode { get; set; }

    public DomainException(string message) : base(message)
    {
    }
    
    public DomainException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
    
    public DomainException(string message, Exception e) : base(message, e)
    {
    }
    
    public DomainException(string message, Exception e, int statusCode) : base(message, e)
    {
        StatusCode = statusCode;
    }
}