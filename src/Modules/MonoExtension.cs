using Il2CppInterop.Runtime;
using UnityEngine;

namespace TheBetterRoles.Modules;

internal interface IMonoExtension
{
    MonoBehaviour? BaseMono { get; set; }
}

internal interface IMonoExtension<T> : IMonoExtension where T : MonoBehaviour
{
    new T? BaseMono { get; set; }

    MonoBehaviour? IMonoExtension.BaseMono
    {
        get => BaseMono;
        set => BaseMono = value as T;
    }
}

internal static class MonoExtensionManager
{
    private static readonly Dictionary<Type, List<ExtensionPair>> _extensionsByBaseType = [];

    internal static T? Get<T>(MonoBehaviour mono) where T : class, IMonoExtension
    {
        if (mono == null) return null;

        if (_extensionsByBaseType.TryGetValue(mono.GetType(), out var extensions))
        {
            foreach (var pair in extensions)
            {
                if (pair.Base == mono && pair.Extension is T result)
                {
                    return result;
                }
            }
        }

        return null;
    }

    internal static void CleanAll()
    {
        foreach (var kvp in _extensionsByBaseType.ToArray())
        {
            var extensions = kvp.Value;
            for (int i = extensions.Count - 1; i >= 0; i--)
            {
                if (extensions[i].Base == null || extensions[i].Extension == null)
                {
                    extensions.RemoveAt(i);
                }
            }

            if (extensions.Count == 0)
            {
                _extensionsByBaseType.Remove(kvp.Key);
            }
        }
    }

    internal static bool RegisterExtension(this IMonoExtension extension)
    {
        // Try to find IMonoExtension<T> implementation
        Type? genericInterface = extension.GetType()
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMonoExtension<>));

        if (genericInterface == null)
        {
            if (extension is MonoBehaviour mono)
                UnityEngine.Object.Destroy(mono);
            return false;
        }

        // Get T from IMonoExtension<T>
        Type monoType = genericInterface.GetGenericArguments()[0];

        if (!monoType.IsAssignableTo(typeof(MonoBehaviour)))
        {
            if (extension is MonoBehaviour mono)
                UnityEngine.Object.Destroy(mono);
            return false;
        }

        var monoBehaviour = extension as MonoBehaviour;
        if (monoBehaviour == null)
            return false;

        // Get the base component (including inactive ones)
        var baseComponent = monoBehaviour.GetComponentInChildren(Il2CppType.From(monoType), true) as MonoBehaviour;
        if (baseComponent == null)
        {
            UnityEngine.Object.Destroy(monoBehaviour);
            return false;
        }

        // Check for duplicate extensions (including inactive ones)
        var existingExtensions = monoBehaviour.GetComponentsInChildren(Il2CppType.From(extension.GetType()), true);
        if (existingExtensions.Length > 1)
        {
            UnityEngine.Object.Destroy(monoBehaviour);
            return false;
        }

        // Register the extension
        if (!_extensionsByBaseType.TryGetValue(monoType, out var extensions))
        {
            extensions = [];
            _extensionsByBaseType[monoType] = extensions;
        }

        extensions.Add(new ExtensionPair { Base = baseComponent, Extension = extension });
        extension.BaseMono = baseComponent;

        return true;
    }

    internal static void UnregisterExtension(this IMonoExtension extension)
    {
        if (extension.BaseMono == null) return;

        if (_extensionsByBaseType.TryGetValue(extension.BaseMono.GetType(), out var extensions))
        {
            for (int i = extensions.Count - 1; i >= 0; i--)
            {
                if (extensions[i].Extension == extension)
                {
                    extensions.RemoveAt(i);
                    break;
                }
            }

            if (extensions.Count == 0)
            {
                _extensionsByBaseType.Remove(extension.BaseMono.GetType());
            }
        }
    }

    private struct ExtensionPair
    {
        internal MonoBehaviour? Base;
        internal IMonoExtension? Extension;
    }
}