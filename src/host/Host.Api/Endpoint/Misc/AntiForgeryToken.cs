using FastEndpoints;
using Microsoft.AspNetCore.Antiforgery;

namespace Host.Api.Endpoint.Misc;

public sealed class AntiForgeryToken_V1 : Ep.NoReq.NoRes
{
    private readonly IAntiforgery _antiForgery;

    public AntiForgeryToken_V1(IAntiforgery antiForgery)
    {
        _antiForgery = antiForgery;
    }

    public override void Configure()
    {
        Get("/misc/anti-forgery-token");
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        _antiForgery.GetAndStoreTokens(HttpContext);

        return Task.CompletedTask;
    }
}