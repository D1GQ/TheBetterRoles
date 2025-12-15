using BepInEx.Unity.IL2CPP.Utils;
using Cpp2IL.Core.Extensions;
using System.Collections;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Core.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Roles.Crewmates;

internal sealed class AurialRole : CrewmateRoleTBR, IRoleAbilityAction
{
    internal sealed override int RoleId => 50;
    internal sealed override string RoleColorHex => "#A14589";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Aurial;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Crewmate;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Information;
    internal sealed override OptionTab? SettingsTab => TBRTabs.CrewmateRoles;

    internal OptionItem? RadiateColourRange;
    internal OptionItem? RadiateMaxRange;
    internal OptionItem? SenseDuration;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                RadiateColourRange = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Aurial.Option.RadiateColourRange", (0.5f, 5f, 0.5f), 1.5f, ("", "x"), RoleOptions.RoleOptionItem),
                RadiateMaxRange = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Aurial.Option.RadiateMaxRange", (0.5f, 5f, 0.5f), 3f, ("", "x"), RoleOptions.RoleOptionItem),
                SenseDuration = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Aurial.Option.SenseDuration", (5f, 15f, 0.5f), 10f, ("", "s"), RoleOptions.RoleOptionItem)
            ];
        }
    }

    private readonly List<ArrowLocator> arrows = [];
    private readonly List<Coroutine?> coroutineList = [];

    internal sealed override void OnDeinitialize()
    {
        var arrowsToRemove = arrows.Clone();
        foreach (var arrow in arrowsToRemove)
        {
            arrow.Remove();
        }

        var coroutineToRemove = coroutineList.Clone();
        foreach (var coroutine in coroutineToRemove)
        {
            if (coroutine != null)
            {
                _roleMono.StopCoroutine(coroutine);
            }
        }
    }

    void IRoleAbilityAction.OnAbilityOther(int id, RoleClass role, TargetType targetType, MonoBehaviour target)
    {
        var user = role._player;
        if (user != _player && _player.IsLocalPlayer())
        {
            if (Vector2.Distance(user.GetTruePosition(), _player.GetTruePosition()) <= RadiateMaxRange.GetFloat())
            {
                bool useColor = Vector2.Distance(user.GetTruePosition(), _player.GetTruePosition()) <= RadiateColourRange.GetFloat();

                var coroutine = _roleMono.StartCoroutine(CreateArrow(user, useColor));
                coroutineList.Add(coroutine);
            }
        }
    }

    private IEnumerator CreateArrow(PlayerControl target, bool useColor)
    {
        var colorId = target.Data.DefaultOutfit.ColorId;
        Color32 color = useColor ? Palette.PlayerColors[colorId] : Color.white;

        var arrow = ArrowLocator.Create(color: color, maxScale: 0.5f, minDistance: 0f);
        var bodyColor = CustomColors.GetPlayerColorById(colorId);
        if (bodyColor.Animated)
        {
            ColorEffectBehavior.Add(arrow.Arrow.image, bodyColor.ColorEffect, false);
        }
        arrows.Add(arrow);
        arrow.Arrow.target = target.GetTruePosition();

        yield return new WaitForSeconds(SenseDuration.GetFloat());

        arrows.Remove(arrow);
        arrow.Remove();
    }
}
