using Asp.Versioning.Builder;
using MyApp.Host.Api.Helpers;

namespace MyApp.Host.Api.Endpoints;

public abstract class VersionedApiBuilder(IVersionedEndpointRouteBuilder builder)
{
    public IVersionedEndpointRouteBuilder Endpoints => builder;
}

public sealed class AuthApiBuilder(IVersionedEndpointRouteBuilder builder) : VersionedApiBuilder(builder);

public sealed class MiscApiBuilder(IVersionedEndpointRouteBuilder builder) : VersionedApiBuilder(builder);

public sealed class UserApiBuilder(IVersionedEndpointRouteBuilder builder) : VersionedApiBuilder(builder);

public static class ApiBuilder
{
    public static AuthApiBuilder MapAuth(this IEndpointRouteBuilder builder)
    {
        return new AuthApiBuilder(builder.NewAppVersionedApi("Auth").RequireAuthorization());
    }

    public static MiscApiBuilder MapMisc(this IEndpointRouteBuilder builder)
    {
        return new MiscApiBuilder(builder.NewAppVersionedApi("Misc"));
    }

    public static UserApiBuilder MapUser(this IEndpointRouteBuilder builder)
    {
        return new UserApiBuilder(builder.NewAppVersionedApi("User").RequireAuthorization());
    }

    /// <summary>
    ///     Create a versioned API with the conventions shared by every endpoint (automatic validation).
    ///     Every <c>MapXxx()</c> must use it instead of <c>NewVersionedApi</c>.
    /// </summary>
    private static IVersionedEndpointRouteBuilder NewAppVersionedApi(this IEndpointRouteBuilder builder, string name)
    {
        return builder
            .NewVersionedApi(name)
            .AddEndpointFilterFactory(ValidationFilterHelper.Create);
    }
}
