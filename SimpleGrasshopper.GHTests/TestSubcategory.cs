using SimpleGrasshopper.Attributes;
using SimpleGrasshopper.Data;

namespace SimpleGrasshopper.GHTests;

[SubCategory("测试子集")]
internal class TestSubcategory
{
    [Icon("SuperHelperIcon_256.png")]
    [DocObj("测试", "测", "嘤嘤嘤")]
    private static void TestMethod(
    [DocObj("秋水", "水", "嗷w")] int a,
    int b, out int c, List<int> e = null)
    {
        c = a + b;
    }

    [DocObj("测试2", "测", "嘤嘤嘤")]
    private static void AnotherTest(string hello, [Angle]double angle, double number, EnumTest e, out string ok)
    {
        ok = "alright";
    }

    [Obsolete, Exposure(Grasshopper.Kernel.GH_Exposure.secondary)]
    [DocObj("测试3", "测", "嘤嘤嘤")]
    private static void PathTest(string hello, [Param(ParamGuids.FilePath)]string path)
    {
    }
}

public enum EnumTest
{
    First,
    Second,
}
