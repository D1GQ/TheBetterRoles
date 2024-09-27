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
            __instance.ReportBodyAction(target);
            return false;
        }

        [HarmonyPatch(nameof(PlayerControl.ReportDeadBody))]
        [HarmonyPrefix]
        public static bool ReportDeadBody_Prefix(PlayerControl __instance, [HarmonyArgument(0)] NetworkedPlayerInfo target)
        {
            __instance.ReportBodyAction(target);
            return false;
        }
    }
}

static class ActionRPCs
{
    public static void VentAction(this PlayerControl player, int ventId, bool Exit, bool bypass = false)
    {
        if (GameStates.IsHost || bypass)
        {
            if (CheckVentAction(player, ventId, Exit) == true || bypass)
            {
                if (!Exit)
                {
                    player.StartCoroutine(player.MyPhysics.CoEnterVent(ventId));
                    if (player.AmOwner)
                    {
                        ShipStatus.Instance.AllVents.FirstOrDefault(vent => vent.Id == ventId).SetButtons(player.CanMoveInVent());
                    }
                }
                else
                {
                    player.StartCoroutine(player.MyPhysics.CoExitVent(ventId));
                    if (player.AmOwner)
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

        if (playerRoleInfo.RoleAssigned && (!playerRoleInfo.Role.CanVent))
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

    public static void MurderAction(this PlayerControl player, PlayerControl target, bool isAbility = false, bool bypass = false)
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

        if (playerRoleInfo.RoleAssigned && (!playerRoleInfo.Role.CanKill || target.IsInVent() || !target.IsAlive()))
        {
            Logger.Log($"Host Canceled Murder Action: Invalid");
            return false;
        }

        return true;
    }

    public static void ReportBodyAction(this PlayerControl player, NetworkedPlayerInfo? bodyInfo, bool bypass = false)
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

        if (playerRoleInfo.RoleAssigned && (!GameStates.IsInGamePlay))
        {
            Logger.Log($"Host Canceled Murder Action: Invalid");
            return false;
        }

        return true;
    }


    public static void ResetAbilityStateAction(this PlayerControl player, int id, bool bypass = false)
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

    private static bool CheckResetAbilityStateAction(PlayerControl player, int id)
    {
        return true;
    }
}
