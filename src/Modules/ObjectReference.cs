using UnityEngine;

namespace TheBetterRoles.Modules;

internal class ObjectReference<T>(string path) where T : UnityEngine.Object
{
    private T? _instance;
    private bool _hasAttemptedLoad; // Prevent repeated failed searches

    internal bool HasInstance => _instance != null;
    internal T Instance
    {
        get
        {
            if (_instance != null || _hasAttemptedLoad)
                return _instance!; // Suppress null after failed load

            _instance = LoadInstance();
            _hasAttemptedLoad = true;

            if (_instance == null)
                throw new NullReferenceException($"Failed to load {typeof(T).Name} at path: {path}");

            return _instance;
        }
    }

    internal void SetInstance(T @ref)
    {
        _hasAttemptedLoad = true;
        _instance = @ref;
    }

    private T? LoadInstance()
    {
        if (string.IsNullOrEmpty(path))
            return null;

        var gameObject = FindGameObjectByPath(path);
        if (gameObject == null)
            return null;

        // Handle GameObject and Component types
        if (typeof(T) == typeof(GameObject))
            return gameObject as T;
        else if (typeof(T).IsSubclassOf(typeof(Component)))
            return gameObject.GetComponent<T>();

        throw new InvalidOperationException($"Unsupported type: {typeof(T)}");
    }

    private static GameObject? FindGameObjectByPath(string path)
    {
        string[] parts = path.Split('/');
        if (parts.Length == 0)
            return null;

        // Find root (optimized for known root names)
        Transform? current = null;
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == parts[0])
            {
                current = root.transform;
                break;
            }
        }
        if (current == null)
            return null;

        // Traverse path (direct children only)
        for (int i = 1; i < parts.Length; i++)
        {
            current = current.Find(parts[i]);
            if (current == null)
                return null;
        }

        return current.gameObject;
    }

    public static implicit operator T(ObjectReference<T> or) => or.Instance;
}