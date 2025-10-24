using UnityEngine;

namespace TheBetterRoles.Helpers;

/// <summary>
/// Provides utility methods for working with Unity GameObjects and MonoBehaviours,
/// including finding objects by name, destroying objects, and managing components.
/// </summary>
internal static class ObjectHelper
{
    /// <summary>
    /// Finds a GameObject by its name in the scene, including inactive objects.
    /// Returns null if no object with the specified name is found.
    /// </summary>
    internal static GameObject? FindObjectByName(string objectName)
    {
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>(true);

        foreach (GameObject obj in allObjects)
        {
            if (obj.name == objectName)
            {
                return obj;
            }
        }

        return null;
    }

    /// <summary>
    /// Destroys a GameObject if it is not null.
    /// </summary>
    internal static void DestroyObj(this GameObject obj)
    {
        if (obj != null)
        {
            UnityEngine.Object.Destroy(obj);
        }
    }

    /// <summary>
    /// Destroys the GameObject associated with a MonoBehaviour if it is not null.
    /// </summary>
    internal static void DestroyObj(this MonoBehaviour mono) => mono?.gameObject?.DestroyObj();

    /// <summary>
    /// Destroys a MonoBehaviour component if it is not null.
    /// </summary>
    internal static void DestroyMono(this MonoBehaviour mono) => UnityEngine.Object.Destroy(mono);

    /// <summary>
    /// Destroys all TextTranslatorTMP components in the children of a GameObject.
    /// </summary>
    internal static void DestroyTextTranslators(this GameObject obj)
    {
        var translators = obj.GetComponentsInChildren<TextTranslatorTMP>();
        if (translators.Length > 0) translators.ToList().ForEach(com => com.DestroyMono());
    }

    /// <summary>
    /// Destroys all TextTranslatorTMP components in the children of a MonoBehaviour's GameObject.
    /// </summary>
    internal static void DestroyTextTranslators(this MonoBehaviour mono) => mono.gameObject.DestroyTextTranslators();
}