using GH_IO;
using GH_IO.Serialization;
using Newtonsoft.Json;
using SimpleGrasshopper.Attributes;

namespace SimpleGrasshopper.Util;

/// <summary>
/// For save and load in Grasshopper.
/// </summary>
public static class IOHelper
{
    /// <summary>
    /// Read from an obj.
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="obj"></param>
    public static void Read(this GH_IReader reader, GH_ISerializable obj)
    {
        foreach (var field in obj.GetType().GetAllRuntimeFields())
        {
            if (field.GetCustomAttribute<DocDataAttribute>() == null) continue;
            Read(reader, obj, field);
        }
        foreach (var prop in obj.GetType().GetAllRuntimeProperties())
        {
            if (prop.GetCustomAttribute<DocDataAttribute>() == null) continue;
            Read(reader, obj, prop);
        }
    }

    private static void Read(GH_IReader reader, object obj, PropertyInfo info)
    {
        if (Read(reader, info.Name, info.PropertyType, out var value))
        {
            info.SetValue(obj, value);
        }
    }

    private static void Read(GH_IReader reader, object obj, FieldInfo info)
    {
        if (Read(reader, info.Name, info.FieldType, out var value))
        {
            info.SetValue(obj, value);
        }
    }

    internal static bool Read<T>(this GH_IReader reader, string name, out T value)
    {
        var result = reader.Read(name, typeof(T), out var v);
        value = result ? (T)v : default!;
        return result;
    }

    internal static bool Read(this GH_IReader reader, string name, Type type, out object value)
    {
        var getMethod = reader.GetType().GetAllRuntimeMethods().FirstOrDefault(m =>
        {
            if (!m.Name.StartsWith("Get")) return false;
            if (m.ReturnParameter.ParameterType != type) return false;
            var parameters = m.GetParameters();
            if (parameters.Length != 1) return false;
            if (parameters[0].ParameterType != typeof(string)) return false;
            return true;
        });

        if (getMethod != null)
        {
            try
            {
                value = getMethod.Invoke(reader, [name]);
                return value != null;
            }
            catch
            {
            }
        }
        else
        {
            var str = string.Empty;
            if (reader.TryGetString(name, ref str))
            {
                var va = typeof(IOHelper).GetAllRuntimeMethods().First(m => m.Name == nameof(DeserializeObject))
                    .MakeGenericMethod(type).Invoke(null, [str]);
                if (va != null)
                {
                    value = va;
                    return true;
                }
            }
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Write to an obj.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="obj"></param>
    public static void Write(this GH_IWriter writer, GH_ISerializable obj)
    {
        foreach (var field in obj.GetType().GetAllRuntimeFields())
        {
            if (field.GetCustomAttribute<DocDataAttribute>() == null) continue;
            Write(writer, obj, field);
        }
        foreach (var prop in obj.GetType().GetAllRuntimeProperties())
        {
            if (prop.GetCustomAttribute<DocDataAttribute>() == null) continue;
            Write(writer, obj, prop);
        }
    }

    private static void Write(GH_IWriter writer, object obj, PropertyInfo info)
    {
        Write(writer, info.Name, info.GetValue(obj));
    }

    private static void Write(GH_IWriter writer, object obj, FieldInfo info)
    {
        Write(writer, info.Name, info.GetValue(obj));
    }

    internal static void Write(this GH_IWriter writer, string name, object value)
    {
        if (writer.Items.Any(i => i.Name == name)) return;

        var type = value.GetType();

        var setMethod = writer.GetType().GetAllRuntimeMethods().FirstOrDefault(m =>
        {
            if (!m.Name.StartsWith("Set")) return false;
            var parameters = m.GetParameters();
            if (parameters.Length != 2) return false;
            if (parameters[0].ParameterType != typeof(string)) return false;
            if (parameters[1].ParameterType != type) return false;
            return true;
        });

        if (setMethod != null)
        {
            setMethod.Invoke(writer, [name, value]);
        }
        else
        {
            writer.SetString(name, SerializeObject(value));
        }
    }

    #region Serialization
    static readonly JsonSerializerSettings _setting = new()
    {
        TypeNameHandling = TypeNameHandling.Objects,
    };

    /// <summary>
    /// Serialize an object.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string SerializeObject(object obj)
    {
        return JsonConvert.SerializeObject(obj, _setting);
    }

    /// <summary>
    /// Get an object from string.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="str"></param>
    /// <returns></returns>
    public static T? DeserializeObject<T>(string str)
    {
        return JsonConvert.DeserializeObject<T>(str, _setting);
    }

    /// <summary>
    /// Get an object from string.
    /// </summary>
    /// <param name="str"></param>
    /// <param name="type">type</param>
    /// <returns></returns>
    public static object? DeserializeObject(string str, Type type)
    {
        return JsonConvert.DeserializeObject(str, type, _setting);
    }
    #endregion
}
