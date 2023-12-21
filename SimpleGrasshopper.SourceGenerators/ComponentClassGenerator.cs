using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SimpleGrasshopper.SourceGenerators;
using System.Collections.Immutable;

namespace SimpleGrasshopper.Generators;

[Generator(LanguageNames.CSharp)]
public class ComponentClassGenerator : ClassGenerator<MethodDeclarationSyntax>
{
    protected override void Execute(SourceProductionContext context, ImmutableArray<(MethodDeclarationSyntax, SemanticModel)> syntaxes)
    {
        var strings = new List<string>(syntaxes.Length);

        foreach (var (syntax, model) in syntaxes)
        {
            var nameSpace = AssemblyPriorityGenerator.GetParent<BaseNamespaceDeclarationSyntax>(syntax)?.Name.ToString() ?? "Null";

            var classSyntax = AssemblyPriorityGenerator.GetParent<TypeDeclarationSyntax>(syntax);

            if (classSyntax?.AttributeLists.Any(attrs => attrs.Attributes.Any(a => model.GetSymbolInfo(a).Symbol?.GetFullMetadataName() == "SimpleGrasshopper.Attributes.TypeComponentAttribute")) ?? false)
            {
                continue;
            }

            var className = classSyntax?.Identifier.Text ?? "Null";

            var methodName = syntax.Identifier.Text;

            string guidStr = Utils.GetGuid(nameSpace, className, methodName);

            if (strings.Contains(guidStr))
            {
                continue;
            }
            strings.Add(guidStr);

            var codeClassName = $"{className}_{methodName}_Component";

            //Obsolete
            if (syntax.IsObsolete())
            {
                codeClassName += "_Obsolete";
            }

            var componentName = "MethodComponent";
            foreach (var attrs in syntax.AttributeLists)
            {
                foreach (var a in attrs.Attributes)
                {
                    var attrSymbol = model.GetSymbolInfo(a).Symbol;
                    if (attrSymbol?.GetFullMetadataName() != "SimpleGrasshopper.Attributes.BaseComponentAttribute") continue;

                    var strs = a.ToString().Split('"');
                    if (strs.Length > 3) continue;

                    componentName = strs[1];
                    break;
                }
            }

            var code = $$"""
             using SimpleGrasshopper.Attributes;
             using SimpleGrasshopper.DocumentObjects;
             using System.Reflection;
             using System.Linq;
             using System;

             namespace {{nameSpace}}
             {
                public partial class {{codeClassName}}()
                    : {{componentName}}(typeof({{className}}).GetRuntimeMethods()
                    .Where(m => m.Name == "{{methodName}}" && m.GetCustomAttribute<IgnoreAttribute>() == null).ToArray())
                {
                    public override Guid ComponentGuid => new ("{{guidStr}}");
                }
             }
             """;

            context.AddSource($"{codeClassName}.g.cs", code);
        }
    }
}
