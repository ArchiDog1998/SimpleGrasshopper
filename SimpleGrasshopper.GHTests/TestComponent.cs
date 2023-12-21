using Grasshopper.Kernel;
using SimpleGrasshopper.Attributes;
using SimpleGrasshopper.DocumentObjects;
using System.Reflection;

namespace SimpleGrasshopper.GHTests;

[BaseComponent]
public abstract class TestComponent(MethodInfo[] methodInfos, string? name = null, string? nickName = null, string? description = null, string? subCategory = null, string? iconPath = null, GH_Exposure? exposure = null, string? message = null, bool isParallel = false) 
    : MethodComponent(methodInfos, name, nickName, description, subCategory, iconPath, exposure, message, isParallel)
{
}
