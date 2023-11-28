using SimpleGrasshopper.Attributes;

namespace SimpleGrasshopper.DocumentObjects;

/// <summary>
/// Create the <see cref="GH_Component"/> about one type.
/// </summary>
/// <typeparam name="T">the type.</typeparam>
public abstract class TypeMethodComponent<T>() 
    : MethodComponent(typeof(T).GetRuntimeMethods().Where(m => !m.IsSpecialName && m.DeclaringType != typeof(object)).ToArray(),
        typeof(T).GetCustomAttribute<TypeComponentAttribute>()?.Name,
        typeof(T).GetCustomAttribute<TypeComponentAttribute>()?.NickName,
        typeof(T).GetCustomAttribute<TypeComponentAttribute>()?.Description,
        typeof(T).GetCustomAttribute<TypeComponentAttribute>()?.SubCategory,
        typeof(T).GetCustomAttribute<TypeComponentAttribute>()?.IconPath,
        typeof(T).GetCustomAttribute<TypeComponentAttribute>()?.Exposure)
{
    /// <inheritdoc/>
    protected override Type? DeclaringType => typeof(T);
}
