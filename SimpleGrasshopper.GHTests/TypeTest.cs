using SimpleGrasshopper.Attributes;

namespace SimpleGrasshopper.GHTests;

[PropertyComponent]
[Icon("CurveRenderAttributeParameter_24-24.png")]
[DocObj("My type", "just a type", "Testing type.")]
public class TypeTest
{
    [DocObj("Value", "V", "")]
    public int FirstValue { get; set; }
}