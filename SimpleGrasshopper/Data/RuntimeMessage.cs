namespace SimpleGrasshopper.Data;

/// <summary>
/// The runtime message in runtime.
/// </summary>
/// <param name="level"></param>
/// <param name="message"></param>
public readonly struct RuntimeMessage(GH_RuntimeMessageLevel level, string message)
{
    internal GH_RuntimeMessageLevel Level => level;
    internal string Message => message;
}
