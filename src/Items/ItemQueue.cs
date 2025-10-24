namespace TheBetterRoles.Items;

/// <summary>
/// Represents a queue of items of type <typeparamref name="T"/>, where each item has a name, a value, and a priority.
/// The queue supports adding, removing, and retrieving items based on their priority.
/// </summary>
internal class ItemQueue<T> where T : class
{
    private List<(string Name, T Item, uint Priority)> queue;
    private T? DefaultItem;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemQueue{T}"/> class.
    /// </summary>
    /// <param name="defaultItem">The default item to use when the queue is empty.</param>
    internal ItemQueue(T? defaultItem = null)
    {
        DefaultItem = defaultItem;
        ResetQueue();
    }

    /// <summary>
    /// Resets the queue to its initial state. If a default item is provided, it is added as the first item.
    /// </summary>
    internal void ResetQueue()
    {
        if (DefaultItem != null)
        {
            queue = new List<(string, T, uint)> { ("Default", DefaultItem, 0) };
        }
        else
        {
            queue = new List<(string, T, uint)>();
        }
    }

    /// <summary>
    /// Adds a new item to the queue with a specified name, value, and priority.
    /// </summary>
    /// <param name="name">The name of the item to add.</param>
    /// <param name="value">The value of the item to add.</param>
    /// <param name="priority">The priority of the item (default is 0).</param>
    internal void Add(string name, T value, uint priority = 0)
    {
        if (!queue.Any(entry => entry.Name == name))
        {
            queue.Add((name, value, priority));
        }
    }

    /// <summary>
    /// Removes an item from the queue by its name.
    /// </summary>
    /// <param name="name">The name of the item to remove.</param>
    /// <returns>True if the item was removed successfully, otherwise false.</returns>
    internal bool Remove(string name)
    {
        var itemToRemove = queue.FirstOrDefault(entry => entry.Name == name);
        if (!string.IsNullOrEmpty(itemToRemove.Name))
        {
            queue.Remove(itemToRemove);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Retrieves the item with the highest priority from the queue.
    /// If the queue is empty, the default item is returned.
    /// </summary>
    /// <returns>The item with the highest priority, or the default item if the queue is empty.</returns>
    internal T? Get()
    {
        if (queue.Count == 0)
        {
            return DefaultItem;
        }

        return queue.OrderByDescending(entry => entry.Priority)
                    .ThenBy(entry => queue.IndexOf(entry))
                    .First()
                    .Item;
    }

    /// <summary>
    /// Calculates the total sum of the priorities of all items in the queue.
    /// </summary>
    /// <returns>The sum of the priorities of all items in the queue.</returns>
    internal long Sum()
    {
        return queue.Sum(entry => entry.Priority);
    }
}
