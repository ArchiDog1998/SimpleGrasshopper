using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using SimpleGrasshopper.Attributes;
using SimpleGrasshopper.Util;
using System.Collections;
using System.Linq.Expressions;

namespace SimpleGrasshopper.Data;

internal readonly struct TypeParam
{
    public int ParamIndex { get; }
    public Type Type { get; }
    public Type RawType { get; }
    public Type CreateType { get; }
    public Type InnerType { get; }
    public Type RawInnerType { get; }
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

        var rawInnerTypeNoGoo = RawInnerType.IsGeneralType(typeof(GH_Goo<>)) is Type rawType
            ? rawType.GetRawType() : RawInnerType;

        CreateInnerType = rawInnerTypeNoGoo.IsEnum
            ? (Access == GH_ParamAccess.tree ? typeof(GH_Integer) : typeof(int))
            : InnerType;

        CreateType = Access switch
        {
            GH_ParamAccess.list => typeof(List<>).MakeGenericType(CreateInnerType),
            GH_ParamAccess.tree => typeof(GH_Structure<>).MakeGenericType(CreateInnerType),
            _ => CreateInnerType,
        };

        ComponentGuid = rawInnerTypeNoGoo.GetDocObjGuid();

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
            else if (type.IsGeneralType(typeof(List<>)) is Type listType)
            {
                access = GH_ParamAccess.list;
                return listType;
            }
            else
            {
                access = GH_ParamAccess.item;
                return type;
            }
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

        object[] pms = [ParamIndex!, Activator.CreateInstance(CreateType)];

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
        ChangeTypeListOrTree(ref value, InnerType, CreateType);

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

        var me = typeof(TypeParam).GetRuntimeMethods().First(m => m.Name == nameof(ChangeTypePrivate)).MakeGenericMethod(sourceType, targetType);

        switch (Access)
        {
            case GH_ParamAccess.list:
                var dele = Delegate.CreateDelegate(Expression.GetDelegateType(sourceType, targetType), me);

                obj = typeof(Enumerable).GetRuntimeMethods()
                    .First(m => m.Name == "Select")
                    .MakeGenericMethod(sourceType, targetType)
                    .Invoke(null, [obj, dele]);
                break;

            case GH_ParamAccess.tree:
                var t = obj.GetType().GetNestedTypes()[0].MakeGenericType(sourceType, sourceType, targetType);
                dele = Delegate.CreateDelegate(t, me);

                obj = obj.GetType().GetRuntimeMethods()
                    .First(m => m.Name == "DuplicateCast")
                    .MakeGenericMethod(targetType)
                    .Invoke(obj, [dele]);
                break;
        }
    }

    private static TR ChangeTypePrivate<TS, TR>(TS source)
    {
        if(typeof(TS).GetInterface("Grasshopper.Kernel.Types.IGH_Goo") != null 
            && typeof(TR).GetInterface("Grasshopper.Kernel.Types.IGH_Goo") != null)
        {
            throw new Exception("Hi!");
        }
        return (TR)source!.ChangeType(typeof(TR));
    }
}

