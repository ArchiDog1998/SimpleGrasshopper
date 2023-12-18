using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace SimpleGrasshopper.SourceGenerators;

[Generator(LanguageNames.CSharp)]
internal class ParameterClassGenerator : ClassGenerator<TypeDeclarationSyntax>
{
    protected override void Execute(SourceProductionContext context, ImmutableArray<(TypeDeclarationSyntax, SemanticModel)> syntaxes)
    {
        foreach (var (syntax, model) in syntaxes)
        {
            var nameSpace = AssemblyPriorityGenerator.GetParent<BaseNamespaceDeclarationSyntax>(syntax)?.Name.ToString() ?? "Null";

            var className = syntax.Identifier.Text;

            var guidStr = Utils.GetGuid(nameSpace, className);
            var interfaces = string.Empty;  
            var interfacesBody = string.Empty;

            var codeClassName = $"{className}_Parameter";

            //Obsolete
            if (syntax.IsObsolete())
            {
                codeClassName += "_Obsolete";
            }

            var typeSymbol = model.GetDeclaredSymbol(syntax) as ITypeSymbol;
            //IGH_PreviewObject
            if (typeSymbol?.AllInterfaces.Any(i => i.GetFullMetadataName() == "SimpleGrasshopper.Data.IPreviewData") ?? false)
            {
                interfaces += ", IGH_PreviewObject";
                interfacesBody += "\n" +
                $$"""
                #region IGH_PreviewObject
                        public bool Hidden { get; set; }
    
                        public bool IsPreviewCapable => true;
                    
                        private readonly static FieldInfo _field = typeof(GH_Param<SimpleGoo<{{className}}>>).GetRuntimeFields().First(f => f.Name == "m_clippingBox");
                    
                        public BoundingBox ClippingBox
                        {
                            get
                            {
                                var box = (BoundingBox)_field.GetValue(this)!;
                                if (box.IsValid) return box;
                    
                                box = BoundingBox.Empty;
                                if (m_data.IsEmpty)
                                {
                                    _field.SetValue(this, box);
                                    return box;
                                }
                    
                                foreach (var branch in m_data.Branches)
                                {
                                    foreach (var item in branch)
                                    {
                                        if (item != null && item.IsValid && item.Value is IPreviewData iPreviewData)
                                        {
                                            BoundingBox clippingBox = iPreviewData.ClippingBox;
                                            if (clippingBox.IsValid)
                                            {
                                                box.Union(clippingBox);
                                            }
                                        }
                                    }
                                }
                    
                                _field.SetValue(this, box);
                                return box;
                            }
                        }
                    
                        public void DrawViewportMeshes(IGH_PreviewArgs args)
                        {
                            if (m_data.IsEmpty || Locked)
                            {
                                return;
                            }

                            var selected = base.Attributes.GetTopLevel.Selected;
                            GH_PreviewMeshArgs args2 = selected
                                ? new (args.Viewport, args.Display, args.ShadeMaterial_Selected, args.MeshingParameters) 
                                : new (args.Viewport, args.Display, args.ShadeMaterial, args.MeshingParameters);
                    
                            foreach (var branch in m_data.Branches)
                            {
                                foreach (var item in branch)
                                {
                                    if (item != null && item.Value is IPreviewData iPreviewData)
                                    {
                                        iPreviewData.DrawViewportMeshes(args2, selected);
                                    }
                                }
                            }
                        }
                    
                        public void DrawViewportWires(IGH_PreviewArgs args)
                        {
                            if (m_data.IsEmpty || Locked)
                            {
                                return;
                            }

                            var selected = base.Attributes.GetTopLevel.Selected;
                            GH_PreviewWireArgs args2 = selected
                                ? new (args.Viewport, args.Display, args.WireColour_Selected, args.DefaultCurveThickness)
                                : new (args.Viewport, args.Display, args.WireColour, args.DefaultCurveThickness);
                    
                            foreach (var branch in m_data.Branches)
                            {
                                foreach (var item in branch)
                                {
                                    if (item != null && item.Value is IPreviewData iPreviewData)
                                    {
                                        iPreviewData.DrawViewportWires(args2, selected);
                                    }
                                }
                            }
                        }
                #endregion
                """;
            }
            //IGH_BakeAwareData
            if (typeSymbol?.AllInterfaces.Any(i => i.GetFullMetadataName() == "Grasshopper.Kernel.IGH_BakeAwareData") ?? false)
            {
                interfaces += ", IGH_BakeAwareObject";
                interfacesBody += "\n" +
                    """
                    #region IGH_BakeAwareObject
                        public bool IsBakeCapable => !m_data.IsEmpty;

                        public void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids)
                        {
                            BakeGeometry(doc, null, obj_ids);
                        }
                    
                        public void BakeGeometry(RhinoDoc doc, ObjectAttributes? att, List<Guid> obj_ids)
                        {
                            GH_BakeUtility gH_BakeUtility = new GH_BakeUtility(OnPingDocument());
                            foreach (var branch in m_data.Branches)
                            {
                                foreach(var item in branch)
                                {
                                    gH_BakeUtility.BakeObject(item.Value, att, doc);
                                }
                            }
                            obj_ids.AddRange(gH_BakeUtility.BakedIds);
                        }
                    #endregion
                    """;
            }

            var code = $$"""
             using Grasshopper.Kernel;
             using Rhino;
             using Rhino.DocObjects;
             using Rhino.Geometry;
             using SimpleGrasshopper.Data;
             using SimpleGrasshopper.DocumentObjects;
             using System;
             using System.Linq;
             using System.Reflection;

             namespace {{nameSpace}}
             {
                 public partial class {{codeClassName}}()
                     : TypeParameter<{{className}}>(){{interfaces}}
                 {
                     public override Guid ComponentGuid => new ("{{guidStr}}");
             {{interfacesBody}}
                 }
             }
             """;

            context.AddSource($"{codeClassName}.g.cs", code);
        }
    }
}
