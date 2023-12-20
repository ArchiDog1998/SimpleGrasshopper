using SimpleGrasshopper.Attributes;

namespace SimpleGrasshopper.DocumentObjects;

/// <summary>
/// Create the <see cref="GH_Component"/> about one type.
/// </summary>
/// <param name="type">the type of it.</param>
public abstract class TypeMethodComponent(Type type)
    : MethodComponent(type.GetMethods()
        .Where(m => !m.IsSpecialName && m.DeclaringType != typeof(object) && m.GetCustomAttribute<IgnoreAttribute>() == null)
        .ToArray(),
        type.GetCustomAttribute<TypeComponentAttribute>()?.Name,
        type.GetCustomAttribute<TypeComponentAttribute>()?.NickName,
        type.GetCustomAttribute<TypeComponentAttribute>()?.Description,
        type.GetCustomAttribute<TypeComponentAttribute>()?.SubCategory,
        type.GetCustomAttribute<TypeComponentAttribute>()?.IconPath,
        type.GetCustomAttribute<TypeComponentAttribute>()?.Exposure,
        type.GetCustomAttribute<MessageAttribute>()?.Message,
        type.GetCustomAttribute<ParallelAttribute>() != null)
{
    /// <inheritdoc/>
    protected override Type? DeclaringType => type;
}
