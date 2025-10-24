using TheBetterRoles.Helpers;
using UnityEngine;

namespace TheBetterRoles.Items;

/// <summary>
/// A MonoBehaviour that manages the unique hash of a GameObject based on its name and hierarchy.
/// </summary>
internal class ObjectHash : MonoBehaviour
{
    internal static readonly List<ObjectHash> ObjectHashes = [];

    internal GameObject Object { get; private set; }

    /// <summary>
    /// The unique hash associated with this object.
    /// </summary>
    internal ushort UniqueHash { get; set; } = 0;

    /// <summary>
    /// Determines whether the object's path (hierarchy) should be included in the hash.
    /// </summary>
    internal bool UsePath { get; set; } = true;

    /// <summary>
    /// Retrieves the unique hash for the object, combining its path (if enabled) and its own unique identifier.
    /// </summary>
    internal ushort Hash => Utils.GetHashUInt16($"{GetUniqueIdentifier(gameObject)}");

    /// <summary>
    /// Generates a unique identifier for the object based on its hierarchy and unique hash.
    /// </summary>
    /// <param name="obj">The GameObject to generate the identifier for.</param>
    /// <returns>A unique string identifier for the object.</returns>
    private string GetUniqueIdentifier(GameObject obj)
    {
        Transform current = obj.transform;
        string path = string.Empty;

        if (UsePath)
        {
            path = obj.name;
            while (current.parent != null)
            {
                current = current.parent;
                path = $"{current.name}/{path}";
            }
            path += ":";
        }

        return $"{path}{ObjectHashes.IndexOf(this)}:{UniqueHash}";
    }

    /// <summary>
    /// Called when the object is initialized. Adds this object to the list of ObjectHashes.
    /// </summary>
    internal void Start()
    {
        ObjectHashes.Add(this);
        Object = gameObject;
    }

    /// <summary>
    /// Called when the object is destroyed. Removes this object from the list of ObjectHashes.
    /// </summary>
    internal void OnDestroy()
    {
        ObjectHashes.Remove(this);
    }
}

/// <summary>
/// Extension methods for managing the unique hash of GameObjects and MonoBehaviours.
/// </summary>
internal static class ObjectHashExtension
{
    internal static ObjectHash? TryGetObjectByHash(ushort hash) => ObjectHash.ObjectHashes.FirstOrDefault(objHash => objHash.Hash == hash);
    internal static ObjectHash GetObjectByHash(ushort hash) => ObjectHash.ObjectHashes.First(objHash => objHash.Hash == hash);

    /// <summary>
    /// Retrieves the unique hash of the specified GameObject.
    /// </summary>
    /// <param name="obj">The GameObject to retrieve the hash for.</param>
    /// <returns>The unique hash of the object, or 0 if the object does not have a hash.</returns>
    internal static ushort GetObjHash(this GameObject obj) => obj?.GetComponent<ObjectHash>()?.Hash ?? 0;

    /// <summary>
    /// Retrieves the unique hash of the GameObject attached to the specified MonoBehaviour.
    /// </summary>
    /// <param name="mono">The MonoBehaviour to retrieve the object's hash for.</param>
    /// <returns>The unique hash of the object, or 0 if the object does not have a hash.</returns>
    internal static ushort GetObjHash(this MonoBehaviour mono) => mono.gameObject.GetObjHash();

    /// <summary>
    /// Sets the unique hash for the specified GameObject, optionally using its hierarchy and a custom hash.
    /// </summary>
    /// <param name="obj">The GameObject to set the unique hash for.</param>
    /// <param name="uniqueHash">The custom unique hash to assign to the object (default is 0).</param>
    /// <param name="usePath">Whether to include the object's path in the hash (default is true).</param>
    internal static void SetObjHash(this GameObject obj, ushort uniqueHash = 0, bool usePath = true)
    {
        obj?.GetComponent<ObjectHash>()?.DestroyMono();
        var hash = obj.AddComponent<ObjectHash>();
        hash.UniqueHash = uniqueHash;
        hash.UsePath = usePath;
    }

    /// <summary>
    /// Sets the unique hash for the MonoBehaviour's attached GameObject.
    /// </summary>
    /// <param name="mono">The MonoBehaviour whose GameObject will have the unique hash set.</param>
    /// <param name="uniqueHash">The custom unique hash to assign to the object (default is 0).</param>
    internal static void SetObjHash(this MonoBehaviour mono, ushort uniqueHash = 0) => mono.gameObject.SetObjHash(uniqueHash);
}