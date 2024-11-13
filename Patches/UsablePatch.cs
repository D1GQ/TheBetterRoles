using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using UnityEngine;

namespace TheBetterRoles.Patches;

public class UsablePatch
{
    [HarmonyPatch(typeof(OptionsConsole))]
    [HarmonyPatch(nameof(OptionsConsole.Use))]
    public class OptionsConsole_Use
    {
        public static bool Prefix(/*OptionsConsole __instance*/)
        {
            if (GameState.IsLobby && !GameState.IsFreePlay)
            {
                return true;
            }

            UnityEngine.Object.Instantiate(GamePrefabHelper.GetPrefabByName("PlayerOptionsMenu"), parent: Camera.main.transform);
            return false;
        }
    }

    [HarmonyPatch(typeof(OptionsConsole))]
    [HarmonyPatch(nameof(OptionsConsole.CanUse))]
    public class OptionsConsole_OnEnable
    {
        public static void Postfix(OptionsConsole __instance)
        {
            if (GameState.IsFreePlay)
            {
                __instance.GetComponentInChildren<SpriteRenderer>().color = Color.cyan;
            }
        }
    }

    [HarmonyPatch(typeof(Ladder), nameof(Ladder.SetDestinationCooldown))]
    public class Ladder_CoolDown
    {
        public static bool Prefix(Ladder __instance)
        {
            __instance.Destination.CoolDown = 1f;
            return false;
        }
    }

    [HarmonyPatch(typeof(ZiplineConsole), nameof(ZiplineConsole.SetDestinationCooldown))]
    public class ZiplineConsole_CoolDown
    {
        public static bool Prefix(ZiplineConsole __instance)
        {
            __instance.destination.CoolDown = 3f;
            return false;
        }
    }

    [HarmonyPatch(typeof(MovingPlatformBehaviour))]
    [HarmonyPatch(nameof(MovingPlatformBehaviour.Use))]
    [HarmonyPatch(new Type[] { typeof(PlayerControl) })]
    public class MovingPlatformBehaviour_Use
    {
        public static bool Prefix(MovingPlatformBehaviour __instance, PlayerControl player)
        {
            Vector3 vector = __instance.transform.position - player.transform.position;

            if (!player.IsAlive(true) || player.Data.Disconnected)
            {
                return false;
            }

            if (__instance.Target || vector.magnitude > 3f)
            {
                return false;
            }

            __instance.IsDirty = true;
            __instance.StartCoroutine(__instance.UsePlatform(player));
            return false;
        }
    }

    [HarmonyPatch(typeof(DoorConsole))]
    [HarmonyPatch(nameof(DoorConsole.Use))]
    public class DoorConsole_Use
    {
        public static bool Prefix(DoorConsole __instance)
        {
            Minigame minigame = UnityEngine.Object.Instantiate<Minigame>(__instance.MinigamePrefab, Camera.main.transform);
            minigame.transform.localPosition = new Vector3(0f, 0f, -50f);
            minigame.Cast<IDoorMinigame>().SetDoor(__instance.MyDoor);
            minigame.Begin(null);

            return false;
        }
    }

    [HarmonyPatch]
    public class Usable_CanUse
    {
        [HarmonyPatch(typeof(OptionsConsole), nameof(OptionsConsole.CanUse))]
        [HarmonyPatch(typeof(Ladder), nameof(Ladder.CanUse))]
        [HarmonyPatch(typeof(ZiplineConsole), nameof(ZiplineConsole.CanUse))]
        [HarmonyPatch(typeof(PlatformConsole), nameof(PlatformConsole.CanUse))]
        [HarmonyPatch(typeof(DeconControl), nameof(DeconControl.CanUse))]
        [HarmonyPatch(typeof(DoorConsole), nameof(DoorConsole.CanUse))]
        [HarmonyPatch(typeof(OpenDoorConsole), nameof(OpenDoorConsole.CanUse))]
        [HarmonyPrefix]
        public static bool Prefix(IUsable __instance, [HarmonyArgument(0)] NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse, ref float __result)
        {
            if (__instance == null) return true;

            bool canUseAsDead = false;
            bool checkCollision = true;
            bool condition = pc.Object.CanMove;
            var mask = Constants.ShipOnlyMask;

            if (CastHelper.TryCast<OptionsConsole>(__instance, out var options))
            {
                canUseAsDead = true;
                checkCollision = false;
                condition &= true;

                options.HostOnly = false;
                if (!GameState.IsTBRLobby || !GameState.IsHost && !BetterDataManager.HostSettings.Any())
                {
                    condition = false;
                }
            }
            else if (CastHelper.TryCast<Ladder>(__instance))
            {
                condition &= true;
            }
            else if (CastHelper.TryCast<ZiplineConsole>(__instance))
            {
                condition &= !pc.Object.isKilling && !pc.IsDead;
            }
            else if (CastHelper.TryCast<PlatformConsole>(__instance, out var platform))
            {
                condition &= !platform.Platform.InUse && Vector2.Distance(platform.Platform.transform.position, platform.transform.position) < 2f;
            }
            else if (CastHelper.TryCast<DeconControl>(__instance, out var decon))
            {
                mask = Constants.ShipAndObjectsMask;
                condition &= decon.System.CurState == DeconSystem.States.Idle;
            }
            else if (CastHelper.TryCast<DoorConsole>(__instance, out var door))
            {
                condition = !door.MyDoor.IsOpen;
            }
            else if (CastHelper.TryCast<OpenDoorConsole>(__instance, out var openDoor))
            {
                condition = !openDoor.myDoor.IsOpen;
            }
            else
            {
                return true;
            }

            CheckCanUse(__instance.Cast<MonoBehaviour>(), pc, ref canUse, ref couldUse, ref __result, mask, condition, canUseAsDead, checkCollision);
            return false;
        }
    }

    public static void CheckCanUse<T>(T console, NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse, ref float __result, int mask, bool extraFlag = true, bool canUseAsDead = false, bool checkCollision = true) where T : MonoBehaviour
    {
        float num = float.MaxValue;
        PlayerControl @object = pc.Object;
        couldUse = (!pc.IsDead || pc.BetterData().IsFakeAlive || canUseAsDead) && extraFlag;
        canUse = couldUse;

        if (canUse)
        {
            Vector2 truePosition = @object.GetTruePosition();
            Vector3 position = console.transform.position;
            num = Vector2.Distance(truePosition, position);

            canUse &= num <= console.Cast<IUsable>().UsableDistance && (!PhysicsHelpers.AnythingBetween(truePosition, position, mask, false) || !checkCollision);
        }

        __result = num;
    }
}
