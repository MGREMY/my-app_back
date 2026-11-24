using MyApp.Services.Abstractions;

namespace MyApp.Services.Tests.TestInfrastructure;

/// <summary>
///     Asserts that a service returned an error of a given kind. The error is a union nested in the response union, so
///     reaching it takes two type checks: <c>result.Value.ShouldBeServiceError&lt;NotFoundError&gt;()</c>.
/// </summary>
public static class ServiceResponseAssertions
{
    extension(object? value)
    {
        public TError ShouldBeServiceError<TError>()
        {
            return value.ShouldBeOfType<ServiceErrorResponse>().Value.ShouldBeOfType<TError>();
        }
    }
}
