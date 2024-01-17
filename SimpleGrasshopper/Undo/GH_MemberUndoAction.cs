using Grasshopper.Kernel.Undo;
using SimpleGrasshopper.Util;

namespace SimpleGrasshopper.Undo;

/// <summary>
/// The undo action for the member in one obj.
/// </summary>
public class GH_MemberUndoAction : GH_UndoAction
{
    readonly FieldInfo? _field;
    readonly PropertyInfo? _prop;

    Guid _guid = default;
    string _value;
    Type? _type;
    readonly AfterUndo _after;
    readonly Action? _action;

    /// <summary>
    /// Default ctor.
    /// </summary>
    /// <param name="obj">The object that contains this value.</param>
    /// <param name="memberName">the value name.</param>
    /// <param name="after">After undo, what should happend.</param>
    /// <param name="action">The action to do after changing</param>
    public GH_MemberUndoAction(IGH_DocumentObject obj, string memberName, AfterUndo after, Action? action = null)
    {
        _guid = obj.InstanceGuid;
        _field = obj.GetType().GetAllRuntimeFields().FirstOrDefault(f => f.Name == memberName);
        _prop = obj.GetType().GetAllRuntimeProperties().FirstOrDefault(p => p.Name == memberName);
        _value = GetValue(obj);
        _after = after;
        _action = action;
    }

    private string GetValue(IGH_DocumentObject obj)
    {
        var value = _field?.GetValue(obj) ?? _prop?.GetValue(obj);

        if (value == null)
        {
            _type = null;
            return string.Empty;
        }

        _type = value.GetType();
        return IOHelper.SerializeObject(value);
    }

    private void SetValue(GH_Document doc, string str)
    {
        if (_type == null || string.IsNullOrEmpty(str)) return;

        var v = IOHelper.DeserializeObject(str, _type);
        var obj = GetObj(doc);
        _field?.SetValue(obj, v);
        _prop?.SetValue(obj, v);
    }

    private IGH_DocumentObject GetObj(GH_Document doc)
    {
        return doc.FindObject(_guid, false);
    }

    /// <inheritdoc/>
    protected override void Internal_Undo(GH_Document doc)
    {
        var obj = GetObj(doc);

        var v = GetValue(obj);
        SetValue(doc, _value);
        _value = v;

        _action?.Invoke();

        if (_after.HasFlag(AfterUndo.Solution))
        {
            obj.ExpireSolution(false);
        }
        if (_after.HasFlag(AfterUndo.Preview))
        {
            obj.ExpirePreview(false);
        }
        if (_after.HasFlag(AfterUndo.Layout))
        {
            obj.Attributes.ExpireLayout();
        }
        if (_after.HasFlag(AfterUndo.Refresh))
        {
            Instances.ActiveCanvas.Refresh();
        }
    }

    /// <inheritdoc/>
    protected override void Internal_Redo(GH_Document doc)
    {
        Internal_Undo(doc);
    }
}
