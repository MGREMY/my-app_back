using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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
    }
}
