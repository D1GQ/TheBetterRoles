using UnityEngine;

namespace TheBetterRoles.Helpers;

public static class ObjectHelper
{
    public static GameObject? FindObjectByName(string objectName)
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
    public static void DestroyObj(this GameObject obj)
    {
        if (obj != null)
        {
            UnityEngine.Object.Destroy(obj);
        }
    }
    public static void DestroyObj(this MonoBehaviour mono) => mono?.gameObject?.DestroyObj();
    public static void DestroyMono(this MonoBehaviour mono) => UnityEngine.Object.Destroy(mono);

    public static void DestroyTextTranslator(this GameObject obj)
    {
        var translator = obj.GetComponent<TextTranslatorTMP>();
        if (translator != null)
        {
            UnityEngine.Object.Destroy(translator);
        }
    }
    public static void DestroyTextTranslator(this MonoBehaviour mono) => mono.gameObject.DestroyTextTranslator();
}
