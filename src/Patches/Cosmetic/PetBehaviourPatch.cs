using HarmonyLib;

namespace TheBetterRoles.Patches.Cosmetic;

[HarmonyPatch(typeof(PetBehaviour))]
internal class PetBehaviourPatch
{
    [HarmonyPatch(nameof(PetBehaviour.Visible), MethodType.Setter)]
    [HarmonyPrefix]
    internal static bool SetVisible_Prefix(PetBehaviour __instance)
    {
        return __instance.targetPlayer != null;
    }
}