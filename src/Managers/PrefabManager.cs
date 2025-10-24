using TheBetterRoles.Helpers;

namespace TheBetterRoles.Managers;

internal class PrefabManager
{
    internal static void CatchPrefabs()
    {
        Prefab.CachePrefab<CrewmateRole>();
        Prefab.CachePrefab<ImpostorRole>();
        Prefab.CachePrefab<CrewmateGhostRole>();
        Prefab.CachePrefab<GuardianAngelRole>();
        Prefab.CachePrefab<ShapeshifterRole>();
        Prefab.CachePrefab<NoisemakerRole>();
        Prefab.CachePrefab<PhantomRole>();
        Prefab.CachePrefab<ScientistRole>();
        Prefab.CachePrefab<TrackerRole>();
    }
}