using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace SimpleGrasshopper.SourceGenerators;

[Generator(LanguageNames.CSharp)]
internal class ParameterClassGenerator : ClassGenerator<TypeDeclarationSyntax>
{
    protected override void Execute(SourceProductionContext context, ImmutableArray<(TypeDeclarationSyntax, SemanticModel)> syntaxes)
    {
        foreach (var (syntax, model) in syntaxes)
        {
            var nameSpace = AssemblyPriorityGenerator.GetParent<BaseNamespaceDeclarationSyntax>(syntax)?.Name.ToString() ?? "Null";

            var className = syntax.Identifier.Text;

            string guidStr = Utils.GetGuid(nameSpace, className);

            var codeClassName = $"{className}_Parameter";

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
                    : TypeParameter<{{className}}>()
                {
                    public override Guid ComponentGuid => new ("{{guidStr}}");
                }
             }
             """;

            context.AddSource($"{codeClassName}.g.cs", code);
        }
    }
}
