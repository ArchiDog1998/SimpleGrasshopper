namespace SimpleGrasshopper.Util;

/// <summary>
/// The assembly priority for adding category icon.
/// </summary>
public abstract class AssemblyPriority : GH_AssemblyPriority
{
    /// <inheritdoc/>
    public override GH_LoadingInstruction PriorityLoad()
    {
        var assembly = GetType().Assembly;
        var icon = assembly.GetAssemblyIcon();
        if (icon != null)
        {
            Instances.ComponentServer.AddCategoryIcon(assembly.GetAssemblyName(), icon);
        }
        return GH_LoadingInstruction.Proceed;
    }
}
