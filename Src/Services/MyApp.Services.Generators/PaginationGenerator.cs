using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MyApp.Services.Generators.Parser;

namespace MyApp.Services.Generators;

// =====================================================================================================================
// READING GUIDE
// =====================================================================================================================
//
// This file is C# that writes C# that builds expression trees. Three different "times" are involved:
//
//   1. COMPILE TIME (this file)   The generator runs inside the compiler. It inspects the entities with the Roslyn
//                                 symbol API (ITypeSymbol, IPropertySymbol...) and produces C# source as strings.
//   2. GENERATED CODE             Those strings are compiled into MyApp.Services, next to the services. They can be
//                                 read under Src/Services/MyApp.Services/obj/Debug/net11.0/generated/ (see AGENTS.md).
//   3. RUNTIME (generated code)   For each request, the generated code builds a LINQ expression tree
//                                 (x => x.Role.Name == @value) that EF Core translates into SQL.
//
// In the templates below:
//   - `$$"""..."""` raw strings: `{{ expr }}` is evaluated NOW (compile time, by the generator),
//     a single `{` / `}` is a literal brace of the generated code.
//   - Anything outside `{{ }}` is the generated code itself: it runs later, at runtime.
//
// ---------------------------------------------------------------------------------------------------------------------
// WHAT IS GENERATED
// ---------------------------------------------------------------------------------------------------------------------
//
// Given `[PaginationHandlerFor<User>]` on UserGet, with User -> Role (reference) and User -> Posts -> Tags
// (collections), the generator emits:
//
//   FilterExpressionHelper.g.cs        shared helpers (Bind, Value, Any, All, Count, quantifiers...)
//   UserFilterBuilder.g.cs             one builder per entity reachable from the root...
//   RoleFilterBuilder.g.cs
//   PostFilterBuilder.g.cs
//   TagFilterBuilder.g.cs
//   UserFilterBuilder.paginationHandler.g.cs   ...and the public entry point of each root (UserPaginationExtensions)
//
// A filter builder looks like this (simplified):
//
//   internal static class UserFilterBuilder
//   {
//       public static Expression Build(Expression target, string path, FilterServiceRequest request, int depth)
//       {
//           // "role.name" -> head = "role", rest = "name"
//           switch (head)
//           {
//               case "username": // scalar: build the comparison on target.UserName
//               case "role":     // reference: RoleFilterBuilder.Build(target.Role, rest, ...)
//               case "posts":    // collection: target.Posts.Any(x1 => PostFilterBuilder.Build(x1, rest, ...))
//               default:         // unknown property -> InvalidPaginationRequestException (400)
//           }
//       }
//   }
//
// ---------------------------------------------------------------------------------------------------------------------
// WALKTHROUGH OF ONE FILTER AT RUNTIME
// ---------------------------------------------------------------------------------------------------------------------
//
// Request: { propertyName: "Posts.$none.Author.Role.Name", filterOperator: Equal, value: "Admin" }
//
//   UserPaginationExtensions.ApplyFiltering
//     NormalizePath -> "posts.$none.author.role.name"
//     UserFilterBuilder.Build(x, "posts.$none.author.role.name", depth 0)
//       head "posts" = collection        ReadQuantifier("$none.author.role.name") -> None, "author.role.name"
//       PostFilterBuilder.Build(x1, "author.role.name", depth 1)
//         head "author" = reference
//         UserFilterBuilder.Build(x1.Author, "role.name", depth 2)
//           head "role" = reference
//           RoleFilterBuilder.Build(x1.Author.Role, "name", depth 3)
//             head "name" = scalar       -> x1.Author.Role.Name == @value
//       wrap with the quantifier         -> !x.Posts.Any(x1 => x1.Author.Role.Name == @value)
//     query.Where(x => !x.Posts.Any(x1 => x1.Author.Role.Name == @value))
//
//   EF Core SQL: WHERE NOT EXISTS (SELECT 1 FROM post p JOIN "user" u ... JOIN role r ... WHERE r.name = @value)
//
// Because each builder only knows its own entity and delegates the rest of the path, every entity is generated once,
// whatever the number of paths leading to it, and cycles (Post -> Author -> Posts...) do not loop at compile time.
// The depth parameter stops overly long paths at runtime.
//
// Sorting works differently: it needs a strongly typed key (OrderBy(x => x.Role.Name)), so every sortable path is
// written out at compile time (CollectSortPaths) instead of being built at runtime.
// =====================================================================================================================

/// <summary>
///     Generates filtering, sorting and paging for every entity used in <c>[PaginationHandlerFor&lt;TEntity&gt;]</c>.
/// </summary>
/// <remarks>
///     <para>For each root entity (the <c>TEntity</c> of an attribute), the generator emits:</para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <c>{Entity}PaginationExtensions.ProcessPaginationRequest(...)</c>, in the <c>MyApp.Services</c>
///                 namespace: the entry point used by services.
///             </description>
///         </item>
///         <item>
///             <description>
///                 One <c>{Entity}FilterBuilder</c> for the root and for every entity reachable from it through
///                 navigation properties (references and collections, nested, cycles included). Each builder is
///                 generated once and reused by every entity pointing to it.
///             </description>
///         </item>
///     </list>
///     <para>
///         Property paths are dotted and case-insensitive (<c>"role.name"</c>, <c>"Posts.Tags.Label"</c>,
///         <c>"user_name"</c>). Filters on references become joins, filters on collections become
///         <c>EXISTS (...)</c> (or <c>NOT EXISTS</c> with <c>$none</c> / <c>$all</c>). Sorting only follows
///         references. Any invalid request throws <c>InvalidPaginationRequestException</c>, turned into a 400 response
///         by the API.
///     </para>
/// </remarks>
// ReSharper disable once ClassNeverInstantiated.Global
[Generator(LanguageNames.CSharp)]
public class ServiceForPaginationRequestGenerator : IIncrementalGenerator
{
    /// <summary>Maximum number of reference navigations followed when generating sort paths.</summary>
    /// <remarks>Every level multiplies the number of generated sort cases, so keep it low.</remarks>
    private const int MaxSortDepth = 3;

    /// <summary>Metadata name of the attribute marking a paginated service (<c>`1</c> = one generic parameter).</summary>
    private const string AttributeFqdn = "MyApp.Services.PaginationHandlerForAttribute`1";

    /// <summary>Namespace of the generated filter builders and helper (internal plumbing).</summary>
    private const string GeneratedNamespace = "MyApp.Services.Generated.Filtering";

    /// <summary>Two properties of an entity normalize to the same path segment: only the first one is usable.</summary>
    private static readonly DiagnosticDescriptor LabelCollision = new(
        "PAG001",
        "Pagination property name collision",
        "Properties '{0}' and '{1}' of '{2}' have the same normalized name '{3}': only '{0}' can be used in filters and sorts",
        "Pagination",
        DiagnosticSeverity.Warning,
        true);

    /// <summary>Two paginated roots share the same simple name: the extensions class of the second one is suffixed.</summary>
    private static readonly DiagnosticDescriptor ExtensionsRenamed = new(
        "PAG002",
        "Pagination extensions renamed",
        "Paginated entity '{0}' has the same name as another paginated entity: its extensions are generated as '{1}'",
        "Pagination",
        DiagnosticSeverity.Info,
        true);

    /// <summary>Source of the <c>[PaginationHandlerFor&lt;TEntity&gt;]</c> attribute, added to the compilation.</summary>
    /// <remarks>
    ///     The attribute is generated (instead of being declared in a project) so the generator and its marker always
    ///     ship together. <c>EmbeddedAttribute</c> keeps it internal to the consuming assembly.
    /// </remarks>
    private const string AttributeSourceCode =
        """
        // <auto-generated/>
        using System;
        namespace MyApp.Services;

        /// <summary>
        ///     Generates <c>ProcessPaginationRequest</c> (filtering, sorting and paging) for <typeparamref name="TEntity" />.
        /// </summary>
        /// <typeparam name="TEntity">Entity queried by the decorated service.</typeparam>
        [AttributeUsage(AttributeTargets.Class)]
        [global::Microsoft.CodeAnalysis.EmbeddedAttribute]
        internal class PaginationHandlerForAttribute<TEntity> : global::System.Attribute
            where TEntity : class {}
        """;

    /// <summary>Source of <c>FilterExpressionHelper</c>, shared by every generated builder.</summary>
    /// <remarks>
    ///     This code does not depend on any entity, so it is written once (post-initialization) instead of being
    ///     repeated in every builder.
    /// </remarks>
    private const string HelperSourceCode =
        $$"""
          // <auto-generated/>
          #nullable enable
          using System;
          using System.Collections.Generic;
          using System.Linq;
          using System.Linq.Expressions;

          namespace {{GeneratedNamespace}};

          /// <summary>Expression helpers used by the generated filter builders.</summary>
          [global::Microsoft.CodeAnalysis.EmbeddedAttribute]
          internal static class FilterExpressionHelper
          {
              /// <summary>Maximum number of navigation hops in a filter path (e.g. "role.permissions.name" = 2).</summary>
              public const int MaxDepth = 4;

              /// <summary>"Role.User_Name", "role.userName", "ROLE.USERNAME"... all become "role.username".</summary>
              /// <remarks>Generated case labels are normalized the same way, so a single label matches every casing.</remarks>
              public static string NormalizePath(string path) => path.Replace("_", string.Empty).ToLowerInvariant();

              /// <summary>Inlines <paramref name="template"/>'s body, substituting its parameters with <paramref name="arguments"/>.</summary>
              /// <remarks>
              ///     Lets the generated code write expressions as typed lambdas instead of reflection-based expression trees.
              ///     Example: Bind((User x) => x.Role, target) with target = "p.Author" returns the expression "p.Author.Role".
              ///     The lambda is checked by the compiler, so a renamed property breaks the build instead of failing at runtime.
              /// </remarks>
              public static Expression Bind(LambdaExpression template, params Expression[] arguments)
              {
                  var body = template.Body;
                  for (var i = 0; i < arguments.Length; i++)
                      body = new ReplaceVisitor(template.Parameters[i], arguments[i]).Visit(body)!;
                  return body;
              }

              /// <summary>Wraps a value in a closure so EF Core turns it into a SQL parameter (not an inlined literal).</summary>
              /// <param name="targetType">Type of the compared property: the value is converted to it (e.g. T to Nullable&lt;T&gt;).</param>
              /// <remarks>
              ///     Expression.Constant would inline the value in the SQL text: one cached query plan per distinct value.
              ///     A captured variable becomes "@p0": one plan for every value. The conversion is needed because
              ///     Expression.Equal refuses to compare an int? property with an int value.
              /// </remarks>
              public static Expression Value<T>(T value, Type targetType)
              {
                  Expression<Func<T>> closure = () => value;
                  return closure.Body.Type == targetType ? closure.Body : Expression.Convert(closure.Body, targetType);
              }

              /// <summary>
              ///     Reserved path segment targeting the number of items of a collection ("posts.$count").
              ///     The "$" prefix avoids any collision with a real property named Count.
              /// </summary>
              public const string CountSegment = "$count";

              /// <summary>Reserved path segment: no item of the collection matches ("posts.$none.title").</summary>
              public const string NoneSegment = "$none";

              /// <summary>Reserved path segment: every item of the collection matches ("posts.$all.likes"). True on an empty collection.</summary>
              public const string AllSegment = "$all";

              /// <summary>How a filter on a collection is applied to its items.</summary>
              public enum Quantifier
              {
                  /// <summary>At least one item matches (default, EXISTS).</summary>
                  Any,

                  /// <summary>No item matches (NOT EXISTS).</summary>
                  None,

                  /// <summary>Every item matches (NOT EXISTS ... WHERE NOT condition).</summary>
                  All
              }

              /// <summary>
              ///     Read the optional quantifier at the start of <paramref name="path"/> (the path after a collection).
              /// </summary>
              /// <param name="path">Normalized path relative to the collection, e.g. "$none.title" or "title"</param>
              /// <param name="itemPath">Path relative to an item, without the quantifier (empty if nothing follows it)</param>
              /// <remarks>"$none.title" -> (None, "title"); "$none" -> (None, ""); "title" -> (Any, "title").</remarks>
              public static Quantifier ReadQuantifier(string path, out string itemPath)
              {
                  foreach (var (segment, quantifier) in new[] { (NoneSegment, Quantifier.None), (AllSegment, Quantifier.All) })
                  {
                      if (path == segment)
                      {
                          itemPath = string.Empty;
                          return quantifier;
                      }

                      if (path.StartsWith(segment + ".", StringComparison.Ordinal))
                      {
                          itemPath = path.Substring(segment.Length + 1);
                          return quantifier;
                      }
                  }

                  itemPath = path;
                  return Quantifier.Any;
              }

              /// <summary>source.All(item => predicate), translated by EF Core to NOT EXISTS (... WHERE NOT predicate).</summary>
              public static Expression All<TItem>(Expression source, ParameterExpression item, Expression predicate)
                  => Bind(
                      (IEnumerable<TItem> s, Func<TItem, bool> p) => s.All(p),
                      source,
                      Expression.Lambda<Func<TItem, bool>>(predicate, item));

              /// <summary>source.Count(), translated by EF Core to a COUNT subquery.</summary>
              public static Expression Count<TItem>(Expression source)
                  => Bind((IEnumerable<TItem> s) => s.Count(), source);

              /// <summary>source.Any(item => predicate), translated by EF Core to EXISTS (...).</summary>
              /// <remarks>
              ///     <paramref name="predicate"/> was built against <paramref name="item"/>: it becomes the body of the
              ///     lambda passed to Any, so "x1" in "x.Posts.Any(x1 => x1.Likes > 10)" is that same parameter.
              /// </remarks>
              public static Expression Any<TItem>(Expression source, ParameterExpression item, Expression predicate)
                  => Bind(
                      (IEnumerable<TItem> s, Func<TItem, bool> p) => s.Any(p),
                      source,
                      Expression.Lambda<Func<TItem, bool>>(predicate, item));

              /// <summary>Replaces every occurrence of one expression node by another.</summary>
              private sealed class ReplaceVisitor(Expression from, Expression to) : ExpressionVisitor
              {
                  public override Expression? Visit(Expression? node) => node == from ? to : base.Visit(node);
              }
          }
          """;

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Step 1: code that does not depend on the user's code (attribute + helper), added once.
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddEmbeddedAttributeDefinition();
            ctx.AddSource("PaginationHandlerFor.g.cs", AttributeSourceCode);
            ctx.AddSource("FilterExpressionHelper.g.cs", HelperSourceCode);
        });

        // Step 2: find every class decorated with [PaginationHandlerFor<TEntity>] and keep (class, TEntity).
        // ForAttributeWithMetadataName is the fast path of incremental generators: Roslyn only re-runs this when a
        // decorated class changes. Collect() gathers all of them, because builders are shared between roots.
        IncrementalValueProvider<ImmutableArray<Target>> classProvider =
            context.SyntaxProvider.ForAttributeWithMetadataName(
                    AttributeFqdn,
                    static (node, _) => node is ClassDeclarationSyntax,
                    static (ctx, _) => new Target(
                        (INamedTypeSymbol)ctx.TargetSymbol,
                        ctx.Attributes[0].AttributeClass!.TypeArguments[0]))
                .Collect();

        // Step 3: generate the builders and the entry points from the list of roots.
        context.RegisterImplementationSourceOutput(classProvider, (spc, targets) =>
        {
            // Several services can paginate the same entity: generate it once
            var roots = targets
                .Select(x => x.Entity)
                .Distinct<ITypeSymbol>(SymbolEqualityComparer.Default)
                .ToArray();

            if (roots.Length == 0) return;

            // Breadth-first walk of the navigation graph: every entity reachable from a root gets its own
            // filter builder, generated once (cycles stop here) and reused by every entity pointing to it.
            // Example: roots = [User] -> queue User, then Role and Post (navigations of User), then Tag (from Post).
            // Post.Author points back to User, which is already known: nothing more is enqueued.
            var builderNames = new Dictionary<ITypeSymbol, string>(SymbolEqualityComparer.Default);
            var usedNames = new HashSet<string>(StringComparer.Ordinal);
            var queue = new Queue<ITypeSymbol>(roots);

            while (queue.Count > 0)
            {
                var type = queue.Dequeue();
                if (builderNames.ContainsKey(type)) continue;

                // Two entities with the same name in different namespaces get distinct builder names
                var name = type.Name + "FilterBuilder";
                for (var i = 2; !usedNames.Add(name); i++) name = $"{type.Name}{i}FilterBuilder";
                builderNames[type] = name;

                foreach (var property in GetProperties(type))
                {
                    var kind = Classify(property, type.ContainingAssembly, out var navigationType);
                    if (kind is PropertyKind.Reference or PropertyKind.Collection) queue.Enqueue(navigationType!);
                }
            }

            // Builder names are all known before emitting, because a builder calls the builders of its navigations
            foreach (var pair in builderNames)
                spc.AddSource($"{pair.Value}.g.cs", EmitFilterBuilder(pair.Key, pair.Value, builderNames, spc.ReportDiagnostic));

            // Public entry points, one per root. Two roots with the same simple name (Billing.Invoice / Sales.Invoice)
            // would generate the same class in MyApp.Services: the second one is suffixed. Services never write this
            // class name (they call the ProcessPaginationRequest extension), so the suffix is invisible to them.
            var usedExtensionNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var root in roots)
            {
                var extensionsName = root.Name + "PaginationExtensions";
                for (var i = 2; !usedExtensionNames.Add(extensionsName); i++)
                    extensionsName = $"{root.Name}{i}PaginationExtensions";

                if (extensionsName != root.Name + "PaginationExtensions")
                {
                    // Info only (no warning): the generated code is valid, this is just a trace
                    var location = targets
                        .First(x => SymbolEqualityComparer.Default.Equals(x.Entity, root))
                        .Class.Locations.FirstOrDefault();
                    spc.ReportDiagnostic(Diagnostic.Create(ExtensionsRenamed, location,
                        root.ToDisplayString(), extensionsName));
                }

                spc.AddSource($"{builderNames[root]}.paginationHandler.g.cs",
                    EmitPaginationExtensions(root, builderNames[root], extensionsName));
            }
        });
    }

    /// <summary>
    ///     Emit <c>{Entity}FilterBuilder</c>: one <c>case</c> per filterable property, matched on the first segment of
    ///     the path. Scalars emit the comparison; navigations delegate the rest of the path to the target builder.
    /// </summary>
    /// <param name="entity">Entity the builder filters.</param>
    /// <param name="builderName">Generated class name.</param>
    /// <param name="builderNames">Builder name of every reachable entity, used for navigation cases.</param>
    /// <param name="report">Reports the name collisions (PAG001).</param>
    private static string EmitFilterBuilder(
        ITypeSymbol entity,
        string builderName,
        Dictionary<ITypeSymbol, string> builderNames,
        Action<Diagnostic> report)
    {
        var entityFullName = entity.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var usedLabels = new Dictionary<string, IPropertySymbol>(StringComparer.Ordinal);
        var cases = new List<string>();

        foreach (var property in GetProperties(entity))
        {
            var kind = Classify(property, entity.ContainingAssembly, out var navigationType);
            if (kind is PropertyKind.Ignored) continue;

            // "UserName" -> "username", "@class" -> "@class". Two properties normalizing to the same label
            // (UserName / User_Name): only the first one is reachable (a second case label would not compile),
            // and a warning tells which one is lost.
            var label = NormalizeLabel(property);
            if (usedLabels.TryGetValue(label, out var first))
            {
                report(Diagnostic.Create(LabelCollision, property.Locations.FirstOrDefault(),
                    first.Name, property.Name, entity.ToDisplayString(), label));
                continue;
            }

            usedLabels[label] = property;

            // Generated expression of "target.Property", e.g. Bind((User x) => x.Role, target).
            // target is whatever expression of type Entity the builder receives (x, x1, x.Author...).
            var access = $"FilterExpressionHelper.Bind(({entityFullName} x) => x.{Identifier(property)}, target)";

            switch (kind)
            {
                // Scalar: the path must end here ("username"), the case returns the comparison.
                // The comparison body comes from ScalarFilterEmitter, depending on the property type.
                case PropertyKind.Scalar:
                    cases.Add($$"""
                                case "{{label}}":
                                {
                                    if (rest is not null) throw new InvalidPaginationRequestException($"No filter available for {request.PropertyName}");

                                    var property = {{access}};
                                    {{ScalarFilterEmitter.Emit(property.Type)}}
                                }
                                """);
                    break;

                // Reference: the path must continue ("role.name"). The member access becomes the target of the
                // navigated entity builder: RoleFilterBuilder.Build(x.Role, "name"). EF Core turns it into a join.
                case PropertyKind.Reference:
                    cases.Add($$"""
                                case "{{label}}":
                                {
                                    if (rest is null) throw new InvalidPaginationRequestException($"{request.PropertyName} is a navigation property, filter on one of its properties instead");

                                    return {{builderNames[navigationType!]}}.Build({{access}}, rest, request, depth + 1);
                                }
                                """);
                    break;

                // Collection: three possible continuations, checked in this order:
                //   "posts.$count"                  -> condition on x.Posts.Count()
                //   "posts.$none.title" / "$all..." -> quantifier, then the item path
                //   "posts.title"                   -> default quantifier (at least one item)
                // For the item path, a new lambda parameter (x1, x2... by depth) represents one item; the item
                // builder builds the predicate against it, then the predicate is wrapped in Any / All.
                case PropertyKind.Collection:
                    var itemFullName = navigationType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    cases.Add($$"""
                                case "{{label}}":
                                {
                                    if (rest is null) throw new InvalidPaginationRequestException($"{request.PropertyName} is a collection, filter on one of its properties or on its {FilterExpressionHelper.CountSegment} instead");

                                    // Going through the collection counts as one navigation, whatever follows it
                                    if (depth + 1 > FilterExpressionHelper.MaxDepth)
                                        throw new InvalidPaginationRequestException($"Filter path {request.PropertyName} is too deep");

                                    // "posts.$count": condition on the number of items (SQL COUNT subquery)
                                    if (rest == FilterExpressionHelper.CountSegment)
                                    {
                                        var property = FilterExpressionHelper.Count<{{itemFullName}}>({{access}});
                                        {{ScalarFilterEmitter.EmitCount()}}
                                    }

                                    // Optional quantifier: "posts.$none.title", "posts.$all.likes" (default: at least one item)
                                    var quantifier = FilterExpressionHelper.ReadQuantifier(rest, out var itemPath);
                                    if (itemPath.Length == 0) throw new InvalidPaginationRequestException($"{request.PropertyName} must be followed by one of the item properties");

                                    // One item of the collection, used as target of the item builder
                                    var item = Expression.Parameter(typeof({{itemFullName}}), "x" + (depth + 1));
                                    var predicate = {{builderNames[navigationType]}}.Build(item, itemPath, request, depth + 1);

                                    // $none = NOT Any (NOT EXISTS), $all = All (NOT EXISTS ... NOT), default = Any (EXISTS)
                                    return quantifier switch
                                    {
                                        FilterExpressionHelper.Quantifier.None => Expression.Not(FilterExpressionHelper.Any<{{itemFullName}}>({{access}}, item, predicate)),
                                        FilterExpressionHelper.Quantifier.All => FilterExpressionHelper.All<{{itemFullName}}>({{access}}, item, predicate),
                                        _ => FilterExpressionHelper.Any<{{itemFullName}}>({{access}}, item, predicate),
                                    };
                                }
                                """);
                    break;

                // Collection of values (List<string>, TimeOnly[]...): the item is the value itself, so the path ends
                // with the collection or its quantifier: "tags" (at least one), "tags.$none", "tags.$all", or
                // "tags.$count". The comparison is the one of the element type, built against each item.
                case PropertyKind.PrimitiveCollection:
                    var elementFullName = navigationType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    cases.Add($$"""
                                case "{{label}}":
                                {
                                    // Going through the collection counts as one navigation, like entity collections
                                    if (depth + 1 > FilterExpressionHelper.MaxDepth)
                                        throw new InvalidPaginationRequestException($"Filter path {request.PropertyName} is too deep");

                                    // "tags.$count": condition on the number of values
                                    if (rest == FilterExpressionHelper.CountSegment)
                                    {
                                        var property = FilterExpressionHelper.Count<{{elementFullName}}>({{access}});
                                        {{ScalarFilterEmitter.EmitCount()}}
                                    }

                                    // "tags" / "tags.$none" / "tags.$all": nothing may follow, the item is a value
                                    var quantifier = FilterExpressionHelper.ReadQuantifier(rest ?? string.Empty, out var itemPath);
                                    if (itemPath.Length != 0) throw new InvalidPaginationRequestException($"{request.PropertyName}: the items of this collection are values, filter on the collection itself");

                                    var item = Expression.Parameter(typeof({{elementFullName}}), "x" + (depth + 1));
                                    var predicate = CompareItem(item);

                                    return quantifier switch
                                    {
                                        FilterExpressionHelper.Quantifier.None => Expression.Not(FilterExpressionHelper.Any<{{elementFullName}}>({{access}}, item, predicate)),
                                        FilterExpressionHelper.Quantifier.All => FilterExpressionHelper.All<{{elementFullName}}>({{access}}, item, predicate),
                                        _ => FilterExpressionHelper.Any<{{elementFullName}}>({{access}}, item, predicate),
                                    };

                                    // Same comparison code as a scalar property of the element type, "property" being one item
                                    Expression CompareItem(Expression property)
                                    {
                                        {{ScalarFilterEmitter.Emit(navigationType)}}
                                    }
                                }
                                """);
                    break;
            }
        }

        // The builder itself: split the path in head / rest and dispatch on head.
#pragma warning disable format // @formatter:off
        return
$$"""
// <auto-generated/>
#nullable disable

using System;
using System.Globalization;
using System.Linq.Expressions;
using MyApp.Services.Abstractions.Requests;

namespace {{GeneratedNamespace}};

/// <summary>Builds filter expressions for <see cref="{{entityFullName.Replace("global::", "")}}"/>, reused by every entity navigating to it.</summary>
internal static class {{builderName}}
{
    /// <summary>Build the filter expression of <paramref name="request"/> for <paramref name="path"/>.</summary>
    /// <param name="target">Expression of type {{entity.Name}} (lambda parameter, navigation member access, Any() item...)</param>
    /// <param name="path">Normalized path relative to <paramref name="target"/> (e.g. "role.name")</param>
    /// <param name="request">Filter request (operator and value)</param>
    /// <param name="depth">Number of navigations already followed, limited to FilterExpressionHelper.MaxDepth</param>
    /// <exception cref="InvalidPaginationRequestException">Unknown path, unsupported operator or invalid value</exception>
    public static Expression Build(Expression target, string path, FilterServiceRequest request, int depth)
    {
        // Each navigation adds 1: stops paths such as posts.author.posts.author... (allowed by cycles)
        if (depth > FilterExpressionHelper.MaxDepth)
            throw new InvalidPaginationRequestException($"Filter path {request.PropertyName} is too deep");

        // "role.name" -> head "role", rest "name"; "username" -> head "username", rest null
        var separatorIndex = path.IndexOf('.');
        var head = separatorIndex < 0 ? path : path.Substring(0, separatorIndex);
        var rest = separatorIndex < 0 ? null : path.Substring(separatorIndex + 1);

        // One case per filterable property of {{entity.Name}}, labels are normalized property names
        switch (head)
        {
            {{string.Join("\n", cases)}}
            default:
                throw new InvalidPaginationRequestException($"No filter available for {request.PropertyName}");
        }
    }
}
""";
#pragma warning restore format // @formatter:on
    }

    /// <summary>
    ///     Emit <c>{Entity}PaginationExtensions</c>: the public <c>ProcessPaginationRequest</c> entry point, plus the
    ///     filtering (through the root filter builder), sorting (flattened sort paths) and paging steps.
    /// </summary>
    /// <param name="entity">Root entity.</param>
    /// <param name="builderName">Name of the root entity filter builder.</param>
    /// <param name="extensionsName">Name of the generated public class (suffixed on name collision).</param>
    private static string EmitPaginationExtensions(ITypeSymbol entity, string builderName, string extensionsName)
    {
        var entityFullName = entity.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Every sortable path of the root, e.g. ("username", "x.UserName"), ("role.name", "x.Role.Name")
        var sortPaths = new List<(string Label, string Access)>();
        CollectSortPaths(entity, "x", "", 0, sortPaths);

        // One case per sortable path. Emitted twice below: the first sort request uses OrderBy, the next ones ThenBy
        // (the method names are parameters because the lambdas are the same).
        string SortCases(string ascending, string descending)
        {
            return string.Join("\n", sortPaths.Select(p => $"""
                                                            case "{p.Label}":
                                                                query = !request.IsDescending
                                                                    ? {ascending}(x => {p.Access})
                                                                    : {descending}(x => {p.Access});
                                                                break;
                                                            """));
        }

#pragma warning disable format // @formatter:off
        return
$$"""
// <auto-generated/>
#nullable disable

using System;
using {{GeneratedNamespace}};
using MyApp.Services.Abstractions.Requests;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

using t1 = {{entityFullName}};

namespace MyApp.Services;

/// <summary>Filtering, sorting and paging for <see cref="{{entityFullName.Replace("global::", "")}}"/> queries.</summary>
public static class {{extensionsName}}
{
    /// <summary>
    ///     Apply the filters, then the sorts, then the paging of <paramref name="request"/> to <paramref name="query"/>.
    /// </summary>
    /// <param name="query">Query to paginate</param>
    /// <param name="request">Pagination request</param>
    /// <param name="countAsync">Counts the filtered rows (before paging), to compute the number of pages</param>
    /// <exception cref="InvalidPaginationRequestException">Unknown property, unsupported operator or invalid value</exception>
    public static IQueryable<t1> ProcessPaginationRequest(this IQueryable<t1> query, PaginationServiceRequest request, out Func<CancellationToken, Task<int>> countAsync)
    {
        // Order matters: count after filtering but before paging, page after sorting
        return query
            .ApplyFiltering(request.FilterRequests, out countAsync)
            .ApplySorting(request.SortRequests)
            .ApplyPagination(request.PageNumber, request.PageSize);
    }

    /// <summary>Skip the previous pages and take one page (page numbers start at 1).</summary>
    private static IQueryable<t1> ApplyPagination(this IQueryable<t1> query, int pageNumber, int pageSize) => query.Skip((pageNumber - 1) * pageSize).Take(pageSize);

    /// <summary>First request is an OrderBy, the following ones are ThenBy. Without request, the order is the database one.</summary>
    private static IQueryable<t1> ApplySorting(this IQueryable<t1> query, IEnumerable<SortServiceRequest> requests)
    {
        bool hasFirstSort = false;

        foreach (var request in requests)
        {
            if (!hasFirstSort)
            {
                // First sort: OrderBy / OrderByDescending
                switch (FilterExpressionHelper.NormalizePath(request.PropertyName))
                {
                    {{SortCases("query.OrderBy", "query.OrderByDescending")}}
                    default:
                        throw new InvalidPaginationRequestException($"No sort available for {request.PropertyName}");
                }

                hasFirstSort = true;
            }
            else
            {
                // Next sorts: ThenBy / ThenByDescending (a second OrderBy would replace the first one)
                switch (FilterExpressionHelper.NormalizePath(request.PropertyName))
                {
                    {{SortCases("((IOrderedQueryable<t1>)query).ThenBy", "((IOrderedQueryable<t1>)query).ThenByDescending")}}
                    default:
                        throw new InvalidPaginationRequestException($"No sort available for {request.PropertyName}");
                }
            }
        }

        return query;
    }

    /// <summary>
    ///     Combine every filter request in one Where clause. Each request is combined with the previous ones using its
    ///     own FilterLogic; nested Filters are grouped (parentheses) and combined with their parent.
    /// </summary>
    /// <remarks>
    ///     [A, B(Or), C(And)] gives ((A OR B) AND C): left to right, without operator precedence.
    ///     A with Filters [B, C(Or)] gives (A AND/OR (B OR C)), the logic between A and the group being the one of B.
    /// </remarks>
    private static IQueryable<t1> ApplyFiltering(this IQueryable<t1> query, IEnumerable<FilterServiceRequest> requests, out Func<CancellationToken, Task<int>> countAsync)
    {
        // The root lambda parameter, shared by every filter so they can be combined in a single Where
        var param = Expression.Parameter(typeof(t1), "x");
        Expression combined = null;

        foreach (var request in requests)
        {
            var filterExpression = BuildFilterExpression(request, param);

            combined = combined is null
                ? filterExpression
                : CombineExpressions(combined, filterExpression, request.FilterLogic);
        }

        query = combined is null
            ? query
            : query.Where(Expression.Lambda<Func<t1, bool>>(combined, param));

        // Captured here: counts the filtered query, before sorting and paging
        countAsync = query.CountAsync;

        return query;

        // One filter request and its nested group, recursively
        static Expression BuildFilterExpression(FilterServiceRequest request, ParameterExpression param)
        {
            var comparision = {{builderName}}.Build(param, FilterExpressionHelper.NormalizePath(request.PropertyName), request, 0);

            if (!request.Filters.Any()) return comparision;

            Expression nestedCombined = null;

            foreach (var subFilter in request.Filters)
            {
                var subExpression = BuildFilterExpression(subFilter, param);

                nestedCombined = nestedCombined is null
                    ? subExpression
                    : CombineExpressions(nestedCombined, subExpression, subFilter.FilterLogic);
            }

            if (nestedCombined is not null)
            {
                comparision = CombineExpressions(comparision, nestedCombined, request.Filters.First().FilterLogic);
            }

            return comparision;
        }
    }

    /// <summary>left AND right / left OR right.</summary>
    private static Expression CombineExpressions(Expression left, Expression right, FilterServiceRequest.Logic logic) =>
        logic switch
        {
            FilterServiceRequest.Logic.And => Expression.AndAlso(left, right),
            FilterServiceRequest.Logic.Or => Expression.OrElse(left, right),
            _ => throw new InvalidPaginationRequestException($"Filter logic {logic} is not supported"),
        };
}
""";
#pragma warning restore format // @formatter:on
    }

    /// <summary>
    ///     Collect every sortable path of <paramref name="type" />, flattened at compile time
    ///     (<c>"role.name"</c> → <c>x =&gt; x.Role.Name</c>), because sorting needs a strongly typed key selector.
    /// </summary>
    /// <remarks>
    ///     Only scalars declared in <see cref="ScalarFilterEmitter" /> are sortable (the same types as filters: an
    ///     unknown struct such as JsonElement could not be translated by EF Core). Only reference navigations are
    ///     followed (sorting through a collection is meaningless), up to <see cref="MaxSortDepth" /> levels.
    ///     Self references are allowed (<c>manager.manager.name</c>): the depth limit is what stops cycles.
    ///     Example for User -> Role: ("id", "x.Id"), ("username", "x.UserName"), ("role.id", "x.Role.Id"),
    ///     ("role.name", "x.Role.Name").
    /// </remarks>
    /// <param name="type">Entity whose properties are collected.</param>
    /// <param name="access">C# member access leading to <paramref name="type" /> (e.g. <c>x.Role</c>).</param>
    /// <param name="labelPrefix">Normalized path leading to <paramref name="type" /> (e.g. <c>role.</c>).</param>
    /// <param name="depth">Number of navigations already followed.</param>
    /// <param name="result">Collected (normalized label, member access) pairs.</param>
    private static void CollectSortPaths(
        ITypeSymbol type, string access, string labelPrefix, int depth,
        List<(string Label, string Access)> result)
    {
        var usedLabels = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in GetProperties(type))
        {
            // Collisions are reported once by EmitFilterBuilder: here the first property silently wins as well
            var label = labelPrefix + NormalizeLabel(property);
            if (!usedLabels.Add(label)) continue;

            var memberAccess = $"{access}.{Identifier(property)}";

            switch (Classify(property, type.ContainingAssembly, out var navigationType))
            {
                case PropertyKind.Scalar:
                    result.Add((label, memberAccess));
                    break;

                case PropertyKind.Reference when depth < MaxSortDepth:
                    CollectSortPaths(navigationType!, memberAccess, label + ".", depth + 1, result);
                    break;

                // Collections (of entities or of values) and ignored properties are not sortable
            }
        }
    }

    /// <summary>Public readable instance properties, including inherited ones (<c>BaseEntity.Id</c>, ...).</summary>
    /// <remarks>Overridden or hidden (<c>new</c>) properties are returned once, the most derived first.</remarks>
    private static IEnumerable<IPropertySymbol> GetProperties(ITypeSymbol type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        // GetMembers() only returns the members declared by a type: walk the base types up to object
        for (var current = type;
             current is not null && current.SpecialType != SpecialType.System_Object;
             current = current.BaseType)
        {
            foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (property.IsStatic || property.IsIndexer) continue;
                if (property.DeclaredAccessibility != Accessibility.Public) continue;
                if (property.GetMethod is null || property.GetMethod.DeclaredAccessibility != Accessibility.Public)
                    continue;
                if (!seen.Add(property.Name)) continue;

                yield return property;
            }
        }
    }

    /// <summary>
    ///     Classify a property to know how the generator handles it.
    /// </summary>
    /// <param name="property">Property to classify.</param>
    /// <param name="modelAssembly">Assembly of the entities (navigations must point to a class of this assembly).</param>
    /// <param name="navigationType">
    ///     Target entity for <see cref="PropertyKind.Reference" /> and <see cref="PropertyKind.Collection" />,
    ///     element type for <see cref="PropertyKind.PrimitiveCollection" />.
    /// </param>
    /// <remarks>
    ///     The checks are ordered: supported scalars first (so a type declared in <see cref="ScalarFilterEmitter" />
    ///     always wins), then strings (a string is an IEnumerable&lt;char&gt;, it must never become a collection),
    ///     then references, then collections. Anything else is ignored: neither filterable nor sortable.
    /// </remarks>
    private static PropertyKind Classify(
        IPropertySymbol property,
        IAssemblySymbol modelAssembly,
        out ITypeSymbol? navigationType)
    {
        navigationType = null;
        var type = property.Type;

        if (ScalarFilterEmitter.IsSupported(type)) return PropertyKind.Scalar;
        if (type.SpecialType == SpecialType.System_String) return PropertyKind.Ignored;

        // "Role?" and "Role" are the same entity: drop the nullable annotation so symbol comparisons match
        if (IsModelEntity(type, modelAssembly))
        {
            navigationType = type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
            return PropertyKind.Reference;
        }

        // byte[] is a binary column (bytea / varbinary), never a collection of values
        if (type is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte }) return PropertyKind.Ignored;

        if (TryGetEnumerableElement(type, out var element))
        {
            if (IsModelEntity(element, modelAssembly))
            {
                navigationType = element.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
                return PropertyKind.Collection;
            }

            // List<string>, TimeOnly[]...: filterable when the element type is a supported scalar.
            // The nullable annotation is kept: List<string?> items can be null.
            if (ScalarFilterEmitter.IsSupported(element))
            {
                navigationType = element;
                return PropertyKind.PrimitiveCollection;
            }
        }

        // Unsupported types (TimeSpan, JsonElement, Dictionary...): skipped until declared in ScalarFilterEmitter
        return PropertyKind.Ignored;
    }

    /// <summary>
    ///     Navigations are only followed into classes declared in the same assembly as the entity
    ///     (i.e. <c>MyApp.Domain.Models</c>), so framework / third-party types are never walked.
    /// </summary>
    private static bool IsModelEntity(ITypeSymbol type, IAssemblySymbol modelAssembly)
    {
        return type.TypeKind == TypeKind.Class
               && type.SpecialType == SpecialType.None
               && SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, modelAssembly);
    }

    /// <summary>Element type of an array or of any <c>IEnumerable&lt;T&gt;</c> (<c>ICollection&lt;T&gt;</c>, <c>List&lt;T&gt;</c>...).</summary>
    private static bool TryGetEnumerableElement(ITypeSymbol type, out ITypeSymbol element)
    {
        element = null!;

        if (type is IArrayTypeSymbol array)
        {
            element = array.ElementType;
            return true;
        }

        // The type itself is IEnumerable<T>, or implements it (ICollection<T> implements IEnumerable<T>)
        var enumerable =
            type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Collections_Generic_IEnumerable_T } self
                ? self
                : type.AllInterfaces.FirstOrDefault(i =>
                    i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);

        if (enumerable is null) return false;

        element = enumerable.TypeArguments[0];
        return true;
    }

    /// <summary>
    ///     Single normalized label per property, matched against <c>FilterExpressionHelper.NormalizePath</c>:
    ///     covers PascalCase, camelCase, snake_case, _underscoreCamelCase, lower and UPPER case at once.
    /// </summary>
    /// <remarks>
    ///     Must stay identical to <c>FilterExpressionHelper.NormalizePath</c>, otherwise no label matches.
    ///     A property named after a C# keyword keeps its <c>@</c> (<c>@class</c>): requests must use <c>"@class"</c>.
    /// </remarks>
    private static string NormalizeLabel(IPropertySymbol property)
    {
        return Identifier(property).Replace("_", string.Empty).ToLowerInvariant();
    }

    /// <summary>
    ///     C# identifier of a property in the generated code: <c>@</c> prefix for reserved keywords.
    /// </summary>
    /// <remarks>Roslyn returns <c>class</c> for a property declared as <c>@class</c>; <c>x.class</c> would not compile.</remarks>
    private static string Identifier(IPropertySymbol property)
    {
        return SyntaxFacts.GetKeywordKind(property.Name) != SyntaxKind.None ? "@" + property.Name : property.Name;
    }

    /// <summary>A class decorated with <c>[PaginationHandlerFor&lt;TEntity&gt;]</c> and its entity.</summary>
    private sealed class Target(INamedTypeSymbol @class, ITypeSymbol entity)
    {
        /// <summary>Decorated service class.</summary>
        public INamedTypeSymbol Class { get; } = @class;

        /// <summary>Entity of the attribute (root of the generation).</summary>
        public ITypeSymbol Entity { get; } = entity;
    }

    /// <summary>How the generator handles a property.</summary>
    private enum PropertyKind
    {
        /// <summary>Supported by <see cref="ScalarFilterEmitter" /> (filterable + sortable).</summary>
        Scalar,

        /// <summary>Single navigation to another model entity (filter = join, sort = followed).</summary>
        Reference,

        /// <summary>Collection navigation to another model entity (filter = EXISTS, not sortable).</summary>
        Collection,

        /// <summary>Collection of supported scalars (List&lt;string&gt;...): filterable on its values, not sortable.</summary>
        PrimitiveCollection,

        /// <summary>Anything else (unsupported types, classes outside the model, blobs): neither filterable nor sortable.</summary>
        Ignored
    }
}
