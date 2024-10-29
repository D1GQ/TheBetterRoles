using HarmonyLib;
using TheBetterRoles.Helpers;
using UnityEngine;

namespace TheBetterRoles.Patches;

public class UsablePatch
{
    [HarmonyPatch(typeof(Ladder), nameof(Ladder.SetDestinationCooldown))]
    public class Ladder_CoolDown
    {
        public static bool Prefix(Ladder __instance)
        {
            __instance.Destination.CoolDown = 0f;
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

            bool condition = pc.Object.CanMove;
            var mask = Constants.ShipOnlyMask;

            if (CastHelper.TryCast<Ladder>(__instance))
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

            CheckCanUse(__instance.Cast<MonoBehaviour>(), pc, ref canUse, ref couldUse, ref __result, mask, condition);
            return false;
        }
    }

    public static void CheckCanUse<T>(T console, NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse, ref float __result, int mask, bool extraFlag = true) where T : MonoBehaviour
    {
        float num = float.MaxValue;
        PlayerControl @object = pc.Object;
        couldUse = (!pc.IsDead || pc.BetterData().IsFakeAlive) && extraFlag;
        canUse = couldUse;

        if (canUse)
        {
            Vector2 truePosition = @object.GetTruePosition();
            Vector3 position = console.transform.position;
            num = Vector2.Distance(truePosition, position);

            canUse &= num <= console.Cast<IUsable>().UsableDistance && !PhysicsHelpers.AnythingBetween(truePosition, position, mask, false);
        }

        __result = num;
    }
}
