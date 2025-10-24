using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using TheBetterRoles.Patches.UI;
using UnityEngine;

namespace TheBetterRoles.Managers;

internal class ModUpdateManager : MonoBehaviour
{
    internal void Update()
    {
        LateTask.Update(Time.deltaTime);
    }

    internal void LateUpdate()
    {
        TBRNotificationManager.LateUpdate();
        KeyListener.LateUpdate();
        LoadingColorPatch.LateUpdate();
        NetworkClass.LateUpdate();
    }
}