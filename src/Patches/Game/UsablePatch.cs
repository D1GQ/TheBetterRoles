using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Monos;
using TheBetterRoles.Network;
using UnityEngine;

namespace TheBetterRoles.Patches.Game;

internal class UsablePatch
{
    [HarmonyPatch(typeof(Ladder), nameof(Ladder.SetDestinationCooldown))]
    internal class Ladder_CoolDown
    {
        private static bool Prefix(Ladder __instance)
        {
            __instance.Destination.CoolDown = 1f;
            return false;
        }
    }

    [HarmonyPatch(typeof(ZiplineConsole), nameof(ZiplineConsole.SetDestinationCooldown))]
    internal class ZiplineConsole_CoolDown
    {
        private static bool Prefix(ZiplineConsole __instance)
        {
            __instance.destination.CoolDown = 3f;
            return false;
        }
    }

    [HarmonyPatch]
    [HarmonyPriority(Priority.First)]
    internal class Usable_CanUse
    {
        [HarmonyPatch(typeof(Console), nameof(Console.CanUse))]
        [HarmonyPatch(typeof(SystemConsole), nameof(SystemConsole.CanUse))]
        [HarmonyPatch(typeof(OptionsConsole), nameof(OptionsConsole.CanUse))]
        [HarmonyPatch(typeof(Ladder), nameof(Ladder.CanUse))]
        [HarmonyPatch(typeof(ZiplineConsole), nameof(ZiplineConsole.CanUse))]
        [HarmonyPatch(typeof(PlatformConsole), nameof(PlatformConsole.CanUse))]
        [HarmonyPatch(typeof(DeconControl), nameof(DeconControl.CanUse))]
        [HarmonyPatch(typeof(DeconControl), nameof(DeconControl.CanUse))]
        [HarmonyPatch(typeof(DoorConsole), nameof(DoorConsole.CanUse))]
        [HarmonyPatch(typeof(OpenDoorConsole), nameof(OpenDoorConsole.CanUse))]
        [HarmonyPrefix]
        private static bool CanUse_Prefix(IUsable __instance, [HarmonyArgument(0)] NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse, ref float __result)
        {
            if (__instance == null || PlayerControl.LocalPlayer == null || pc?.Object == null) return true;

            bool canUseAsDead = false;
            bool checkFakeDead = false;
            bool checkCollision = true;
            bool condition = pc.Object && !GameState.IsInIntro && !CustomLoadingBarManager.LoadingBar.gameObject.active;
            bool onlyFromBelow = false;
            bool onlySameRoom = false;
            var mask = Constants.ShipOnlyMask;

            if (__instance.TryCast<Console>(out var console))
            {
                if (console == null) return true;

                mask = Constants.ShadowMask;
                canUseAsDead = true;
                checkCollision = pc.Object.IsAlive() && console.checkWalls;
                onlyFromBelow = console.onlyFromBelow;
                onlySameRoom = console.onlySameRoom;
                if (PlayerControl.LocalPlayer.Role() != null)
                {
                    condition &= PlayerControl.LocalPlayer.Role().HasTask || PlayerControl.LocalPlayer.Role().HasSelfTask;
                }
                else
                {
                    condition &= false;
                }
                if (condition || console.AllowImpostor)
                {
                    if (console.AllowImpostor && (PlayerControl.LocalPlayer.ExtendedPC().CanRepairSabotageQueue || PlayerControl.LocalPlayer.ExtendedData().IsFakeDead))
                    {
                        condition = false;
                        goto Skip;
                    }

                    return true;
                }
            }
            else if (__instance.TryCast<OptionsConsole>(out var options))
            {
                if (options == null) return true;

                canUseAsDead = true;
                checkCollision = false;
                condition &= true;

                options.HostOnly = false;
                if (!GameState.IsTBRLobby)
                {
                    condition = false;
                }
            }
            else if (__instance.TryCast<SystemConsole>(out var system))
            {
                if (system == null) return true;

                onlyFromBelow = system.onlyFromBelow;
                checkCollision = false;
                checkFakeDead = true;
                canUseAsDead = !system.MinigamePrefab.TryCast<EmergencyMinigame>();
                condition &= true;
            }
            else if (CastHelper.TryCast<Ladder>(__instance))
            {
                condition &= true;
            }
            else if (CastHelper.TryCast<ZiplineConsole>(__instance))
            {
                condition &= !pc.Object.isKilling;
            }
            else if (__instance.TryCast<PlatformConsole>(out var platform))
            {
                if (platform == null) return true;

                condition &= !platform.Platform.InUse && Vector2.Distance(platform.Platform.transform.position, platform.transform.position) < 2f;
            }
            else if (__instance.TryCast<DeconControl>(out var decon))
            {
                if (decon == null) return true;

                mask = Constants.ShipAndObjectsMask;
                condition &= decon.System.CurState == DeconSystem.States.Idle;
            }
            else if (__instance.TryCast<DoorConsole>(out var door))
            {
                if (door == null) return true;

                condition = !door.MyDoor.IsOpen;
            }
            else if (__instance.TryCast<OpenDoorConsole>(out var openDoor))
            {
                if (openDoor == null) return true;

                condition = !openDoor.myDoor.IsOpen;
            }
            else
            {
                return true;
            }

        Skip:;

            CheckCanUse(__instance.Cast<MonoBehaviour>(), pc, ref canUse, ref couldUse, ref __result, mask, condition, canUseAsDead, checkFakeDead, checkCollision, onlyFromBelow, onlySameRoom);
            return false;
        }
    }

    private static void CheckCanUse<T>(T console, NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse, ref float __result, int mask, bool extraFlag, bool canUseAsDead, bool checkFakeDead, bool checkCollision, bool onlyFromBelow, bool onlySameRoom) where T : MonoBehaviour
    {
        if (console == null) return;
        float num = float.MaxValue;
        PlayerControl @object = pc.Object;
        couldUse = (PlayerControl.LocalPlayer.IsAlive(checkFakeDead) || canUseAsDead) && extraFlag && console.enabled && PlayerControl.LocalPlayer.CanMove;
        canUse = couldUse;

        if (canUse && @object != null)
        {
            Vector2 truePosition = @object.GetTruePosition();
            Vector3 position = console.transform.position;
            num = Vector2.Distance(truePosition, position);

            canUse &= num <= console.Cast<IUsable>().UsableDistance && (!PhysicsHelpers.AnythingBetween(truePosition, position, mask, false) || !checkCollision)
                && (!onlyFromBelow || truePosition.y < position.y)
                && (!onlySameRoom || console.TryCast<Console>(out var con) && con.InRoom(truePosition));
        }

        __result = num;
    }
}
