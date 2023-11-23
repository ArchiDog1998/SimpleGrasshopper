using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SimpleGrasshopper.Generators;

[Generator(LanguageNames.CSharp)]
public class ComponentClassGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName("SimpleGrasshopper.Attributes.DocObjAttribute",
            static (node, _) => node is MethodDeclarationSyntax,
            static(n, ct) => (MethodDeclarationSyntax)n.TargetNode)
            .Where(m => m is not null);

        context.RegisterSourceOutput(provider.Collect(), Execute);
    }

    private static T? GetParent<T>(SyntaxNode? node) where T : SyntaxNode
    {
        if (node == null) return null;
        if (node is T result) return result;
        return GetParent<T>(node.Parent);
    }

    private void Execute(SourceProductionContext context, ImmutableArray<MethodDeclarationSyntax> syntaxes)
    {
        foreach(var syntax in syntaxes)
        {
            if(!syntax.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StaticKeyword))
            {
                var desc = new DiagnosticDescriptor(
                "SG0001",
                "Wrong Keyword",
                "The method should be a static method!",
                "Problem",
                DiagnosticSeverity.Warning,
                true);

                context.ReportDiagnostic(Diagnostic.Create(desc, syntax.ChildNodes().FirstOrDefault(n => n is PredefinedTypeSyntax)?.GetLocation() ?? syntax.GetLocation()));
                continue;
            }

            var nameSpace = GetParent<NamespaceDeclarationSyntax>(syntax)?.Name.ToString() ?? GetParent<FileScopedNamespaceDeclarationSyntax>(syntax)?.Name.ToString() ?? "Null";

            var className = GetParent<ClassDeclarationSyntax>(syntax)?.Identifier.Text ?? "Null";

            var methodName = syntax.Identifier.Text;

            string guidStr = string.Empty;
            using (MD5 md5 = MD5.Create())
            {
                var guidId = string.Join(".", nameSpace, className, methodName);
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(guidId));
                guidStr = new Guid(hash).ToString("B");
            }

            var codeClassName = $"{className}_{methodName}Component";

            //Obsolete
            if (syntax.AttributeLists.Any(list => list.Attributes.Any(attr => attr.Name.ToString() is "Obsolete" or "ObsoleteAttribute" or "System.Obsolete" or "System.ObsoleteAttribute")))
            {
                codeClassName += "Obsolete";
            }

            var code = $$"""
             using SimpleGrasshopper.DocumentObjects;
             using System.Reflection;

             namespace {{nameSpace}}
             {
                public class {{codeClassName}}()
                    :MethodComponent(typeof({{className}}).GetRuntimeMethods().FirstOrDefault(m => m.Name == "{{methodName}}") ?? throw new ArgumentNullException("Failed reflect the method!"))
                {
                    public override Guid ComponentGuid => new ("{{guidStr}}");
                }
             }
             """;

            context.AddSource($"{codeClassName}.g.cs", code);
        }
    }
}
