using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace SimpleGrasshopper.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public class BaseComponentGenerator : IIncrementalGenerator
{
    private static string? _methodComponentName;
    public static string MethodComponentName => _methodComponentName ?? "MethodComponent";

    private static string? _typeMethodComponentName;
    public static string TypeMethodComponentName => _typeMethodComponentName ?? "TypeMethodComponent";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName("SimpleGrasshopper.Attributes.BaseComponentAttribute",
            static (node, _) => node is ClassDeclarationSyntax,
            static (n, ct) => ((ClassDeclarationSyntax)n.TargetNode, n.SemanticModel))
            .Where(m => m.Item1 is not null);

        context.RegisterSourceOutput(provider.Collect(), Execute);
    }

    private void Execute(SourceProductionContext context, ImmutableArray<(ClassDeclarationSyntax, SemanticModel SemanticModel)> syntaxes)
    {
        foreach (var (classSyntax, model) in syntaxes)
        {
            var classSymbol = model.GetDeclaredSymbol(classSyntax) as ITypeSymbol;

            switch (classSymbol?.BaseType?.GetFullMetadataName())
            {
                case "SimpleGrasshopper.DocumentObjects.MethodComponent":
                    _methodComponentName = classSymbol.GetFullMetadataName();
                    break;
                case "SimpleGrasshopper.DocumentObjects.TypeMethodComponent":
                    _typeMethodComponentName = classSymbol.GetFullMetadataName();
                    break;
            }
        }
    }
}
