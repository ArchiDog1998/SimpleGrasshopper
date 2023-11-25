using SimpleGrasshopper.Attributes;
using System.Drawing;

namespace SimpleGrasshopper.GHTests
{
    internal partial class SettingClass
    {
        [Setting, Config("Major Bool")]
        private static readonly bool firstSetting = true;

        [Setting, Config("A color")]
        private static readonly Color secondSetting = Color.AliceBlue;

        [Setting, Config("One Setting", parent: "Major Bool")]
        private static readonly string anotherSetting = default!;

        [Config("My value", parent: "Major Bool")]
        public static int Value { get; set; }
    }
}