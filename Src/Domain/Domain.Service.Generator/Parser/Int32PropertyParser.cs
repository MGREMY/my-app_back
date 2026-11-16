using Microsoft.CodeAnalysis;

namespace Domain.Service.Generator.Parser;

public static class Int32PropertyParser
{
    public static string Parse(IPropertySymbol propertySymbol, bool nullable = false)
    {
        var exceptionContent = !nullable
            ? "throw new InvalidCastException($\"Can't cast {request.Value} to type {typeof(Int32)})\");"
            : string.Empty;

        return $$"""
                 MemberExpression property = Expression.Property(param, nameof(t1.{{propertySymbol.Name}}));

                 if (!int.TryParse(request.Value, out var parsedValue))
                 {
                     {{exceptionContent}}
                 }

                 comparision = request.FilterOperator switch
                 {
                     FilterRequest.Operator.Equal => Expression.Equal(property, Expression.Constant(parsedValue)),
                     FilterRequest.Operator.NotEqual => Expression.NotEqual(property, Expression.Constant(parsedValue)),
                     FilterRequest.Operator.GreaterThan => Expression.GreaterThan(property, Expression.Constant(parsedValue)),
                     FilterRequest.Operator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(property, Expression.Constant(parsedValue)),
                     FilterRequest.Operator.LessThan => Expression.LessThan(property, Expression.Constant(parsedValue)),
                     FilterRequest.Operator.LessThanOrEqual => Expression.LessThanOrEqual(property, Expression.Constant(parsedValue)),
                     _ => throw new NotImplementedException(),
                 };
                 """;
    }
}