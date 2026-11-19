using FluentValidation;

namespace MyApp.Host.Api.Helpers;

/// <summary>
///     Endpoint filter running FluentValidation automatically on every handler argument
///     that has a registered <see cref="IValidator{T}" />.
/// </summary>
/// <remarks>
///     Minimal APIs never call FluentValidation by themselves. The factory inspects each endpoint once,
///     at startup, and only adds the filter when at least one argument has a validator.
/// </remarks>
internal static class ValidationFilter
{
    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        var isService = context.ApplicationServices.GetRequiredService<IServiceProviderIsService>();

        var validatedArguments = context.MethodInfo.GetParameters()
            .Select((parameter, index) => (
                Index: index,
                ValidatorType: typeof(IValidator<>).MakeGenericType(parameter.ParameterType)))
            .Where(x => isService.IsService(x.ValidatorType))
            .ToArray();

        if (validatedArguments is []) return next;

        return async invocationContext =>
        {
            var services = invocationContext.HttpContext.RequestServices;
            var ct = invocationContext.HttpContext.RequestAborted;

            foreach (var (index, validatorType) in validatedArguments)
            {
                if (invocationContext.Arguments[index] is not { } argument) continue;

                var validator = (IValidator)services.GetRequiredService(validatorType);
                var result = await validator.ValidateAsync(new ValidationContext<object>(argument), ct);

                if (!result.IsValid)
                    return EndpointHelper.FormatValidationErrorResponse(invocationContext.HttpContext, result.Errors);
            }

            return await next(invocationContext);
        };
    }
}
