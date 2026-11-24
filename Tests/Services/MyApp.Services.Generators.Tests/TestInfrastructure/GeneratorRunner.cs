using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MyApp.Services.Abstractions.Requests;

namespace MyApp.Services.Generators.Tests.TestInfrastructure;

/// <summary>Runs the pagination generator in memory on a source snippet, like the compiler does on a project.</summary>
internal static class GeneratorRunner
{
    // Implicit usings of an SDK project: the generated code relies on them
    private const string GlobalUsings =
        """
        global using System;
        global using System.Collections.Generic;
        global using System.Linq;
        global using System.Threading;
        global using System.Threading.Tasks;
        """;

    // Every assembly of the test host (framework, EF Core, MyApp.Services.Abstractions...)
    private static readonly Lazy<MetadataReference[]> References = new(() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Select(x => (MetadataReference)MetadataReference.CreateFromFile(x))
        .ToArray());

    /// <param name="source">Entities and handlers decorated with <c>[MyApp.Services.PaginationHandlerFor&lt;TEntity&gt;]</c>.</param>
    public static Result Run(string source)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        var compilation = CSharpCompilation.Create(
            "GeneratorInput",
            [CSharpSyntaxTree.ParseText(source, parseOptions), CSharpSyntaxTree.ParseText(GlobalUsings, parseOptions)],
            References.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        CSharpGeneratorDriver
            .Create([new ServiceForPaginationRequestGenerator().AsSourceGenerator()], parseOptions: parseOptions)
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        return new Result(diagnostics, output);
    }

    /// <param name="Diagnostics">Diagnostics reported by the generator.</param>
    /// <param name="Output">Compilation including the generated sources.</param>
    public sealed record Result(ImmutableArray<Diagnostic> Diagnostics, Compilation Output)
    {
        public IEnumerable<Diagnostic> CompilationErrors =>
            Output.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error);

        public string GeneratedSource =>
            string.Join(Environment.NewLine, Output.SyntaxTrees.Skip(2).Select(x => x.ToString()));

        private Assembly? _assembly;

        /// <summary>The compiled input, generated code included, loaded in the test process.</summary>
        private Assembly Assembly => _assembly ??= Load();

        /// <summary>New instance of the entity <paramref name="typeName" /> with the given property values.</summary>
        /// <param name="typeName">Full name of an entity of the compiled source.</param>
        /// <param name="values">Names and values of the properties to set on the instance.</param>
        public object Create(string typeName, params (string Property, object? Value)[] values)
        {
            var type = Assembly.GetType(typeName, throwOnError: true)!;
            var instance = Activator.CreateInstance(type)!;
            foreach (var (property, value) in values)
                type.GetProperty(property)!.SetValue(instance, value);

            return instance;
        }

        /// <summary>
        /// Runs the generated <c>ProcessPaginationRequest</c> of <paramref name="typeName" /> on <paramref name="rows" />,
        /// in memory (LINQ to Objects). Exceptions of the generated code are rethrown as is.
        /// </summary>
        /// <param name="typeName">Full name of an entity of the compiled source.</param>
        /// <param name="rows">Instances of that entity (from <see cref="Create"/>).</param>
        /// <param name="request">Pagination request passed to the generated code.</param>
        public List<object> Paginate(string typeName, IEnumerable<object> rows, PaginationServiceRequest request)
        {
            var type = Assembly.GetType(typeName, throwOnError: true)!;
            var items = rows.ToArray();
            var array = Array.CreateInstance(type, items.Length);
            Array.Copy(items, array, items.Length);

            var method = Assembly.GetType($"MyApp.Services.{type.Name}PaginationExtensions", throwOnError: true)!
                .GetMethod("ProcessPaginationRequest")!;

            try
            {
                var result = method.Invoke(null, [Queryable.AsQueryable(array), request, null])!;
                return ((System.Collections.IEnumerable)result).Cast<object>().ToList();
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                ExceptionDispatchInfo.Throw(exception.InnerException);
                throw;
            }
        }

        private Assembly Load()
        {
            using var stream = new MemoryStream();
            var emit = Output.Emit(stream);
            if (!emit.Success)
                throw new InvalidOperationException(string.Join(Environment.NewLine, emit.Diagnostics));

            return Assembly.Load(stream.ToArray());
        }
    }
}
