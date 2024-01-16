using GH_IO.Serialization;
using Grasshopper.Kernel.Undo;
using SimpleGrasshopper.Util;

namespace SimpleGrasshopper.Undo;

/// <summary>
/// The undo action for all <see cref="GH_SettingsServer"/>.
/// </summary>
/// <param name="setting">The setting.</param>
/// <param name="name">The key name</param>
public class GH_SettingUndoAction(GH_SettingsServer setting, string name) : GH_UndoAction
{
    private static readonly MethodInfo
        _getInstace = typeof(GH_SettingsServer).GetAllRuntimeMethods().First(m => m.Name == "GetInstance"),
       _setInstace = typeof(GH_SettingsServer).GetAllRuntimeMethods().First(m => m.Name == "SetInstance");

    private object? _data = _getInstace.Invoke(setting, [name]);

    /// <inheritdoc/>
    protected override void Internal_Redo(GH_Document doc)
    {
        Internal_Undo(doc);
    }

    /// <inheritdoc/>
    protected override void Internal_Undo(GH_Document doc)
    {
        var data = _getInstace.Invoke(setting, [name]);

        if (_data != null)
        {
            _setInstace.Invoke(setting, [_data]);
        }
        else
        {
            setting.DeleteValue(name);
        }

        _data = data;
    }
}
