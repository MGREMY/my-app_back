using Microsoft.CodeAnalysis;

namespace MyApp.Services.Generators.Parser;

/// <summary>
///     Emits the body of a filter <c>case</c> for a scalar property (string, number, bool, enum, Guid, dates).
/// </summary>
/// <remarks>
///     <para>
///         The emitted code is inserted in a generated <c>{Entity}FilterBuilder.Build</c> method and expects two locals in
///         scope: <c>property</c> (the member access <see cref="System.Linq.Expressions.Expression" />, already bound to the
///         current target) and <c>request</c> (the <c>FilterServiceRequest</c>). It always ends with a <c>return</c> of
///         the comparison expression.
///     </para>
///     <para>
///         Values are wrapped in a closure (<c>FilterExpressionHelper.Value</c>) so EF Core sends them as SQL parameters,
///         and converted to the property type so nullable properties can be compared.
///         Invalid input (unsupported operator, unparsable value) throws <c>InvalidPaginationRequestException</c>.
///     </para>
/// </remarks>
public static class ScalarFilterEmitter
{
    /// <summary>Operators available for a scalar type.</summary>
    private enum OperatorSet
    {
        /// <summary>Equality and case-insensitive text operators (contains, starts with, ends with).</summary>
        String,

        /// <summary>Equality and ordering operators (greater than, less than...).</summary>
        Comparable,

        /// <summary>Equality operators only.</summary>
        Equatable
    }

    /// <summary>Everything needed to emit the parsing and the comparison of one scalar type.</summary>
    private sealed class ScalarInfo(string typeName, bool isNullable, OperatorSet operators, string parse, string converted)
    {
        /// <summary>Fully qualified, non-nullable type name.</summary>
        public string TypeName { get; } = typeName;

        /// <summary>Whether the property accepts <c>null</c> (<c>Nullable&lt;T&gt;</c> or annotated string).</summary>
        public bool IsNullable { get; } = isNullable;

        /// <summary>Operators supported by the type.</summary>
        public OperatorSet Operators { get; } = operators;

        /// <summary>A boolean expression that parses <c>request.Value</c> into <c>out var parsed</c>.</summary>
        public string Parse { get; } = parse;

        /// <summary>Expression turning <c>parsed</c> into the value used in the comparison.</summary>
        public string Converted { get; } = converted;
    }

    /// <summary>
    ///     Whether <paramref name="type" /> is a scalar the generator knows how to filter on.
    /// </summary>
    /// <param name="type">Property type, nullable value types included.</param>
    public static bool IsSupported(ITypeSymbol type)
    {
        return TryGetInfo(type, out _);
    }

    /// <summary>
    ///     Emit the parsing and comparison code for a property of type <paramref name="type" />.
    /// </summary>
    /// <param name="type">Property type, nullable value types included.</param>
    /// <returns>The code of the <c>case</c> body, or an empty string when the type is not supported.</returns>
    public static string Emit(ITypeSymbol type)
    {
        if (!TryGetInfo(type, out var info)) return string.Empty;

        return info.Operators == OperatorSet.String
            ? EmitString(info)
            : EmitParsed(info);
    }

    /// <summary>
    ///     String properties: the value is used as is. Text operators are case-insensitive
    ///     (<c>lower(property) LIKE lower(value)</c> in SQL).
    /// </summary>
    private static string EmitString(ScalarInfo info)
    {
        var nullCheck = info.IsNullable
            ? string.Empty
            : "if (request.Value is null) throw new InvalidPaginationRequestException($\"A value is required to filter on {request.PropertyName}\");";

        return $$"""
                 {{nullCheck}}
                 var value = FilterExpressionHelper.Value(request.Value, property.Type);

                 return request.FilterOperator switch
                 {
                     FilterServiceRequest.Operator.Equal => Expression.Equal(property, value),
                     FilterServiceRequest.Operator.NotEqual => Expression.NotEqual(property, value),
                     FilterServiceRequest.Operator.Contains => FilterExpressionHelper.Bind((string p, string v) => p.ToLower().Contains(v.ToLower()), property, value),
                     FilterServiceRequest.Operator.NotContains => Expression.Not(FilterExpressionHelper.Bind((string p, string v) => p.ToLower().Contains(v.ToLower()), property, value)),
                     FilterServiceRequest.Operator.StartWith => FilterExpressionHelper.Bind((string p, string v) => p.ToLower().StartsWith(v.ToLower()), property, value),
                     FilterServiceRequest.Operator.EndWith => FilterExpressionHelper.Bind((string p, string v) => p.ToLower().EndsWith(v.ToLower()), property, value),
                     _ => throw new InvalidPaginationRequestException($"Operator {request.FilterOperator} is not supported for {request.PropertyName}"),
                 };
                 """;
    }

    /// <summary>
    ///     Non-string properties: <c>request.Value</c> is parsed into the property type first.
    ///     For nullable properties, <c>null</c>, an empty value or <c>"null"</c> means <c>IS NULL</c>.
    /// </summary>
    private static string EmitParsed(ScalarInfo info)
    {
        var parse = info.IsNullable
            ? $$"""
                {{info.TypeName}}? parsedValue = null;
                if ({{info.Parse}})
                    parsedValue = {{info.Converted}};
                else if (!string.IsNullOrEmpty(request.Value) && !string.Equals(request.Value, "null", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidPaginationRequestException($"Can't convert {request.Value} to {typeof({{info.TypeName}})} for {request.PropertyName}");
                """
            : $$"""
                if (!{{info.Parse}})
                    throw new InvalidPaginationRequestException($"Can't convert {request.Value} to {typeof({{info.TypeName}})} for {request.PropertyName}");
                var parsedValue = {{info.Converted}};
                """;

        var comparisons = info.Operators == OperatorSet.Comparable
            ? """
              FilterServiceRequest.Operator.GreaterThan => Expression.GreaterThan(property, value),
              FilterServiceRequest.Operator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(property, value),
              FilterServiceRequest.Operator.LessThan => Expression.LessThan(property, value),
              FilterServiceRequest.Operator.LessThanOrEqual => Expression.LessThanOrEqual(property, value),
              """
            : string.Empty;

        return $$"""
                 {{parse}}

                 // Closure-based value: EF Core sends it as a SQL parameter instead of inlining a literal
                 var value = FilterExpressionHelper.Value(parsedValue, property.Type);

                 return request.FilterOperator switch
                 {
                     FilterServiceRequest.Operator.Equal => Expression.Equal(property, value),
                     FilterServiceRequest.Operator.NotEqual => Expression.NotEqual(property, value),
                     {{comparisons}}
                     _ => throw new InvalidPaginationRequestException($"Operator {request.FilterOperator} is not supported for {request.PropertyName}"),
                 };
                 """;
    }

    /// <summary>
    ///     Describe how to parse and compare <paramref name="type" />.
    /// </summary>
    /// <remarks>
    ///     Supported: <c>string</c>, <c>bool</c>, <c>int</c>, <c>long</c>, <c>decimal</c>, <c>double</c>, enums,
    ///     <see cref="System.Guid" />, <see cref="System.DateTime" />, <see cref="System.DateTimeOffset" />,
    ///     <c>DateOnly</c>, and their nullable versions. Numbers and dates are parsed with the invariant culture;
    ///     dates are converted to UTC (Npgsql only accepts UTC for <c>timestamptz</c>).
    /// </remarks>
    /// <returns><c>true</c> when the type is supported.</returns>
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
            info = new ScalarInfo("string", type.NullableAnnotation == NullableAnnotation.Annotated, OperatorSet.String,
                "", "");
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
