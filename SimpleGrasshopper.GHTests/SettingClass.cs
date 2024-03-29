﻿using SimpleGrasshopper.Attributes;
using SimpleGrasshopper.Util;
using System.ComponentModel;

namespace SimpleGrasshopper.GHTests;

internal partial class SettingClass
{
    [DocData]
    private static readonly Dictionary<int, int?[]> _dictTest = new()
    {
        {5, [6, null] },
    };

    [DocData]
    private static readonly string? _testingValue = "Hello";

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

    [Setting, Config("EnumValue")]
    public static EnumTesting _test = EnumTesting.Why;

    [Shortcut(Keys.E | Keys.Control, "Ctrl+E")]
    [Config("Button", parent: "EnumValue.Why")]
    public static object Button
    {
        get => false;
        set
        {
            var v = DictTest;
            var a = typeof(SettingClass).Assembly.GetString("testing.txt");
             MessageBox.Show("Clicked");
        }
    }
}

public enum EnumTesting : byte
{
    [Description("No. 1")]
    What,
    [Icon("b7798b74-037e-4f0c-8ac7-dc1043d093e0")]
    Why,
    How,
}