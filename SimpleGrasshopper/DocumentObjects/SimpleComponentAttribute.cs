using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel.Attributes;

namespace SimpleGrasshopper.DocumentObjects;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class SimpleComponentAttribute(MethodComponent component)
    : GH_ComponentAttributes(component)
{
    private RectangleF _rectangle;

    protected override void Layout()
    {
        base.Layout();

        if(component.AddShouldRun)
        {
            _rectangle = new RectangleF(Bounds.X, Bounds.Bottom + 2, Bounds.Width, 20);

            var bound = Bounds;
            bound.Inflate(-2, -2);
            bound = RectangleF.Union(bound, _rectangle);
            bound.Inflate(2, 2);
            Bounds = bound;
        }
    }

    protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
    {
        base.Render(canvas, graphics, channel);

        if (channel != GH_CanvasChannel.Objects) return;

        if (component.AddShouldRun)
        {
            using var capsule = GH_Capsule.CreateTextCapsule(_rectangle, _rectangle, GH_Palette.Black, "Run", 3, 0);
            capsule.Render(graphics, Selected, Owner.Locked, false);
        }
    }

    public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
    {
        if (e.Button == MouseButtons.Left && _rectangle.Contains(e.CanvasLocation))
        {
            component.ShouldRun = true;
            Owner.Message = DateTime.Now.ToLocalTime().ToString();
            Owner.ExpireSolution(true);
            component.ShouldRun = false;

            return GH_ObjectResponse.Handled;
        }
        return base.RespondToMouseUp(sender, e);
    }
}
