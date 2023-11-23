using SimpleGrasshopper.Attributes;
using SimpleGrasshopper.Data;
using SimpleGrasshopper.Util;
using System.Drawing;

namespace SimpleGrasshopper.DocumentObjects;

public abstract class TypeParameter<T>() 
    : GH_PersistentParam<SimpleGoo<T>>(typeof(T).GetDocObjName(),
                   typeof(T).GetDocObjNickName(),
                   typeof(T).GetDocObjDescription(),
                   typeof(T).GetAssemblyName(),
                   "Parameters")
{
    public override GH_Exposure Exposure => typeof(T).GetCustomAttribute<ExposureAttribute>()?.Exposure ?? base.Exposure;

    private Bitmap? _icon;
    protected override Bitmap Icon
    {
        get
        {
            if (_icon != null) return _icon;
            var path = typeof(T).GetCustomAttribute<IconAttribute>()?.IconPath;
            if (path == null) return base.Icon;

            var assembly = GetType().Assembly;
            var name = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(path));
            if (name == null) return base.Icon;
            using var stream = assembly.GetManifestResourceStream(name);
            if (stream == null) return base.Icon;

            try
            {
#pragma warning disable CA1416 // Validate platform compatibility
                return _icon = new(stream);
#pragma warning restore CA1416 // Validate platform compatibility
            }
            catch
            {
                return base.Icon;
            }
        }
    }

    protected override GH_GetterResult Prompt_Plural(ref List<SimpleGoo<T>> values)
    {
        return GH_GetterResult.cancel;
    }

    protected override GH_GetterResult Prompt_Singular(ref SimpleGoo<T> value)
    {
        return GH_GetterResult.cancel;
    }
}