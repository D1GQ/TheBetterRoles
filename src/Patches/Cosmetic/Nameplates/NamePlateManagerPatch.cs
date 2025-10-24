using Cpp2IL.Core.Extensions;
using HarmonyLib;
using TheBetterRoles.Managers;

namespace TheBetterRoles.Patches.Cosmetic.Nameplates
{
    [HarmonyPatch(typeof(HatManager))]
    internal static class NamePlateManagerPatch
    {
        private static bool isRunning;

        [HarmonyPatch(nameof(HatManager.GetNamePlateById))]
        [HarmonyPrefix]
        private static void GetNamePlateById_Prefix(HatManager __instance)
        {
            if (isRunning || CustomHatManager.UnregisteredNamePlates.Count <= 0) return;

            isRunning = true;
            var allNamePlates = __instance.allNamePlates.ToList();

            var unregisteredNamePlatesCache = CustomHatManager.UnregisteredNamePlates.Clone();
            foreach (var nameplate in unregisteredNamePlatesCache)
            {
                if (nameplate == null) continue;
                allNamePlates.Add(CustomHatManager.CreateNamePlateBehaviour(nameplate));
                CustomHatManager.UnregisteredNamePlates.Remove(nameplate);
            }

            unregisteredNamePlatesCache.Clear();
            __instance.allNamePlates = allNamePlates.ToArray();
            isRunning = false;
        }
    }
}