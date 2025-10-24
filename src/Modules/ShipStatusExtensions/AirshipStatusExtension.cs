using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using UnityEngine;

namespace TheBetterRoles.Modules;

internal class AirshipStatusExtension : ShipStatusExtension
{
    internal override void SetUp()
    {
        InitialSpawnCenter = new Vector2(-25f, 40f);
        MeetingSpawnCenter = new Vector2(20f, 9f);
        MeetingSpawnCenter2 = new Vector2(20f, 9f);
        SpawnRadius = 1.55f;
    }

    internal override void BetterSetUp()
    {
    }

    internal override void SetReverse()
    {
        if (TBRGameSettings.ReverseAirship.GetBool())
        {
            ReverseMapSystem.AddSystem();
        }
    }

    internal override bool SpawnPlayer(PlayerControl player, int numPlayers, bool initialSpawn)
    {
        Vector2 position = Vector2.zeroVector;

        if (GameState.IsFreePlay)
        {
            position = new Vector2(-0.66f, -0.5f);
        }
        else if (initialSpawn)
        {
            position = InitialSpawnCenter;
        }

        if (player.IsLocalPlayer())
        {
            player.NetTransform.RpcSnapTo(position);
        }
        else
        {
            player.NetTransform.SnapTo(position);
        }

        return true;
    }
}