using SimpleGrasshopper.Attributes;
using System.Drawing;

namespace SimpleGrasshopper.GHTests;

internal static partial class SettingClass
{
    [GH_Setting]
    private static readonly bool firstSetting = true;

    [GH_Setting]
    private static readonly Color secondSetting = Color.AliceBlue;
}

internal readonly partial struct SettingStruct
{
    [GH_Setting]
    private static readonly string anotherSetting = default!;
}
