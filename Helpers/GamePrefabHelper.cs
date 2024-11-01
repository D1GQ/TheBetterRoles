using AmongUs.GameOptions;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace TheBetterRoles.Helpers;

public class GamePrefabHelper
{
    private static GameObject? prefabs;
    private static readonly List<RoleBehaviour> RoleInstances = [];

    public static T? GetRolePrefab<T>(RoleTypes roleType) where T : RoleBehaviour
    {
        if (prefabs == null)
        {
            prefabs = new GameObject("BaseRolePrefabs");
        }

        if (RoleInstances.Any(role => role.Role == roleType))
        {
            var existingRole = RoleInstances.FirstOrDefault(roles => roles.Role == roleType);
            return existingRole as T;
        }

        var rolePrefab = RoleManager.Instance?.AllRoles.FirstOrDefault(role => role.Role == roleType);
        if (rolePrefab == null)
        {
            return null;
        }

        var roleSetInstance = UnityEngine.Object.Instantiate(rolePrefab, prefabs.transform);
        var roleInstance = roleSetInstance.gameObject.GetComponent<T>();
        if (roleInstance == null)
        {
            return null;
        }
        RoleInstances.Add(roleInstance);

        return roleInstance;
    }

    public static UnityEngine.Object? GetPrefabByName(string objectName)
    {
        UnityEngine.Object[] allObjects = Resources.FindObjectsOfTypeAll(Il2CppType.Of<UnityEngine.Object>());

        var obj = allObjects.FirstOrDefault(obj => obj.hideFlags == HideFlags.None && obj.name == objectName);
        if (obj != null)
        {
            return obj;
        }

        return null;
    }
}
