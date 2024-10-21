using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheBetterRoles.Patches;

public class UsablePatch
{
    [HarmonyPatch(typeof(MovingPlatformBehaviour))]
    [HarmonyPatch(nameof(MovingPlatformBehaviour.Use))]
    [HarmonyPatch(new Type[] { typeof(PlayerControl) })]  // Specify the argument type(s)
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

    [HarmonyPatch(typeof(Ladder), nameof(Ladder.CanUse))]
    public class Ladder_CanUse
    {
        public static bool Prefix(Ladder __instance, [HarmonyArgument(0)] NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse, ref float __result)
        {
            CheckCanUse(__instance, pc, ref canUse, ref couldUse, ref __result);
            return false;
        }
    }

    [HarmonyPatch(typeof(ZiplineConsole), nameof(ZiplineConsole.CanUse))]
    public class ZiplineConsole_CanUse
    {
        public static bool Prefix(ZiplineConsole __instance, [HarmonyArgument(0)] NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse, ref float __result)
        {
            CheckCanUse(__instance, pc, ref canUse, ref couldUse, ref __result,
                !pc.Object.isKilling && !pc.IsDead);
            return false;
        }
    }

    [HarmonyPatch(typeof(PlatformConsole), nameof(PlatformConsole.CanUse))]
    public class PlatformConsole_CanUse
    {
        public static bool Prefix(PlatformConsole __instance, [HarmonyArgument(0)] NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse, ref float __result)
        {
            CheckCanUse(__instance, pc, ref canUse, ref couldUse, ref __result,
                !__instance.Platform.InUse && Vector2.Distance(__instance.Platform.transform.position, __instance.transform.position) < 2f);
            return false;
        }
    }

    public static void CheckCanUse<T>(T console, NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse, ref float __result, bool extraFlag = true) where T : MonoBehaviour
    {
        float num = float.MaxValue;
        PlayerControl @object = pc.Object;
        couldUse = (!pc.IsDead || pc.BetterData().IsFakeAlive && @object.CanMove) && extraFlag;
        canUse = couldUse;

        if (canUse)
        {
            Vector2 truePosition = @object.GetTruePosition();
            Vector3 position = console.transform.position;
            num = Vector2.Distance(truePosition, position);

            float usableDistance = 1f;
            var usableDistanceProperty = console.GetType().GetProperty("UsableDistance", BindingFlags.Public | BindingFlags.Instance);

            if (usableDistanceProperty != null)
            {
                object? value = usableDistanceProperty.GetValue(console);
                if (value is float v)
                {
                    usableDistance = v;
                }
            }

            canUse &= num <= usableDistance && !PhysicsHelpers.AnythingBetween(truePosition, position, Constants.ShipOnlyMask, false);
        }

        __result = num;
    }
}
