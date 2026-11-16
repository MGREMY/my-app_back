using Microsoft.CodeAnalysis;

namespace Domain.Service.Generator.Parser;

public static class StringPropertyParser
{
    public static string Parse(IPropertySymbol propertySymbol, bool nullable = false)
    {
        var exceptionContent = !nullable
            ? "throw new InvalidCastException($\"Can't cast {request.Value} to type {typeof(String)})\");"
            : string.Empty;

        return $$"""
                 MemberExpression property = Expression.Property(param, nameof(t1.{{propertySymbol.Name}}));

                 if (request.Value is null)
                 {
                     {{exceptionContent}}
                 }

                 comparision = request.FilterOperator switch
                 {
                     FilterRequest.Operator.Equal => Expression.Equal(property, Expression.Constant(request.Value)),
                     FilterRequest.Operator.NotEqual => Expression.NotEqual(property, Expression.Constant(request.Value)),
                     FilterRequest.Operator.Contains => Expression.Call(Expression.Call(property, _string_toLowerMethod), _string_containsMethod, Expression.Call(Expression.Constant(request.Value), _string_toLowerMethod)),
                     FilterRequest.Operator.NotContains => Expression.Not(Expression.Call(Expression.Call(property, _string_toLowerMethod), _string_containsMethod, Expression.Call(Expression.Constant(request.Value), _string_toLowerMethod))),
                     FilterRequest.Operator.StartWith => Expression.Call(Expression.Call(property, _string_toLowerMethod), _string_startsWithMethod, Expression.Call(Expression.Constant(request.Value), _string_toLowerMethod)),
                     FilterRequest.Operator.EndWith => Expression.Call(Expression.Call(property, _string_toLowerMethod), _string_endsWithMethod, Expression.Call(Expression.Constant(request.Value), _string_toLowerMethod)),
                     _ => throw new NotImplementedException(),
                 };
                 """;
    }
}