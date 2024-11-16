using TheBetterRoles.Modules;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheBetterRoles.Managers;

public class CoroutineManager : MonoBehaviour
{
    public static CoroutineManager? Instance {  get; private set; }
    public void Start()
    {
        Instance = this;
    }
}
