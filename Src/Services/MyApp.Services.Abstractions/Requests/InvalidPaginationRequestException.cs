namespace MyApp.Services.Abstractions.Requests;

/// <summary>
///     Thrown by the generated pagination code when a <see cref="PaginationServiceRequest" /> cannot be applied:
///     unknown property or path, unsupported operator for the property type, unparsable value, path too deep...
/// </summary>
/// <remarks>
///     It always describes a client error: the API turns it into a <c>400 Bad Request</c> problem whose detail is
///     <see cref="Exception.Message" />, so the message must stay understandable by an API consumer.
/// </remarks>
public sealed class InvalidPaginationRequestException(string message) : Exception(message);
