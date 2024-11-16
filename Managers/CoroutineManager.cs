using UnityEngine;

namespace TheBetterRoles.Managers;

public class CoroutineManager : MonoBehaviour
{
    public static CoroutineManager? Instance { get; private set; }
    public void Start()
    {
        Instance = this;
    }
}
