using TheBetterRoles.Data;
using UnityEngine;

namespace TheBetterRoles.Modules;

internal class SkeldStatusExtension : ShipStatusExtension
{
    internal override void SetUp()
    {
        InitialSpawnCenter = new Vector2(-0.72f, 0.62f);
        MeetingSpawnCenter = new Vector2(-0.72f, 0.62f);
        MeetingSpawnCenter2 = new Vector2(0f, 0f);
        SpawnRadius = 1.6f;
    }

    internal override void BetterSetUp()
    {
    }

    internal override void SetReverse()
    {
        if (TBRGameSettings.ReverseSkeld.GetBool())
        {
            ReverseMapSystem.AddSystem();
        }
    }
}