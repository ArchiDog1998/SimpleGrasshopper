using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using SimpleGrasshopper.Attributes;
using SimpleGrasshopper.Util;
using System.Collections;
using System.Linq.Expressions;

namespace SimpleGrasshopper.Data;

internal class TypeParam
{
    public int ParamIndex { get; set; }
    public Type Type { get; }
    public Type RawType { get; }
    public Type CreateType { get; }
    public Type InnerType { get; }
    public Type RawInnerType { get; }
    public Type RawInnerTypeNoGoo { get; }
    public Type CreateInnerType { get; }

    public GH_ParamAccess Access { get; }

    public Guid ComponentGuid { get; }

    public TypeParam(Type type, int index)
    {
        ParamIndex = index;

        Type = type;
        RawType = Type.GetRawType();

        InnerType = GetAccessAndType(RawType, out var access);
        Access = access;
        RawInnerType = InnerType.GetRawType();

        RawInnerTypeNoGoo = RawInnerType.IsGeneralType(typeof(GH_Goo<>)) is Type rawType
            ? rawType.GetRawType() : RawInnerType;

        CreateInnerType = RawInnerTypeNoGoo.IsEnum
            ? (Access == GH_ParamAccess.tree ? typeof(GH_Integer) : typeof(int))
            : InnerType;

        CreateType = Access switch
        {
            GH_ParamAccess.list => typeof(List<>).MakeGenericType(CreateInnerType),
            GH_ParamAccess.tree => typeof(GH_Structure<>).MakeGenericType(CreateInnerType),
            _ => CreateInnerType,
        };

        ComponentGuid = RawInnerType.GetDocObjGuid();

        static Type GetAccessAndType(Type type, out GH_ParamAccess access)
        {
            if (type.IsGeneralType(typeof(GH_Structure<>)) is Type treeType)
            {
                access = GH_ParamAccess.tree;
                return treeType;
            }
            else if (type.IsArray)
            {
                access = GH_ParamAccess.list;
                return type.GetElementType()!;
            }

            foreach (var listType in AssemblyPriority.ListTypes)
            {
                if (type.IsGeneralType(listType) is Type ltType)
                {
                    access = GH_ParamAccess.list;
                    return ltType;
                }
            }

            access = GH_ParamAccess.item;
            return type;
        }
    }

    public void GetNames(string defaultName, string defaultNickName, out string name, out string nickName, out string description)
    {
        var attr = RawInnerType.GetCustomAttribute<DocObjAttribute>();
        defaultName = RawInnerType.Name ?? defaultName;
        defaultNickName = RawInnerType.Name ?? defaultNickName;

        name = attr?.Name ?? defaultName;
        nickName = attr?.NickName ?? defaultNickName;
        description = attr?.Description ?? defaultName;
    }

    public IGH_Param CreateParam()
    {
        if (Instances.ComponentServer.EmitObjectProxy(ComponentGuid).CreateInstance()
            is not IGH_Param param)
        {
            throw new ArgumentException("The guid is not valid for creating a IGH_Param!");
        }

        return param;
    }

    public bool GetValue(IGH_DataAccess DA, out object value)
    {
        var method = (Access switch
        {
            GH_ParamAccess.list => GetDaMethod(DA, nameof(DA.GetDataList)),
            GH_ParamAccess.tree => GetDaMethod(DA, nameof(DA.GetDataTree)),
            _ => GetDaMethod(DA, nameof(DA.GetData)),
        }).MakeGenericMethod(CreateInnerType);

        object[] pms = [ParamIndex!, CreateType.CreateInstance()];

        value = null!;
        if (!(bool)method.Invoke(DA, pms)) return false;

        value = ChangeType(pms[1]);
        return true;

        static MethodInfo GetDaMethod(IGH_DataAccess DA, string name)
        {
            return DA.GetType().GetRuntimeMethods().First(m =>
            {
                if (m.Name != name) return false;
                var pms = m.GetParameters();
                if (pms.Length != 2) return false;
                if (pms[0].ParameterType.GetRawType() != typeof(int)) return false;
                return true;
            });
        }
    }

    private object ChangeType(object obj)
    {
        //Change inner type.
        ChangeTypeListOrTree(ref obj, CreateInnerType, InnerType);

        //To list or array.
        if (Access == GH_ParamAccess.list)
        {
            var name = RawType.IsArray ? "ToArray" : "ToList";
            obj = typeof(Enumerable).GetRuntimeMethods()
                .First(m => m.Name == name)
                .MakeGenericMethod(InnerType)
                .Invoke(null, [obj]);
        }

        return obj.ChangeType(Type);
    }

    public bool SetValue(IGH_DataAccess DA, object value)
    {
        ChangeTypeListOrTree(ref value, InnerType, CreateInnerType);

        return Access switch
        {
            GH_ParamAccess.list => DA.SetDataList(ParamIndex, value as IEnumerable),
            GH_ParamAccess.tree => DA.SetDataTree(ParamIndex, value as IGH_Structure),
            _ => DA.SetData(ParamIndex, value),
        };
    }

    private void ChangeTypeListOrTree(ref object obj, Type sourceType, Type targetType)
    {
        if (obj == null) return;
        if (Access == GH_ParamAccess.item) return;
        if (targetType == sourceType) return;

        switch (Access)
        {
            case GH_ParamAccess.list:
                ChangeTypeList(ref obj, sourceType, targetType);
                break;

            case GH_ParamAccess.tree:
                var dataInfo = obj.GetType().GetRuntimeFields().FirstOrDefault(p => p.Name == "m_data");

                var data = dataInfo.GetValue(obj);

                var pathsInfo = data.GetType().GetRuntimeFields().FirstOrDefault(p => p.Name == "keys");
                var valuesInfo = data.GetType().GetRuntimeFields().FirstOrDefault(p => p.Name == "values");

                var paths = (pathsInfo.GetValue(data) as IList)!;
                var values = (valuesInfo.GetValue(data) as IList)!;

                var treeType = typeof(GH_Structure<>).MakeGenericType(targetType);
                var result = treeType.CreateInstance();

                var addMethod = treeType.GetRuntimeMethods().FirstOrDefault(m => m.Name == "AppendRange" && m.GetParameters().Length == 2);

                for (int i = 0; i < paths.Count; i++)
                {
                    var path = paths[i];
                    var list = values[i];
                    ChangeTypeList(ref list, sourceType, targetType);
                    addMethod.Invoke(result, [list, path]);
                }

                obj = result;
                break;
        }
    }

    private static void ChangeTypeList(ref object list, Type sourceType, Type targetType)
    {
        var me = typeof(TypeParam).GetRuntimeMethods().First(m => m.Name == nameof(ChangeTypePrivate)).MakeGenericMethod(sourceType, targetType);
        var dele = Delegate.CreateDelegate(Expression.GetDelegateType(sourceType, targetType), me);

        list = typeof(Enumerable).GetRuntimeMethods()
            .First(m => m.Name == "Select")
            .MakeGenericMethod(sourceType, targetType)
            .Invoke(null, [list, dele]);
    }

    private static TR ChangeTypePrivate<TS, TR>(TS source)
    {
        if (typeof(TS).IsGeneralType(typeof(GH_Goo<>)) != null
            && typeof(TR).IsGeneralType(typeof(GH_Goo<>)) != null)
        {
            var getProp = typeof(TS).GetRuntimeProperties().FirstOrDefault(p => p.Name == "Value");
            var setProp = typeof(TR).GetRuntimeProperties().FirstOrDefault(p => p.Name == "Value");

            var result = (TR)typeof(TR).CreateInstance();

            var v = getProp.GetValue(source).ChangeType(setProp.PropertyType);
            setProp.SetValue(result, v);
            return result;
        }
        return (TR)source!.ChangeType(typeof(TR));
    }
}

