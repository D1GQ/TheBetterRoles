using HarmonyLib;
using TheBetterRoles.Modules;

namespace TheBetterRoles.Patches.Game.Minigames;

[HarmonyPatch(typeof(SpawnInMinigame))]
internal class SpawnInMinigamePatch
{
    [HarmonyPatch(nameof(SpawnInMinigame.SpawnAt))]
    [HarmonyPrefix]
    private static void SpawnAt_Prefix(/*SpawnInMinigame __instance,*/ ref SpawnInMinigame.SpawnLocation spawnPoint)
    {
        if (ReverseMapSystem.IsReverseActive())
        {
            var pos = spawnPoint.Location;
            spawnPoint.Location = new UnityEngine.Vector3(ShipStatus.Instance.transform.position.x - pos.x, pos.y, pos.z);
        }
    }
}