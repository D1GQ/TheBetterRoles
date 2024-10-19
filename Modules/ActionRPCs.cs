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
    public static void EndGameSync(List<byte> winners, EndGameReason reason, CustomRoleTeam team, bool IsRPC = false)
    {
        if (!GameStates.IsHost && !IsRPC) return;

        if (ValidateHostCheck())
        {
            if (GameStates.IsHost)
            {
                var writer = AmongUsClient.Instance.StartActionSyncRpc(RpcAction.EndGame, PlayerControl.LocalPlayer);
                writer.Write((byte)reason);
                writer.Write((byte)team);
                writer.Write(winners.Count);
                foreach (byte ids in winners)
                {
                    writer.Write(ids);
                }
                AmongUsClient.Instance.EndActionSyncRpc(writer);
            }

            CustomGameManager.EndGame(winners, reason, team);
        }
    }

    // Set player role
    public static void SetRoleSync(this PlayerControl player, CustomRoles role, bool RemoveAddon = false, bool IsRPC = false)
    {
        if (CheckSetRoleAction(player, role) == true)
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
        }

        if (IsRPC) return;

        var writer = AmongUsClient.Instance.StartActionSyncRpc(RpcAction.SetRole, player);
        writer.Write((int)role);
        writer.Write(RemoveAddon);
        AmongUsClient.Instance.EndActionSyncRpc(writer);
    }

    private static bool CheckSetRoleAction(PlayerControl player, CustomRoles role) => true;

    // Make a player kill a target
    public static void MurderSync(this PlayerControl player, PlayerControl target, bool isAbility = false, bool snapToTarget = true,
        bool spawnBody = true, bool showAnimation = true, bool playSound = true, bool IsRPC = false)
    {
        if (CheckMurderAction(player, target, isAbility) == true)
        {
            // Run after checks for roles
            CustomRoleManager.RoleListener(player, role => role.OnMurder(player, target, player == target, isAbility));
            CustomRoleManager.RoleListener(target, role => role.OnMurder(player, target, player == target, isAbility));

            CustomRoleManager.RoleListenerOther(role => role.OnMurderOther(player, target, player == target, isAbility));

            player.BetterData().RoleInfo.Kills++;
            target.BetterData().RoleInfo.RoleTypeWhenAlive = target.BetterData().RoleInfo.RoleType;
            player.CustomMurderPlayer(target, snapToTarget, spawnBody, showAnimation, playSound);
        }

        if (IsRPC) return;

        var writer = AmongUsClient.Instance.StartActionSyncRpc(RpcAction.Murder, player);
        writer.WriteNetObject(target);
        byte flags = 0;
        flags |= (byte)((isAbility ? 1 : 0) << 0);
        flags |= (byte)((snapToTarget ? 1 : 0) << 1);
        flags |= (byte)((spawnBody ? 1 : 0) << 2);
        flags |= (byte)((showAnimation ? 1 : 0) << 3);
        flags |= (byte)((playSound ? 1 : 0) << 4);
        writer.Write(flags);
        AmongUsClient.Instance.EndActionSyncRpc(writer);
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

        if (!CustomRoleManager.RoleChecksAny(player, role => role.CanKill) && !isAbility || target.IsInVent() || !target.IsAlive())
        {
            Logger.Log($"Canceled Murder Action: Invalid");
            return false;
        }

        return true;
    }

    // Revive player
    public static void ReviveSync(this PlayerControl player, bool IsRPC = false)
    {
        if (CheckReviveAction(player) == true)
        {
            player.Revive();
            player.RawSetRole(RoleTypes.Crewmate);
        }

        if (IsRPC) return;

        var writer = AmongUsClient.Instance.StartActionSyncRpc(RpcAction.Revive, player);
        AmongUsClient.Instance.EndActionSyncRpc(writer);
    }

    private static bool CheckReviveAction(PlayerControl player) => player != null && ValidateSenderCheck(player);

    // Make a player start meeting
    public static void ReportBodySync(this PlayerControl player, NetworkedPlayerInfo? bodyInfo, bool IsRPC = false)
    {
        var flag = bodyInfo == null;

        if (CheckReportBodyAction(player, bodyInfo, flag) == true)
        {
            // Run after checks for roles
            CustomRoleManager.RoleListenerOther(role => role.OnResetAbilityState(false));
            CustomRoleManager.RoleListener(player, role => role.OnBodyReport(player, bodyInfo, flag));
            CustomRoleManager.RoleListenerOther(role => role.OnBodyReportOther(player, bodyInfo, flag));

            // Start Meeting
            if (GameStates.IsHost)
            {
                MeetingRoomManager.Instance.AssignSelf(player, bodyInfo);
                DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(player);
            }
            player.StartMeeting(bodyInfo);
        }

        if (IsRPC) return;

        var writer = AmongUsClient.Instance.StartActionSyncRpc(RpcAction.ReportBody, player);
        writer.WriteNetObject(bodyInfo);
        AmongUsClient.Instance.EndActionSyncRpc(writer);
    }

    private static bool CheckReportBodyAction(PlayerControl player, NetworkedPlayerInfo? bodyInfo, bool isButton)
    {
        if (MeetingHud.Instance)
        {
            return false;
        }
        if (AmongUsClient.Instance.IsGameOver)
        {
            return false;
        }
        if (player == null)
        {
            return false;
        }
        if (!CustomRoleManager.RoleChecks(player, role => role.CheckBodyReport(player, bodyInfo, isButton)))
        {
            return false;
        }
        if (!CustomRoleManager.RoleChecksOther(role => role.CheckBodyReportOther(player, bodyInfo, isButton)))
        {
            return false;
        }

        return true;
    }

    // Resync after ability duration
    public static void ResetAbilityStateSync(this PlayerControl player, int id, int roleType, bool isTimeOut, bool IsRPC = false)
    {
        if ((CheckResetAbilityStateAction(player, id) == true))
        {
            CustomRoleManager.RoleListener(player, role => role.OnAbilityDurationEnd(id, isTimeOut), role => role.RoleType == (CustomRoles)roleType);
            CustomRoleManager.RoleListener(player, role => role.OnAbilityDurationEnd(id, isTimeOut), role => role.RoleType == (CustomRoles)roleType);
        }

        if (IsRPC) return;

        var writer = AmongUsClient.Instance.StartActionSyncRpc(RpcAction.ResetAbilityState, player);
        writer.Write(id);
        writer.Write(roleType);
        writer.Write(isTimeOut);
        AmongUsClient.Instance.EndActionSyncRpc(writer);
    }

    private static bool CheckResetAbilityStateAction(PlayerControl player, int id) => true;

    // Sync when player is pressed, for certain roles
    public static void PlayerMenuSync(this PlayerControl player, int Id, int roleType, NetworkedPlayerInfo? target, PlayerMenu? menu, ShapeshifterPanel? playerPanel, bool close, bool IsRPC = false)
    {
        if (CheckPlayerMenuAction(player, target) == true)
        {
            CustomRoleManager.RoleListener(player, role => role.OnPlayerMenu(Id, target?.Object, target, menu, playerPanel, close), role => role.RoleType == (CustomRoles)roleType);
        }

        if (IsRPC) return;

        var writer = AmongUsClient.Instance.StartActionSyncRpc(RpcAction.PlayerMenu, player);
        writer.Write(Id);
        writer.Write(roleType);
        writer.Write(close);
        writer.WriteNetObject(target ?? null);
        AmongUsClient.Instance.EndActionSyncRpc(writer);
    }

    private static bool CheckPlayerMenuAction(PlayerControl player, NetworkedPlayerInfo? target) => true;

    // Sync when player is pressed, for certain roles
    public static void PlayerPressSync(this PlayerControl player, PlayerControl target, bool IsRPC = false)
    {
        if (CheckPlayerPressAction(player, target) == true)
        {
            // Run after checks for roles
            CustomRoleManager.RoleListener(player, role => role.OnPlayerPress(player, target));
            CustomRoleManager.RoleListener(target, role => role.OnPlayerPress(player, target));
            CustomRoleManager.RoleListenerOther(role => role.OnPlayerPressOther(player, target));
        }

        if (IsRPC) return;

        var writer = AmongUsClient.Instance.StartActionSyncRpc(RpcAction.PlayerPress, player);
        writer.WriteNetObject(target);
        AmongUsClient.Instance.EndActionSyncRpc(writer);
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
        }

        if (IsRPC) return;

        var writer = AmongUsClient.Instance.StartActionSyncRpc(RpcAction.Vent, player);
        writer.Write(ventId);
        writer.Write(Exit);
        AmongUsClient.Instance.EndActionSyncRpc(writer);
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
