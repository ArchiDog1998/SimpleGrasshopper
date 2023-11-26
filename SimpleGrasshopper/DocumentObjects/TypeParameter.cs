using SimpleGrasshopper.Attributes;
using SimpleGrasshopper.Data;
using SimpleGrasshopper.Util;

namespace SimpleGrasshopper.DocumentObjects;

/// <summary>
/// A simple <see cref="GH_PersistentParam{T}"/> for one object.
/// </summary>
/// <typeparam name="T">the object that it contains.</typeparam>
public abstract class TypeParameter<T>()
    : GH_PersistentParam<SimpleGoo<T>>(typeof(T).GetDocObjName(),
                   typeof(T).GetDocObjNickName(),
                   typeof(T).GetDocObjDescription(),
                   typeof(T).GetAssemblyName(),
                   "Parameters")
{
    /// <inheritdoc/>
    public override GH_Exposure Exposure => typeof(T).GetCustomAttribute<ExposureAttribute>()?.Exposure ?? base.Exposure;

    private Bitmap? _icon;
    /// <inheritdoc/>
    protected override Bitmap Icon
    {
        get
        {
            if (_icon != null) return _icon;
            var path = typeof(T).GetCustomAttribute<IconAttribute>()?.IconPath;
            if (path == null) return base.Icon;

            return _icon = GetType().Assembly.GetBitmap(path) ?? base.Icon;
        }
    }

    /// <inheritdoc/>
    protected override GH_GetterResult Prompt_Plural(ref List<SimpleGoo<T>> values)
    {
        return GH_GetterResult.cancel;
    }

    /// <inheritdoc/>
    protected override GH_GetterResult Prompt_Singular(ref SimpleGoo<T> value)
    {
        return GH_GetterResult.cancel;
    }
}