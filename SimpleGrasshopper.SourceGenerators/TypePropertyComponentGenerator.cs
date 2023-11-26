using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;

namespace SimpleGrasshopper.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public class TypePropertyComponentGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName("SimpleGrasshopper.Attributes.PropertyComponentAttribute",
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

            string guidStr = GetGuid(nameSpace, className, "PropertyComponent");

            var codeClassName = $"{className}_PropertyComponent";

            //Obsolete
            if (IsObsolete(syntax))
            {
                codeClassName += "_Obsolete";
            }

            var code = $$"""
             using SimpleGrasshopper.DocumentObjects;
             using System;

             namespace {{nameSpace}}
             {
                public partial class {{codeClassName}}()
                    : TypePropertyComponent<{{className}}>()
                {
                    public override Guid ComponentGuid => new ("{{guidStr}}");
                }
             }
             """;

            context.AddSource($"{codeClassName}.g.cs", code);
        }
    }

    public static string GetGuid(params string[] ids)
    {
        var id = string.Join(".", ids);
        using MD5 md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(id));
        return new Guid(hash).ToString("B");
    }

    public static bool IsObsolete(MemberDeclarationSyntax syntax)
    {
        return syntax.AttributeLists.Any(list => list.Attributes.Any(attr => attr.Name.ToString() is "Obsolete" or "ObsoleteAttribute" or "System.Obsolete" or "System.ObsoleteAttribute"));
    }
}
