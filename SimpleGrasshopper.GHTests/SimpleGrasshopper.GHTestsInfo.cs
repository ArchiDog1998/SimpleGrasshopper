using Grasshopper.GUI;
using Grasshopper.Kernel;
using System.Drawing;

namespace SimpleGrasshopper.GHTests;

public class SimpleGrasshopper_GHTestsInfo : GH_AssemblyInfo
{
    public override string Name => "SimpleGrasshopperTesting";

    private Bitmap? _icon;
    //Return a 24x24 pixel bitmap to represent this GHA library.
    public override Bitmap Icon
    {
        get
        {
            if (_icon != null) return _icon;

            var assembly = GetType().Assembly;
            var name = assembly.GetManifestResourceNames().FirstOrDefault();
            if (name == null) return base.Icon;
            using var stream = assembly.GetManifestResourceStream(name);
            if (stream == null) return base.Icon;
            return _icon = new(stream);
        }
    }

    //Return a short string describing the purpose of this GHA library.
    public override string Description => "";

    public override Guid Id => new("8bc4c536-97be-4160-8f39-3eb65ba1f5a8");

    //Return a string identifying you or your company.
    public override string AuthorName => "";

    //Return a string representing your preferred contact details.
    public override string AuthorContact => "";
}

partial class SimpleAssemblyPriority
{
    protected override int? MenuIndex => 4;

    protected override void DoWithEditor(GH_DocumentEditor editor)
    {
        base.DoWithEditor(editor);
        CustomShortcutClicked += SimpleAssemblyPriority_CustomShortcutClicked;
        CustomShortcuts[Keys.A] = () => MessageBox.Show("ShortcutTest!");
    }

    private bool SimpleAssemblyPriority_CustomShortcutClicked(Keys arg)
    {
        if (arg != Keys.B) return false;
        MessageBox.Show("Clicked!");
        return true;
    }
}