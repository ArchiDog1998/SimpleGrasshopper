using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SimpleGrasshopper.SourceGenerators;
using System.Collections.Immutable;

namespace SimpleGrasshopper.Generators;

[Generator(LanguageNames.CSharp)]
public class ComponentClassGenerator : ClassGenerator<MethodDeclarationSyntax>
{
    protected override void Execute(SourceProductionContext context, ImmutableArray<MethodDeclarationSyntax> syntaxes)
    {
        var strings = new List<string>(syntaxes.Length);

        foreach (var syntax in syntaxes)
        {
            var loc = syntax.Identifier.GetLocation();

            if (!syntax.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StaticKeyword))
            {
                var desc = new DiagnosticDescriptor(
                "SG0001",
                "Wrong Keyword",
                "The method should be a static method!",
                "Problem",
                DiagnosticSeverity.Warning,
                true);

                context.ReportDiagnostic(Diagnostic.Create(desc, loc));
                continue;
            }

            var nameSpace = AssemblyPriorityGenerator.GetParent<BaseNamespaceDeclarationSyntax>(syntax)?.Name.ToString() ?? "Null";

            var className = AssemblyPriorityGenerator.GetParent<TypeDeclarationSyntax>(syntax)?.Identifier.Text ?? "Null";

            var methodName = syntax.Identifier.Text;

            string guidStr = TypePropertyComponentGenerator.GetGuid(nameSpace, className, methodName);

            if (strings.Contains(guidStr))
            {
                continue;
            }
            strings.Add(guidStr);

            var codeClassName = $"{className}_{methodName}_Component";

            //Obsolete
            if (TypePropertyComponentGenerator.IsObsolete(syntax))
            {
                codeClassName += "_Obsolete";
            }

            var code = $$"""
             using SimpleGrasshopper.DocumentObjects;
             using System.Reflection;
             using System.Linq;
             using System;

             namespace {{nameSpace}}
             {
                public partial class {{codeClassName}}()
                    : MethodComponent(typeof({{className}}).GetRuntimeMethods().Where(m => m.Name == "{{methodName}}").ToArray())
                {
                    public override Guid ComponentGuid => new ("{{guidStr}}");
                }
             }
             """;

            context.AddSource($"{codeClassName}.g.cs", code);
        }
    }
}
