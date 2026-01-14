namespace Domain.Service;

public static class ServiceConstant
{
    public static class Auth
    {
        public const string AuthCacheKey = "auth";
        public const string SyncCacheKey = $"{AuthCacheKey}:sync";
    }

    public static class Error
    {
        // ReSharper disable InconsistentNaming
        public const string user_not_found = nameof(user_not_found);

        public const string user_already_deleted = nameof(user_already_deleted);
        // ReSharper restore InconsistentNaming
    }
}