using UnityEngine;

namespace TheBetterRoles.Managers;

internal class CoroutineManager(IntPtr intPtr) : MonoBehaviour(intPtr)
{
    internal static CoroutineManager? Instance { get; private set; }
    private void Start()
    {
        Instance = this;
    }
}
