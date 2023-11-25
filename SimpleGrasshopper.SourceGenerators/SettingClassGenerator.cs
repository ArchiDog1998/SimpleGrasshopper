using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace SimpleGrasshopper.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public class SettingClassGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName
("SimpleGrasshopper.Attributes.SettingAttribute",
    static (node, _) => node is VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: FieldDeclarationSyntax { Parent: ClassDeclarationSyntax or StructDeclarationSyntax } } },
    static (n, ct) => (VariableDeclaratorSyntax)n.TargetNode)
    .Where(m => m is not null);
        context.RegisterSourceOutput(provider.Collect(), Execute);
    }

    private void Execute(SourceProductionContext context, ImmutableArray<VariableDeclaratorSyntax> array)
    {
        var typeGrps = array.GroupBy(variable => variable.Parent!.Parent!.Parent!);

        foreach (var grp in typeGrps)
        {
            var type = (TypeDeclarationSyntax)grp.Key;

            var nameSpace = AssemblyPriorityGenerator.GetParent<BaseNamespaceDeclarationSyntax>(type)?.Name.ToString() ?? "Null";

            var classType = type is ClassDeclarationSyntax ? "class" : "struct";

            var className = type.Identifier.Text;

            var propertyCodes = new List<string>();
            foreach (var variableInfo in grp)
            {
                var field = (FieldDeclarationSyntax)variableInfo.Parent!.Parent!;

                var variableName = variableInfo.Identifier.ToString();
                var propertyName = ToPascalCase(variableName);

                if (variableName == propertyName)
                {
                    var desc = new DiagnosticDescriptor(
                    "SG0005",
                    "Wrong Name",
                    "Please don't use Pascal Case to name your field!",
                    "Problem",
                    DiagnosticSeverity.Warning,
                    true);

                    context.ReportDiagnostic(Diagnostic.Create(desc, variableInfo.Identifier.GetLocation()));
                    continue;
                }

                if (!field.Modifiers.Any(SyntaxKind.StaticKeyword))
                {
                    var desc = new DiagnosticDescriptor(
                    "SG0001",
                    "Wrong Keyword",
                    "The field should be a static method!",
                    "Problem",
                    DiagnosticSeverity.Warning,
                    true);

                    context.ReportDiagnostic(Diagnostic.Create(desc, variableInfo.Identifier.GetLocation()));
                    return;
                }

                var key = string.Join(".", nameSpace, className, propertyName);

                var fieldTypeStr = field.Declaration.Type;
                
                if (!IsFieldTypeValid(fieldTypeStr.ToString()))
                {
                    var desc = new DiagnosticDescriptor(
                    "SG0004",
                    "Wrong Type",
                    "This type can't be a grasshopper setting type!",
                    "Problem",
                    DiagnosticSeverity.Warning,
                    true);

                    context.ReportDiagnostic(Diagnostic.Create(desc, fieldTypeStr.GetLocation()));
                    continue;
                }

                var names = new List<string>();
                foreach (var attrSet in field.AttributeLists)
                {
                    if (attrSet == null) continue;
                    foreach (var attr in attrSet.Attributes)
                    {
                        if (IsAttribute(attr.Name.ToString(), "Config")
                            || IsAttribute(attr.Name.ToString(), "Range"))
                        {
                            names.Add(attr.ToString());
                        }
                    }
                }
                var propertyCode = $$"""
                        [{{string.Join(", ", names)}}]
                        public static {{fieldTypeStr}} {{propertyName}}
                        {
                            get => Instances.Settings.GetValue("{{key}}", {{variableName}});
                            set
                            {
                                if ({{propertyName}} == value) return;
                                Instances.Settings.SetValue("{{key}}", value);
                                On{{propertyName}}Changed();
                                OnPropertyChanged("{{propertyName}}");
                            }
                        }
                        static partial void On{{propertyName}}Changed();

                        public static void Reset{{propertyName}}()
                        {
                            {{propertyName}} = {{variableName}};
                        }
                """;

                propertyCodes.Add(propertyCode);
            }

            var code = $$"""
             using Grasshopper;
             using System.Drawing;
             using SimpleGrasshopper.Attributes;

             namespace {{nameSpace}}
             {
                 partial {{classType}} {{className}}
                 {
             {{string.Join("\n", propertyCodes)}}

                     static partial void OnPropertyChanged(string propertyName);
                 }
             }
             """;

            context.AddSource($"{nameSpace}_{className}.g.cs", code);
        }
    }

    internal static bool IsAttribute(string attribute, string attributeName)
    {
        if (attribute == attributeName) return true;
        if (attribute == $"{attributeName}Attribute") return true;
        if (attribute == $"Attributes.{attributeName}") return true;
        if (attribute == $"Attributes.{attributeName}Attribute") return true;
        if (attribute == $"SimpleGrasshopper.Attributes.{attributeName}") return true;
        if (attribute == $"SimpleGrasshopper.Attributes.{attributeName}Attribute") return true;

        return false;
    }

    private static readonly string[] _validTypes =
        [
            "bool",
            "Boolean",
            "System.Boolean",
            "byte",
            "Byte",
            "System.Byte",
            "DateTime",
            "System.DateTime",
            "double",
            "Double",
            "System.Double",
            "int",
            "Int32",
            "System.Int32",
            "string",
            "String",
            "System.String",
            "Color",
            "Drawing.Color",
            "System.Drawing.Color",
            "Point",
            "Drawing.Point",
            "System.Drawing.Point",
            "Rectangle",
            "Drawing.Rectangle",
            "System.Drawing.Rectangle",
            "Size",
            "Drawing.Size",
            "System.Drawing.Size",
        ];
    private static bool IsFieldTypeValid(string typeName)
    {
        foreach (var validType in _validTypes)
        {
            if (typeName.EndsWith(validType))
            {
                return true;
            }
        }
        return false;
    }

    public static string ToPascalCase(string input)
    {
        return string.Join(".", input.Split('.').Select(ConvertToPascalCase));
    }
    private static string ConvertToPascalCase(string input)
    {
        Regex invalidCharsRgx = new(@"[^_a-zA-Z0-9]");
        Regex whiteSpace = new(@"(?<=\s)");
        Regex startsWithLowerCaseChar = new("^[a-z]");
        Regex firstCharFollowedByUpperCasesOnly = new("(?<=[A-Z])[A-Z0-9]+$");
        Regex lowerCaseNextToNumber = new("(?<=[0-9])[a-z]");
        Regex upperCaseInside = new("(?<=[A-Z])[A-Z]+?((?=[A-Z][a-z])|(?=[0-9]))");

        // replace white spaces with undescore, then replace all invalid chars with empty string
        var pascalCase = invalidCharsRgx.Replace(whiteSpace.Replace(input, "_"), string.Empty)
            // split by underscores
            .Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
            // set first letter to uppercase
            .Select(w => startsWithLowerCaseChar.Replace(w, m => m.Value.ToUpper()))
            // replace second and all following upper case letters to lower if there is no next lower (ABC -> Abc)
            .Select(w => firstCharFollowedByUpperCasesOnly.Replace(w, m => m.Value.ToLower()))
            // set upper case the first lower case following a number (Ab9cd -> Ab9Cd)
            .Select(w => lowerCaseNextToNumber.Replace(w, m => m.Value.ToUpper()))
            // lower second and next upper case letters except the last if it follows by any lower (ABcDEf -> AbcDef)
            .Select(w => upperCaseInside.Replace(w, m => m.Value.ToLower()));

        return string.Concat(pascalCase);
    }
}
