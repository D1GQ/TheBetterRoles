using System.Collections;
using TheBetterRoles.Network.Configs;

namespace TheBetterRoles.Data;

internal class ReadOnlyManager
{
    internal static readonly AddOnlyList<UserData> AllUsers = [new UserData("Default")];

    internal static readonly AddOnlyList<BannedUserData> AllBannedUsers = [new BannedUserData("Default")];

    internal static readonly AddOnlyList<CustomCosmeticConfig> AllCustomCosmeticConfigurations = [new CustomCosmeticConfig("default_config")];
}

/// <summary>
/// A read-only list that allows adding elements but not removing them.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
internal class AddOnlyList<T> : IReadOnlyList<T>
{
    private readonly List<T> _items = [];

    /// <summary>
    /// Gets the element at the specified index.
    /// </summary>
    /// <param name="index">The index of the element.</param>
    public T this[int index] => _items[index];

    /// <summary>
    /// Gets the number of elements contained in the list.
    /// </summary>
    public int Count => _items.Count;

    /// <summary>
    /// Adds an item to the list.
    /// </summary>
    /// <param name="item">The item to add.</param>
    internal void Add(T item) => _items.Add(item);

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
}