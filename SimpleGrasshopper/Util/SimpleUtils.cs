using Grasshopper.GUI;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Commands;
using SimpleGrasshopper.Attributes;
using SimpleGrasshopper.Data;
using SimpleGrasshopper.DocumentObjects;
using SimpleGrasshopper.Undo;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Net;

namespace SimpleGrasshopper.Util;

/// <summary>
/// Some extra methods.
/// </summary>
public static class SimpleUtils
{
    internal static string GetAssemblyName(this MethodInfo method)
    {
        var assembly = method.DeclaringType?.Assembly;
        if (assembly == null) return string.Empty;

        return GetAssemblyName(assembly);
    }

    /// <summary>
    /// Get the assmebly name from a type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static string GetAssemblyName(this Type type)
    {
        return GetAssemblyName(type.Assembly);
    }

    /// <summary>
    /// Get the assembly name from an assembly.
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns></returns>
    public static string GetAssemblyName(this Assembly assembly)
        => assembly.GetAssemblyStringProperty("Name");

    internal static string GetAssemblyDescription(this Assembly assembly)
        => assembly.GetAssemblyStringProperty("Description");

    private static string GetAssemblyStringProperty(this Assembly assembly, string propertyName)
    {
        var type = assembly.GetTypes().FirstOrDefault(t => typeof(GH_AssemblyInfo).IsAssignableFrom(t));

        if (type != null)
        {
            if (typeof(GH_AssemblyInfo).GetRuntimeProperty(propertyName)?.GetValue(type.CreateInstance()) is string str) return str;
        }

        return assembly.GetName().Name ?? string.Empty;
    }

    internal static Bitmap? GetAssemblyIcon(this Assembly assembly)
    {
        var type = assembly.GetTypes().FirstOrDefault(t => typeof(GH_AssemblyInfo).IsAssignableFrom(t));

        if (type != null)
        {
            if (typeof(GH_AssemblyInfo).GetRuntimeProperty("Icon")?.GetValue(type.CreateInstance()) is Bitmap icon) return icon;
        }

        return null;
    }

    internal static string GetDeclaringClassName(this MethodInfo method)
    {
        var obj = method.DeclaringType;
        if (obj == null) return string.Empty;

        return obj.GetCustomAttribute<SubCategoryAttribute>()?.SubCategory
            ?? obj.Name;
    }

    internal static string GetDocObjName(this MemberInfo method)
        => GetDocObjProperty(method, a => a.Name) ?? method.Name.SpaceStr();

    internal static string GetDocObjNickName(this MemberInfo method)
        => GetDocObjProperty(method, a => a.NickName) ?? method.Name.UpperStr();

    internal static string GetDocObjDescription(this MemberInfo method)
        => GetDocObjProperty(method, a => a.Description) ?? method.Name.SpaceStr();

    private static string? GetDocObjProperty(this MemberInfo member, Func<DocObjAttribute, string?> getProperty)
    {
        var attr = member.GetCustomAttribute<DocObjAttribute>();
        if (attr != null) return getProperty(attr);
        if (member is MethodInfo method && method.IsSpecialName)
        {
            var parameters = string.Join(", ", method.GetParameters()
                .Select(p => p.ParameterType.GetRawType().GetDocObjName()));
            return $"({parameters}) -> {method.ReturnType.GetRawType().GetDocObjName()}";
        }
        return member.Name;
    }

    internal static string GetDocObjName(this Type type)
        => GetDocObjProperty(type, a => a.Name) ?? type.Name.SpaceStr();

    internal static string GetDocObjNickName(this Type type)
        => GetDocObjProperty(type, a => a.NickName) ?? type.Name.UpperStr();

    internal static string GetDocObjDescription(this Type type)
        => GetDocObjProperty(type, a => a.Description) ?? type.Name.SpaceStr();

    private static string? GetDocObjProperty(this Type type, Func<DocObjAttribute, string?> getProperty)
    {
        var attr = type.GetCustomAttribute<DocObjAttribute>();
        return attr == null ? type.Name : getProperty(attr);
    }

    internal static Guid GetDocObjGuid(this Type type)
    {
        if (type == typeof(GH_Surface))
        {
            return new Guid(ParamGuids.Surface);
        }

        if (type.IsGeneralType(typeof(GH_Goo<>)) is Type rawType)
        {
            type = rawType.GetRawType();
        }

        if (type.IsEnum)
        {
            return new Guid(ParamGuids.Integer);
        }

        if (type == typeof(string)) //A lot of people using this.
        {
            return new Guid(ParamGuids.String);
        }
        else if(type == typeof(byte) || type == typeof(long))
        {
            return new Guid(ParamGuids.Integer);
        }

        var paramTypes = Instances.ComponentServer.ObjectProxies
            .Where(p =>
            {
                if (p.Kind != GH_ObjectType.CompiledObject) return false;

                if (p.Type.IsGeneralType(typeof(GH_PersistentParam<>)) == null) return false;

                var obj = p.CreateInstance();
                obj.CreateAttributes();
                if (obj.Attributes is not GH_FloatingParamAttributes) return false;

                return true;
            })
            .Select(p => p.Type)
            .OrderBy(t => t.Assembly != typeof(GH_Component).Assembly)
            .ToArray();

        foreach (var paramType in paramTypes)
        {
            if (GetGuid(type, paramType) is Guid guid)
            {
                return guid;
            }
        }

        //General
        return new Guid("{8EC86459-BF01-4409-BAEE-174D0D2B13D0}");

        static Guid? GetGuid(Type type, Type paramType)
        {
            if (type == null) return null;

            if (paramType.CreateInstance() is not IGH_Param param) return null;

            var gooType = param.Type;
            if (type == gooType) return param.ComponentGuid;

            if (gooType.IsGeneralType(typeof(GH_Goo<>)) is Type rawType)
            {
                if (type == rawType) return param.ComponentGuid;
            }
            foreach (var info in gooType.GetRuntimeProperties().Where(p => p.Name == "Value"))
            {
                if (type == info.PropertyType) return param.ComponentGuid;
            }

            return null;
        }
    }

    internal static Type? IsGeneralType(this Type? type, Type targetType)
    {
        if (type == null) return null;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == targetType)
        {
            return type.GetGenericArguments()[0];
        }
        return IsGeneralType(type.BaseType, targetType);
    }

    internal static Type GetRawType(this Type type)
    {
        return type.GetRefType().GetNullType();
    }

    private static Type GetRefType(this Type type)
    {
        if (type.IsByRef)
        {
            type = type.GetElementType() ?? type;
        }
        return type;
    }

    private static Type GetNullType(this Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            type = Nullable.GetUnderlyingType(type);
        }
        return type;
    }

    internal static object ChangeType(this object obj, Type type)
    {
        type = type.GetRefType();
        if (type.IsInterface) return obj;

        var tNull = type.GetNullType();
        obj = ChangeTypeNotNull(obj, tNull);
        return ChangeTypeNotNull(obj, type);

        static object ChangeTypeNotNull(object obj, Type type)
        {
            if (type.IsEnum) return Enum.ToObject(type, obj);

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                && Nullable.GetUnderlyingType(type) == obj.GetType())
            {
                return Activator.CreateInstance(typeof(Nullable<>).MakeGenericType(obj.GetType()), obj);
            }

            if (type.IsAssignableFrom(obj.GetType())) return obj;

            return Convert.ChangeType(obj, type);
        }
    }

    internal static object CreateInstance(this Type type, bool nullForClass = false)
    {
        try
        {
            if (type.IsEnum || type.IsValueType
                || !nullForClass && type.GetConstructor(Type.EmptyTypes) != null)
            {
                return Activator.CreateInstance(type)!;
            }
        }
        catch
        {
#if DEBUG
            throw;
#endif
        }
        return null!;
    }

    internal static void SetPersistentData(ref IGH_Param param, object data)
    {
        var persistType = typeof(GH_PersistentParam<>);
        if (param.GetType().IsGeneralType(persistType) is not Type persistParam) return;

        if (data is IGH_Structure)
        {
            var method = persistType.MakeGenericType(persistParam).GetRuntimeMethod("SetPersistentData", [typeof(GH_Structure<>).MakeGenericType(persistParam)]);
            if (method == null) return;
            method.Invoke(param, [data]);
        }
        else
        {
            var method = persistType.MakeGenericType(persistParam).GetRuntimeMethod("SetPersistentData", [typeof(object[])]);

            if (method == null) return;

            if (data.GetType().IsArray)
            {
                var length = ((Array)data).Length;
                var array = Array.CreateInstance(typeof(object), length);
                Array.Copy((Array)data, array, length);
                method.Invoke(param, [array]);
            }
            else
            {
                method.Invoke(param, [new object[] { data }]);
            }
        }
    }

    /// <summary>
    /// Get the bitmap in some assembly.
    /// </summary>
    /// <param name="assembly"></param>
    /// <param name="path">name or local path or guid or url</param>
    /// <returns>bitmap</returns>
    public static Bitmap? GetBitmap(this Assembly assembly, string path)
    {
        var bitmap = GetBitmapRaw(assembly, path);
        if (bitmap != null) return bitmap;

        try
        {
            if (Guid.TryParse(path, out var id))
            {
                return Instances.ComponentServer.EmitObjectProxy(id)?.Icon;
            }
            if (File.Exists(path))
            {
                return new Bitmap(path);
            }
            if (Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out var url)
                && url != null)
            {
                using var client = new WebClient();
                var data = client.DownloadData(url);
                var stream = new MemoryStream(data);
                return new Bitmap(stream);
            }
        }
        catch
        {
#if DEBUG
            //throw;
#endif
        }

        return null;

        static Bitmap? GetBitmapRaw(Assembly assembly, string path)
        {
            var name = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(path));
            if (name == null) return null;
            using var stream = assembly.GetManifestResourceStream(name);
            if (stream == null) return null;
            try
            {
                return new(stream);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Get stream value in the assembly
    /// </summary>
    /// <param name="assembly"></param>
    /// <param name="path">name or local path or url</param>
    /// <returns></returns>
    public static string? GetString(this Assembly assembly, string path)
    {
        var str = GetStringRaw(assembly, path);
        if (str != null) return str;

        try
        {
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
            if (Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out var url)
                && url != null)
            {
                using var client = new WebClient();
                var data = client.DownloadData(url);
                var stream = new MemoryStream(data);
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
        }
        catch
        {
#if DEBUG
            //throw;
#endif
        }

        return null;

        static string? GetStringRaw(Assembly assembly, string path)
        {
            var name = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(path));
            if (name == null) return null;
            using var stream = assembly.GetManifestResourceStream(name);
            if (stream == null) return null;
            try
            {
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch
            {
                return null;
            }
        }
    }

    internal static void AddRuntimeMessages(this IGH_Param param, IEnumerable<RuntimeMessage> messages)
    {
        foreach (var msg in messages)
        {
            param.AddRuntimeMessage(msg.Level, $"Param \"{param.NickName}\": {msg.Message}");
        }
    }

    internal static void AddRuntimeMessages(this IGH_Component component, IEnumerable<RuntimeMessage> messages)
    {
        foreach (var msg in messages)
        {
            component.AddRuntimeMessage(msg.Level, msg.Message);
        }
    }

    internal static List<RuntimeMessage> ModifyRange(ref object value, RangeAttribute range, GH_ParamAccess access)
    {
        var messages = new List<RuntimeMessage>();
        switch (access)
        {
            case GH_ParamAccess.item:
                var message = ModifyValueItem(ref value, range);
                if (message.HasValue) messages.Add(message.Value);
                break;

            case GH_ParamAccess.list:
                messages.AddRange(ModifyValueList(ref value, range));
                break;

            case GH_ParamAccess.tree:
                messages.AddRange(ModifyValueTree(ref value, range));
                break;
        }
        return messages;

        static RuntimeMessage[] ModifyValueTree(ref object value, RangeAttribute range)
        {
            if (value is not IGH_Structure structure) return [];

            var messages = new List<RuntimeMessage>();

            foreach (var path in structure.Paths)
            {
                object list = structure.get_Branch(path);
                messages.AddRange(ModifyValueList(ref list, range));
            }
            return [.. messages];
        }

        static RuntimeMessage[] ModifyValueList(ref object value, RangeAttribute range)
        {
            var messages = new List<RuntimeMessage>();

            if (value is not IList list) return [];

            var lt = new List<object>();
            foreach (var item in list)
            {
                var i = item;
                var message = ModifyValueItem(ref i, range);
                lt.Add(i);
                if (message != null) messages.Add(message.Value);
            }

            list.Clear();
            foreach (var item in lt)
            {
                list.Add(item);
            }

            return [.. messages];
        }

        static RuntimeMessage? ModifyValueItem(ref object value, RangeAttribute range)
        {
            if (value is IGH_Goo && value.GetType().GetRuntimeProperty("Value") is PropertyInfo property)
            {
                var v = property.GetValue(value);
                var message = ModifyValueItemRaw(ref v, range);
                property.SetValue(value, v);
                return message;
            }

            var field = value.GetType().GetAllRuntimeFields().FirstOrDefault(f => f.GetCustomAttribute<RangeValueAttribute>() != null);
            var prop = value is IGH_Goo ? value.GetType().GetRuntimeProperty("Value")
                : value.GetType().GetAllRuntimeProperties().FirstOrDefault(f => f.GetCustomAttribute<RangeValueAttribute>() != null);

            if (prop != null)
            {
                var v = prop.GetValue(value);
                var message = ModifyValueItemRaw(ref v, range);
                if (message.HasValue)
                {
                    if (prop.SetMethod != null)
                    {
                        prop.SetValue(value, v);
                    }
                    else
                    {
                        throw new Exception($"%SimpleGrasshopper_RangeSettingThe value {value} is out of range {range.MinD} to {range.MaxD}. And, it can't be set!");
                    }
                }
                return message;
            }
            else if (field != null)
            {
                var v = field.GetValue(value);
                var message = ModifyValueItemRaw(ref v, range);
                if (message.HasValue)
                {
                    field.SetValue(value, v);
                }
                return message;
            }
            else
            {
                return ModifyValueItemRaw(ref value, range);
            }

            static RuntimeMessage? ModifyValueItemRaw(ref object value, RangeAttribute range)
            {
                var min = range.MinD;
                var max = range.MaxD;
                var warning = $"The value {value} is out of range {min} to {max}. It was set to {{0}}.";

                if (value is int)
                {
                    return ChangeValue(ref value, min, max, warning, Convert.ToDouble, Convert.ToInt32);
                }
                else if (value is double)
                {
                    return ChangeValue(ref value, min, max, warning, Convert.ToDouble, Convert.ToDouble);
                }

                return new RuntimeMessage(GH_RuntimeMessageLevel.Error, "This value can't be used with range! please contact with the author!");

                static RuntimeMessage? ChangeValue<T>(ref object value, double min, double max, in string warning,
                Converter<T, double> getValue, Converter<double, T> setvalue)
                {
                    var v = getValue((T)value);
                    if (v < min)
                    {
                        value = setvalue(min)!;
                        return new RuntimeMessage(GH_RuntimeMessageLevel.Warning, string.Format(warning, value));
                    }
                    else if (v > max)
                    {
                        value = setvalue(max)!;
                        return new RuntimeMessage(GH_RuntimeMessageLevel.Warning, string.Format(warning, value));
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }
    }

    internal static void ModifyAngle(ref object value, IGH_Param? param)
    {
        if (param is not Param_Number numberParam) return;
        if (!numberParam.AngleParameter) return;
        if (!numberParam.UseDegrees) return;

        if (value is double d)
        {
            value = RhinoMath.ToRadians(d);
        }
        else if (value is GH_Number n)
        {
            n.Value = RhinoMath.ToRadians(n.Value);
            value = n;
        }
    }

    internal static void SetSpecial(ref IGH_Param param, Type rawInnerType, bool hasAngle, bool hasHidden)
    {
        if (param is Param_Integer integerParam && rawInnerType.IsEnum)
        {
            if (rawInnerType.GetCustomAttribute<FlagsAttribute>() != null)
            {
                param = integerParam = new Param_IntegerFlags();
            }

            var underType = Enum.GetUnderlyingType(rawInnerType);

            foreach (object obj in Enum.GetValues(rawInnerType))
            {
                var v = Convert.ToInt32(Convert.ChangeType(obj, underType));
                var name = ((Enum)obj).GetDescription();
                integerParam.AddNamedValue(name, v);
            }
        }
        else if (param is Param_Number numberParam && hasAngle)
        {
            numberParam.AngleParameter = true;
        }
        else if (param is IGH_PreviewObject previewObject && hasHidden)
        {
            previewObject.Hidden = true;
        }
    }

    internal static string GetDescription(this Enum @enum)
    {
        return @enum.GetCustomAttribute<DescriptionAttribute>()?.Description ?? @enum.ToString();
    }

    /// <summary>
    /// Get custom attribute
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="enum"></param>
    /// <returns></returns>
    public static T? GetCustomAttribute<T>(this Enum @enum) where T : Attribute
    {
        return @enum.GetMemberInfo()?.GetCustomAttribute<T>();
    }

    /// <summary>
    /// Get custom attribute.
    /// </summary>
    /// <param name="enum"></param>
    /// <returns></returns>
    public static MemberInfo? GetMemberInfo(this Enum @enum)
    {
        return @enum.GetType().GetMember(@enum.ToString()).FirstOrDefault();
    }

    internal static bool IsIn(this ParameterInfo info)
    {
        return !(info.ParameterType.IsByRef && info.IsOut);
    }

    internal static bool IsOut(this ParameterInfo info)
    {
        return info.ParameterType.IsByRef;
    }

    /// <summary>
    /// Get all runtime fields.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static IEnumerable<FieldInfo> GetAllRuntimeFields(this Type type)
    {
        if (type == null) return [];
        return type.GetRuntimeFields().Concat(GetAllRuntimeFields(type.BaseType));
    }

    /// <summary>
    /// Get all runtime properties.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static IEnumerable<PropertyInfo> GetAllRuntimeProperties(this Type type)
    {
        if (type == null) return [];
        return type.GetRuntimeProperties().Concat(GetAllRuntimeProperties(type.BaseType));
    }

    /// <summary>
    /// Get all runtime methods.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static IEnumerable<MethodInfo> GetAllRuntimeMethods(this Type type)
    {
        if (type == null) return [];
        return type.GetRuntimeMethods().Concat(GetAllRuntimeMethods(type.BaseType));
    }

    internal static MethodInfo? GetOperatorCast(Type type, Type returnType, Type paramType)
    {
        return type.GetAllRuntimeMethods().FirstOrDefault(m =>
        {
            if (!m.IsSpecialName) return false;
            if (m.Name is not "op_Explicit" and not "op_Implicit") return false;

            if (m.GetCustomAttribute<IgnoreAttribute>() != null) return false;

            if (m.ReturnType != returnType) return false;

            var parameters = m.GetParameters();

            if (parameters.Length != 1) return false;

            return parameters[0].ParameterType.GetRawType().IsAssignableFrom(paramType);
        });
    }

    /// <summary>
    /// Create a scroller.
    /// </summary>
    /// <param name="menu"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <param name="decimalPlace"></param>
    /// <param name="originValue"></param>
    /// <param name="setValue"></param>
    /// <returns></returns>
    public static GH_DigitScroller AddScroller(this ToolStripDropDown menu, decimal min, decimal max, decimal originValue, int decimalPlace, Action<decimal> setValue)
    {
        GH_DigitScroller slider = new()
        {
            MinimumValue = min,
            MaximumValue = max,
            DecimalPlaces = decimalPlace,
            Value = originValue,
            Size = new Size(150, 24),
        };

        slider.ValueChanged += (sender, e) =>
        {
            var result = e.Value;
            result = result >= min ? result : min;
            result = result <= max ? result : max;
            slider.Value = result;
            setValue(result);
        };

        GH_DocumentObject.Menu_AppendCustomItem(menu, slider);
        return slider;
    }

    /// <summary>
    /// Record the doc obj member changed.
    /// </summary>
    /// <param name="obj">The obj.</param>
    /// <param name="memberName">The member to record.</param>
    /// <param name="after">After undo, what should happend.</param>
    /// <param name="action">The action to do after changing</param>
    public static void RecordDocumentObjectMember(this IGH_DocumentObject obj, string memberName, AfterUndo after, Action? action = null)
    {
        var doc = obj.OnPingDocument();
        doc.UndoUtil.RecordEvent(memberName + " Changed", new GH_MemberUndoAction(obj, memberName, after, action));
    }

    /// <summary>
    /// Add the space to the strings.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string SpaceStr(this string str)
    {
        var result = string.Empty;

        bool isLastUpper = true;
        foreach (var c in str)
        {
            if (!isLastUpper && char.IsUpper(c))
            {
                result += " ";
            }
            result += c;
            isLastUpper = char.IsUpper(c);
        }
        return result;
    }

    /// <summary>
    /// Get the upper string.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string UpperStr(this string str)
    {
        var result = string.Empty;
        bool isFirst = true;
        foreach (var c in str)
        {
            if (isFirst)
            {
                result += char.ToUpper(c);
                isFirst = false;
            }
            else if (char.IsUpper(c))
            {
                result += c;
            }
        }
        return result;
    }

    internal static void SearchDropdown<T>(ToolStripDropDown dropdown, T[] items, Func<T, bool> isSelected, SelectionMode mode, EventHandler? boxSelectedValueChanged = null)
    {
        var width = (int)Math.Round(220f * GH_GraphicsUtil.UiScale);

        var textItem = new ToolStripTextBox
        {
            Text = string.Empty,
            BorderStyle = BorderStyle.FixedSingle,
            Width = width,
            AutoSize = false,
            ToolTipText = "Searching...",
        };

        dropdown.Items.Add(textItem);

        var box = new ListBox()
        {
            BorderStyle = BorderStyle.FixedSingle,
            Width = width,
            Height = (int)Math.Round(180f * GH_GraphicsUtil.UiScale),
            SelectionMode = mode,
        };

        GH_DocumentObject.Menu_AppendCustomItem(dropdown, box);

        textItem.TextChanged += (sender, e) => UpdateItems();

        if (boxSelectedValueChanged != null)
        {
            box.SelectedValueChanged += boxSelectedValueChanged;
        }

        dropdown.MaximumSize = new(500, 600);

        UpdateItems();

        void UpdateItems()
        {
            box.Items.Clear();

            foreach (var item in items.OrderByDescending(i => Similarity(i?.ToString() ?? string.Empty, textItem.Text)))
            {
                var index = box.Items.Add(item);
                box.SetSelected(index, isSelected(item));
            }
        }
    }

    private static readonly char[] _splitChar = [' ', ',', '、', '.', '。'];

    /// <summary>
    /// The similarity of the two texts.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static float Similarity(string text, string key)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        var chars = text.Split(_splitChar, StringSplitOptions.RemoveEmptyEntries);
        var keys = key.Split(_splitChar, StringSplitOptions.RemoveEmptyEntries);

        var startWithCount = chars.Count(i => keys.Any(k => i.StartsWith(k, StringComparison.OrdinalIgnoreCase)));

        var containCount = chars.Count(i => keys.Any(k => i.ToUpper().Contains(k.ToUpper())));

        return startWithCount * 3 + containCount;
    }
}
