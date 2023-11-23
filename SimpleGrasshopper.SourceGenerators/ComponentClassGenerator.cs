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
            var loc = syntax.ChildNodes().FirstOrDefault(n => n is PredefinedTypeSyntax)?.GetLocation() ?? syntax.GetLocation();

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
                return;
            }

            var nameSpace = AssemblyPriorityGenerator.GetParent<BaseNamespaceDeclarationSyntax>(syntax)?.Name.ToString() ?? "Null";

            var className = AssemblyPriorityGenerator.GetParent<TypeDeclarationSyntax>(syntax)?.Identifier.Text ?? "Null";

            var methodName = syntax.Identifier.Text;

            string guidStr = GetGuid(nameSpace, className, methodName);

            if (strings.Contains(guidStr))
            {
                var desc = new DiagnosticDescriptor(
                "SG0002",
                "Same method name",
                "The method name should be unique for creating components!",
                "Problem",
                DiagnosticSeverity.Error,
                true);

                context.ReportDiagnostic(Diagnostic.Create(desc, loc));
                return;
            }
            strings.Add(guidStr);

            var codeClassName = $"{className}_{methodName}_Component";

            //Obsolete
            if (IsObsolete(syntax))
            {
                codeClassName += "_Obsolete";
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
