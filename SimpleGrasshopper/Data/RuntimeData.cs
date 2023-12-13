namespace SimpleGrasshopper.Data;

/// <summary>
/// the runtime data during the compute.
/// </summary>
/// <param name="message"></param>
/// <param name="runtimeMessages"></param>
public readonly struct RuntimeData(string? message, List<RuntimeMessage> runtimeMessages)
{
    internal string? Message => message;
    internal List<RuntimeMessage> RuntimeMessages => runtimeMessages;
}
