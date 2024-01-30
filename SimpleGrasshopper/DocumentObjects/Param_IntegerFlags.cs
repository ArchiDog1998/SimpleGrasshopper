using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using SimpleGrasshopper.Util;
using System.Collections;

namespace SimpleGrasshopper.DocumentObjects;

/// <inheritdoc/>
public class Param_IntegerFlags : Param_Integer
{
    private static FieldInfo? namedValues = null, nameField = null, valueField = null;

    /// <inheritdoc/>
    public override GH_Exposure Exposure => GH_Exposure.hidden;

    /// <inheritdoc/>
    public override Guid ComponentGuid => new ("{0C1A40EC-5B1A-4962-B372-A1E250E36051}");

    /// <inheritdoc/>
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
        if (!HasNamedValues)
        {
            base.AppendAdditionalMenuItems(menu);
            return;
        }
        Menu_AppendWireDisplay(menu);
        Menu_AppendDisconnectWires(menu);
        Menu_AppendReverseParameter(menu);
        Menu_AppendFlattenParameter(menu);
        Menu_AppendGraftParameter(menu);
        Menu_AppendSimplifyParameter(menu);
        GH_DocumentObject.Menu_AppendSeparator(menu);
        if (Kind == GH_ParamKind.input || Kind == GH_ParamKind.floating)
        {
            int num = -2147483647;
            if (SourceCount == 0 && base.PersistentDataCount == 1)
            {
                GH_Integer gH_Integer = base.PersistentData.get_FirstItem(filter_nulls: true);
                if (gH_Integer != null)
                {
                    num = gH_Integer.Value;
                }
            }

            namedValues ??= typeof(Param_Integer).GetAllRuntimeFields().First(f => f.Name == "m_namedValues");

            foreach (object namedValue in (IList)namedValues.GetValue(this))
            {
                nameField ??= namedValue.GetType().GetAllRuntimeFields().First(f => f.Name == "Name");
                valueField ??= namedValue.GetType().GetAllRuntimeFields().First(f => f.Name == "Value");
                var flag = (int)valueField.GetValue(namedValue);

                GH_DocumentObject.Menu_AppendItem(menu, (string)nameField.GetValue(namedValue), Menu_NamedValueClicked, SourceCount == 0, (flag & num) == flag).Tag = namedValue;
            }
        }
        GH_DocumentObject.Menu_AppendSeparator(menu);
        Menu_AppendDestroyPersistent(menu);
        Menu_AppendInternaliseData(menu);
        Menu_AppendExtractParameter(menu);
        Menu_AppendExpression(menu);
    }

    private void Menu_NamedValueClicked(object sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem toolStripMenuItem) return;
        if (toolStripMenuItem.Tag == null) return;

        if (nameField == null || valueField == null) return;

        var value = this.PersistentData.get_FirstItem(true);
        if (value == null) return;

        object namedValue = toolStripMenuItem.Tag;
        RecordUndoEvent("Set: " + (string)nameField.GetValue(namedValue));

        value.Value ^= (int)valueField.GetValue(namedValue);

        ExpireSolution(recompute: true);
    }
}
