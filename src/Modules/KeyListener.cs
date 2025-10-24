using Reactor.Networking.Rpc;
using System.Text;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Monos;
using TheBetterRoles.Network;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Patches.Manager;
using UnityEngine;

namespace TheBetterRoles.Modules;

internal class KeyListener
{
    private static int AddonInfoIndex = 0;
    private static int AddonSettingsIndex = 0;
    internal static void LateUpdate()
    {
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.LeftAlt))
        {
            // Test key
            if (Input.GetKeyDown(KeyCode.T))
            {
            }

            if (GameState.IsHost)
            {
                if (Input.GetKeyDown(KeyCode.L))
                {
                    if (GameState.IsInGamePlay && !GameState.IsFreePlay)
                    {
                        Rpc<RpcEndGame>.Instance.Send(new([], EndGameReason.ByHost, RoleClassTeam.None), true);
                    }
                }

                var boolN = Input.GetKeyDown(KeyCode.N);
                if (Input.GetKeyDown(KeyCode.M) || boolN)
                {
                    if (GameState.IsInGamePlay)
                    {
                        if (!GameState.IsMeeting && !boolN)
                        {
                            PlayerControl.LocalPlayer.SendRpcReportBody(null);
                        }
                        else if (GameState.IsMeeting)
                        {
                            if (!boolN)
                            {
                                MeetingHudPatch.DoNotCalculate = true;
                            }
                            MeetingHud.Instance?.RpcClose();
                        }
                    }
                }
            }
        }

        if (!GameState.IsLobby)
        {
            if (Input.GetKeyDown(KeyCode.F1) && PlayerControl.LocalPlayer.Role() != null)
            {
                var role = PlayerControl.LocalPlayer.Role();
                StringBuilder sb = new();
                sb.Append($"<size=75%>{Translator.GetString("Role", [Utils.GetCustomRoleNameAndColor(role.RoleType)])}\n");
                sb.Append($"{Translator.GetString("Role.Team", [$"<{Utils.GetCustomRoleTeamColorHex(role.RoleTeam)}>{Utils.GetCustomRoleTeamName(role.RoleTeam)}</color>"])}\n");
                sb.Append(Translator.GetString("Role.Category", [$"{Utils.GetCustomRoleCategoryName(role.RoleCategory)}\n\n"]));
                sb.Append($"{Utils.GetCustomRoleInfo(role.RoleType, true)}</size>");
                HudManager.Instance?.ShowPopUp(sb.ToString());
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                var addons = PlayerControl.LocalPlayer.ExtendedData().RoleInfo.Addons.ToList();
                if (addons.Any())
                {
                    if (AddonInfoIndex >= addons.Count) AddonInfoIndex = 0;
                    var addon = addons[AddonInfoIndex];
                    AddonInfoIndex++;

                    if (addon != null)
                    {
                        StringBuilder sb = new();
                        sb.Append($"<size=75%>{Translator.GetString("Role.Addon", [Utils.GetCustomRoleNameAndColor(addon.RoleType)])}\n");
                        sb.Append(Translator.GetString("Role.Category", [$"{Utils.GetCustomRoleCategoryName(addon.RoleCategory)}\n\n"]));
                        sb.Append($"{Utils.GetCustomRoleInfo(addon.RoleType, true)}</size>");
                        HudManager.Instance.ShowPopUp(sb.ToString());
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.F3) && PlayerControl.LocalPlayer.Role() != null)
            {
                var role = Utils.GetCustomRoleClass(PlayerControl.LocalPlayer.Role().RoleType);
                HudManager.Instance?.ShowPopUp(role.RoleOptions.RoleOptionItem?.FormatOptionsToTextTree() ?? string.Empty);
            }

            if (Input.GetKeyDown(KeyCode.F4) && PlayerControl.LocalPlayer.Role() != null)
            {
                var addons = PlayerControl.LocalPlayer.ExtendedData().RoleInfo.Addons.ToList();
                if (addons.Any())
                {
                    if (AddonSettingsIndex >= addons.Count) AddonSettingsIndex = 0;
                    var addon = addons[AddonSettingsIndex];
                    AddonSettingsIndex++;

                    if (addon != null)
                    {
                        HudManager.Instance?.ShowPopUp(addon.RoleOptions.RoleOptionItem?.FormatOptionsToTextTree() ?? string.Empty);
                    }
                }
            }
        }
        else
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    SettingsHudDisplay.Instance?.Previous();
                }
            }
            else if (Input.GetKeyDown(KeyCode.Tab))
            {
                SettingsHudDisplay.Instance?.Next();
            }
        }

        UpdateVanillaKeys();
    }

    private static void UpdateVanillaKeys()
    {
        if (KeyboardJoystick.player == null) return;
        var localPlayer = PlayerControl.LocalPlayer;

        if (localPlayer?.Role() != null)
        {
            // Kill keybind
            if (KeyboardJoystick.player.GetButtonDown(8) &&
                localPlayer.Role().RoleButtons.KillButton != null)
            {
                localPlayer.Role().RoleButtons.KillButton.Button.OnClick.Invoke();
            }

            // Vent keybind
            if (KeyboardJoystick.player.GetButtonDown(50) &&
                localPlayer.Role().RoleButtons.VentButton != null)
            {
                localPlayer.Role().RoleButtons.VentButton.Button.OnClick.Invoke();
            }
        }
    }
}
