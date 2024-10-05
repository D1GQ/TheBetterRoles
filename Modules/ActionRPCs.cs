using AmongUs.GameOptions;
using HarmonyLib;
using InnerNet;

namespace TheBetterRoles;

static class ActionPatch
{
    [HarmonyPatch(typeof(PlayerControl))]
    class PlayerActionPatch
    {
        [HarmonyPatch(nameof(PlayerControl.CmdReportDeadBody))]
        [HarmonyPrefix]
        public static bool CmdReportDeadBody_Prefix(PlayerControl __instance, [HarmonyArgument(0)] NetworkedPlayerInfo target)
        {
            __instance.ReportBodySync(target);
            return false;
        }

        [HarmonyPatch(nameof(PlayerControl.ReportDeadBody))]
        [HarmonyPrefix]
        public static bool ReportDeadBody_Prefix(PlayerControl __instance, [HarmonyArgument(0)] NetworkedPlayerInfo target)
        {
            __instance.ReportBodySync(target);
            return false;
        }
    }
}

static class ActionRPCs
{
    public static PlayerControl? SenderPlayer;

    private static bool ValidateSenderCheck(PlayerControl? player = null) =>
        // If there is no SenderPlayer, validation passes
        SenderPlayer == null
        // If SenderPlayer is the host's character and the game is not in host state, validation passes
        || AmongUsClient.Instance.GetHost().Character == SenderPlayer && !GameStates.IsHost
        // If the player is provided, is the same as SenderPlayer, and the game is in host state, validation passes
        || player != null && player == SenderPlayer && GameStates.IsHost;

    private static bool ValidateHostCheck() => SenderPlayer != null && AmongUsClient.Instance.GetHost().Character == SenderPlayer || GameStates.IsHost;

    // Needs to be fixed, clients do not receive the RPC
    public static void EndGameSync(List<byte> winners, EndGameReason reason, CustomRoleTeam team)
    {
        if (ValidateHostCheck())
        {
            if (GameStates.IsHost)
            {
                var writer = AmongUsClient.Instance.StartActionRpc(RpcAction.EndGame, PlayerControl.LocalPlayer);
                writer.Write((byte)reason);
                writer.Write((byte)team);
                writer.Write(winners.Count);
                foreach (byte ids in winners)
                {
                    writer.Write(ids);
                }
                AmongUsClient.Instance.EndActionRpc(writer);
            }

            CustomGameManager.EndGame(winners, reason, team);
        }
    }

    // Set player role
    public static void SetRoleSync(this PlayerControl player, CustomRoles role, bool RemoveAddon = false, bool bypass = false)
    {
        if (GameStates.IsHost || bypass)
        {
            if (CheckSetRoleAction(player, role) == true || bypass)
            {
                if (!Utils.GetCustomRoleClass(role).IsAddon)
                {
                    CustomRoleManager.SetCustomRole(player, role);
                }
                else
                {
                    if (!RemoveAddon)
                    {
                        CustomRoleManager.AddAddon(player, role);
                    }
                    else
                    {
                        CustomRoleManager.RemoveAddon(player, role);
                    }
                }

                if (bypass) return;
            }
            else
            {
                return;
            }
        }

        var writer = AmongUsClient.Instance.StartActionRpc(RpcAction.SetRole, player);
        writer.Write((int)role);
        writer.Write(RemoveAddon);
        AmongUsClient.Instance.EndActionRpc(writer);
    }

    private static bool CheckSetRoleAction(PlayerControl player, CustomRoles role) => true;

    // Make a player kill a target
    public static void MurderSync(this PlayerControl player, PlayerControl target, bool isAbility = false, bool bypass = false)
    {
        if (GameStates.IsHost || bypass)
        {
            if (CheckMurderAction(player, target, isAbility) == true || bypass)
            {
                // Run after checks for roles
                CustomRoleManager.RoleListener(player, role => role.OnMurder(player, target, player == target, isAbility));
                CustomRoleManager.RoleListener(target, role => role.OnMurder(player, target, player == target, isAbility));

                CustomRoleManager.RoleListenerOther(role => role.OnMurderOther(player, target, player == target, isAbility));

                player.MurderPlayer(target, MurderResultFlags.Succeeded);

                if (bypass) return;
            }
            else
            {
                return;
            }
        }

        var writer = AmongUsClient.Instance.StartActionRpc(RpcAction.Murder, player);
        writer.WriteNetObject(target);
        writer.Write(isAbility);
        AmongUsClient.Instance.EndActionRpc(writer);
    }

    private static bool CheckMurderAction(PlayerControl player, PlayerControl target, bool isAbility)
    {
        if (!CustomRoleManager.RoleChecks(player, role => role.CheckMurder(player, target, player == target, isAbility)))
        {
            return false;
        }

        if (!CustomRoleManager.RoleChecks(target, role => role.CheckMurder(player, target, player == target, isAbility)))
        {
            return false;
        }

        if (!CustomRoleManager.RoleChecksOther(role => role.CheckMurderOther(player, target, player == target, isAbility)))
        {
            return false;
        }

        if (!CustomRoleManager.RoleChecks(player, role => role.CanKill) && !isAbility || target.IsInVent() || !target.IsAlive() || !ValidateSenderCheck(player))
        {
            Logger.Log($"Host Canceled Murder Action: Invalid");
            return false;
        }

        return true;
    }

    // Revive player
    public static void ReviveSync(this PlayerControl player, bool bypass = false)
    {
        if (GameStates.IsHost || bypass)
        {
            if (CheckReviveAction(player) == true || bypass)
            {
                player.Revive();
                player.RawSetRole(RoleTypes.Crewmate);
                if (bypass) return;
            }
            else
            {
                return;
            }
        }

        var writer = AmongUsClient.Instance.StartActionRpc(RpcAction.Revive, player);
        AmongUsClient.Instance.EndActionRpc(writer);
    }

    private static bool CheckReviveAction(PlayerControl player) => player != null && ValidateSenderCheck(player);

    // Make a player start meeting
    public static void ReportBodySync(this PlayerControl player, NetworkedPlayerInfo? bodyInfo, bool bypass = false)
    {
        var isButton = bodyInfo == null;

        if (GameStates.IsHost || bypass)
        {
            if (CheckReportBodyAction(player, bodyInfo, isButton) == true || bypass)
            {
                // Run after checks for roles
                CustomRoleManager.RoleListenerOther(role => role.OnResetAbilityState());
                CustomRoleManager.RoleListener(player, role => role.OnBodyReport(player, bodyInfo, isButton));
                CustomRoleManager.RoleListenerOther(role => role.OnBodyReportOther(player, bodyInfo, isButton));

                // Start Meeting
                DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(player);
                player.StartMeeting(bodyInfo);

                if (bypass) return;
            }
            else
            {
                return;
            }
        }

        var writer = AmongUsClient.Instance.StartActionRpc(RpcAction.ReportBody, player);
        writer.WriteNetObject(bodyInfo);
        AmongUsClient.Instance.EndActionRpc(writer);
    }

    private static bool CheckReportBodyAction(PlayerControl player, NetworkedPlayerInfo? bodyInfo, bool isButton)
    {

        if (!CustomRoleManager.RoleChecks(player, role => role.CheckBodyReport(player, bodyInfo, isButton)))
        {
            return false;
        }

        if (!CustomRoleManager.RoleChecksOther(role => role.CheckBodyReportOther(player, bodyInfo, isButton)))
        {
            return false;
        }

        if (!GameStates.IsInGamePlay || !ValidateSenderCheck(player))
        {
            Logger.Log($"Host Canceled Murder Action: Invalid");
            return false;
        }

        return true;
    }

    // Resync after ability duration
    public static void ResetAbilityStateSync(this PlayerControl player, int id, bool bypass = false)
    {
        if (GameStates.IsHost || bypass)
        {
            if ((CheckResetAbilityStateAction(player, id) == true || bypass) && player.RoleAssigned())
            {
                player.BetterData().RoleInfo.Role.OnAbilityDurationEnd(id);
                if (bypass) return;
            }
            else
            {
                return;
            }
        }

        var writer = AmongUsClient.Instance.StartActionRpc(RpcAction.ResetAbilityState, player);
        writer.Write(id);
        AmongUsClient.Instance.EndActionRpc(writer);
    }

    private static bool CheckResetAbilityStateAction(PlayerControl player, int id) => true;

    // Sync when player is pressed, for certain roles
    public static void PlayerPressSync(this PlayerControl player, PlayerControl target, bool bypass = false)
    {
        if (GameStates.IsHost || bypass)
        {
            if (CheckPlayerPressAction(player, target) == true || bypass)
            {
                // Run after checks for roles
                CustomRoleManager.RoleListener(player, role => role.OnPlayerPress(player, target));
                CustomRoleManager.RoleListener(target, role => role.OnPlayerPress(player, target));
                CustomRoleManager.RoleListenerOther(role => role.OnPlayerPressOther(player, target));

                if (bypass) return;
            }
            else
            {
                return;
            }
        }

        var writer = AmongUsClient.Instance.StartActionRpc(RpcAction.PlayerPress, player);
        writer.WriteNetObject(target);
        AmongUsClient.Instance.EndActionRpc(writer);
    }

    private static bool CheckPlayerPressAction(PlayerControl player, PlayerControl target) => true;

    // Make a player go in or out a vent
    public static void VentSync(this PlayerControl player, int ventId, bool Exit, bool IsRPC = false)
    {
        if (CheckVentAction(player, ventId, Exit) == true)
        {
            // Run after checks for roles
            CustomRoleManager.RoleListener(player, role => role.OnVent(player, ventId, Exit));

            CustomRoleManager.RoleListenerOther(role => role.OnVentOther(player, ventId, Exit));

            if (!Exit)
            {
                player.StartCoroutine(player.MyPhysics.CoEnterVent(ventId));
                if (player.IsLocalPlayer())
                {
                    ShipStatus.Instance.AllVents.FirstOrDefault(vent => vent.Id == ventId).SetButtons(
                        player.IsLocalPlayer() && CustomRoleManager.RoleChecks(player, role => role.CanMoveInVents, false));
                }
            }
            else
            {
                player.StartCoroutine(player.MyPhysics.CoExitVent(ventId));
                if (player.IsLocalPlayer())
                {
                    ShipStatus.Instance.AllVents.FirstOrDefault(vent => vent.Id == ventId).SetButtons(false);
                }
            }

            if (IsRPC) return;
        }

        var writer = AmongUsClient.Instance.StartActionRpc(RpcAction.Vent, player, true);
        writer.Write(ventId);
        writer.Write(Exit);
        AmongUsClient.Instance.EndActionRpc(writer);
    }

    private static bool CheckVentAction(PlayerControl player, int ventId, bool Exit)
    {
        if (!CustomRoleManager.RoleChecks(player, role => role.CheckVent(player, ventId, Exit)))
        {
            return false;
        }

        if (!CustomRoleManager.RoleChecksOther(role => role.CheckVentOther(player, ventId, Exit)))
        {
            return false;
        }

        if (ShipStatus.Instance == null)
        {
            Logger.Log($"Canceled Vent Action: ShipStatus Null");
        }

        return true;
    }
}
