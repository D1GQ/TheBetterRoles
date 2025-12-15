using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Modules;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Roles.Core.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Roles.Core.RoleBase;

internal class RoleButtons
{
    /// <summary>
    /// List of all local ability buttons available to the player for this role. This can include things like kill, sabotage, or vent buttons.
    /// </summary>
    internal List<BaseButton> Buttons { get; set; } = [];

    /// <summary>
    /// The haunt button for the role, allowing the player to haunt a player when dead.
    /// </summary>
    internal BaseAbilityButton? HauntButton { get; set; }

    /// <summary>
    /// The report button for the role, allowing the player to report a body.
    /// </summary>
    internal DeadBodyAbilityButton? ReportButton { get; set; }

    /// <summary>
    /// The kill button for the role, allowing the player to perform kills if applicable.
    /// </summary>
    internal PlayerAbilityButton? KillButton { get; set; }

    /// <summary>
    /// The sabotage button for the role, allowing the player to perform sabotage actions.
    /// </summary>
    internal BaseAbilityButton? SabotageButton { get; set; }

    /// <summary>
    /// The vent button for the role, allowing the player to use vents if they have that ability.
    /// </summary>
    internal VentAbilityButton? VentButton { get; set; }

    internal void SetUpHauntButton(RoleClass role)
    {
        HauntButton = AddButton(BaseAbilityButton.Create(0, Translator.GetString(StringNames.HauntAbilityName), 0f, 0f, 0, Prefab.GetCachedPrefab<CrewmateGhostRole>().Ability.Image, role, true));
        HauntButton.ActionButton.graphic.material.shader = AssetBundles.GrayscaleShader;
        HauntButton.ActionButton.graphic.material.SetColor("_Color", Utils.GetCustomRoleTeamColor(role.RoleTeam) * 1.2f);
        HauntButton.UseAsDead = true;
        HauntButton.Text.SetOutlineColor(Utils.GetCustomRoleTeamColor(role.RoleTeam) * 1.2f);
        HauntButton.VisibleCondition = () => { return !HauntButton._player.IsAlive() && !HauntButton._player.IsGhostRole(); };
        HauntButton.OnClick = () =>
        {
            Minigame HauntMenu = Prefab.GetCachedPrefab<CrewmateGhostRole>().HauntMenu;
            if (Minigame.Instance)
            {
                if (Minigame.Instance is HauntMenuMinigame)
                {
                    Minigame.Instance.Close();
                }
                return;
            }
            Minigame minigame = UnityEngine.Object.Instantiate(HauntMenu);
            minigame.transform.SetParent(HauntButton.ActionButton.transform.parent, false);
            minigame.transform.SetLocalZ(-5f);
            minigame.transform.localScale = new(0.75f, 0.75f, 0.75f);
            minigame.Begin(null);
        };
    }

    internal void SetUpReportButton(RoleClass role)
    {
        ReportButton = AddButton(DeadBodyAbilityButton.Create(0, Translator.GetString(StringNames.ReportButton), 0f, 0f, 0, HudManager.Instance.ReportButton.graphic.sprite, null, true, TBRGameSettings.ReportDistance.GetValue() * 2));
        ReportButton.Text.SetOutlineColor(Color.black);
        ReportButton.ShowHighLight = false;
        ReportButton.VisibleCondition = () => { return !GameState.IsLobby; };
        ReportButton.AddDeadBodyCondition((body) => { return !body.Reported; });
        ReportButton.OnClick = () =>
        {
            if (ReportButton.lastDeadBody != null)
            {
                var data = Utils.PlayerDataFromPlayerId(ReportButton.lastDeadBody.ParentId);
                if (data != null)
                {
                    if (RoleListener.CheckAllRoles<IRoleReportAction>(role => role.CheckBody(ReportButton.lastDeadBody), player: PlayerControl.LocalPlayer) == false) return;
                    if (RoleListener.CheckAllRoles<IRoleReportAction>(role => role.CheckBodyOther(ReportButton.lastDeadBody)) == false) return;

                    PlayerControl.LocalPlayer.SendRpcReportBody(data);
                }
            }
        };
    }

    internal void SetUpSabotageButton(RoleClass role)
    {
        SabotageButton = AddButton(BaseAbilityButton.Create(1, Translator.GetString(StringNames.SabotageLabel), 0f, 0f, 0, HudManager._instance.SabotageButton.graphic.sprite, role, true));
        SabotageButton.UseAsDead = true;
        SabotageButton.VisibleCondition = () => { return SabotageButton.Role.CanSabotage; };
        SabotageButton.OnClick = () =>
        {
            if (SabotageButton.ActionButton.canInteract)
            {
                if (role.CanSabotage)
                {
                    HudManager.Instance.ToggleMapVisible(new MapOptions
                    {
                        Mode = MapOptions.Modes.Sabotage
                    });
                }
            }
        };
    }

    internal void SetUpKillButton(RoleClass role)
    {
        KillButton = AddButton(PlayerAbilityButton.Create(2, Translator.GetString(StringNames.KillLabel), VanillaGameSettings.KillCooldown.GetFloat(), 0, 0, HudManager.Instance.KillButton.graphic.sprite, role, true, VanillaGameSettings.KillDistance.GetValue()));
        KillButton.VisibleCondition = () => { return KillButton.Role.CanKill; };
        KillButton.TargetCondition = (target) =>
        {
            return !role.IsImpostor || !target.IsImpostorTeammate();
        };
        KillButton.OnClick = () =>
        {
            if (KillButton.lastTarget != null)
            {
                role.localPlayer.SendRpcMurder(KillButton.lastTarget);
                KillButton.SetCooldown();
            }
        };
    }

    internal void SetUpVentButton(RoleClass role)
    {
        VentButton = AddButton(VentAbilityButton.Create(3, Translator.GetString(StringNames.VentLabel), role.RoleOptions.VentCooldownOptionItem?.GetFloat() ?? 0, role.RoleOptions.VentDurationOptionItem?.GetFloat() ?? 0, 0, role, null, false, true));
        VentButton.VisibleCondition = () => { return RoleListener.CheckAnyRoles(role => role.CanVent, player: role._player); };
        VentButton.CanCancelDuration = true;
    }

    internal T AddButton<T>(T button) where T : BaseButton
    {
        Buttons.Add(button);
        return button;
    }

    internal void RemoveButton(BaseButton button)
    {
        if (button != null)
        {
            button.RemoveButton();
            Buttons.Remove(button);
        }
    }

    internal void ClearButtons()
    {
        foreach (var button in Buttons)
        {
            RemoveButton(button);
        }
    }
}
