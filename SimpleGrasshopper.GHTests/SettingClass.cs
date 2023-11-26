using SimpleGrasshopper.Attributes;
using System.Drawing;

namespace SimpleGrasshopper.GHTests;

internal partial class SettingClass
{
    [Setting, Config("Major Bool")]
    private static readonly bool firstSetting = true;

    [Setting, Config("SimpleGrasshopperTesting")]
    private static readonly bool majorBool = true;

    [Setting, Config("A color")]
    private static readonly Color secondSetting = Color.AliceBlue;

    [Setting, Config("One Setting", parent: "Major Bool")]
    private static readonly string anotherSetting = string.Empty;

    [Range(0, 10, 0)]
    [Setting, Config("My value", parent: "Major Bool")]
    public static int _value;
}