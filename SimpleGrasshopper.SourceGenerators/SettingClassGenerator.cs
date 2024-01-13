using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

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
                var propertyName = variableName.ToPascalCase();

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
                var fieldStr = fieldTypeStr.ToString();

                var names = new List<string>();
                foreach (var attrSet in field.AttributeLists)
                {
                    if (attrSet == null) continue;
                    foreach (var attr in attrSet.Attributes)
                    {
                        if (model.GetSymbolInfo(attr).Symbol?.GetFullMetadataName()
                            is "SimpleGrasshopper.Attributes.ConfigAttribute"
                            or "SimpleGrasshopper.Attributes.RangeAttribute"
                            or "SimpleGrasshopper.Attributes.ShortcutAttribute"
                            or "SimpleGrasshopper.Attributes.ToolButtonAttribute")
                        {
                            names.Add(attr.ToString());
                        }
                    }
                }

                string getValueStr, setValueStr;

                if (!IsFieldTypeValid(fieldType))
                {
                    fieldStr = fieldType.GetFullMetadataName();
                    getValueStr = $"Instances.Settings.GetValue(\"{key}\", null) is string str && !string.IsNullOrEmpty(str) ? IOHelper.DeserializeObject<{fieldStr}>(str) : {variableName}";
                    setValueStr = $"Instances.Settings.SetValue(\"{key}\", IOHelper.SerializeObject(value))";
                }
                else if (fieldType.TypeKind == TypeKind.Enum)
                {
                    fieldStr = fieldType.GetFullMetadataName();
                    getValueStr = $"({fieldStr})Enum.ToObject(typeof({fieldStr}), Instances.Settings.GetValue(\"{key}\", Convert.ToInt32({variableName})))";
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
                        public static {{fieldStr}} {{propertyName}}
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

                        public static event Action<{{fieldStr}}> On{{propertyName}}Changed;

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
             using SimpleGrasshopper.Data;
             using SimpleGrasshopper.Util;

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
    public static bool IsFieldTypeValid(ITypeSymbol typeSymbol)
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

}
