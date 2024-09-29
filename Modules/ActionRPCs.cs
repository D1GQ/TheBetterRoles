using Epic.OnlineServices.Mods;
using HarmonyLib;
using InnerNet;
using Microsoft.Extensions.Logging;

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
    public static void EndGameSync(List<byte> winners, EndGameReason reason)
    {
        if (ValidateHostCheck())
        {
            if (GameStates.IsHost)
            {
                var writer = AmongUsClient.Instance.StartActionRpc(RpcAction.EndGame, PlayerControl.LocalPlayer);
                writer.Write((byte)reason);
                writer.Write(winners.Count);
                foreach (byte ids in winners)
                {
                    writer.Write(ids);
                }
                AmongUsClient.Instance.EndActionRpc(writer);
            }

            CustomGameManager.EndGame(winners, reason);
        }
    }

    // Set player role
    public static void SetRoleSync(this PlayerControl player, CustomRoles role, bool IsAddon = false, bool RemoveAddon = false, bool bypass = false)
    {
        if (GameStates.IsHost || bypass)
        {
            if (CheckSetRoleAction(player, role) == true || bypass)
            {
                if (!IsAddon)
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
        writer.Write(IsAddon);
        writer.Write(RemoveAddon);
        AmongUsClient.Instance.EndActionRpc(writer);
    }

    private static bool CheckSetRoleAction(PlayerControl player, CustomRoles role) => true;

    // Make a player go in or out a vent
    public static void VentSync(this PlayerControl player, int ventId, bool Exit, bool bypass = false)
    {
        if (GameStates.IsHost || bypass)
        {
            if (CheckVentAction(player, ventId, Exit) == true || bypass)
            {
                if (!Exit)
                {
                    player.StartCoroutine(player.MyPhysics.CoEnterVent(ventId));
                    if (player.IsLocalPlayer())
                    {
                        ShipStatus.Instance.AllVents.FirstOrDefault(vent => vent.Id == ventId).SetButtons(player.CanMoveInVent());
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

                // Run after checks for roles
                Main.AllPlayerControls.First(pc => pc == player && pc.RoleAssigned())
                    .BetterData().RoleInfo.Role.OnVent(player, ventId, Exit);

                Main.AllPlayerControls.Where(pc => pc.RoleAssigned()).ToList()
                    .ForEach(pc => pc.BetterData().RoleInfo.Role.OnVentOther(player, ventId, Exit));

                if (bypass) return;
            }
            else
            {
                return;
            }
        }

        var writer = AmongUsClient.Instance.StartActionRpc(RpcAction.Vent, player);
        writer.Write(ventId);
        writer.Write(Exit);
        AmongUsClient.Instance.EndActionRpc(writer);
    }

    private static bool CheckVentAction(PlayerControl player, int ventId, bool Exit)
    {
        var playerRoleInfo = player.BetterData().RoleInfo;

        if (playerRoleInfo.RoleAssigned && playerRoleInfo.Role.CheckVent(player, ventId, Exit) == false)
        {
            Logger.Log($"{player.Data.PlayerName} {playerRoleInfo.Role.GetType().Name}.cs Canceled Vent Action");
            return false;
        }

        foreach (var pc in Main.AllPlayerControls)
        {
            var otherRoleInfo = pc.BetterData().RoleInfo;
            if (!otherRoleInfo.RoleAssigned) continue;

            if (otherRoleInfo.Role.CheckVentOther(player, ventId, Exit) == false)
            {
                Logger.Log($"{pc.Data.PlayerName} {otherRoleInfo.Role.GetType().Name}.cs Canceled Vent Action");
                return false;
            }
        }

        if (playerRoleInfo.RoleAssigned && (!playerRoleInfo.Role.CanVent || !ValidateSenderCheck(player)))
        {
            Logger.Log($"Host Canceled Vent Action: Invalid");
            return false;
        }

        if (ShipStatus.Instance == null)
        {
            Logger.Log($"Host Canceled Vent Action: ShipStatus Null");
        }

        return true;
    }

    // Make a player kill a target
    public static void MurderSync(this PlayerControl player, PlayerControl target, bool isAbility = false, bool bypass = false)
    {
        if (GameStates.IsHost || bypass)
        {
            if (CheckMurderAction(player, target, isAbility) == true || bypass)
            {
                player.MurderPlayer(target, MurderResultFlags.Succeeded);

                // Run after checks for roles
                Main.AllPlayerControls.Where(pc => pc == player || pc == target && pc.RoleAssigned()).ToList()
                    .ForEach(pc => pc.BetterData().RoleInfo.Role.OnMurder(player, target, isAbility));

                Main.AllPlayerControls.Where(pc => pc.RoleAssigned()).ToList()
                    .ForEach(pc => pc.BetterData().RoleInfo.Role.OnMurderOther(player, target, isAbility));

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
        var playerRoleInfo = player.BetterData().RoleInfo;
        var targetRoleInfo = target.BetterData().RoleInfo;

        if (playerRoleInfo.RoleAssigned && playerRoleInfo.Role.CheckMurder(player, target, isAbility) == false)
        {
            Logger.Log($"{player.Data.PlayerName} {playerRoleInfo.Role.GetType().Name}.cs Canceled Murder Action");
            return false;
        }

        if (targetRoleInfo.RoleAssigned && targetRoleInfo.Role.CheckMurder(player, target, isAbility) == false)
        {
            Logger.Log($"{target.Data.PlayerName} {targetRoleInfo.Role.GetType().Name}.cs Canceled Murder Action");
            return false;
        }

        foreach (var pc in Main.AllPlayerControls)
        {
            var otherRoleInfo = pc.BetterData().RoleInfo;
            if (!otherRoleInfo.RoleAssigned) continue;

            if (otherRoleInfo.Role.CheckMurderOther(player, target, isAbility) == false)
            {
                Logger.Log($"{pc.Data.PlayerName} {otherRoleInfo.Role.GetType().Name}.cs Canceled Murder Action");
                return false;
            }
        }

        if (playerRoleInfo.RoleAssigned && (!playerRoleInfo.Role.CanKill && !isAbility || target.IsInVent() || !target.IsAlive() || !ValidateSenderCheck(player)))
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
                // Start Meeting
                DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(player);
                player.StartMeeting(bodyInfo);

                // Run after checks for roles
                Main.AllPlayerControls.Where(pc => pc == player || pc.Data == bodyInfo && pc.RoleAssigned()).ToList()
                    .ForEach(pc => pc.BetterData().RoleInfo.Role.OnBodyReport(player, bodyInfo, isButton));

                Main.AllPlayerControls.Where(pc => pc.RoleAssigned()).ToList()
                    .ForEach(pc => pc.BetterData().RoleInfo.Role.OnBodyReportOther(player, bodyInfo, isButton));

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
        var playerRoleInfo = player.BetterData().RoleInfo;
        var targetRoleInfo = bodyInfo?.BetterData();

        if (playerRoleInfo.RoleAssigned && playerRoleInfo.Role.CheckBodyReport(player, bodyInfo, isButton) == false)
        {
            Logger.Log($"{player.Data.PlayerName} {playerRoleInfo.Role.GetType().Name}.cs Canceled Murder Action");
            return false;
        }

        if (targetRoleInfo?.RoleInfo?.RoleAssigned == true && targetRoleInfo.RoleInfo.Role.CheckBodyReport(player, bodyInfo, isButton) == false)
        {
            Logger.Log($"{targetRoleInfo.RealName} {targetRoleInfo.RoleInfo.Role.GetType().Name}.cs Canceled Murder Action");
            return false;
        }

        foreach (var pc in Main.AllPlayerControls)
        {
            var otherRoleInfo = pc.BetterData().RoleInfo;
            if (!otherRoleInfo.RoleAssigned) continue;

            if (otherRoleInfo.Role.CheckBodyReportOther(player, bodyInfo, isButton) == false)
            {
                Logger.Log($"{pc.Data.PlayerName} {otherRoleInfo.Role.GetType().Name}.cs Canceled Murder Action");
                return false;
            }
        }

        if (playerRoleInfo.RoleAssigned && (!GameStates.IsInGamePlay || !ValidateSenderCheck(player)))
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
                Main.AllPlayerControls.FirstOrDefault(pc => pc == target && pc.RoleAssigned()).BetterData().RoleInfo.Role.OnPlayerPress(player, target);

                Main.AllPlayerControls.Where(pc => pc.RoleAssigned()).ToList()
                    .ForEach(pc => pc.BetterData().RoleInfo.Role.OnPlayerPressOther(player, target));

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
}
