using System.Reflection;
using TheBetterRoles.Commands;
using TheBetterRoles.Roles.Core;

namespace TheBetterRoles.Items.Attributes;

internal abstract class InstanceAttribute : Attribute
{
    internal static void RegisterAll()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var types = assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(InstanceAttribute)) && !t.IsAbstract && t.IsSealed)
            .ToArray();

        foreach (var type in types)
        {
            if (Activator.CreateInstance(type) is InstanceAttribute attribute)
            {
                attribute.RegisterInstances();
            }
        }
    }

    protected abstract void RegisterInstances();
}

[AttributeUsage(AttributeTargets.Class)]
internal abstract class StaticInstanceAttribute<T> : InstanceAttribute where T : class
{
    private static readonly List<T> _instances = [];
    internal static IReadOnlyList<T> Instances => _instances.AsReadOnly();
    internal static J? GetClassInstance<J>() where J : class => _instances.FirstOrDefault(instance => instance.GetType() == typeof(J)) as J;

    protected override void RegisterInstances()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var attributedTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttributes(GetType(), false).Any());

        foreach (var type in attributedTypes)
        {
            if (typeof(T).IsAssignableFrom(type))
            {
                var constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Type.EmptyTypes, null);
                if (constructor != null && constructor.Invoke(null) is T instance)
                {
                    _instances.Add(instance);
                }
            }
        }
    }
}

// Class instances
internal sealed class RegisterRoleAttribute : StaticInstanceAttribute<RoleClass>
{
}

internal sealed class RegisterCommandAttribute : StaticInstanceAttribute<BaseCommand>
{
}