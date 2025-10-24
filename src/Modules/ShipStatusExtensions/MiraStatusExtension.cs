using TheBetterRoles.Data;
using UnityEngine;

namespace TheBetterRoles.Modules;

internal class MiraStatusExtension : ShipStatusExtension
{
    internal override void SetUp()
    {
        InitialSpawnCenter = new Vector2(-4.4f, 2.2f);
        MeetingSpawnCenter = new Vector2(24.043f, 1.72f);
        MeetingSpawnCenter2 = new Vector2(0f, 0f);
        SpawnRadius = 1.55f;
    }

    internal override void BetterSetUp()
    {
    }

    internal override void SetReverse()
    {
        if (TBRGameSettings.ReverseMira.GetBool())
        {
            ReverseMapSystem.AddSystem();
        }
    }
}