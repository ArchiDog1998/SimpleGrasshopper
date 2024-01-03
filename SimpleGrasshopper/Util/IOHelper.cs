using GH_IO.Serialization;
using GH_IO.Types;
using SimpleGrasshopper.Attributes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace SimpleGrasshopper.Util;

/// <summary>
/// For save and load in Grasshopper.
/// </summary>
public static class IOHelper
{
    internal static void Read(this GH_IReader reader, object obj)
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
        if (type == typeof(bool))
        {
            bool v = false;
            if (reader.TryGetBoolean(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(byte))
        {
            byte v = 0;
            if (reader.TryGetByte(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(int))
        {
            int v = 0;
            if (reader.TryGetInt32(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(long))
        {
            long v = 0;
            if (reader.TryGetInt64(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(float))
        {
            float v = 0;
            if (reader.TryGetSingle(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(double))
        {
            double v = 0;
            if (reader.TryGetDouble(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(decimal))
        {
            decimal v = 0;
            if (reader.TryGetDecimal(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(DateTime))
        {
            DateTime v = DateTime.Now;
            if (reader.TryGetDate(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(Guid))
        {
            Guid v = Guid.Empty;
            if (reader.TryGetGuid(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(string))
        {
            string v = string.Empty;
            if (reader.TryGetString(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(Point))
        {
            Point v = default;
            if (reader.TryGetDrawingPoint(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(PointF))
        {
            PointF v = default;
            if (reader.TryGetDrawingPointF(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(Size))
        {
            Size v = default;
            if (reader.TryGetDrawingSize(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(SizeF))
        {
            SizeF v = default;
            if (reader.TryGetDrawingSizeF(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(Rectangle))
        {
            Rectangle v = default;
            if (reader.TryGetDrawingRectangle(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(RectangleF))
        {
            RectangleF v = default;
            if (reader.TryGetDrawingRectangleF(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(Color))
        {
            Color v = default;
            if (reader.TryGetDrawingColor(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(GH_Point2D))
        {
            GH_Point2D v = default;
            if (reader.TryGetPoint2D(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(GH_Point3D))
        {
            GH_Point3D v = default;
            if (reader.TryGetPoint3D(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(GH_Point4D))
        {
            GH_Point4D v = default;
            if (reader.TryGetPoint4D(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(GH_Interval1D))
        {
            GH_Interval1D v = default;
            if (reader.TryGetInterval1D(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(GH_Interval2D))
        {
            GH_Interval2D v = default;
            if (reader.TryGetInterval2D(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(GH_Line))
        {
            GH_Line v = default;
            if (reader.TryGetLine(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(GH_BoundingBox))
        {
            GH_BoundingBox v = default;
            if (reader.TryGetBoundingBox(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(GH_Plane))
        {
            GH_Plane v = default;
            if (reader.TryGetPlane(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(GH_Version))
        {
            GH_Version v = default;
            if (reader.TryGetVersion(name, ref v))
            {
                value = v;
                return true;
            }
        }
        else if (type == typeof(Bitmap))
        {
            value = reader.GetDrawingBitmap(name);
            return value != null;
        }
        else if(type == typeof(byte[]))
        {
            value = reader.GetByteArray(name);
            return value != null;
        }
        else if(type == typeof(double[]))
        {
            value = reader.GetDoubleArray(name);
            return value != null;
        }
        else
        {
            var v = reader.GetByteArray(name);
            if (v != null)
            {
                var va = DeserializeObject(v);
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

    internal static void Write(this GH_IWriter writer, object obj)
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
        var type = value.GetType();

        if (type == typeof(bool))
        {
            writer.SetBoolean(name, (bool)value);
        }
        else if (type == typeof(byte))
        {
            writer.SetByte(name, (byte)value);
        }
        else if (type == typeof(int))
        {
            writer.SetInt32(name, (int)value);
        }
        else if (type == typeof(long))
        {
            writer.SetInt64(name, (long)value);
        }
        else if (type == typeof(float))
        {
            writer.SetSingle(name, (float)value);
        }
        else if (type == typeof(double))
        {
            writer.SetDouble(name, (double)value);
        }
        else if (type == typeof(decimal))
        {
            writer.SetDecimal(name, (decimal)value);
        }
        else if (type == typeof(DateTime))
        {
            writer.SetDate(name, (DateTime)value);
        }
        else if (type == typeof(Guid))
        {
            writer.SetGuid(name, (Guid)value);
        }
        else if (type == typeof(string))
        {
            writer.SetString(name, (string)value);
        }
        else if (type == typeof(Point))
        {
            writer.SetDrawingPoint(name, (Point)value);
        }
        else if (type == typeof(PointF))
        {
            writer.SetDrawingPointF(name, (PointF)value);
        }
        else if (type == typeof(Size))
        {
            writer.SetDrawingSize(name, (Size)value);
        }
        else if (type == typeof(SizeF))
        {
            writer.SetDrawingSizeF(name, (SizeF)value);
        }
        else if (type == typeof(Rectangle))
        {
            writer.SetDrawingRectangle(name, (Rectangle)value);
        }
        else if (type == typeof(RectangleF))
        {
            writer.SetDrawingRectangleF(name, (RectangleF)value);
        }
        else if (type == typeof(Color))
        {
            writer.SetDrawingColor(name, (Color)value);
        }
        else if (type == typeof(GH_Point2D))
        {
            writer.SetPoint2D(name, (GH_Point2D)value);
        }
        else if (type == typeof(GH_Point3D))
        {
            writer.SetPoint3D(name, (GH_Point3D)value);
        }
        else if (type == typeof(GH_Point4D))
        {
            writer.SetPoint4D(name, (GH_Point4D)value);
        }
        else if (type == typeof(GH_Interval1D))
        {
            writer.SetInterval1D(name, (GH_Interval1D)value);
        }
        else if (type == typeof(GH_Interval2D))
        {
            writer.SetInterval2D(name, (GH_Interval2D)value);
        }
        else if (type == typeof(GH_Line))
        {
            writer.SetLine(name, (GH_Line)value);
        }
        else if (type == typeof(GH_BoundingBox))
        {
            writer.SetBoundingBox(name, (GH_BoundingBox)value); 
        }
        else if (type == typeof(GH_Plane))
        {
            writer.SetPlane(name, (GH_Plane)value); 
        }
        else if (type == typeof(GH_Version))
        {
            writer.SetVersion(name, (GH_Version)value);
        }
        else if (type == typeof(Bitmap))
        {
            writer.SetDrawingBitmap(name, (Bitmap)value);
        }
        else if (type == typeof(byte[]))
        {
            writer.SetByteArray(name, (byte[])value);
        }
        else if (type == typeof(double[]))
        {
            writer.SetDoubleArray(name, (double[])value);
        }
        else
        {
            writer.SetByteArray(name, SerializeObject(value));
        }
    }

    #region Serialization
    static readonly List<Type> _unserializedTypes = [];
    /// <summary>
    /// Serialize an object.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static byte[] SerializeObject(object obj)
    {
        var type = obj.GetType();
        if (_unserializedTypes.Contains(type)) return [];
        try
        {
            BinaryFormatter bf = new BinaryFormatter();
            using var ms = new MemoryStream();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }
        catch
        {
            _unserializedTypes.Add(type);
            return [];
        }
    }

    /// <summary>
    ///  Serialize an object from string.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string SerializeObjectStr(object obj)
    {
        return Encoding.ASCII.GetString(SerializeObject(obj));
    }
    /// <summary>
    /// Get an object from string.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="str"></param>
    /// <returns></returns>
    public static T? DeserializeObject<T>(string str)
    {
        var obj = DeserializeObject(str);
        return obj is T t ? t : default;
    }

    /// <summary>
    /// Get an object from byte array.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static T? DeserializeObject<T>(byte[] bytes)
    {
        var obj = DeserializeObject(bytes);
        return obj is T t ? t : default;
    }

    /// <summary>
    /// Get an object from string.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static object? DeserializeObject(string str)
    {
        return DeserializeObject(Encoding.ASCII.GetBytes(str));
    }

    /// <summary>
    /// Get an object from byte array.
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static object? DeserializeObject(byte[] bytes)
    {
        using var memStream = new MemoryStream();
        var binForm = new BinaryFormatter();
        memStream.Write(bytes, 0, bytes.Length);
        memStream.Seek(0, SeekOrigin.Begin);
        return binForm.Deserialize(memStream);
    }

    #endregion
}
