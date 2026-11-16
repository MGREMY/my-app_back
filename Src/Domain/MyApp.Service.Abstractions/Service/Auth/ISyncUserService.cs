namespace MyApp.Service.Abstractions.Service.Auth;

public interface ISyncUserService
    : IServiceAsync<ISyncUserService.Request>
{
    public sealed record Request(string AuthId, string UserName, string Email);
}