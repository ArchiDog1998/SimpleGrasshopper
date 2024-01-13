using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace SimpleGrasshopper.SourceGenerators;

[Generator(LanguageNames.CSharp)]

public class DocDataAttributeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName
            ("SimpleGrasshopper.Attributes.DocDataAttribute",
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
                var typeSymbol = model.GetDeclaredSymbol(type) as ITypeSymbol;

                //GH_ISerializable
                if (typeSymbol?.AllInterfaces.Any(i => i.GetFullMetadataName() == "GH_IO.GH_ISerializable") ?? false)
                {
                    continue;
                }

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

                if (!SettingClassGenerator.IsFieldTypeValid(fieldType))
                {
                    fieldStr = fieldType.GetFullMetadataName();
                    getValueStr = $"AssemblyPriority.GetDocument()?.ValueTable.GetValue(\"{key}\", null) is string str && !string.IsNullOrEmpty(str) ? IOHelper.DeserializeObject<{fieldStr}>(str) : {variableName}";
                    setValueStr = $"AssemblyPriority.GetDocument()?.ValueTable.SetValue(\"{key}\", IOHelper.SerializeObject(value))";
                }
                else if (fieldType.TypeKind == TypeKind.Enum)
                {
                    fieldStr = fieldType.GetFullMetadataName();
                    getValueStr = $"({fieldStr})Enum.ToObject(typeof({fieldStr}), AssemblyPriority.GetDocument()?.ValueTable.GetValue(\"{key}\", Convert.ToInt32({variableName})) ?? default!)";
                    setValueStr = $"AssemblyPriority.GetDocument()?.ValueTable.SetValue(\"{key}\", Convert.ToInt32(value))";
                }
                else
                {
                    getValueStr = $"AssemblyPriority.GetDocument()?.ValueTable.GetValue(\"{key}\", {variableName}) ?? default!";
                    setValueStr = $"AssemblyPriority.GetDocument()?.ValueTable.SetValue(\"{key}\", value)";
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
                                OnDataPropertyChanged?.Invoke("{{propertyName}}", value);
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

            if (propertyCodes.Count == 0) continue;

            var code = $$"""
             using Grasshopper;
             using Grasshopper.Kernel;
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

                     public static event Action<string, object> OnDataPropertyChanged;
                 }
             }
             """;

            context.AddSource($"{nameSpace}_{className}.g.cs", code);
        }
    }
}
