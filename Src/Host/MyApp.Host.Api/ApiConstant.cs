namespace MyApp.Host.Api;

public static class ApiConstant
{
    public static class Error
    {
        // ReSharper disable InconsistentNaming
        public const string validation_failed = nameof(validation_failed);

        public const string invalid_pagination_request = nameof(invalid_pagination_request);
        // ReSharper enable InconsistentNaming
    }

    public static class AuthorizationPolicies
    {
        public const string Admin = nameof(Admin);
    }

    public static class Role
    {
        public const string Admin = "admin";
    }
}