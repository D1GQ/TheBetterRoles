namespace TheBetterRoles;

public static class ShuffleListExtension
{
    private static readonly Random rng = new();

    /// <summary>
    /// Shuffles all elements in a collection randomly
    /// </summary>
    /// <typeparam name="T">The type of the collection</typeparam>
    /// <param name="collection">The collection to be shuffled</param>
    /// <param name="random">An instance of a randomizer algorithm</param>
    /// <returns>The shuffled collection</returns>
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> collection)
    {
        var list = collection.ToList();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = IRandom.Instance != null ? IRandom.Instance.Next(n + 1) : rng.Next(n + 1);
            (list[n], list[k]) = (list[k], list[n]);
        }
        return list;
    }
}
