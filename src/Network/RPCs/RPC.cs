using AmongUs.Data;
using HarmonyLib;
using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches.Manager;
using TheBetterRoles.Roles;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Interfaces;

namespace TheBetterRoles.Network.RPCs;

internal static class RPC
{
    static class RpcsPatch
    {
        [HarmonyPatch(typeof(PlayerControl))]
        class PlayerActionPatch
        {
            [HarmonyPatch(nameof(PlayerControl.CmdReportDeadBody))]
            [HarmonyPrefix]
            internal static bool CmdReportDeadBody_Prefix(PlayerControl __instance, [HarmonyArgument(0)] NetworkedPlayerInfo target)
            {
                if (__instance.Role().RoleButtons.ReportButton.CanInteractOnPress() || target == null)
                    __instance.SendRpcReportBody(target);

                return false;
            }

            [HarmonyPatch(nameof(PlayerControl.ReportDeadBody))]
            [HarmonyPrefix]
            internal static bool ReportDeadBody_Prefix(PlayerControl __instance, [HarmonyArgument(0)] NetworkedPlayerInfo target)
            {
                if (__instance.Role().RoleButtons.ReportButton.CanInteractOnPress() || target == null)
                    __instance.SendRpcReportBody(target);

                return false;
            }
        }
    }

    internal static void HandleCustomRPC(PlayerControl player, byte callId, MessageReader oldReader)
    {
        if (player == null || player.IsLocalPlayer() || player.Data == null || Enum.IsDefined(typeof(RpcCalls), callId)) return;

        if (Enum.IsDefined(typeof(CustomRPC), (int)unchecked(callId)))
        {
            MessageReader reader = MessageReader.Get(oldReader);

            switch ((CustomRPC)callId)
            {
            }
        }
    }

    internal static bool HandleRPC(PlayerControl player, byte callId, MessageReader oldReader)
    {
        if (player == null || player.Data == null) return true;

        MessageReader reader = MessageReader.Get(oldReader);

        // Unused vanilla rpcs for TBR
        if ((RpcCalls)callId is RpcCalls.MurderPlayer
            or RpcCalls.CheckMurder
            or RpcCalls.ProtectPlayer
            or RpcCalls.CheckProtect
            or RpcCalls.Shapeshift
            or RpcCalls.RejectShapeshift
            or RpcCalls.CheckShapeshift
            or RpcCalls.StartVanish
            or RpcCalls.CheckVanish
            or RpcCalls.StartAppear
            or RpcCalls.CheckAppear
            or RpcCalls.ReportDeadBody
            or RpcCalls.StartMeeting
            or RpcCalls.EnterVent
            or RpcCalls.ExitVent
            or RpcCalls.SendChat) return false;

        switch ((RpcCalls)callId)
        {
        }

        return true;
    }


    [MethodRpc((uint)ReactorRPCs.Chat)]
    internal static void SendRpcChatMsg(this PlayerControl player, string text)
    {
        HudManager.Instance.Chat.AddChat(player, text, DataManager.settings.Multiplayer.CensorChat);
    }

    internal static void SendRpcPlayIntro()
    {
        if (!GameState.IsHost) return;

        PlayerControl.LocalPlayer.SendTrueRpcPlayIntro();
    }

    [MethodRpc((uint)ReactorRPCs.PlayIntro)]
    private static void SendTrueRpcPlayIntro(this PlayerControl player)
    {
        if (player.IsHost())
        {
            CustomLoadingBarManager.ToggleLoadingBar(false);
            CustomRoleManager.PlayIntro();
        }
    }

    internal static void SendRpcSetLoadingBar(float percent, string loadText, bool localHandle = true)
    {
        if (!GameState.IsHost) return;

        PlayerControl.LocalPlayer.SendTrueRpcSetLoadingBar(percent, loadText, localHandle);
    }

    [MethodRpc((uint)ReactorRPCs.LoadingBar)]
    private static void SendTrueRpcSetLoadingBar(this PlayerControl player, float percent, string loadText, bool localHandle)
    {
        if (!localHandle && player.IsLocalPlayer()) return;

        if (player.IsHost())
        {
            CustomLoadingBarManager.SetLoadingPercent(percent, loadText);
        }
    }

    [MethodRpc((uint)ReactorRPCs.ResetAbilityState)]
    internal static void SendRpcResetAbilityState(this PlayerControl player, int id, bool isTimeOut, ushort roleHash)
    {
        if (CheckResetAbilityStateRpc(player, id) == true)
        {
            RoleListener.InvokeRoles<IRoleAbilityAction>(role => role.AbilityDurationEnd(id, isTimeOut), role => role.RoleHash == roleHash, player);
            RoleListener.InvokeRoles(role => role.DurationEnd(id, isTimeOut), role => role.RoleHash == roleHash);
        }
    }
    private static bool CheckResetAbilityStateRpc(PlayerControl player, int id) => true;

    internal static RoleClass? SendRpcSetCustomRole(this PlayerControl player, RoleClassTypes roleType, bool removeAddon = false, bool isAssigned = false, bool bypassAssigned = false)
    {
        player.SendTrueRpcSetCustomRole((int)roleType, removeAddon, isAssigned, bypassAssigned);
        return player.ExtendedData().RoleInfo.AllRoles.FirstOrDefault(r => r.RoleType == roleType);
    }

    [MethodRpc((uint)ReactorRPCs.SetRole)]
    private static void SendTrueRpcSetCustomRole(this PlayerControl player, int roleTypeInt, bool removeAddon, bool isAssigned, bool bypassAssigned)
    {
        var role = (RoleClassTypes)roleTypeInt;
        if (CheckSetRoleRpc(player, role))
        {
            if (!Utils.GetCustomRoleClass(role).IsAddon)
            {
                CustomRoleManager.SetCustomRole(player, role, isAssigned, bypassAssigned);
            }
            else
            {
                if (!removeAddon)
                {
                    CustomRoleManager.AddAddon(player, role, isAssigned);
                }
                else
                {
                    CustomRoleManager.RemoveAddon(player, role);
                }
            }
        }
    }

    private static bool CheckSetRoleRpc(PlayerControl player, RoleClassTypes role) => true;

    internal static void SendRpcMurder(this PlayerControl player,
        PlayerControl target,
        bool isAbility = false,
        bool assignGhostRole = true,
        MultiMurderFlags flags = MultiMurderFlags.snapToTarget | MultiMurderFlags.spawnBody | MultiMurderFlags.showAnimation | MultiMurderFlags.playSound)
    {
        player.SendTrueRpcMurder(target, isAbility, assignGhostRole, (byte)flags);
    }

    [MethodRpc((uint)ReactorRPCs.Murder)]
    private static void SendTrueRpcMurder(this PlayerControl player,
        PlayerControl target,
        bool isAbility,
        bool assignGhostRole,
        byte flags)
    {
        if (CheckMurderRpc(player, target, isAbility))
        {
            player.InvokeRoles<IRoleMurderAction>(role => role.Murder(player, target, player == target, isAbility));
            target.InvokeRoles<IRoleMurderAction>(role => role.Murder(player, target, player == target, isAbility));
            RoleListener.InvokeRoles<IRoleMurderAction>(role => role.MurderOther(player, target, player == target, isAbility));

            player.ExtendedData().RoleInfo.Kills++;

            bool snapToTarget = (flags & (byte)MultiMurderFlags.snapToTarget) != 0;
            bool spawnBody = (flags & (byte)MultiMurderFlags.spawnBody) != 0;
            bool showAnimation = (flags & (byte)MultiMurderFlags.showAnimation) != 0;
            bool playSound = (flags & (byte)MultiMurderFlags.playSound) != 0;

            player.CustomMurderPlayer(target, snapToTarget, spawnBody, showAnimation, playSound, assignGhostRole);
        }
    }

    private static bool CheckMurderRpc(PlayerControl player, PlayerControl target, bool isAbility)
    {
        if (!player.CheckAllRoles<IRoleMurderAction>(role => role.CheckMurder(player, target, player == target, isAbility)))
        {
            return false;
        }

        if (!target.CheckAllRoles<IRoleMurderAction>(role => role.CheckMurder(player, target, player == target, isAbility)))
        {
            return false;
        }

        if (!RoleListener.CheckAllRoles<IRoleMurderAction>(role => role.CheckMurderOther(player, target, player == target, isAbility)))
        {
            return false;
        }

        if (!player.CheckAnyRoles(role => role.CanKill) && !isAbility || target.IsInVent() || !target.IsAlive() || GameState.IsMeeting || GameState.IsExilling)
        {
            Logger.Log($"Canceled Murder Action: Invalid");
            return false;
        }

        return true;
    }

    [MethodRpc((uint)ReactorRPCs.Revive)]
    internal static void SendRpcRevive(this PlayerControl player, bool setData = true, bool setReason = true)
    {
        if (CheckReviveRpc(player) == true)
        {
            player.CustomRevive(setData, setReason);
        }
    }

    private static bool CheckReviveRpc(PlayerControl player) => true;

    [MethodRpc((uint)ReactorRPCs.Exiled)]
    internal static void SendRpcExiled(this PlayerControl player, bool assignGhostRole = false)
    {
        if (CheckExiledRpc(player) == true)
        {
            player.CustomExiled(assignGhostRole);
        }
    }

    private static bool CheckExiledRpc(PlayerControl player) => true;

    internal static void SendRpcReportBody(this PlayerControl player, NetworkedPlayerInfo bodyInfo)
    {
        player.SendTrueRpcReportBody(bodyInfo?.PlayerId ?? 255);
    }

    [MethodRpc((uint)ReactorRPCs.ReportBody)]
    private static void SendTrueRpcReportBody(this PlayerControl player, byte bodyInfoId)
    {
        var bodyInfo = Utils.PlayerDataFromPlayerId(bodyInfoId);
        var flag = bodyInfo == null;

        if (CheckReportBodyRpc(player, bodyInfo, flag) == true)
        {
            // Run after checks for roles
            BaseSystem.InvokeOnMeetingStart();
            RoleListener.InvokeRoles<IRoleAbilityAction>(role => role.OnResetAbilityState(false));
            PlayerControl.LocalPlayer.InvokeRoles(role => role.SetAllCooldowns());
            player.InvokeRoles<IRoleReportAction>(role => role.BodyReport(player, bodyInfo, flag));
            RoleListener.InvokeRoles<IRoleReportAction>(role => role.BodyReportOther(player, bodyInfo, flag));

            if (GameState.IsHost)
            {
                MeetingRoomManager.Instance.AssignSelf(player, bodyInfo);
                HudManager.Instance.OpenMeetingRoom(player);
            }
            player.StartMeeting(bodyInfo);
        }
    }

    private static bool CheckReportBodyRpc(PlayerControl player, NetworkedPlayerInfo? bodyInfo, bool isButton)
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
        if (!player.CheckAllRoles<IRoleReportAction>(role => role.CheckBodyReport(player, bodyInfo, isButton)))
        {
            return false;
        }
        if (!RoleListener.CheckAllRoles<IRoleReportAction>(role => role.CheckBodyReportOther(player, bodyInfo, isButton)))
        {
            return false;
        }

        return true;
    }

    internal static void SendRpcRemoveBody(this DeadBody body)
    {
        PlayerControl.LocalPlayer.SendTrueRpcRemoveBody(body.GetObjHash());
    }

    [MethodRpc((uint)ReactorRPCs.RemoveBody)]
    private static void SendTrueRpcRemoveBody(this PlayerControl player, ushort bodyInfoHash)
    {
        var body = Main.AllDeadBodys.FirstOrDefault(b => b.GetObjHash() == bodyInfoHash);
        if (CheckRemoveBodyRpc(body) == true)
        {
            body.DestroyObj();
        }
    }

    private static bool CheckRemoveBodyRpc(DeadBody body) => true;

    [MethodRpc((uint)ReactorRPCs.PlayerPress)]
    internal static void SendRpcPlayerPress(this PlayerControl player, PlayerControl target)
    {
        if (CheckPlayerPressRpc(player, target) == true)
        {
            // Run after checks for roles
            player.InvokeRoles<IRolePressAction>(role => role.PlayerPress(player, target));
            target.InvokeRoles<IRolePressAction>(role => role.PlayerPress(player, target));
            RoleListener.InvokeRoles<IRolePressAction>(role => role.PlayerPressOther(player, target));
        }
    }

    private static bool CheckPlayerPressRpc(PlayerControl player, PlayerControl target) => true;


    [MethodRpc((uint)ReactorRPCs.Vent)]
    internal static void SendRpcVent(this PlayerControl player, int ventId, bool Exit)
    {
        if (CheckVentRpc(player, ventId, Exit) == true)
        {
            // Run after checks for roles
            RoleListener.InvokeRoles<IRoleVentAction>(role => role.OnVent(player, ventId, Exit), player: player);
            RoleListener.InvokeRoles<IRoleVentAction>(role => role.OnVentOther(player, ventId, Exit));

            if (!Exit)
            {
                player.MyPhysics.StopAllCoroutines();
                player.MyPhysics.StartCoroutine(player.MyPhysics.CoEnterVent(ventId));
                if (player.IsLocalPlayer())
                {
                    ShipStatus.Instance.AllVents.FirstOrDefault(vent => vent.Id == ventId).SetButtons(
                        player.IsLocalPlayer() && player.CheckAllRoles(role => role.CanMoveInVents));
                }
            }
            else
            {
                player.MyPhysics.StopAllCoroutines();
                player.MyPhysics.StartCoroutine(player.MyPhysics.CoExitVent(ventId));
                if (player.IsLocalPlayer())
                {
                    ShipStatus.Instance.AllVents.FirstOrDefault(vent => vent.Id == ventId).SetButtons(false);
                }
            }
        }
    }

    private static bool CheckVentRpc(PlayerControl player, int ventId, bool Exit)
    {
        if (!player.CheckAllRoles<IRoleVentAction>(role => role.CheckVent(player, ventId, Exit)))
        {
            return false;
        }

        if (!RoleListener.CheckAllRoles<IRoleVentAction>(role => role.CheckVentOther(player, ventId, Exit)))
        {
            return false;
        }

        if (ShipStatus.Instance == null)
        {
            Logger.Log($"Canceled Vent Action: ShipStatus Null");
        }

        return true;
    }

    [MethodRpc((uint)ReactorRPCs.BootVent)]
    internal static void SendRpcBootFromVent(this PlayerControl player, int ventId)
    {
        if (CheckBootFromVentRpc(player, ventId) == true)
        {
            if (player.inVent)
            {
                player.MyPhysics.BootFromVent(ventId);
            }
        }
    }

    private static bool CheckBootFromVentRpc(PlayerControl player, int ventId) => true;

    [MethodRpc((uint)ReactorRPCs.SnapTo)]
    internal static void SendSnapTo(this PlayerControl player, UnityEngine.Vector2 pos)
    {
        if (CheckSnapToRpc(player, pos) == true)
        {
            player.NetTransform.SnapTo(pos);
        }
    }

    private static bool CheckSnapToRpc(PlayerControl player, UnityEngine.Vector2 pos) => true;

    internal static void SendRpcGuessPlayer(this PlayerControl player, PlayerControl target, RoleClassTypes roleType, bool checkGuess = true)
    {
        player.SendTrueRpcGuessPlayer(target, (int)roleType, checkGuess);
    }

    [MethodRpc((uint)ReactorRPCs.GuessPlayer)]
    private static void SendTrueRpcGuessPlayer(this PlayerControl player, PlayerControl target, int roleTypeInt, bool checkGuess)
    {
        var roleType = (RoleClassTypes)roleTypeInt;
        if (CheckGuessPlayerRpc(player, target, roleType) == true)
        {
            if (checkGuess)
            {
                RoleListener.InvokeRoles<IRoleDeathAction>(role => role.OnDeath(target, DeathReasons.Guessed), player: target);
                RoleListener.InvokeRoles<IRoleDeathAction>(role => role.OnDeathOther(target, DeathReasons.Guessed));
                RoleListener.InvokeRoles<IRoleGuessAction>(role => role.Guess(player, target, roleType), player: player);
                RoleListener.InvokeRoles<IRoleGuessAction>(role => role.Guess(player, target, roleType), player: target);
                RoleListener.InvokeRoles<IRoleGuessAction>(role => role.GuessOther(player, target, roleType));
            }

            CustomSoundsManager.Instance.Play(Sounds.Gunfire, 3.5f);
            if (target.CheckAnyRoles(role => role.RoleType == roleType))
            {
                HudManager.Instance.KillOverlay.ShowKillAnimation(target.Data, target.Data);
                target.CustomExiled();
                target.SetDeathReason(DeathReasons.Guessed, Utils.GetCustomRoleTeamColorHex(RoleClassTeam.Crewmate));
                MeetingHudPatch.AdjustVotesOnGuess(target);
            }
            else
            {
                HudManager.Instance.KillOverlay.ShowKillAnimation(player.Data, player.Data);
                player.CustomExiled();
                player.SetDeathReason(DeathReasons.Guessed, Utils.GetCustomRoleTeamColorHex(RoleClassTeam.Crewmate));
                MeetingHudPatch.AdjustVotesOnGuess(player);
            }
        }
    }

    private static bool CheckGuessPlayerRpc(PlayerControl player, PlayerControl target, RoleClassTypes roleType)
    {
        if (!player.CheckAllRoles<IRoleGuessAction>(role => role.CheckGuess(player, target, roleType)))
        {
            return false;
        }
        if (!target.CheckAllRoles<IRoleGuessAction>(role => role.CheckGuess(player, target, roleType)))
        {
            return false;
        }
        if (!RoleListener.CheckAllRoles<IRoleGuessAction>(role => role.CheckGuessOther(player, target, roleType)))
        {
            return false;
        }

        return true;
    }
}