namespace SimpleGrasshopper.Attributes;

/// <summary>
/// Set the <see cref="GH_Component.Message"/> by default.
/// </summary>
/// <param name="message"></param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct)]
public class MessageAttribute(string message) : Attribute
{
    internal string Message => message;
}
