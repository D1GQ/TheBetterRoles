using TheBetterRoles.Data;
using UnityEngine;

namespace TheBetterRoles.Modules;

internal class FungleStatusExtension : ShipStatusExtension
{
    internal override void SetUp()
    {
        InitialSpawnCenter = new Vector2(-9.81f, 1f);
        MeetingSpawnCenter = new Vector2(-3f, -2.1f);
        MeetingSpawnCenter2 = new Vector2(0f, 0f);
        SpawnRadius = 1.5f;
    }

    internal override void BetterSetUp()
    {
    }

    internal override void SetReverse()
    {
        if (TBRGameSettings.ReverseFungle.GetBool())
        {
            ReverseMapSystem.AddSystem();

            var ziplines = FindObjectsOfType<ZiplineBehaviour>();
            foreach (var zipline in ziplines)
            {
                if (zipline == null) continue;
                AdjustPosition(zipline.landingPositionBottom);
                AdjustPosition(zipline.landingPositionTop);
            }

            var mushrooms = FindObjectsOfType<Mushroom>();
            foreach (var mushroom in mushrooms)
            {
                if (mushroom == null) continue;
                mushroom.origPosition = AdjustPosition(mushroom.origPosition);
            }
        }
    }

    private static void AdjustPosition(Transform target)
    {
        if (target != null && ShipStatus.Instance != null)
        {
            float shipX = ShipStatus.Instance.transform.position.x;
            target.position = new Vector3(2 * shipX - target.position.x, target.position.y, target.position.z);
        }
    }

    private static Vector3 AdjustPosition(Vector3 target)
    {
        if (ShipStatus.Instance != null)
        {
            float shipX = ShipStatus.Instance.transform.position.x;
            return new Vector3(2 * shipX - target.x, target.y, target.z);
        }

        return new();
    }
}