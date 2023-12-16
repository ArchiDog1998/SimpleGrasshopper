using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Render;
using SimpleGrasshopper.Attributes;
using SimpleGrasshopper.Data;
using System.Reflection;

namespace SimpleGrasshopper.GHTests;

[TypeComponent("Type Methods", "methods")]
[PropertyComponent]
[Icon("CurveRenderAttributeParameter_24-24.png")]
[DocObj("My type", "just a type", "Testing type.")]
public class TypeTest : IPreviewData, IGH_BakeAwareData
{
    [DocObj("Value", "V", "")]
    public int FirstValue { get; set; }

    public BoundingBox ClippingBox => new (0,0,0,1,1,1);

    [DocObj("Add Property", "A P", "Testing")]
    public void AddValue(int value)
    {
        FirstValue += value;
    }

    public void AppendRenderGeometry(GH_RenderArgs args, RenderMaterial material)
    {
        var method = args.GetType().GetRuntimeMethods().First(m => m.Name == "AddGeometry");
        method.Invoke(args, [Mesh.CreateFromBox(ClippingBox, 1, 1, 1), material]);
    }

    public bool BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid obj_guid)
    {
        obj_guid = doc.Objects.AddBox(new Box(ClippingBox));
        return true;
    }

    public void DrawViewportMeshes(GH_PreviewMeshArgs args, bool selected)
    {
    }

    public void DrawViewportWires(GH_PreviewWireArgs args, bool selected)
    {
        args.Pipeline.DrawBox(ClippingBox, selected ? args.Color : System.Drawing.Color.White);
    }

    [DocObj("Reduce Property", "R P", "Testing")]
    public void ReduceValue(int value)
    {
        FirstValue -= value;
    }

    public static explicit operator int(TypeTest _) => 10;
    public static implicit operator double (TypeTest _) => 20;

    public static explicit operator TypeTest(int i) => new() { FirstValue = i};
    public static implicit operator TypeTest(double d) => new() { FirstValue = (int)d };
}