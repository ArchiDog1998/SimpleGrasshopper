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
    static (n, ct) => ((VariableDeclaratorSyntax)n.TargetNode, n.SemanticModel))
    .Where(m => m.Item1 != null);
        context.RegisterSourceOutput(provider.Collect(), Execute);
    }

    private void Execute(SourceProductionContext context, ImmutableArray<(VariableDeclaratorSyntax, SemanticModel SemanticModel)> array)
    {
        var typeGrps = array.GroupBy(variable => variable.Item1.Parent!.Parent!.Parent!);

        foreach (var grp in typeGrps)
        {
            var type = (TypeDeclarationSyntax)grp.Key;

            var nameSpace = AssemblyPriorityGenerator.GetParent<BaseNamespaceDeclarationSyntax>(type)?.Name.ToString() ?? "Null";

            var classType = type is ClassDeclarationSyntax ? "class" : "struct";

            var className = type.Identifier.Text;

            var propertyCodes = new List<string>();
            foreach (var (variableInfo, model) in grp)
            {
                var field = (FieldDeclarationSyntax)variableInfo.Parent!.Parent!;

                var variableName = variableInfo.Identifier.ToString();
                var propertyName = ToPascalCase(variableName);

                if (variableName == propertyName)
                {
                    context.DiagnosticWrongName(variableInfo.Identifier.GetLocation(),
                        "Please don't use Pascal Case to name your field!");
                    continue;
                }

                if (!field.Modifiers.Any(SyntaxKind.StaticKeyword))
                {
                    context.DiagnosticWrongKeyword(variableInfo.Identifier.GetLocation(), "The field should be a static method!");
                    continue;
                }

                var key = string.Join(".", nameSpace, className, propertyName);

                var fieldTypeStr = field.Declaration.Type;
                var fieldType = model.GetTypeInfo(fieldTypeStr).Type!;

                if (!IsFieldTypeValid(fieldType))
                {
                    context.DiagnosticWrongType(fieldTypeStr.GetLocation(),
                        "This type can't be a grasshopper setting type!");
                    continue;
                }

                var names = new List<string>();
                foreach (var attrSet in field.AttributeLists)
                {
                    if (attrSet == null) continue;
                    foreach (var attr in attrSet.Attributes)
                    {
                        if (model.GetSymbolInfo(attr).Symbol?.GetFullMetadataName()
                            is "SimpleGrasshopper.Attributes.ConfigAttribute"
                            or "SimpleGrasshopper.Attributes.RangeAttribute"
                            or "SimpleGrasshopper.Attributes.ToolButtonAttribute")
                        {
                            names.Add(attr.ToString());
                        }
                    }
                }

                string getValueStr, setValueStr;
                if (fieldType.TypeKind == TypeKind.Enum)
                {
                    getValueStr = $"({fieldTypeStr})Enum.ToObject(typeof({fieldTypeStr}), Instances.Settings.GetValue(\"{key}\", Convert.ToInt32({variableName})))";
                    setValueStr = $"Instances.Settings.SetValue(\"{key}\", Convert.ToInt32(value))";
                }
                else
                {
                    getValueStr = $"Instances.Settings.GetValue(\"{key}\", {variableName})";
                    setValueStr = $"Instances.Settings.SetValue(\"{key}\", value)";
                }

                var attributeStr = names.Count == 0 ? "" : $"[{string.Join(", ", names)}]";
                var propertyCode = $$"""
                        {{attributeStr}}
                        public static {{fieldTypeStr}} {{propertyName}}
                        {
                            get => {{getValueStr}};
                            set
                            {
                                if ({{propertyName}} == value) return;
                                {{setValueStr}};

                                On{{propertyName}}Changed?.Invoke(value);
                                OnPropertyChanged?.Invoke("{{propertyName}}", value);
                            }
                        }

                        public static event Action<{{fieldTypeStr}}> On{{propertyName}}Changed;

                        public static void Reset{{propertyName}}()
                        {
                            {{propertyName}} = {{variableName}};
                        }
                """;

                propertyCodes.Add(propertyCode);
            }

            var code = $$"""
             using Grasshopper;
             using System;
             using System.Drawing;
             using SimpleGrasshopper.Attributes;

             namespace {{nameSpace}}
             {
                 partial {{classType}} {{className}}
                 {
             {{string.Join("\n \n", propertyCodes)}}

                     public static event Action<string, object> OnPropertyChanged;
                 }
             }
             """;

            context.AddSource($"{nameSpace}_{className}.g.cs", code);
        }
    }

    private static readonly string[] _validTypes =
        [
            "System.Drawing.Color",
            "System.Drawing.Point",
            "System.Drawing.Rectangle",
            "System.Drawing.Size",
        ];
    private static bool IsFieldTypeValid(ITypeSymbol typeSymbol)
    {
        if (typeSymbol.TypeKind == TypeKind.Enum) return true;

        var typeName = typeSymbol.GetFullMetadataName();
        if (typeSymbol.SpecialType
            is SpecialType.System_Boolean
            or SpecialType.System_Byte
            or SpecialType.System_Double
            or SpecialType.System_DateTime
            or SpecialType.System_Int32
            or SpecialType.System_String)
        {
            return true;
        }

        foreach (var validType in _validTypes)
        {
            if (typeName == validType)
            {
                return true;
            }
        }
        return false;
    }

    private static string ToPascalCase(string input)
    {
        return string.Join(".", input.Split('.').Select(ConvertToPascalCase));

        static string ConvertToPascalCase(string input)
        {
            Regex invalidCharsRgx = new(@"[^_a-zA-Z0-9]");
            Regex whiteSpace = new(@"(?<=\s)");
            Regex startsWithLowerCaseChar = new("^[a-z]");
            Regex firstCharFollowedByUpperCasesOnly = new("(?<=[A-Z])[A-Z0-9]+$");
            Regex lowerCaseNextToNumber = new("(?<=[0-9])[a-z]");
            Regex upperCaseInside = new("(?<=[A-Z])[A-Z]+?((?=[A-Z][a-z])|(?=[0-9]))");

            var pascalCase = invalidCharsRgx.Replace(whiteSpace.Replace(input, "_"), string.Empty)
                .Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => startsWithLowerCaseChar.Replace(w, m => m.Value.ToUpper()))
                .Select(w => firstCharFollowedByUpperCasesOnly.Replace(w, m => m.Value.ToLower()))
                .Select(w => lowerCaseNextToNumber.Replace(w, m => m.Value.ToUpper()))
                .Select(w => upperCaseInside.Replace(w, m => m.Value.ToLower()));

            return string.Concat(pascalCase);
        }
    }
}
