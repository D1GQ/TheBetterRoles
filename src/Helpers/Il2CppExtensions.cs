using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace TheBetterRoles.Helpers;

/// <summary>
/// Provides extension methods for working with IL2CPP collections and dictionary operations.
/// </summary>
internal static class Il2CppExtensions
{
    internal static void ForEachIl2Cpp<T>(this Il2CppSystem.Collections.Generic.IEnumerable<T> source, Action<T> action)
    {
        if (source == null || action == null) return;

        var list = new Il2CppSystem.Collections.Generic.List<T>(source);
        for (int i = 0; i < list.Count; i++)
        {
            action(list[i]);
        }
    }

    /// <summary>
    /// Converts an IEnumerable collection to an Il2CppArrayBase.
    /// </summary>
    internal static Il2CppArrayBase<T> ToIl2CppArray<T>(this IEnumerable<T> list) where T : Il2CppSystem.Object
    {
        var il2cppList = new Il2CppSystem.Collections.Generic.List<T>();
        foreach (var item in list)
            il2cppList.Add(item);
        return il2cppList.ToArray();
    }

    /// <summary>
    /// Converts an IEnumerable collection to an IL2CPP-compatible generic list.
    /// </summary>
    internal static Il2CppSystem.Collections.Generic.List<T> ToIl2CppList<T>(this IEnumerable<T> list) where T : Il2CppSystem.Object
    {
        var il2cppList = new Il2CppSystem.Collections.Generic.List<T>();
        foreach (var item in list)
            il2cppList.Add(item);
        return il2cppList;
    }

    /// <summary>
    /// Filters elements in an IL2CPP list based on a predicate.
    /// </summary>
    internal static List<T> WhereIL2CPP<T>(this Il2CppSystem.Collections.Generic.List<T> list, Func<T, bool> predicate)
    {
        var result = new List<T>();
        foreach (var item in list)
        {
            if (predicate(item))
                result.Add(item);
        }
        return result;
    }

    /// <summary>
    /// Filters elements in an IL2CPP list based on a predicate.
    /// </summary>
    internal static List<T> WhereIL2CPP<T>(this Il2CppArrayBase<T> array, Func<T, bool> predicate)
    {
        var result = new List<T>();
        foreach (var item in array)
        {
            if (predicate(item))
                result.Add(item);
        }
        return result;
    }

    /// <summary>
    /// Maps elements of an IL2CPP list using a selector function.
    /// </summary>
    internal static List<TResult> SelectIL2CPP<T, TResult>(this Il2CppSystem.Collections.Generic.List<T> list, Func<T, TResult> selector)
    {
        var result = new List<TResult>();
        foreach (var item in list)
            result.Add(selector(item));
        return result;
    }

    /// <summary>
    /// Maps elements of an IL2CPP list using a selector function.
    /// </summary>
    internal static List<TResult> SelectIL2CPP<T, TResult>(this Il2CppArrayBase<T> array, Func<T, TResult> selector)
    {
        var result = new List<TResult>();
        foreach (var item in array)
            result.Add(selector(item));
        return result;
    }

    /// <summary>
    /// Finds the first element that matches a predicate in an IL2CPP list.
    /// </summary>
    internal static T FirstIL2CPP<T>(this Il2CppSystem.Collections.Generic.List<T> list, Func<T, bool> predicate)
    {
        foreach (var item in list)
        {
            if (predicate(item))
            {
                if (item == null)
                    throw new InvalidOperationException("Element is null.");

                return item;
            }
        }
        throw new InvalidOperationException("No element satisfies the condition.");
    }

    /// <summary>
    /// Finds the first element that matches a predicate in an IL2CPP list.
    /// </summary>
    internal static T FirstIL2CPP<T>(this Il2CppSystem.Collections.Generic.List<T> list)
    {
        if (list.Count == 0)
        {
            throw new InvalidOperationException("No elements in list.");
        }

        T item = list[0];
        return item == null ? throw new InvalidOperationException("Element is null.") : item;
    }

    /// <summary>
    /// Finds the first element that matches a predicate in an IL2CPP list.
    /// </summary>
    internal static T FirstIL2CPP<T>(this Il2CppArrayBase<T> array, Func<T, bool> predicate)
    {
        foreach (var item in array)
        {
            if (predicate(item))
            {
                if (item == null)
                    throw new InvalidOperationException("Element is null.");

                return item;
            }
        }
        throw new InvalidOperationException("No element satisfies the condition.");
    }

    /// <summary>
    /// Finds the first element that matches a predicate in an IL2CPP list.
    /// </summary>
    internal static T FirstIL2CPP<T>(this Il2CppArrayBase<T> array)
    {
        if (array.Count == 0)
        {
            throw new InvalidOperationException("No elements in list.");
        }

        T item = array[0];
        return item == null ? throw new InvalidOperationException("Element is null.") : item;
    }

    /// <summary>
    /// Finds the first element that matches a predicate in an IL2CPP list.
    /// </summary>
    internal static T? FirstOrDefaultIL2CPP<T>(this Il2CppSystem.Collections.Generic.List<T> list, Func<T, bool> predicate)
    {
        foreach (var item in list)
        {
            if (predicate(item))
                return item;
        }
        return default;
    }

    /// <summary>
    /// Finds the first element that matches a predicate in an IL2CPP list.
    /// </summary>
    internal static T? FirstOrDefaultIL2CPP<T>(this Il2CppSystem.Collections.Generic.List<T> list)
    {
        if (list.Count == 0)
        {
            throw new InvalidOperationException("No elements in list.");
        }

        return list[0];
    }

    /// <summary>
    /// Finds the first element that matches a predicate in an IL2CPP list.
    /// </summary>
    internal static T? FirstOrDefaultIL2CPP<T>(this Il2CppArrayBase<T> array, Func<T, bool> predicate)
    {
        foreach (var item in array)
        {
            if (predicate(item))
                return item;
        }
        return default;
    }

    /// <summary>
    /// Finds the first element that matches a predicate in an IL2CPP list.
    /// </summary>
    internal static T? FirstOrDefaultIL2CPP<T>(this Il2CppArrayBase<T> array)
    {
        if (array.Count == 0)
        {
            throw new InvalidOperationException("No elements in list.");
        }

        return array[0];
    }

    internal static int CountIl2Cpp<T>(this Il2CppSystem.Collections.Generic.List<T> source, Func<T, bool>? predicate = null)
    {
        if (source == null) return 0;

        int count = 0;
        for (int i = 0; i < source.Count; i++)
        {
            if (predicate == null || predicate(source[i]))
                count++;
        }
        return count;
    }

    /// <summary>
    /// Checks if an IL2CPP list contains an element.
    /// </summary>
    internal static bool ContainsIL2CPP<T>(this Il2CppSystem.Collections.Generic.List<T> list, T item)
    {
        foreach (var element in list)
        {
            if (EqualityComparer<T>.Default.Equals(element, item))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if an IL2CPP list contains an element.
    /// </summary>
    internal static bool ContainsIL2CPP<T>(this Il2CppArrayBase<T> array, T item)
    {
        foreach (var element in array)
        {
            if (EqualityComparer<T>.Default.Equals(element, item))
                return true;
        }
        return false;
    }

    internal static List<T> WhereIl2Cpp<T>(this Il2CppSystem.Collections.Generic.List<T> source, Func<T, bool> predicate)
    {
        if (source == null || predicate == null) return [];

        var result = new List<T>();
        for (int i = 0; i < source.Count; i++)
        {
            var item = source[i];
            if (predicate(item))
                result.Add(item);
        }
        return result;
    }

    internal static bool AllIl2Cpp<T>(this Il2CppSystem.Collections.Generic.List<T> source, Func<T, bool> predicate)
    {
        if (source == null || predicate == null) return false;

        for (int i = 0; i < source.Count; i++)
        {
            if (!predicate(source[i]))
                return false;
        }
        return true;
    }

    public static bool AnyIl2Cpp<T>(this Il2CppSystem.Collections.Generic.List<T> list, Func<T, bool> predicate)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (predicate(list[i])) return true;
        }
        return false;
    }

    /// <summary>
    /// Finds the key-value pair with the maximum value in a dictionary and checks for ties.
    /// </summary>
    /// <param name="self">The dictionary to search.</param>
    /// <param name="tie">Outputs <c>true</c> if there is a tie for the maximum value, otherwise <c>false</c>.</param>
    /// <returns>The key-value pair with the highest value. If multiple pairs have the highest value, the first encountered is returned.</returns>
    internal static KeyValuePair<byte, int> MaxPair(this Dictionary<byte, int> self, out bool tie)
    {
        tie = true;
        KeyValuePair<byte, int> result = new KeyValuePair<byte, int>(byte.MaxValue, int.MinValue);
        foreach (KeyValuePair<byte, int> keyValuePair in self)
        {
            if (keyValuePair.Value > result.Value)
            {
                result = keyValuePair;
                tie = false;
            }
            else if (keyValuePair.Value == result.Value)
            {
                tie = true;
            }
        }
        return result;
    }
}