using Asp.Versioning.Builder;

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
        => new(builder.NewVersionedApi("Auth").RequireAuthorization());

    public static MiscApiBuilder MapMisc(this IEndpointRouteBuilder builder)
        => new(builder.NewVersionedApi("Misc").RequireAuthorization());

    public static UserApiBuilder MapUser(this IEndpointRouteBuilder builder)
        => new(builder.NewVersionedApi("User").RequireAuthorization());
}