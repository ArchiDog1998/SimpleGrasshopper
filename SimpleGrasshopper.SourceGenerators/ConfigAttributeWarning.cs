using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SimpleGrasshopper.SourceGenerators;

[Generator(LanguageNames.CSharp)]

public class ConfigAttributeWarning : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        InitOneAttribute(context, "Config", null);
    }

    private static void InitOneAttribute(IncrementalGeneratorInitializationContext context, string attributeName, string[]? validTypes)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName
            ($"SimpleGrasshopper.Attributes.{attributeName}Attribute",
                static (node, _) => node is VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: FieldDeclarationSyntax { Parent: TypeDeclarationSyntax } } },
                static (n, ct) => (VariableDeclaratorSyntax)n.TargetNode)
                .Where(m => m is not null);

        context.RegisterSourceOutput(provider.Collect(), (spc, array) =>
        {
            foreach (var variableInfo in array)
            {
                var field = (FieldDeclarationSyntax)variableInfo.Parent!.Parent!;

                if (field.AttributeLists.Any(m => m.Attributes.Any(a => SettingClassGenerator.IsAttribute(a.Name.ToString(), "Setting"))))
                {
                    continue;
                }

                var loc = variableInfo.Identifier.GetLocation();

                foreach (var attrs in field.AttributeLists)
                {
                    foreach (var attr in attrs.Attributes)
                    {
                        if (SettingClassGenerator.IsAttribute(attr.Name.ToString(), attributeName))
                        {
                            loc = attr.Name.GetLocation();
                            break;
                        }
                    }
                }

                var desc = new DiagnosticDescriptor(
                    "SG0007",
                    "Field Attribute",
                    $"The attribute SimpleGrasshopper.Attributes.{attributeName}Attribute must be used with the attribute SimpleGrasshopper.Attributes.SettingAttribute!",
                    "Problem",
                    DiagnosticSeverity.Warning,
                    true);

                spc.ReportDiagnostic(Diagnostic.Create(desc, loc));
            }
        });

        var provider2 = context.SyntaxProvider.ForAttributeWithMetadataName
            ($"SimpleGrasshopper.Attributes.{attributeName}Attribute",
                static (node, _) => node is PropertyDeclarationSyntax { Parent: TypeDeclarationSyntax },
                static (n, ct) => (PropertyDeclarationSyntax)n.TargetNode)
                .Where(m => m is not null);

        context.RegisterSourceOutput(provider2.Collect(), (spc, array) =>
        {
            foreach (var property in array)
            {
                if (!property.Modifiers.Any(SyntaxKind.StaticKeyword))
                {
                    var desc = new DiagnosticDescriptor(
                        "SG0001",
                        "Wrong Keyword",
                        "The property should be a static method!",
                        "Problem",
                        DiagnosticSeverity.Warning,
                        true);

                    spc.ReportDiagnostic(Diagnostic.Create(desc, property.Identifier.GetLocation()));
                }

                if (property.AccessorList?.Accessors is SyntaxList<AccessorDeclarationSyntax> accessor
                    && (!accessor.Any(x => x.IsKind(SyntaxKind.GetAccessorDeclaration))
                    || !accessor.Any(x => x.IsKind(SyntaxKind.SetAccessorDeclaration))))
                {
                    var desc = new DiagnosticDescriptor(
                        "SG0007",
                        "Wrong Get Set",
                        $"The property should has a getter and a setter!",
                        "Problem",
                        DiagnosticSeverity.Warning,
                        true);

                    spc.ReportDiagnostic(Diagnostic.Create(desc, property.Identifier.GetLocation()));
                }

                if (validTypes != null)
                {
                    var typeName = property.Type.ToString();

                    var rightType = false;
                    foreach (var validType in validTypes)
                    {
                        if (typeName.EndsWith(validType))
                        {
                            rightType = true;
                            break;
                        }
                    }

                    if (!rightType)
                    {
                        var desc = new DiagnosticDescriptor(
                        "SG0004",
                        "Wrong Type",
                        $"This type can't be tagged with SimpleGrasshopper.Attributes.{attributeName}Attribute!",
                        "Problem",
                        DiagnosticSeverity.Warning,
                        true);

                        spc.ReportDiagnostic(Diagnostic.Create(desc, property.Type.GetLocation()));
                    }
                }
            }
        });
    }
}
