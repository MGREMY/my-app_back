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

                 var toLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
                 var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
                 var startsWithMethod = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;
                 var endsWithMethod = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;

                 comparision = request.FilterOperator switch
                 {
                     FilterRequest.Operator.Equal => Expression.Equal(property, Expression.Constant(request.Value)),
                     FilterRequest.Operator.NotEqual => Expression.NotEqual(property, Expression.Constant(request.Value)),
                     FilterRequest.Operator.Contains => Expression.Call(Expression.Call(property, toLowerMethod), containsMethod, Expression.Call(Expression.Constant(request.Value), toLowerMethod)),
                     FilterRequest.Operator.NotContains => Expression.Not(Expression.Call(Expression.Call(property, toLowerMethod), containsMethod, Expression.Call(Expression.Constant(request.Value), toLowerMethod))),
                     FilterRequest.Operator.StartWith => Expression.Call(Expression.Call(property, toLowerMethod), startsWithMethod, Expression.Call(Expression.Constant(request.Value), toLowerMethod)),
                     FilterRequest.Operator.EndWith => Expression.Call(Expression.Call(property, toLowerMethod), endsWithMethod, Expression.Call(Expression.Constant(request.Value), toLowerMethod)),
                     _ => throw new NotImplementedException(),
                 };
                 """;
    }
}