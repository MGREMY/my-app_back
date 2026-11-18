namespace MyApp.Services.Abstractions;

public sealed record ServiceErrorResponse(int StatusCode, string Message);

public sealed record ServiceSuccessResponse;

public union ServiceResponse(ServiceSuccessResponse, ServiceErrorResponse);

public union ServiceResponse<T>(T, ServiceErrorResponse);