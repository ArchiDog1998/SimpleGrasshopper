using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using SimpleGrasshopper.Attributes;

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

    private static string GetAssemblyName(this Assembly assembly)
    {
        var type = assembly.GetTypes().FirstOrDefault(t => t.IsAssignableTo(typeof(GH_AssemblyInfo)));

        if (type != null)
        {
            var category = typeof(GH_AssemblyInfo).GetRuntimeProperty("Name")?.GetValue(Activator.CreateInstance(type)) as string;

            if (category != null) return category;
        }

        return assembly.GetName().Name ?? string.Empty;
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

    public static Type? IsGeneralType(this Type? type, Type targetType)
    {
        if (type == null) return null;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == targetType)
        {
            return type.GetGenericArguments()[0];
        }
        return IsGeneralType(type.BaseType, targetType);
    }

    private static Type[]? _paramTypes = null;
    public static Guid GetDocObjGuid(this Type type)
    {
        var proxy = Instances.ComponentServer.ObjectProxies;

        _paramTypes ??= Instances.ComponentServer.ObjectProxies
            .Where(p => p.Kind == GH_ObjectType.CompiledObject)
            .Select(p => p.Type)
            .OrderByDescending(t => t.Assembly != typeof(GH_Component).Assembly)
            .Where(t => t.IsGeneralType(typeof(GH_PersistentParam<>)) != null
                && t != typeof(Param_FilePath))
            .ToArray();

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

        foreach (var paramType in _paramTypes)
        {
            if(type.GetGuid(paramType) is Guid guid)
            {
                return guid;
            }
        }
        return new Guid("{8EC86459-BF01-4409-BAEE-174D0D2B13D0}");
    }

    private static Guid? GetGuid(this Type type, Type paramType)
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

    public static Type? GetRawType(this Type? type)
    {
        if (type == null) return null;
        if (type.IsByRef)
        {
            type = type.GetElementType() ?? type;
        }
        return type;
    }

    public static object ChangeType(this object obj, Type type)
    {
        return type.IsEnum ? Enum.ToObject(type, obj)
                : Convert.ChangeType(obj, type);
    }
}
