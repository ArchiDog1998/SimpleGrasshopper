using SimpleGrasshopper.Attributes;
using System.Drawing;
using System.Windows.Forms;

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
    [Setting, Config("My value", parent: "Major Bool", section: 1)]
    public static int _value = 0;

    [Setting]
    private static readonly EnumTesting _enumTest;

    [Setting, Config("My Time", parent: "EnumValue")]
    private static readonly DateTime _time = DateTime.Now;

    [Config("EnumValue")]
    public static EnumTesting Test { get; set; }

    [Config("Button")]
    public static object Button
    {
        get => false;
        set
        {
            MessageBox.Show("Clicked");
        }
    }

    #region For the case you want your property can be reset. not suggested.
    public static event Action<object>? OnTestChanged;
    public static void ResetTest()
    {
        Test = EnumTesting.What;
        OnTestChanged?.Invoke(Test);
    }
    #endregion
}

public enum EnumTesting : byte
{
    What,
    Why,
    How,
}