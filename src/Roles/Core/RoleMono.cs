using Il2CppInterop.Runtime.Attributes;
using TheBetterRoles.Roles.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Roles.Core;

internal class RoleMono : MonoBehaviour
{
    private RoleClass? _role;
    [HideFromIl2Cpp]
    internal RoleClass? Role => _role;

    internal static RoleMono Create()
    {
        var go = new GameObject("RoleMono");
        var rm = go.AddComponent<RoleMono>();
        return rm;
    }

    [HideFromIl2Cpp]
    internal void Setup(RoleClass role)
    {
        if (_role != null) return;
        _role = role;
        gameObject.name = $"RoleMono({role.RoleName})";
    }

    internal void Deinitialize()
    {
        Destroy(gameObject);
    }

    private void Update()
    {
        if (_role._player == null)
        {
            Deinitialize();
            return;
        }

        if (_role is IRoleUpdateAction action)
        {
            action.Update();
        }
    }

    private void FixedUpdate()
    {
        if (_role is IRoleUpdateAction action)
        {
            action.FixedUpdate();
        }
    }

    private void LateUpdate()
    {
        if (_role is IRoleUpdateAction action)
        {
            action.LateUpdate();
        }
    }

    private void OnDestroy()
    {
        if (_role == null || _role.HasDeinitialize) return;
        _role.Deinitialize();
    }
}
