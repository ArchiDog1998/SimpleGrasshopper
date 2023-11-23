using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace SimpleGrasshopper.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public class AssemblyPriorityGenerator : IIncrementalGenerator
{
    public static TS? GetParent<TS>(SyntaxNode? node) where TS : SyntaxNode
    {
        if (node == null) return null;
        if (node is TS result) return result;
        return GetParent<TS>(node.Parent);
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.CreateSyntaxProvider(
            (c, _) => c is ClassDeclarationSyntax cd,
            (n, ct) =>
            {
                var symbol = n.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)n.Node, ct) as INamedTypeSymbol;

                while(symbol != null)
                {
                    if(symbol.Name == "GH_AssemblyInfo")
                    {
                        return (ClassDeclarationSyntax)n.Node;
                    }
                    symbol = symbol.BaseType;
                }
                return null;
            })
            .Where(m => m is not null);

        context.RegisterSourceOutput(provider.Collect(), Execute);
    }

    private void Execute(SourceProductionContext context, ImmutableArray<ClassDeclarationSyntax?> array)
    {
        switch (array.Length)
        {
            case > 1:
                var desc = new DiagnosticDescriptor(
                "SG0003",
                "Many Infos",
                "There should be only one assembly info!",
                "Problem",
                DiagnosticSeverity.Warning,
                true);

                context.ReportDiagnostic(Diagnostic.Create(desc, Location.None));
                return;

            case 1:
                var syntax = array[0];
                if(syntax !=  null)
                {
                    AddPriority(context, syntax);
                }
                return;
        }

        static void AddPriority(SourceProductionContext context, ClassDeclarationSyntax syntax)
        {
            var nameSpace = GetParent<BaseNamespaceDeclarationSyntax>(syntax)?.Name.ToString() ?? "Null";

            var code = $$"""
             using SimpleGrasshopper.Util;

             namespace {{nameSpace}}
             {
                public class SimpleAssemblyPriority : AssemblyPriority
                {
                }
             }
             """;

            context.AddSource("SimpleAssemblyPriority.g.cs", code);

        }
    }
}
