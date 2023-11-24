using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using SimpleGrasshopper.Attributes;
using SimpleGrasshopper.Data;
using System.Drawing.Imaging;

namespace SimpleGrasshopper.GHTests;

[SubCategory("Just a test")]
internal class SimpleSubcategory
{
    [Icon("ConstructRenderItemComponent_24-24.png")] // The name of the png that is embedded in your dll.
    [Exposure(Grasshopper.Kernel.GH_Exposure.secondary)]
    [DocObj("Addition", "Add", "The addition of the integers.")]
    private static void SimpleMethod(int a, int b, out int c)
    {
        c = a + b;
    }

    [DocObj("Addition2", "Add2", "The addition of the integers2.")]
    private static void SimpleMethod(double abc, double werb, out double dfa)
    {
        dfa = abc + werb;
    }

    [DocObj("Special Param", "Spe", "Special Params")]
    private static void ParamTest(
        [DocObj("Name", "N", "The name of sth.")] string name,
        [Param(ParamGuids.FilePath)] string path,
        [Angle] out double angle)
    {
        angle = Math.PI;
    }

    [DocObj("Enum Param", "Enu", "Enum testing")]
    private static void EnumTypeTest(out EnumTest type, EnumTest input = EnumTest.First)
    {
        type = EnumTest.First;
    }

    [DocObj("Type Testing", "T", "Testing for my type")]
    private static void MyTypeTest(GH_Structure<SimpleGoo< TypeTest>> type, GH_Structure<GH_Boolean> bools)
    {

    }

    [Exposure(Grasshopper.Kernel.GH_Exposure.secondary)]
    [DocObj("Test", "T", "Ttt")]

    private static void Test(bool a)
    {

    }

    [DocObj("Test2", "T2", "Ttt")]

    private static void Test(out bool a)
    {
        a = false;
    }
}

public enum EnumTest : byte
{
    First,
    Second,
}
