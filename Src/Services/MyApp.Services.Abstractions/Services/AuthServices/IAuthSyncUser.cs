namespace MyApp.Services.Abstractions.Services.AuthServices;

public interface IAuthSyncUser : IServiceAsync<IAuthSyncUser.Request>
{
    public sealed record Request(string AuthId, string UserName, string Email);
}