using System.Runtime.InteropServices;
using UnityEngine;

namespace TheBetterRoles;

public static class ObjectHelper
{
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
