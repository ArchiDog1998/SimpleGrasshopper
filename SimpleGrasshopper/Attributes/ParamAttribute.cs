using SimpleGrasshopper.Data;

namespace SimpleGrasshopper.Attributes;

/// <summary>
/// the string of <see cref="IGH_DocumentObject.ComponentGuid"/> that you want this param to show with.
/// </summary>
/// <param name="guid">the string of <seealso cref="Guid"/>. You can seek the guid in <see cref="ParamGuids"/>.</param>
[AttributeUsage(AttributeTargets.Parameter)]
public class ParamAttribute(string guid) : Attribute
{
    internal Guid Guid => new(guid);
}
