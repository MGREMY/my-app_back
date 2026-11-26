namespace MyApp.Services.Abstractions;

public sealed record ServiceSuccessResponse;

public sealed record NotFoundError(string Code, IReadOnlyDictionary<string, object?>? Params = null);

public sealed record ConflictError(string Code, IReadOnlyDictionary<string, object?>? Params = null);

public sealed record ForbiddenError(string Code, IReadOnlyDictionary<string, object?>? Params = null);

public sealed record InvalidError(string Code, IReadOnlyDictionary<string, object?>? Params = null);

public union ServiceErrorResponse(NotFoundError, ConflictError, ForbiddenError, InvalidError)
{
    public static ServiceErrorResponse NotFound(string code, IReadOnlyDictionary<string, object?>? @params = null)
        => new NotFoundError(code, @params);

    public static ServiceErrorResponse Conflict(string code, IReadOnlyDictionary<string, object?>? @params = null)
        => new ConflictError(code, @params);

    public static ServiceErrorResponse Forbidden(string code, IReadOnlyDictionary<string, object?>? @params = null)
        => new ForbiddenError(code, @params);

    public static ServiceErrorResponse Invalid(string code, IReadOnlyDictionary<string, object?>? @params = null)
        => new InvalidError(code, @params);
}

public union ServiceResponse(ServiceSuccessResponse, ServiceErrorResponse);

public union ServiceResponse<T>(T, ServiceErrorResponse);