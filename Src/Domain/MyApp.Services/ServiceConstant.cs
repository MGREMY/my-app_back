namespace MyApp.Services;

public static class ServiceConstant
{
    public static class Cache
    {
        private const string BaseKey = "";

        public static string UserKey(Guid id)
        {
            return $"{BaseKey}:users:{id}";
        }
    }

    public static class Error
    {
        // ReSharper disable InconsistentNaming
        public const string user_not_found = nameof(user_not_found);

        public const string user_already_deleted = nameof(user_already_deleted);
        // ReSharper restore InconsistentNaming
    }
}