using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using UnityEngine;

namespace TheBetterRoles.Modules;

internal class PolusStatusExtension : ShipStatusExtension
{
    internal override bool IsBetter => TBRGameSettings.BetterPolus.GetBool();

    internal override void SetUp()
    {
        InitialSpawnCenter = new Vector2(16.64f, -2.46f);
        MeetingSpawnCenter = new Vector2(17.4f, -16.286f);
        MeetingSpawnCenter2 = new Vector2(17.4f, -17.515f);
        SpawnRadius = 1f;
    }

    internal override void BetterSetUp()
    {
        InitialSpawnCenter = new Vector2(16.64f, -2.46f);
        MeetingSpawnCenter = new Vector2(17.4f, -16.286f);
        MeetingSpawnCenter2 = new Vector2(17.4f, -17.515f);
        SpawnRadius = 1f;

        GameObject Outside = GameObject.Find("Outside");
        GameObject Office = GameObject.Find("Office");
        GameObject Science = GameObject.Find("Science");

        GameObject WifiTask = GameObject.Find("panel_wifi");
        GameObject NavTask = GameObject.Find("panel_nav");
        if (WifiTask != null && NavTask != null)
        {
            (WifiTask.transform.position, NavTask.transform.position) = (NavTask.transform.position, WifiTask.transform.position);
            var WifiParent = WifiTask.transform.parent;
            var NavParent = NavTask.transform.parent;
            WifiTask.transform.SetParent(NavParent, true);
            NavTask.transform.SetParent(WifiParent, true);
            var NavConsole = NavTask.GetComponent<Console>();
            if (NavConsole != null) NavConsole.onlyFromBelow = true;
        }

        GameObject Vitals = GameObject.Find("panel_vitals");
        GameObject BoardingpassTask = GameObject.Find("panel_boardingpass");
        GameObject TempcoldTask = GameObject.Find("Science/panel_tempcold");
        if (Vitals != null && TempcoldTask != null)
        {
            Vitals.transform.localPosition = new Vector2(9.0679f, 10.8273f);
            Vitals.transform.SetParent(Science.transform, true);

            BoardingpassTask.transform.localPosition = new Vector2(4.2236f, 1.2392f);
            BoardingpassTask.transform.SetParent(Office.transform, true);

            TempcoldTask.transform.localPosition = new Vector3(-26.0598f, -6.5194f, -1f);
            TempcoldTask.transform.SetParent(Outside.transform, true);
            var TempcoldConsole = TempcoldTask.GetComponent<Console>();
            if (TempcoldConsole != null) TempcoldConsole.onlyFromBelow = true;
            var TempcoldCollider1 = TempcoldTask.AddComponent<BoxCollider2D>();
            var TempcoldCollider2 = TempcoldTask.AddComponent<BoxCollider2D>();
            TempcoldCollider1.size = new Vector2(0.64f, 0.67f);
            TempcoldCollider2.size = new Vector2(0.3305f, 0.1811f);
        }

        Vent ElectricalVent = Main.AllVents.FirstOrDefault(vent => vent.Id == 0);
        Vent ElectricBuildingVent = Main.AllVents.FirstOrDefault(vent => vent.Id == 10);
        if (ElectricalVent != null && ElectricBuildingVent != null)
        {
            ElectricBuildingVent.Right = null;
            ElectricBuildingVent.Left = ElectricalVent;
            ElectricalVent.Center = ElectricBuildingVent;
        }

        Vent OutsideBathroomVent = Main.AllVents.FirstOrDefault(vent => vent.Id == 7);
        Vent StorageVent = Main.AllVents.FirstOrDefault(vent => vent.Id == 8);
        Vent ScienceBuildingVent = Main.AllVents.FirstOrDefault(vent => vent.Id == 9);
        if (StorageVent != null && ScienceBuildingVent != null && OutsideBathroomVent != null)
        {
            Vent SpecimenVent = StorageVent.Copy("SpecimenVent");
            SpecimenVent.transform.SetParent(Outside.transform, true);
            SpecimenVent.transform.localPosition = new Vector2(38.7936f, -21.4063f);
            SpecimenVent.Left = OutsideBathroomVent;
            OutsideBathroomVent.Center = SpecimenVent;

            ScienceBuildingVent.Left = null;
            ScienceBuildingVent.Center = StorageVent;
            StorageVent.Center = ScienceBuildingVent;
        }
    }

    internal override void SetReverse()
    {
        if (TBRGameSettings.ReversePolus.GetBool())
        {
            ReverseMapSystem.AddSystem();
        }
    }

    internal override bool SpawnPlayer(PlayerControl player, int numPlayers, bool initialSpawn)
    {
        if (initialSpawn)
        {
            base.SpawnPlayer(player, numPlayers, initialSpawn);
            return false;
        }

        int halfPlayers = Mathf.FloorToInt(numPlayers / 2f);
        int spawnIndex = player.PlayerId % 15;
        Vector2 position;

        if (spawnIndex < halfPlayers)
        {
            position = MeetingSpawnCenter + Vector2.right * spawnIndex * 0.6f;
        }
        else
        {
            position = MeetingSpawnCenter2 + Vector2.right * (spawnIndex - halfPlayers) * 0.6f;
        }

        if (player.IsLocalPlayer())
        {
            player.NetTransform.RpcSnapTo(position);
        }
        else
        {
            player.NetTransform.SnapTo(position);
        }

        return false;
    }
}