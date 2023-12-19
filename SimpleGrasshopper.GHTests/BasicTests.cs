using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using SimpleGrasshopper.Attributes;
using SimpleGrasshopper.Data;

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
        var  result = a + b;
        c += result;
        return result;
    }

    [Message("Hello")]
    [Icon("https://raw.githubusercontent.com/ArchiDog1998/WatermarkPainter/master/WatermarkIcon512.png")]
    [DocObj("Addition2", "Add2", "The addition of the doubles.")]
    public static double Add(double a, double b) => a + b;

    [DocObj("DataType test", "Data Type", "A test for data type.")]
    public static RuntimeData DataTypeTest(int a, 
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


    [DocObj("Type Testing", "T", "Testing for my type")]
    private static void MyTypeTest(
        GH_Structure<SimpleGoo<TypeTest>> type, 
        GH_Structure<GH_Boolean> bools)
    {

    }
}

public enum EnumTest : byte
{
    First,
    Second,
}
