namespace SimpleGrasshopper.Undo;

/// <summary>
/// What happend after undo.
/// </summary>
[Flags]
public enum AfterUndo : byte
{
    /// <summary>
    /// Nothing to do.
    /// </summary>
    None = 0,

    /// <summary>
    /// <see cref="IGH_DocumentObject.ExpireSolution(bool)"/>
    /// </summary>
    Solution = 1 << 0,

    /// <summary>
    /// <see cref="IGH_DocumentObject.ExpirePreview(bool)"/>
    /// </summary>
    Preview = 1 << 1,

    /// <summary>
    /// <see cref="IGH_Attributes.ExpireLayout"/>
    /// </summary>
    Layout = 1 << 2,

    /// <summary>
    /// <see cref="Control.Refresh"/> of the <see cref="Instances.ActiveCanvas"/>
    /// </summary>
    Refresh = 1 << 3,

    /// <summary>
    /// Combination of <see cref="Layout"/> and <see cref="Refresh"/>
    /// </summary>
    LayoutFresh = Layout | Refresh,
}
