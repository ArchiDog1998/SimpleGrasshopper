using SimpleGrasshopper.Attributes;

namespace SimpleGrasshopper.GHTests;

[TypeComponent("Type Methods", "methods")]
[PropertyComponent]
[Icon("CurveRenderAttributeParameter_24-24.png")]
[DocObj("My type", "just a type", "Testing type.")]
public class TypeTest
{
    [DocObj("Value", "V", "")]
    public int FirstValue { get; set; }

    [DocObj("Add Property", "A P", "Testing")]
    public void AddValue(int value)
    {
        FirstValue += value;
    }

    [DocObj("Reduce Property", "R P", "Testing")]
    public void ReduceValue(int value)
    {
        FirstValue -= value;
    }
}