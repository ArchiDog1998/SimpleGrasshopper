using Rhino.Geometry;

namespace SimpleGrasshopper.Data;

/// <summary>
/// For the case that your type needs to be previewed in the canvas.
/// </summary>
public interface IPreviewData
{
    /// <summary>
    /// clipping box
    /// </summary>
    BoundingBox ClippingBox { get; }

    /// <summary>
    /// Draw meshes.
    /// </summary>
    /// <param name="args"></param>
    /// <param name="selected">is this item be selected.</param>
    void DrawViewportMeshes(GH_PreviewMeshArgs args, bool selected);

    /// <summary>
    /// Draw wires
    /// </summary>
    /// <param name="args"></param>
    /// <param name="selected">is this item be selected.</param>
    void DrawViewportWires(GH_PreviewWireArgs args, bool selected);
}
