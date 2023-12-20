using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace SimpleGrasshopper.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public class TypePropertyComponentGenerator : TypeComponentGenerator
{
    protected override string AttrName => "PropertyComponent";

    protected override string ComponentParent => "TypePropertyComponent<{0}>()";
}

[Generator(LanguageNames.CSharp)]
public class TypeMethodComponentGenerator : TypeComponentGenerator
{
    protected override string AttrName => "TypeComponent";

    protected override string ComponentParent => "TypeMethodComponent(typeof({0}))";
}

public abstract class TypeComponentGenerator : IIncrementalGenerator
{
    protected abstract string AttrName { get; }

    protected abstract string ComponentParent { get; }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName($"SimpleGrasshopper.Attributes.{AttrName}Attribute",
            static (node, _) => node is TypeDeclarationSyntax,
            static (n, ct) => (TypeDeclarationSyntax)n.TargetNode)
            .Where(m => m is not null);

        context.RegisterSourceOutput(provider.Collect(), Execute);
    }

    private void Execute(SourceProductionContext context, ImmutableArray<TypeDeclarationSyntax> syntaxes)
    {
        foreach (var syntax in syntaxes)
        {
            var nameSpace = AssemblyPriorityGenerator.GetParent<BaseNamespaceDeclarationSyntax>(syntax)?.Name.ToString() ?? "Null";

            var className = syntax.Identifier.Text;

            string guidStr = Utils.GetGuid(nameSpace, className, AttrName);

            var codeClassName = $"{className}_{AttrName}";

            //Obsolete
            if (syntax.IsObsolete())
            {
                codeClassName += "_Obsolete";
            }

            var code = $$"""
             using SimpleGrasshopper.DocumentObjects;
             using System;

             namespace {{nameSpace}}
             {
                public partial class {{codeClassName}}()
                    : {{string.Format(ComponentParent, className)}}
                {
                    public override Guid ComponentGuid => new ("{{guidStr}}");
                }
             }
             """;

            context.AddSource($"{codeClassName}.g.cs", code);
        }
    }
}
