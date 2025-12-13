using Reactor.Utilities.Extensions;
using UnityEngine;

namespace TheBetterRoles.Managers;

internal class CoroutineManager : MonoBehaviour
{
    private static CoroutineManager? instanceGlobal;
    internal static CoroutineManager Global
    {
        get
        {
            if (instanceGlobal == null)
            {
                instanceGlobal = Create(true);
            }

            return instanceGlobal;
        }
    }

    private static CoroutineManager? instanceScene;
    internal static CoroutineManager Scene
    {
        get
        {
            if (instanceScene == null)
            {
                instanceScene = Create(false);
            }

            return instanceScene;
        }
    }

    private static CoroutineManager Create(bool global)
    {
        if (global)
        {
            var cm = new GameObject("CoroutineManager(Global)").AddComponent<CoroutineManager>();
            cm.DontDestroy();
            return cm;
        }
        else
        {
            var cm = new GameObject("CoroutineManager(Scene)").AddComponent<CoroutineManager>();
            return cm;
        }
    }
}
