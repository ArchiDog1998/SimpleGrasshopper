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
    object _value;

    /// <summary>
    /// Default ctor.
    /// </summary>
    /// <param name="obj">The object that contains this value.</param>
    /// <param name="memberName">the value name.</param>
    public GH_MemberUndoAction(IGH_DocumentObject obj, string memberName)
    {
        _guid = obj.InstanceGuid;
        _field = obj.GetType().GetAllRuntimeFields().FirstOrDefault(f => f.Name == memberName);
        _prop = obj.GetType().GetAllRuntimeProperties().FirstOrDefault(p => p.Name == memberName);
        _value = GetValue(obj.OnPingDocument());
    }

    private object GetValue(GH_Document doc)
    {
        var obj = GetObj(doc);
        return _field?.GetValue(obj) ?? _prop?.GetValue(obj) ?? default!;
    }

    private void SetValue(GH_Document doc, object value)
    {
        var obj = GetObj(doc);
        _field?.SetValue(obj, value);
        _prop?.SetValue(obj, value);
    }

    private IGH_DocumentObject GetObj(GH_Document doc)
    {
        return doc.FindObject(_guid, false);
    }

    /// <inheritdoc/>
    protected override void Internal_Undo(GH_Document doc)
    {
        var v = GetValue(doc);
        SetValue(doc, _value);
        _value = v;
    }

    /// <inheritdoc/>
    protected override void Internal_Redo(GH_Document doc)
    {
        Internal_Undo(doc);
    }
}
