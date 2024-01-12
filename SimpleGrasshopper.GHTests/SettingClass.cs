using SimpleGrasshopper.Attributes;
using System.ComponentModel;

namespace SimpleGrasshopper.GHTests;

internal partial class SettingClass
{
    [DocData]
    private static readonly Dictionary<int, string> _dictTest = [];

    [DocData]
    private static readonly string _testingValue = "Hello";

    [DocData]
    private static readonly EnumTesting _testingEnum = EnumTesting.How;

    [Setting]
    private static readonly TypeTest _testingData = null!;

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

    [Shortcut(Keys.Q | Keys.Control, "Ctrl+Q")]
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
    public static event Action<EnumTesting>? OnTestChanged;
    public static void ResetTest()
    {
        Test = EnumTesting.What;
        OnTestChanged?.Invoke(Test);
    }
    #endregion
}

public enum EnumTesting : byte
{
    [Description("No. 1")]
    What,
    Why,
    How,
}