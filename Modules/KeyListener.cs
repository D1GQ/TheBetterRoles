using Reactor.Networking.Rpc;
using System.Text;
using TheBetterRoles.Helpers;
using TheBetterRoles.Managers;
using TheBetterRoles.RPCs;
using UnityEngine;

namespace TheBetterRoles.Modules;

public class KeyListener
{
    private static int AddonInfoIndex = 0;
    private static int AddonSettingsIndex = 0;
    public static void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (GameState.IsHost)
            {
                if (Input.GetKeyDown(KeyCode.L))
                {
                    if (GameState.IsInGamePlay && !GameState.IsFreePlay)
                    {
                        Rpc<RpcEndGame>.Instance.Send(PlayerControl.LocalPlayer, new([], EndGameReason.ByHost, CustomRoleTeam.None));
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
                        else
                        {
                            if (!boolN)
                            {
                                MeetingHudPatch.DoNotCalculate = true;
                            }
                            DestroyableSingleton<MeetingHud>.Instance?.RpcClose();
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
                sb.Append($"<size=75%>{string.Format(Translator.GetString("Role"), Utils.GetCustomRoleNameAndColor(role.RoleType))}\n");
                sb.Append($"{string.Format(Translator.GetString("Role.Team"), $"<{Utils.GetCustomRoleTeamColor(role.RoleTeam)}>{Utils.GetCustomRoleTeamName(role.RoleTeam)}</color>")}\n");
                sb.Append(string.Format(Translator.GetString("Role.Category"), $"{Utils.GetCustomRoleCategoryName(role.RoleCategory)}\n\n"));
                sb.Append($"{Utils.GetCustomRoleInfo(role.RoleType, true)}</size>");
                DestroyableSingleton<HudManager>.Instance?.ShowPopUp(sb.ToString());
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                var addons = PlayerControl.LocalPlayer.BetterData().RoleInfo.Addons.ToList();
                if (addons.Any())
                {
                    if (AddonInfoIndex >= addons.Count) AddonInfoIndex = 0;
                    var addon = addons[AddonInfoIndex];
                    AddonInfoIndex++;

                    if (addon != null)
                    {
                        StringBuilder sb = new();
                        sb.Append($"<size=75%>{string.Format(Translator.GetString("Role.Addon"), Utils.GetCustomRoleNameAndColor(addon.RoleType))}\n");
                        sb.Append(string.Format(Translator.GetString("Role.Category"), $"{Utils.GetCustomRoleCategoryName(addon.RoleCategory)}\n\n"));
                        sb.Append($"{Utils.GetCustomRoleInfo(addon.RoleType, true)}</size>");
                        DestroyableSingleton<HudManager>.Instance.ShowPopUp(sb.ToString());
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.F3) && PlayerControl.LocalPlayer.Role() != null)
            {
                var role = Utils.GetCustomRoleClass(PlayerControl.LocalPlayer.Role().RoleType);
                DestroyableSingleton<HudManager>.Instance?.ShowPopUp(role.RoleOptionItem?.FormatOptionsToText() ?? string.Empty);
            }

            if (Input.GetKeyDown(KeyCode.F4) && PlayerControl.LocalPlayer.Role() != null)
            {
                var addons = PlayerControl.LocalPlayer.BetterData().RoleInfo.Addons.ToList();
                if (addons.Any())
                {
                    if (AddonSettingsIndex >= addons.Count) AddonSettingsIndex = 0;
                    var addon = addons[AddonSettingsIndex];
                    AddonSettingsIndex++;

                    if (addon != null)
                    {
                        DestroyableSingleton<HudManager>.Instance?.ShowPopUp(addon.RoleOptionItem?.FormatOptionsToText() ?? string.Empty);
                    }
                }
            }
        }
    }
}
