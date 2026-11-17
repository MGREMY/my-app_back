namespace MyApp.Services.Abstractions.Requests;

public sealed record SortServiceRequest(string PropertyName, bool IsDescending);