using System.Text.Json.Serialization;

namespace MyApp.Host.Api.Responses;

/// <summary>One invalid field of a validation problem (RFC 9457 "errors" member).</summary>
public sealed record ValidationErrorApiResponse(
    string Pointer,
    string Code,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<string, object?>? Params,
    string Detail);