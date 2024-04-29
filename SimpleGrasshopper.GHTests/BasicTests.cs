using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using SimpleGrasshopper.Attributes;
using SimpleGrasshopper.Data;
using SimpleGrasshopper.DocumentObjects;
using System.ComponentModel;

namespace SimpleGrasshopper.GHTests;

[SubCategory("Basic Tests")]
internal static class BasicTests
{
    [Icon("ConstructRenderItemComponent_24-24.png")]
    [Exposure(GH_Exposure.secondary)]
    [DocObj("Addition", "Add", "The addition of the integers.")]
    [return: DocObj("Added Result", "A R", "Result")]
    public static int Add(
        [DocObj("A", "A", "A"), Range(0, 5)] int a,
        [DocObj("B", "B", "B"), Range(double.NegativeInfinity, 0)] int b,
        [DocObj("C", "C", "C")] ref int c)
    {
        var result = a + b;
        c += result;
        return result;
    }

    [Message("Hello")]
    [Icon("https://raw.githubusercontent.com/ArchiDog1998/WatermarkPainter/master/WatermarkIcon512.png")]
    [DocObj("Addition2", "Add2", "The addition of the doubles.")]
    public static double Add(double a, double b) => a + b;

    [DocObj("DataType test", "Data Type", "A test for data type.")]
    public static RuntimeData DataTypeTest(
        [Range(0, 5)] int a,
        [Range(0, 5)] List<int> b,
        [Range(0, 5)] GH_Structure<GH_Integer> c)
    {
        return new RuntimeData("Hi", [new RuntimeMessage(GH_RuntimeMessageLevel.Warning, "Haha")]);
    }

    [DocObj("Special Param", "Spe", "Special Params")]
    private static void ParamTest(
    [DocObj("Name", "N", "The name of sth.")] string name,
    [Param(ParamGuids.FilePath)] string path,
    [Angle] ref double angle)
    {
    }

    [DocObj("Enum Param", "Enu", "Enum testing")]
    private static void EnumTypeTest(out EnumTest type,
        EnumTest? input = null,
        EnumTest?[] input1 = null!,
        GH_Structure<SimpleGoo<EnumTest?>> input2 = null!)
    {
        type = input ?? EnumTest.First;
    }

    private static GH_Structure<GH_Boolean>? _testingPD = null;
    private static GH_Structure<GH_Boolean> TestingPD
    {
        get
        {
            if (_testingPD != null) return _testingPD;
            _testingPD = new();
            _testingPD.Append(new GH_Boolean(true));
            _testingPD.Append(new GH_Boolean(false));
            _testingPD.Append(new GH_Boolean(false));
            return _testingPD;
        }
    }

    private static bool[] TestingArray = [false, true, true];
    private static bool TestingItem = false;

    [DocObj("Type Testing", "T", "Testing for my type")]
    private static void MyTypeTest(
        GH_Structure<SimpleGoo<ITypeTest>> type,
        [PersistentData(nameof(TestingItem))] GH_Structure<GH_Boolean> bools,
        out ITypeTest typeTest)
    {
        typeTest = new SubTypeTest();
    }

    [DocObj("Create Type", "C T", "Create a new type")]
    private static ITypeTest CreateTest(ref string __Message, MethodComponent ___instance, int number)
    {
        var t = ___instance.GetType();

        __Message = "123";
        return new TypeTest();
    }

    [BaseComponent("SimpleGrasshopper.GHTests.TestComponent")]
    [DocObj("Save", "S", "S")]

    private static GH_Surface SaveTest(MethodComponent ___instance, Curve srf)
    {
        var t = ___instance.GetType();
        return null!;
    }

    private static void SaveTest(MethodComponent ___instance, out string output, string input = null!)
    {
        //SettingClass.RecordTestingData(___instance.OnPingDocument());
        //var data = SettingClass.TestingData;
        //data.FirstValue = 20;
        //SettingClass.TestingData = data;
        output = SettingClass.TestingData.FirstValue.ToString();
    }

    private static Color defaultcolor = Color.White;

    [DocObjAttr("SimpleGrasshopper.GHTests.MyAttr")]
    [DocObj("Tag", "T", "T")]
    private static void TagTest(
        [ParamTag(true, GH_DataMapping.Flatten, true, true)] int a,
        [PersistentData(nameof(defaultcolor))] Color color)
    {

    }

    [DocObj("Range", "R", "R")]
    public static void Test(double angle, GH_Rectangle rect, double centerX, double centerY, out double x, out double y)
    {
        var xMin = rect.Boundingbox.Min.X;
        var yMin = rect.Boundingbox.Min.Y;
        var xMax = rect.Boundingbox.Max.X;
        var yMax = rect.Boundingbox.Max.Y;

        var cos = Math.Cos(angle);
        var xRange = cos > 0 ? (xMax - centerX) : (centerX - xMin);
        var sin = Math.Sin(angle);
        var yRange = sin > 0 ? (yMax - centerY) : (centerY - yMin);

        var angleForPos = Math.Atan2(sin * xRange, cos * yRange);

        x = centerX + Math.Cos(angleForPos) * xRange;
        y = centerY + Math.Sin(angleForPos) * yRange;

        x = Math.Min(Math.Max(x, xMin), xMax);
        y = Math.Min(Math.Max(y, yMin), yMax);
    }
}

public class MyAttr(IGH_Component component) 
    : GH_ComponentAttributes(component)
{
    protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
    {
        base.Render(canvas, graphics, channel);

        if (channel == GH_CanvasChannel.Objects)
        {
            graphics.DrawRectangle(new Pen(new SolidBrush(Color.Black)), Bounds);
        }
    }
}

public enum EnumTest : byte
{
    [Description("No. 1")]
    First,
    Second,
}
