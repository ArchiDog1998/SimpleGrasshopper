using SimpleGrasshopper.Attributes;
using System.Drawing;

namespace SimpleGrasshopper.GHTests;

internal partial class SettingClass
{
    [Setting, Config("Major Bool")]
    private static readonly bool firstSetting = true;

    [Setting, Config("SimpleGrasshopperTesting"), 
        ToolButton("b7798b74-037e-4f0c-8ac7-dc1043d093e0")]
    private static readonly bool majorBool = true;

    [Setting, Config("A color")]
    private static readonly Color secondSetting = Color.AliceBlue;

    [Setting, Config("One Setting", parent: "Major Bool")]
    private static readonly string anotherSetting = string.Empty;

    [Range(0, 10, 0)]
    [Setting, Config("My value", parent: "Major Bool")]
    public static int _value;
}