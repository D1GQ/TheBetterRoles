using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Managers;
using TheBetterRoles.RPCs;


namespace TheBetterRoles.Modules;

[Flags]
enum MultiMurderFlags : short
{
    snapToTarget = 1 << 1,  // 2  (0b00010)
    spawnBody = 1 << 2,     // 4  (0b00100)
    showAnimation = 1 << 3, // 8  (0b01000)
    playSound = 1 << 4      // 16 (0b10000)
}

public enum CustomRPC : int
{
    // Cheat RPC's
    Sicko = 420, // Results in 164
    AUM = 42069, // Results in 85
    AUMChat = 101,
}

public enum ReactorRPCs : uint
{
    // Main
    VersionRequest,
    VersionRequestCallBack,
    SyncAllSettings,
    SyncOption,
    RoleAbility,
    SyncAction,
    SyncRole,

    // Sync
    ResetAbilityState,
    PlayIntro,
    SetRole,
    EndGame,
    ReportBody,
    StartMeeting,
    Vent,
    SnapTo,
    BootVent,
    Revive,
    Murder,
    PlayerPress,
    PlayerMenu,
    GuessPlayer
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
internal class PlayerControlRPCHandlerPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        if (RPC.HandleRPC(__instance, callId, reader) == false)
        {
            return false;
        }

        return true;
    }
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        RPC.HandleCustomRPC(__instance, callId, reader);
    }
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
internal class PlayerPhysicsRPCHandlerPatch
{
    public static void Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        RPC.HandleRPC(__instance.myPlayer, callId, reader);
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.UpdateSystem), typeof(SystemTypes), typeof(PlayerControl), typeof(MessageReader))]
public static class MessageReaderUpdateSystemPatch
{
    public static bool Prefix(/*ShipStatus __instance,*/ [HarmonyArgument(0)] SystemTypes systemType, [HarmonyArgument(1)] PlayerControl player, [HarmonyArgument(2)] MessageReader reader)
    {
        if (GameState.IsHideNSeek) return false;

        var amount = MessageReader.Get(reader).ReadByte();

        return true;
    }
}

internal static class RPC
{
    static class RpcsPatch
    {
        [HarmonyPatch(typeof(PlayerControl))]
        class PlayerActionPatch
        {
            [HarmonyPatch(nameof(PlayerControl.CmdReportDeadBody))]
            [HarmonyPrefix]
            public static bool CmdReportDeadBody_Prefix(PlayerControl __instance, [HarmonyArgument(0)] NetworkedPlayerInfo target)
            {
                if (__instance.IsAlive())
                    __instance.SendRpcReportBody(target?.PlayerId ?? 255);

                return false;
            }

            [HarmonyPatch(nameof(PlayerControl.ReportDeadBody))]
            [HarmonyPrefix]
            public static bool ReportDeadBody_Prefix(PlayerControl __instance, [HarmonyArgument(0)] NetworkedPlayerInfo target)
            {
                if (__instance.IsAlive())
                    __instance.SendRpcReportBody(target?.PlayerId ?? 255);

                return false;
            }
        }
    }

    public static void HandleCustomRPC(PlayerControl player, byte callId, MessageReader oldReader)
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

    public static bool HandleRPC(PlayerControl player, byte callId, MessageReader oldReader)
    {
        if (player == null || player.Data == null) return true;

        MessageReader reader = MessageReader.Get(oldReader);

        switch ((RpcCalls)callId)
        {
            case RpcCalls.CompleteTask:
                {
                    uint Id = reader.ReadUInt32();
                    CustomRoleManager.RoleListenerOther(role => role.OnTaskCompleteOther(player, Id));
                }
                break;
        }

        return true;
    }

    [MethodRpc((uint)ReactorRPCs.VersionRequest, SendImmediately = true)]
    public static void SendVersionRequest(this PlayerControl player, string version)
    {
        version = version.Replace(" ", ".");
        if (version == Main.GetVersionText().Replace(" ", "."))
        {
            player.BetterData().HasMod = true;
            player.BetterData().Version = version;

            if (GameState.IsHost)
            {
                Rpc<RpcSyncAllSettings>.Instance.Send(new(null));
            }
        }

        if (!player.IsLocalPlayer())
        {
            PlayerControl.LocalPlayer.SendVersionRequestCallBack(Main.GetVersionText());
        }

        Utils.DirtyAllNames();
    }

    [MethodRpc((uint)ReactorRPCs.VersionRequestCallBack, SendImmediately = true, LocalHandling = RpcLocalHandling.None)]
    public static void SendVersionRequestCallBack(this PlayerControl player, string version)
    {
        version = version.Replace(" ", ".");
        if (version == Main.GetVersionText().Replace(" ", "."))
        {
            player.BetterData().HasMod = true;
            player.BetterData().Version = version;

            if (GameState.IsHost)
            {
                Rpc<RpcSyncAllSettings>.Instance.Send(new(null));
            }
        }

        Utils.DirtyAllNames();
    }

    [MethodRpc((uint)ReactorRPCs.PlayIntro, SendImmediately = true)]
    public static void SendRpcPlayIntro(PlayerControl player)
    {
        if (player.IsHost())
        {
            CustomRoleManager.PlayIntro();
        }
    }

    [MethodRpc((uint)ReactorRPCs.ResetAbilityState, SendImmediately = true)]
    public static void SendRpcResetAbilityState(this PlayerControl player, int id, bool isTimeOut, int roleHash)
    {
        if (CheckResetAbilityStateRpc(player, id) == true)
        {
            CustomRoleManager.RoleListener(player, role => role.OnAbilityDurationEnd(id, isTimeOut), role => role.RoleHash == roleHash);
            CustomRoleManager.RoleListener(player, role => role.OnDurationEnd(id, isTimeOut), role => role.RoleHash == roleHash);
        }
    }
    private static bool CheckResetAbilityStateRpc(PlayerControl player, int id) => true;


    [MethodRpc((uint)ReactorRPCs.SetRole, SendImmediately = true)]
    public static void SendRpcSetCustomRole(this PlayerControl player, int roleTypeInt, bool RemoveAddon = false)
    {
        var role = (CustomRoles)roleTypeInt;
        if (CheckSetRoleRpc(player, role))
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
    }

    private static bool CheckSetRoleRpc(PlayerControl player, CustomRoles role) => true;

    public static void SendRpcMurder(this PlayerControl player,
        PlayerControl target,
        bool isAbility = false,
        MultiMurderFlags flags = MultiMurderFlags.snapToTarget | MultiMurderFlags.spawnBody | MultiMurderFlags.showAnimation | MultiMurderFlags.playSound)
    {
        player.SendTrueRpcMurder(target, isAbility, (byte)flags);
    }

    [MethodRpc((uint)ReactorRPCs.Murder, SendImmediately = true)]
    private static void SendTrueRpcMurder(this PlayerControl player,
        PlayerControl target,
        bool isAbility = false,
        byte flags = (byte)(MultiMurderFlags.snapToTarget | MultiMurderFlags.spawnBody | MultiMurderFlags.showAnimation | MultiMurderFlags.playSound))
    {
        if (CheckMurderRpc(player, target, isAbility))
        {
            CustomRoleManager.RoleListener(player, role => role.OnMurder(player, target, player == target, isAbility));
            CustomRoleManager.RoleListener(target, role => role.OnMurder(player, target, player == target, isAbility));
            CustomRoleManager.RoleListenerOther(role => role.OnMurderOther(player, target, player == target, isAbility));

            player.BetterData().RoleInfo.Kills++;
            target.BetterData().RoleInfo.RoleTypeWhenAlive = target.BetterData().RoleInfo.RoleType;

            bool snapToTarget = (flags & (byte)MultiMurderFlags.snapToTarget) != 0;
            bool spawnBody = (flags & (byte)MultiMurderFlags.spawnBody) != 0;
            bool showAnimation = (flags & (byte)MultiMurderFlags.showAnimation) != 0;
            bool playSound = (flags & (byte)MultiMurderFlags.playSound) != 0;

            player.CustomMurderPlayer(target, snapToTarget, spawnBody, showAnimation, playSound);
        }
    }

    private static bool CheckMurderRpc(PlayerControl player, PlayerControl target, bool isAbility)
    {
        if (!player.RoleChecks(role => role.CheckMurder(player, target, player == target, isAbility)))
        {
            return false;
        }

        if (!target.RoleChecks(role => role.CheckMurder(player, target, player == target, isAbility)))
        {
            return false;
        }

        if (!CustomRoleManager.RoleChecksOther(role => role.CheckMurderOther(player, target, player == target, isAbility)))
        {
            return false;
        }

        if (!player.RoleChecksAny(role => role.CanKill) && !isAbility || target.IsInVent() || !target.IsAlive())
        {
            Logger.Log($"Canceled Murder Action: Invalid");
            return false;
        }

        return true;
    }

    [MethodRpc((uint)ReactorRPCs.Revive, SendImmediately = true)]
    public static void SendRpcRevive(this PlayerControl player, bool setData = true)
    {
        if (CheckReviveRpc(player) == true)
        {
            player.CustomRevive(setData);
            player.RawSetRole(RoleTypes.Crewmate);
        }
    }

    private static bool CheckReviveRpc(PlayerControl player) => true;

    [MethodRpc((uint)ReactorRPCs.ReportBody, SendImmediately = true)]
    public static void SendRpcReportBody(this PlayerControl player, int bodyInfoId)
    {
        var bodyInfo = Utils.PlayerDataFromPlayerId(bodyInfoId);
        var flag = bodyInfo == null;

        if (CheckReportBodyRpc(player, bodyInfo, flag) == true)
        {
            // Run after checks for roles
            CustomRoleManager.RoleListenerOther(role => role.OnResetAbilityState(false));
            CustomRoleManager.RoleListener(player, role => role.OnBodyReport(player, bodyInfo, flag));
            CustomRoleManager.RoleListenerOther(role => role.OnBodyReportOther(player, bodyInfo, flag));

            if (GameState.IsHost)
            {
                MeetingRoomManager.Instance.AssignSelf(player, bodyInfo);
                DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(player);
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
        if (!player.RoleChecks(role => role.CheckBodyReport(player, bodyInfo, isButton)))
        {
            return false;
        }
        if (!CustomRoleManager.RoleChecksOther(role => role.CheckBodyReportOther(player, bodyInfo, isButton)))
        {
            return false;
        }

        return true;
    }

    public static void SendRpcPlayerMenu(this PlayerControl player, int Id, int roleType, NetworkedPlayerInfo? target, PlayerMenu? menu, ShapeshifterPanel? playerPanel, bool close)
    {
        if (CheckPlayerMenuRpc(player, target) == true)
        {
            CustomRoleManager.RoleListener(player, role => role.OnPlayerMenu(Id, target?.Object, target, menu, playerPanel, close), role => role.RoleType == (CustomRoles)roleType);
        }

        player.SendTrueRpcPlayerMenu(Id, roleType, target?.PlayerId ?? 255, close);
    }

    [MethodRpc((uint)ReactorRPCs.PlayerMenu, SendImmediately = true, LocalHandling = RpcLocalHandling.None)]
    public static void SendTrueRpcPlayerMenu(this PlayerControl player, int Id, int roleType, byte targetId, bool close)
    {
        var target = Utils.PlayerDataFromPlayerId(targetId);

        if (CheckPlayerMenuRpc(player, target) == true)
        {
            CustomRoleManager.RoleListener(player, role => role.OnPlayerMenu(Id, target?.Object, target, null, null, close), role => role.RoleType == (CustomRoles)roleType);
        }
    }
    private static bool CheckPlayerMenuRpc(PlayerControl player, NetworkedPlayerInfo? target) => true;

    [MethodRpc((uint)ReactorRPCs.PlayerPress, SendImmediately = true)]
    public static void SendRpcPlayerPress(this PlayerControl player, PlayerControl target)
    {
        if (CheckPlayerPressRpc(player, target) == true)
        {
            // Run after checks for roles
            CustomRoleManager.RoleListener(player, role => role.OnPlayerPress(player, target));
            CustomRoleManager.RoleListener(target, role => role.OnPlayerPress(player, target));
            CustomRoleManager.RoleListenerOther(role => role.OnPlayerPressOther(player, target));
        }
    }

    private static bool CheckPlayerPressRpc(PlayerControl player, PlayerControl target) => true;


    [MethodRpc((uint)ReactorRPCs.Vent, SendImmediately = true)]
    public static void SendRpcVent(this PlayerControl player, int ventId, bool Exit)
    {
        if (CheckVentRpc(player, ventId, Exit) == true)
        {
            // Run after checks for roles
            CustomRoleManager.RoleListener(player, role => role.OnVent(player, ventId, Exit));
            CustomRoleManager.RoleListenerOther(role => role.OnVentOther(player, ventId, Exit));

            if (!Exit)
            {
                player.MyPhysics.StopAllCoroutines();
                player.MyPhysics.StartCoroutine(player.MyPhysics.CoEnterVent(ventId));
                if (player.IsLocalPlayer())
                {
                    ShipStatus.Instance.AllVents.FirstOrDefault(vent => vent.Id == ventId).SetButtons(
                        player.IsLocalPlayer() && player.RoleChecks(role => role.CanMoveInVents, false));
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
        if (!player.RoleChecks(role => role.CheckVent(player, ventId, Exit)))
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

    [MethodRpc((uint)ReactorRPCs.SnapTo, SendImmediately = true)]
    public static void SendSnapTo(this PlayerControl player, UnityEngine.Vector2 pos)
    {
        if (CheckSnapToRpc(player, pos) == true)
        {
            player.NetTransform.SnapTo(pos);
        }
    }

    private static bool CheckSnapToRpc(PlayerControl player, UnityEngine.Vector2 pos) => true;

    [MethodRpc((uint)ReactorRPCs.GuessPlayer, SendImmediately = true)]
    public static void SendRpcGuessPlayer(this PlayerControl player, PlayerControl target, int roleTypeInt)
    {
        var roleType = (CustomRoles)roleTypeInt;
        if (CheckGuessPlayerRpc(player, target, roleType) == true)
        {
            CustomRoleManager.RoleListener(player, role => role.OnGuess(player, target, roleType));
            CustomRoleManager.RoleListener(target, role => role.OnGuess(player, target, roleType));
            CustomRoleManager.RoleListenerOther(role => role.OnGuessOther(player, target, roleType));

            CustomSoundsManager.Play("Gunfire", 3.5f);
            MeetingHud.Instance.ButtonParent.gameObject.SetActive(false);
            _ = new LateTask(() =>
            {
                if (!DestroyableSingleton<HudManager>.Instance.KillOverlay.IsOpen)
                {
                    MeetingHud.Instance.ButtonParent.gameObject.SetActive(true);
                }
            }, 2.5f, shoudLog: false);
            if (target.RoleChecksAny(role => role.RoleType == roleType, false))
            {
                DestroyableSingleton<HudManager>.Instance.KillOverlay.ShowKillAnimation(target.Data, target.Data);
                target.Exiled();
                MeetingHudPatch.AdjustVotesOnGuess(target);
                if (target.IsLocalPlayer())
                {
                    HudManager.Instance.AbilityButton.gameObject.SetActive(false);
                }
            }
            else
            {
                DestroyableSingleton<HudManager>.Instance.KillOverlay.ShowKillAnimation(player.Data, player.Data);
                player.Exiled();
                MeetingHudPatch.AdjustVotesOnGuess(player);
                if (player.IsLocalPlayer())
                {
                    HudManager.Instance.AbilityButton.gameObject.SetActive(false);
                }
            }
        }
        else if (player.IsLocalPlayer())
        {
            _ = new LateTask(() =>
            {
                if (!DestroyableSingleton<HudManager>.Instance.KillOverlay.IsOpen)
                {
                    MeetingHud.Instance.ButtonParent.gameObject.SetActive(true);
                }
            }, 0.25f, shoudLog: false);
        }
    }

    private static bool CheckGuessPlayerRpc(PlayerControl player, PlayerControl target, CustomRoles roleType)
    {
        if (!CustomRoleManager.RoleChecks(player, role => role.CheckGuess(player, target, roleType)))
        {
            return false;
        }
        if (!CustomRoleManager.RoleChecks(target, role => role.CheckGuess(player, target, roleType)))
        {
            return false;
        }
        if (!CustomRoleManager.RoleChecksOther(role => role.CheckGuessOther(player, target, roleType)))
        {
            return false;
        }

        return true;
    }
}