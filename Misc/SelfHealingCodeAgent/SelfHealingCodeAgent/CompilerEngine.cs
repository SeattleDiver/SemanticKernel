using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;

public static class CompilerEngine
{
    public static (bool IsSuccess, string Message) AttemptCompilation(string sourceCode)
    {
        // 1. Parse the source code into an Abstract Syntax Tree (AST)
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        // 2. Set up the basic .NET references the compiled code will need
        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).GetTypeInfo().Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).GetTypeInfo().Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location)
        };

        // 3. Configure the compiler
        var compilation = CSharpCompilation.Create(
            "DynamicAIGeneratedAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // 4. Attempt to compile to a memory stream
        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (result.Success)
        {
            return (true, "Compilation Successful!");
        }

        // 5. If it fails, format the errors to send BACK to Gemini
        var errors = result.Diagnostics
            .Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diag => $"- Line {diag.Location.GetLineSpan().StartLinePosition.Line + 1}: {diag.GetMessage()}");

        return (false, string.Join("\n", errors));
    }
}