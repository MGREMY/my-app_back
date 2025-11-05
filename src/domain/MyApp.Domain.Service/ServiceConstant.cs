namespace MyApp.Domain.Service;

public static class ServiceConstant
{
    public const string AuthCacheKeyPrefix = "auth";

    public static class Error
    {
        // ReSharper disable InconsistentNaming
        public const string user_not_found = nameof(user_not_found);
        // ReSharper restore InconsistentNaming
    }
}