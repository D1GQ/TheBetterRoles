using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(RoleManager))]
public class RoleManagerPatch
{
    public static Dictionary<PlayerControl, RoleTypes> SetPlayerRole = []; // Player, Role
    public static Dictionary<string, int> ImpostorMultiplier = []; // HashPuid, Multiplier
    private static Random random = new Random();

    // Better role algorithm
    [HarmonyPatch(nameof(RoleManager.SelectRoles))]
    [HarmonyPrefix]
    public static bool RoleManager_Prefix(/*RoleManager __instance*/)
    {
        return true;

        if (!GameStates.IsHideNSeek)
        {
            RegularBetterRoleAssignment();
        }
        else
        {
            HideAndSeekBetterRoleAssignment();
        }

        return false;
    }

    public static void RegularBetterRoleAssignment()
    {
    }

    public static void HideAndSeekBetterRoleAssignment()
    {
    }

    [HarmonyPatch(nameof(RoleManager.AssignRoleOnDeath))]
    [HarmonyPrefix]
    public static bool AssignRoleOnDeath_Prefix(/*RoleManager __instance*/ [HarmonyArgument(0)] PlayerControl player)
    {
        player.RawSetRole(RoleTypes.CrewmateGhost);

        return false;
    }

    private static bool IsImpostorRole(RoleTypes role) => role is RoleTypes.Impostor or RoleTypes.Shapeshifter or RoleTypes.Phantom;

    private static Dictionary<TKey, TValue> Shuffle<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
    {
        List<TKey> keys = dictionary.Keys.ToList();

        // Fisher-Yates shuffle algorithm
        for (int i = keys.Count - 1; i > 0; i--)
        {
            int j = random.Next(0, i + 1);
            TKey temp = keys[i];
            keys[i] = keys[j];
            keys[j] = temp;
        }

        // Rebuild dictionary with shuffled keys
        Dictionary<TKey, TValue> shuffledDictionary = new Dictionary<TKey, TValue>();
        foreach (var key in keys)
        {
            shuffledDictionary[key] = dictionary[key];
        }

        return shuffledDictionary;
    }

    public static int RNG()
    {
        switch (BetterGameSettings.RoleRandomizer.GetValue())
        {
            case 1:
                return UnityEngine.Random.Range(0, 100);

            default:
                Random Random = new Random();
                return Random.Next(0, 100);
        }
    }
}