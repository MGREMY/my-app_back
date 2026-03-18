using Microsoft.CodeAnalysis;

namespace Domain.Service.Generator.Parser;

public static class GuidPropertyParser
{
    public static string Parse(IPropertySymbol propertySymbol, bool nullable = false)
    {
        var exceptionContent = !nullable
            ? "throw new InvalidCastException($\"Can't cast {request.Value} to type {typeof(Guid)})\");"
            : string.Empty;

        return $$"""
                 MemberExpression property = Expression.Property(param, nameof(t1.{{propertySymbol.Name}}));

                 if (!Guid.TryParse(request.Value, out var parsedValue))
                 {
                     {{exceptionContent}}
                 }

                 comparision = request.FilterOperator switch
                 {
                     FilterServiceRequest.Operator.Equal => Expression.Equal(property, Expression.Constant(parsedValue)),
                     FilterServiceRequest.Operator.NotEqual => Expression.NotEqual(property, Expression.Constant(parsedValue)),
                     _ => throw new NotImplementedException(),
                 };
                 """;
    }
}