using Microsoft.CodeAnalysis;

namespace Domain.Service.Generator.Parser;

/// <summary>
/// Emits the body of a filter <c>case</c> for a scalar property.
/// The generated code expects two locals in scope: <c>property</c> (the member access <see cref="System.Linq.Expressions.Expression"/>)
/// and <c>request</c> (the <c>FilterRequest</c>), and must <c>return</c> the comparison expression.
/// </summary>
public static class ScalarFilterEmitter
{
    private enum OperatorSet
    {
        String,
        Comparable,
        Equatable,
    }

    private sealed class ScalarInfo(string typeName, bool isNullable, OperatorSet operators, string parse, string converted)
    {
        /// <summary>Fully qualified, non-nullable type name.</summary>
        public string TypeName { get; } = typeName;

        public bool IsNullable { get; } = isNullable;
        public OperatorSet Operators { get; } = operators;

        /// <summary>A boolean expression that parses <c>request.Value</c> into <c>out var parsed</c>.</summary>
        public string Parse { get; } = parse;

        /// <summary>Expression turning <c>parsed</c> into the value used in the comparison.</summary>
        public string Converted { get; } = converted;
    }

    public static bool IsSupported(ITypeSymbol type) => TryGetInfo(type, out _);

    public static string Emit(ITypeSymbol type)
    {
        if (!TryGetInfo(type, out var info)) return string.Empty;

        return info.Operators == OperatorSet.String
            ? EmitString(info)
            : EmitParsed(info);
    }

    private static string EmitString(ScalarInfo info)
    {
        var nullCheck = info.IsNullable
            ? string.Empty
            : "if (request.Value is null) throw new InvalidCastException($\"Can't cast null to type {typeof(string)}\");";

        return $$"""
                 {{nullCheck}}
                 var value = FilterExpressionHelper.Value(request.Value, property.Type);

                 return request.FilterOperator switch
                 {
                     FilterRequest.Operator.Equal => Expression.Equal(property, value),
                     FilterRequest.Operator.NotEqual => Expression.NotEqual(property, value),
                     FilterRequest.Operator.Contains => FilterExpressionHelper.Bind((string p, string v) => p.ToLower().Contains(v.ToLower()), property, value),
                     FilterRequest.Operator.NotContains => Expression.Not(FilterExpressionHelper.Bind((string p, string v) => p.ToLower().Contains(v.ToLower()), property, value)),
                     FilterRequest.Operator.StartWith => FilterExpressionHelper.Bind((string p, string v) => p.ToLower().StartsWith(v.ToLower()), property, value),
                     FilterRequest.Operator.EndWith => FilterExpressionHelper.Bind((string p, string v) => p.ToLower().EndsWith(v.ToLower()), property, value),
                     _ => throw new NotImplementedException($"Operator {request.FilterOperator} is not supported for {request.PropertyName}"),
                 };
                 """;
    }

    private static string EmitParsed(ScalarInfo info)
    {
        // Nullable properties: null / empty / "null" means "IS NULL"; anything unparsable is an error.
        var parse = info.IsNullable
            ? $$"""
                {{info.TypeName}}? parsedValue = null;
                if ({{info.Parse}})
                    parsedValue = {{info.Converted}};
                else if (!string.IsNullOrEmpty(request.Value) && !string.Equals(request.Value, "null", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidCastException($"Can't cast {request.Value} to type {typeof({{info.TypeName}}?)}");
                """
            : $$"""
                if (!{{info.Parse}})
                    throw new InvalidCastException($"Can't cast {request.Value} to type {typeof({{info.TypeName}})}");
                var parsedValue = {{info.Converted}};
                """;

        var comparisons = info.Operators == OperatorSet.Comparable
            ? """
              FilterRequest.Operator.GreaterThan => Expression.GreaterThan(property, value),
              FilterRequest.Operator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(property, value),
              FilterRequest.Operator.LessThan => Expression.LessThan(property, value),
              FilterRequest.Operator.LessThanOrEqual => Expression.LessThanOrEqual(property, value),
              """
            : string.Empty;

        return $$"""
                 {{parse}}

                 // Closure-based value: EF Core sends it as a SQL parameter instead of inlining a literal
                 var value = FilterExpressionHelper.Value(parsedValue, property.Type);

                 return request.FilterOperator switch
                 {
                     FilterRequest.Operator.Equal => Expression.Equal(property, value),
                     FilterRequest.Operator.NotEqual => Expression.NotEqual(property, value),
                     {{comparisons}}
                     _ => throw new NotImplementedException($"Operator {request.FilterOperator} is not supported for {request.PropertyName}"),
                 };
                 """;
    }

    private static bool TryGetInfo(ITypeSymbol type, out ScalarInfo info)
    {
        info = null!;

        var isNullable = false;
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
        {
            isNullable = true;
            type = nullable.TypeArguments[0];
        }

        var name = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if (type.SpecialType == SpecialType.System_String)
        {
            info = new ScalarInfo("string", type.NullableAnnotation == NullableAnnotation.Annotated, OperatorSet.String, "", "");
            return true;
        }

        if (type.TypeKind == TypeKind.Enum)
        {
            info = new ScalarInfo(name, isNullable, OperatorSet.Equatable,
                $"global::System.Enum.TryParse<{name}>(request.Value, true, out var parsed)", "parsed");
            return true;
        }

        switch (type.SpecialType)
        {
            case SpecialType.System_Boolean:
                info = new ScalarInfo(name, isNullable, OperatorSet.Equatable,
                    "bool.TryParse(request.Value, out var parsed)", "parsed");
                return true;
            case SpecialType.System_Int32:
            case SpecialType.System_Int64:
            case SpecialType.System_Decimal:
            case SpecialType.System_Double:
                info = new ScalarInfo(name, isNullable, OperatorSet.Comparable,
                    $"{name}.TryParse(request.Value, CultureInfo.InvariantCulture, out var parsed)", "parsed");
                return true;
            case SpecialType.System_DateTime:
                info = new ScalarInfo(name, isNullable, OperatorSet.Comparable,
                    $"{name}.TryParse(request.Value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed)",
                    "parsed");
                return true;
        }

        switch (name)
        {
            case "global::System.Guid":
                info = new ScalarInfo(name, isNullable, OperatorSet.Equatable,
                    $"{name}.TryParse(request.Value, out var parsed)", "parsed");
                return true;
            case "global::System.DateTimeOffset":
                // Npgsql only accepts UTC offsets for timestamptz parameters
                info = new ScalarInfo(name, isNullable, OperatorSet.Comparable,
                    $"{name}.TryParse(request.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)",
                    "parsed.ToUniversalTime()");
                return true;
            case "global::System.DateOnly":
                info = new ScalarInfo(name, isNullable, OperatorSet.Comparable,
                    $"{name}.TryParse(request.Value, CultureInfo.InvariantCulture, out var parsed)", "parsed");
                return true;
        }

        return false;
    }
}
