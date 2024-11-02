using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using InnerNet;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Managers;

namespace TheBetterRoles.Modules;


[Flags]
enum MultiMurderFlags : short
{
    snapToTarget = 1 << 1,  // 2  (0b00010)
    spawnBody = 1 << 2,     // 4  (0b00100)
    showAnimation = 1 << 3, // 8  (0b01000)
    playSound = 1 << 4      // 16 (0b10000)
}

static class ActionPatch
{
    [HarmonyPatch(typeof(PlayerControl))]
    class PlayerActionPatch
    {
        [HarmonyPatch(nameof(PlayerControl.CmdReportDeadBody))]
        [HarmonyPrefix]
        public static bool CmdReportDeadBody_Prefix(PlayerControl __instance, [HarmonyArgument(0)] NetworkedPlayerInfo target)
        {
            if (__instance.IsAlive())
                __instance.ReportBodySync(target);

            return false;
        }

        [HarmonyPatch(nameof(PlayerControl.ReportDeadBody))]
        [HarmonyPrefix]
        public static bool ReportDeadBody_Prefix(PlayerControl __instance, [HarmonyArgument(0)] NetworkedPlayerInfo target)
        {
            if (__instance.IsAlive())
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
        || AmongUsClient.Instance.GetHost().Character == SenderPlayer && !GameState.IsHost
        // If the player is provided, is the same as SenderPlayer, and the game is in host state, validation passes
        || player != null && player == SenderPlayer && GameState.IsHost;

    private static bool ValidateHostCheck() => SenderPlayer != null && AmongUsClient.Instance.GetHost().Character == SenderPlayer || GameState.IsHost;

    // Needs to be fixed, clients do not receive the RPC
    public static void EndGameSync(List<byte> winners, EndGameReason reason, CustomRoleTeam team, bool IsRPC = false)
    {
        if (!GameState.IsHost && !IsRPC) return;

        if (ValidateHostCheck())
        {
            if (GameState.IsHost)
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

    // Set player role
    public static void PlayIntroSync(bool IsRPC = false)
    {
        if (ValidateHostCheck() == true)
        {
            CustomRoleManager.PlayIntro();
        }

        if (IsRPC) return;

        var writer = AmongUsClient.Instance.StartActionSyncRpc(RpcAction.PlayIntro);
        AmongUsClient.Instance.EndActionSyncRpc(writer);
    }

    // Make a player kill a target
    public static void MurderSync(
        this PlayerControl player,
        PlayerControl target,
        bool isAbility = false,
        MultiMurderFlags flags = MultiMurderFlags.snapToTarget | MultiMurderFlags.spawnBody | MultiMurderFlags.showAnimation | MultiMurderFlags.playSound,
        bool IsRPC = false)
    {
        if (CheckMurderAction(player, target, isAbility))
        {
            // Run after checks for roles
            CustomRoleManager.RoleListener(player, role => role.OnMurder(player, target, player == target, isAbility));
            CustomRoleManager.RoleListener(target, role => role.OnMurder(player, target, player == target, isAbility));
            CustomRoleManager.RoleListenerOther(role => role.OnMurderOther(player, target, player == target, isAbility));

            player.BetterData().RoleInfo.Kills++;
            target.BetterData().RoleInfo.RoleTypeWhenAlive = target.BetterData().RoleInfo.RoleType;

            bool snapToTarget = (flags & MultiMurderFlags.snapToTarget) != 0;
            bool spawnBody = (flags & MultiMurderFlags.spawnBody) != 0;
            bool showAnimation = (flags & MultiMurderFlags.showAnimation) != 0;
            bool playSound = (flags & MultiMurderFlags.playSound) != 0;

            player.CustomMurderPlayer(target, snapToTarget, spawnBody, showAnimation, playSound);
        }

        if (IsRPC) return;

        var writer = AmongUsClient.Instance.StartActionSyncRpc(RpcAction.Murder, player);
        writer.WritePlayerId(target);
        writer.Write(isAbility);
        writer.Write((byte)flags);
        AmongUsClient.Instance.EndActionSyncRpc(writer);
    }

    private static bool CheckMurderAction(PlayerControl player, PlayerControl target, bool isAbility)
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
            TBRLogger.Log($"Canceled Murder Action: Invalid");
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
            if (GameState.IsHost)
            {
                MeetingRoomManager.Instance.AssignSelf(player, bodyInfo);
                DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(player);
            }
            player.StartMeeting(bodyInfo);
        }

        if (IsRPC) return;

        var writer = AmongUsClient.Instance.StartActionSyncRpc(RpcAction.ReportBody, player);
        writer.WritePlayerDataId(bodyInfo);
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

    // Resync after ability duration
    public static void ResetAbilityStateSync(this PlayerControl player, int id, int roleType, bool isTimeOut, bool IsRPC = false)
    {
        if (CheckResetAbilityStateAction(player, id) == true)
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
        writer.WritePlayerDataId(target);
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
                        player.IsLocalPlayer() && player.RoleChecks(role => role.CanMoveInVents, false));
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
        writer.Write((byte)ventId);
        writer.Write(Exit);
        AmongUsClient.Instance.EndActionSyncRpc(writer);
    }

    private static bool CheckVentAction(PlayerControl player, int ventId, bool Exit)
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
            TBRLogger.Log($"Canceled Vent Action: ShipStatus Null");
        }

        return true;
    }

    // Sync when player is guessed
    public static void GuessPlayerSync(this PlayerControl player, PlayerControl target, CustomRoles roleType, bool IsRPC = false)
    {
        if (CheckGuessPlayerAction(player, target, roleType) == true)
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

        if (IsRPC) return;

        var writer = AmongUsClient.Instance.StartActionSyncRpc(RpcAction.GuessPlayer, player);
        writer.WritePlayerId(target);
        writer.Write((int)roleType);
        AmongUsClient.Instance.EndActionSyncRpc(writer);
    }

    private static bool CheckGuessPlayerAction(PlayerControl player, PlayerControl target, CustomRoles roleType)
    {
        if (!player.RoleChecks(role => role.CheckGuess(player, target, roleType)))
        {
            return false;
        }
        if (!target.RoleChecks(role => role.CheckGuess(player, target, roleType)))
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
