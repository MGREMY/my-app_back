using FluentValidation;

namespace MyApp.Host.Api.Extension.Configuration;

public static class ValidationConfiguration
{
    extension<TBuilder>(TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        /// <summary>
        ///     Register every FluentValidation validator of the API assembly.
        ///     They are run automatically by <c>ValidationFilter</c>, applied to all endpoints in <c>UseAppEndpoints()</c>.
        /// </summary>
        public TBuilder AddAppValidation()
        {
            builder.Services.AddValidatorsFromAssemblyContaining<Program>();

            return builder;
        }
    }
}
