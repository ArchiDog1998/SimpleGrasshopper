using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino;
using SimpleGrasshopper.Attributes;
using SimpleGrasshopper.Data;
using System.Linq;
using System.Net;

namespace SimpleGrasshopper.Util;

internal static class Utils
{
    public static string GetAssemblyName(this MethodInfo method)
    {
        var assembly = method.DeclaringType?.Assembly;
        if (assembly == null) return string.Empty;

        return GetAssemblyName(assembly);
    }

    public static string GetAssemblyName(this Type type)
    {
        return GetAssemblyName(type.Assembly);
    }

    public static string GetAssemblyName(this Assembly assembly)
        => assembly.GetAssemblyStringProperty("Name");

    public static string GetAssemblyDescription(this Assembly assembly)
        => assembly.GetAssemblyStringProperty("Description");

    private static string GetAssemblyStringProperty(this Assembly assembly, string propertyName)
    {
        var type = assembly.GetTypes().FirstOrDefault(t => typeof(GH_AssemblyInfo).IsAssignableFrom(t));

        if (type != null)
        {
            if (typeof(GH_AssemblyInfo).GetRuntimeProperty(propertyName)?.GetValue(Activator.CreateInstance(type)) is string str) return str;
        }

        return assembly.GetName().Name ?? string.Empty;
    }

    public static Bitmap? GetAssemblyIcon(this Assembly assembly)
    {
        var type = assembly.GetTypes().FirstOrDefault(t => typeof(GH_AssemblyInfo).IsAssignableFrom(t));

        if (type != null)
        {
            if (typeof(GH_AssemblyInfo).GetRuntimeProperty("Icon")?.GetValue(Activator.CreateInstance(type)) is Bitmap icon) return icon;
        }

        return null;
    }

    public static string GetDeclaringClassName(this MethodInfo method)
    {
        var obj = method.DeclaringType;
        if (obj == null) return string.Empty;

        return obj.GetCustomAttribute<SubCategoryAttribute>()?.SubCategory
            ?? obj.Name;
    }

    public static string GetDocObjName(this MemberInfo method)
        => GetDocObjProperty(method, a => a.Name);

    public static string GetDocObjNickName(this MemberInfo method)
        => GetDocObjProperty(method, a => a.NickName);

    public static string GetDocObjDescription(this MemberInfo method)
        => GetDocObjProperty(method, a => a.Description);

    private static string GetDocObjProperty(this MemberInfo method, Func<DocObjAttribute, string> getProperty)
    {
        var attr = method.GetCustomAttribute<DocObjAttribute>();
        return attr == null ? method.Name : getProperty(attr);
    }

    public static string GetDocObjName(this Type type)
        => GetDocObjProperty(type, a => a.Name);

    public static string GetDocObjNickName(this Type type)
        => GetDocObjProperty(type, a => a.NickName);

    public static string GetDocObjDescription(this Type type)
        => GetDocObjProperty(type, a => a.Description);

    private static string GetDocObjProperty(this Type type, Func<DocObjAttribute, string> getProperty)
    {
        var attr = type.GetCustomAttribute<DocObjAttribute>();
        return attr == null ? type.Name : getProperty(attr);
    }

    public static Guid GetDocObjGuid(this Type type)
    {
        if (type.IsGeneralType(typeof(GH_Goo<>)) is Type rawType)
        {
            type = rawType.GetRawType();
        }

        if (type.IsEnum)
        {
            //Integer.
            return new Guid("{2E3AB970-8545-46bb-836C-1C11E5610BCE}");
        }

        if (type == typeof(string)) //A lot of people using this.
        {
            //String.
            return new Guid("{3EDE854E-C753-40eb-84CB-B48008F14FD4}");
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
            .OrderByDescending(t => t.Assembly != typeof(GH_Component).Assembly)
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

            if (Activator.CreateInstance(paramType) is not IGH_Param param) return null;

            var gooType = param.Type;
            if (type == gooType) return param.ComponentGuid;

            if (gooType.IsGeneralType(typeof(GH_Goo<>)) is Type rawType)
            {
                if (type == rawType) return param.ComponentGuid;
            }

            return null;
        }
    }

    public static Type? IsGeneralType(this Type? type, Type targetType)
    {
        if (type == null) return null;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == targetType)
        {
            return type.GetGenericArguments()[0];
        }
        return IsGeneralType(type.BaseType, targetType);
    }

    public static Type GetRawType(this Type type)
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

    public static object ChangeType(this object obj, Type type)
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
            return Convert.ChangeType(obj, type);
        }
    }

    public static object CreateInstance(this Type type)
    {
        if (type.IsEnum || type.IsValueType) return Activator.CreateInstance(type)!;
        try
        {
            return Activator.CreateInstance(type)!;
        }
        catch
        {
            return null!;
        }
    }

    public static Bitmap? GetBitmap(this Assembly assembly, string path)
    {
        var bitmap = assembly.GetBitmapRaw(path);
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
                && url is not null)
            {
                using var client = new WebClient();
                var data = client.DownloadData(url);
                var stream = new MemoryStream(data);
                return new Bitmap(stream);
            }
        }
        catch
        {
        }

        return null;
    }

    private static Bitmap? GetBitmapRaw(this Assembly assembly, string path)
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

    public static void AddRuntimeMessages(this IGH_Param param, IEnumerable<RuntimeMessage> messages)
    {
        foreach (var msg in messages)
        {
            param.AddRuntimeMessage(msg.Level, $"Param \"{param.NickName}\": {msg.Message}");
        }
    }

    public static void AddRuntimeMessages(this IGH_Component component, IEnumerable<RuntimeMessage> messages)
    {
        foreach (var msg in messages)
        {
            component.AddRuntimeMessage(msg.Level, msg.Message);
        }
    }

    public static List<RuntimeMessage> ModifyRange(ref object value, RangeAttribute range, GH_ParamAccess access)
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
            if (value is GH_Structure<GH_Integer> intTree)
            {
                var result = ModifyValue(ref intTree, range);
                value = intTree;
                return result;
            }
            else if (value is GH_Structure<GH_Number> numTree)
            {
                var result = ModifyValue(ref numTree, range);
                value = numTree;
                return result;
            }
            return [];

            static RuntimeMessage[] ModifyValue<T>(ref GH_Structure<T> structure, RangeAttribute range)
                where T : IGH_Goo
            {
                var messages = new List<RuntimeMessage>();

                structure = structure.DuplicateCast(item =>
                {
                    var i = (object)item;
                    var message = ModifyValueItem(ref i, range);
                    if (message.HasValue) messages.Add(message.Value);
                    return (T)i;
                });
                return [.. messages];
            }
        }

        static RuntimeMessage[] ModifyValueList(ref object value, RangeAttribute range)
        {
            var messages = new List<RuntimeMessage>();

            if (value is List<int> intList)
            {
                messages.AddRange(ModifyValuesLoc(ref intList, range));
                value = intList;
            }
            else if (value is int[] intArray)
            {
                var input = intArray.ToList();
                messages.AddRange(ModifyValuesLoc(ref input, range));
                value = input;
            }
            else if (value is List<double> doubleList)
            {
                messages.AddRange(ModifyValuesLoc(ref doubleList, range));
                value = doubleList;
            }
            else if (value is double[] doubleArray)
            {
                var input = doubleArray.ToList();
                messages.AddRange(ModifyValuesLoc(ref input, range));
                value = input;
            }
            else if (value is List<GH_Integer> ghIntList)
            {
                messages.AddRange(ModifyValuesLoc(ref ghIntList, range));
                value = ghIntList;
            }
            else if (value is GH_Integer[] ghIntArray)
            {
                var input = ghIntArray.ToList();
                messages.AddRange(ModifyValuesLoc(ref input, range));
                value = input;
            }
            else if (value is List<GH_Number> ghNumList)
            {
                messages.AddRange(ModifyValuesLoc(ref ghNumList, range));
                value = ghNumList;
            }
            else if (value is GH_Number[] ghNumArray)
            {
                var input = ghNumArray.ToList();
                messages.AddRange(ModifyValuesLoc(ref input, range));
                value = input;
            }

            return [.. messages];

            static RuntimeMessage[] ModifyValuesLoc<T>(ref List<T> values, RangeAttribute range)
            {
                var messages = new List<RuntimeMessage>(values.Count);
                for (int i = 0; i < values.Count; i++)
                {
                    object item = values[i]!;
                    var message = ModifyValueItem(ref item, range);
                    if (message.HasValue) messages.Add(message.Value);
                    values[i] = (T)item;
                }
                return [.. messages];
            }
        }

        static RuntimeMessage? ModifyValueItem(ref object value, RangeAttribute range)
        {
            var min = range.MinD;
            var max = range.MaxD;
            var warning = $"The value {value} is out of range {min} to {max}, it was set to {{0}}.";
            if (value is int)
            {
                return ChangeValue(ref value, min, max, warning, Convert.ToDouble, Convert.ToInt32);
            }
            else if (value is double)
            {
                return ChangeValue(ref value, min, max, warning, Convert.ToDouble, Convert.ToDouble);
            }
            else if (value is GH_Integer integer)
            {
                return ChangeValue(ref value, min, max, warning, i => Convert.ToDouble(i.Value),
                    i => new GH_Integer(Convert.ToInt32(i)));
            }
            else if (value is GH_Number number)
            {
                return ChangeValue(ref value, min, max, warning, i => Convert.ToDouble(i.Value),
                    i => new GH_Number(Convert.ToDouble(i)));
            }

            return null;

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

    public static void ModifyAngle(ref object value, IGH_Param? param)
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

    public static void SetSpecial(ref IGH_Param param, Type rawInnerType, bool hasAngle, bool hasHidden)
    {
        if (param is Param_Integer integerParam && rawInnerType.IsEnum)
        {
            var names = Enum.GetNames(rawInnerType);
            int index = 0;
            var underType = Enum.GetUnderlyingType(rawInnerType);
            foreach (object obj in Enum.GetValues(rawInnerType))
            {
                var v = Convert.ToInt32(Convert.ChangeType(obj, underType));
                integerParam.AddNamedValue(names[index++], v);
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

    public static bool IsIn(this ParameterInfo info)
    {
        return !(info.ParameterType.IsByRef && info.IsOut);
    }

    public static bool IsOut(this ParameterInfo info)
    {
        return info.ParameterType.IsByRef;
    }

    public static IEnumerable<FieldInfo> GetAllRuntimeFields(this Type type)
    {
        if (type == null) return [];
        return type.GetRuntimeFields().Concat(GetAllRuntimeFields(type.BaseType));
    }

    public static IEnumerable<PropertyInfo> GetAllRuntimeProperties(this Type type)
    {
        if (type == null) return [];
        return type.GetRuntimeProperties().Concat(GetAllRuntimeProperties(type.BaseType));
    }
}
