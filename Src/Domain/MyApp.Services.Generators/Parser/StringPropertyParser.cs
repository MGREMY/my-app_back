using Microsoft.CodeAnalysis;

namespace MyApp.Services.Generators.Parser;

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
                     FilterServiceRequest.Operator.Equal => Expression.Equal(property, Expression.Constant(request.Value)),
                     FilterServiceRequest.Operator.NotEqual => Expression.NotEqual(property, Expression.Constant(request.Value)),
                     FilterServiceRequest.Operator.Contains => Expression.Call(Expression.Call(property, _string_toLowerMethod), _string_containsMethod, Expression.Call(Expression.Constant(request.Value), _string_toLowerMethod)),
                     FilterServiceRequest.Operator.NotContains => Expression.Not(Expression.Call(Expression.Call(property, _string_toLowerMethod), _string_containsMethod, Expression.Call(Expression.Constant(request.Value), _string_toLowerMethod))),
                     FilterServiceRequest.Operator.StartWith => Expression.Call(Expression.Call(property, _string_toLowerMethod), _string_startsWithMethod, Expression.Call(Expression.Constant(request.Value), _string_toLowerMethod)),
                     FilterServiceRequest.Operator.EndWith => Expression.Call(Expression.Call(property, _string_toLowerMethod), _string_endsWithMethod, Expression.Call(Expression.Constant(request.Value), _string_toLowerMethod)),
                     _ => throw new NotImplementedException(),
                 };
                 """;
    }
}