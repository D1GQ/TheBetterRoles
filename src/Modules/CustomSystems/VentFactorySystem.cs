using BepInEx.Unity.IL2CPP.Utils;
using Hazel;
using Reactor.Utilities.Attributes;
using System.Collections;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Modules.CustomSystems;

[RegisterInIl2Cpp(typeof(ISystemType), typeof(IActivatable))]
internal class VentFactorySystem : BaseSystem, IISystemType, IIActivatable
{
    internal readonly List<Vent> AllVents = [];
    internal static CustomSystemTypes Type => CustomSystemTypes.VentFactory;
    public bool IsDirty { get; private set; }
    public bool IsActive { get; private set; }

    private GameObject? _customVentsObj;
    internal GameObject CustomVentsObj
    {
        get
        {
            if (_customVentsObj == null)
            {
                _customVentsObj = new GameObject("Vents");
                _customVentsObj.transform.SetParent(ShipStatus.Instance.transform, false);

            }
            return _customVentsObj;
        }
    }

    internal static VentFactorySystem? Instance { get; private set; }

    /// <summary>
    /// Integrate custom system into AU systems.
    /// </summary>
    internal static void AddSystem()
    {
        if (Instance != null) return;
        var ventFactory = ShipStatus.Instance.gameObject.AddComponent<VentFactorySystem>();
        ShipStatus.Instance.Systems.Add(Type, ventFactory.Cast<ISystemType>());
        Instance = ventFactory;
    }

    internal static Vent? VentPrefab { get; private set; }

    internal override void SetUp()
    {
        foreach (var vent in ShipStatus.Instance.AllVents)
        {
            AllVents.Add(vent);
        }

        VentPrefab = Instantiate(ShipStatus.Instance.AllVents.First());
        VentPrefab.gameObject.name = "VentPrefab";
        VentPrefab.gameObject.SetActive(false);
        VentPrefab.transform.SetParent(CustomVentsObj.transform, false);
        VentPrefab.UnsetVents();
    }

    internal override void Destroy()
    {
        Instance = null;
    }

    internal static void SendSyncVents()
    {
        MessageWriter messageWriter = MessageWriter.Get(SendOption.Reliable);
        messageWriter.Write((byte)0);
        ShipStatus.Instance.RpcUpdateSystem(Type, messageWriter);
        messageWriter.Recycle();
    }

    internal int NextVentId;

    internal static void SendAddVentToHost(VentData ventData, Action<Vent> callback = null)
    {
        if (Instance != null)
        {
            ventData.Id = (10000 * (PlayerControl.LocalPlayer.PlayerId + 1)) + Instance.NextVentId;
            Instance.NextVentId++;
            MessageWriter messageWriter = MessageWriter.Get(SendOption.Reliable);
            messageWriter.Write((byte)1);
            ventData.Serialize(messageWriter);
            ShipStatus.Instance.RpcUpdateSystem(Type, messageWriter);
            messageWriter.Recycle();
            if (callback != null)
            {
                ShipStatus.Instance.StartCoroutine(CoWaitSendAddVentToHost(ventData, callback));
            }
        }
    }

    private static IEnumerator CoWaitSendAddVentToHost(VentData ventData, Action<Vent> callback)
    {
        while (!Instance.AllVents.Any(v => v.Id == ventData.Id))
        {
            yield return null;
        }

        callback?.Invoke(Instance.AllVents.First(v => v.Id == ventData.Id));
    }

    private static Vent AddVent(Vector2 pos, int ventId)
    {
        var newVent = Instantiate(VentPrefab, Instance.CustomVentsObj.transform, false);
        newVent.gameObject.name = "Vent";
        newVent.transform.position = new(pos.x, pos.y, Utils.GetPlayerZPosAtVector2(pos) + 0.05f);
        newVent.Id = ventId;
        var con = newVent.GetComponent<VentCleaningConsole>();
        con?.ConsoleId = newVent.Id;
        newVent.gameObject.SetActive(true);
        newVent.AddToShipStatus();
        Instance.AllVents.Add(newVent);

        return newVent;
    }

    internal static void SendRemoveVentToHost(Vent vent, Action callback = null)
    {
        if (Instance != null)
        {
            if (callback != null)
            {
                ShipStatus.Instance.StartCoroutine(CoWaitSendRemoveVentToHost(vent.Id, callback));
            }

            MessageWriter messageWriter = MessageWriter.Get(SendOption.Reliable);
            messageWriter.Write((byte)2);
            messageWriter.Write(vent.Id);
            ShipStatus.Instance.RpcUpdateSystem(Type, messageWriter);
            messageWriter.Recycle();
        }
    }

    private static IEnumerator CoWaitSendRemoveVentToHost(int ventId, Action callback)
    {
        while (Instance.AllVents.Any(v => v.Id == ventId))
        {
            yield return null;
        }

        callback?.Invoke();
    }

    private static void RemoveVent(Vent vent)
    {
        Instance.AllVents.Remove(vent);
        vent.RemoveFromShipStatus();
        Destroy(vent.gameObject);
    }

    private static void RemoveVentsBulk(List<Vent> ventsToRemove)
    {
        if (ventsToRemove.Count == 0) return;

        var allVents = ShipStatus.Instance.AllVents.ToList();
        foreach (var vent in ventsToRemove)
        {
            allVents.Remove(vent);
            Destroy(vent.gameObject);
        }
        ShipStatus.Instance.AllVents = allVents.ToArray();
    }

    public void Deteriorate(float deltaTime)
    {
    }

    public void UpdateSystem(PlayerControl player, MessageReader msgReader)
    {
        byte count = msgReader.ReadByte();
        switch (count)
        {
            case 0:
                IsDirty = true;
                break;
            case 1:
                VentData ventData = VentData.Deserialize(msgReader);
                var newVent = AddVent(ventData.Position, ventData.Id);
                var ventDictionary = ShipStatus.Instance.AllVents.ToDictionary(v => v.Id, v => v);
                ventData.Setup(newVent, ventDictionary);
                IsDirty = true;
                break;
            case 2:
                int ventId = msgReader.ReadInt32();
                var vent = Instance.AllVents.FirstOrDefault(v => v.Id == ventId);
                RemoveVent(vent);
                IsDirty = true;
                break;
        }
    }

    public void Serialize(MessageWriter writer, bool initialState)
    {
        writer.Write(ShipStatus.Instance.AllVents.Count);
        foreach (var vent in ShipStatus.Instance.AllVents)
        {
            VentData.CreateFromVent(vent).Serialize(writer);
        }
        IsDirty = false;
    }

    public void Deserialize(MessageReader reader, bool initialState)
    {
        int syncedVentCount = reader.ReadInt32();

        Dictionary<int, VentData> syncedVents = [];

        for (int i = 0; i < syncedVentCount; i++)
        {
            var ventData = VentData.Deserialize(reader);
            syncedVents[ventData.Id] = ventData;
        }

        var currentVents = ShipStatus.Instance.AllVents.ToList();
        var ventsToRemove = new List<Vent>();

        // First pass: Process existing vents
        foreach (var currentVent in currentVents)
        {
            if (syncedVents.TryGetValue(currentVent.Id, out var data))
            {
                if (Vector2.Distance(currentVent.transform.position, data.Position) > 0.01f)
                {
                    currentVent.transform.position = new Vector3(
                        data.Position.x,
                        data.Position.y,
                        currentVent.transform.position.z
                    );
                }
                // Don't remove from syncedVents yet - we need all data for connection setup
            }
            else
            {
                ventsToRemove.Add(currentVent);
            }
        }

        if (ventsToRemove.Count > 0)
        {
            RemoveVentsBulk(ventsToRemove);
        }

        // Create missing vents
        var newlyCreatedVents = new List<Vent>();
        foreach (var kvp in syncedVents.Where(v => !ShipStatus.Instance.AllVents.Any(existing => existing.Id == v.Key)))
        {
            var newVent = AddVent(kvp.Value.Position, kvp.Key);
            newlyCreatedVents.Add(newVent);
        }

        var ventDictionary = ShipStatus.Instance.AllVents.ToDictionary(v => v.Id, v => v);

        foreach (var vent in ShipStatus.Instance.AllVents)
        {
            if (syncedVents.TryGetValue(vent.Id, out var ventData))
            {
                vent.Left = ventData.LeftId != -1 && ventDictionary.TryGetValue(ventData.LeftId, out var leftVent)
                    ? leftVent : null;
                vent.Center = ventData.CenterId != -1 && ventDictionary.TryGetValue(ventData.CenterId, out var centerVent)
                    ? centerVent : null;
                vent.Right = ventData.RightId != -1 && ventDictionary.TryGetValue(ventData.RightId, out var rightVent)
                    ? rightVent : null;
            }
        }
    }

    internal class VentData
    {
        internal VentData() { }

        internal VentData(Vector2 pos)
        {
            Position = pos;
        }

        internal int Id;
        internal int LeftId = -1;
        internal int CenterId = -1;
        internal int RightId = -1;
        internal Vector2 Position = Vector2.zero;

        internal void Setup(Vent vent, Dictionary<int, Vent> ventDictionary)
        {
            if (vent == null) return;

            vent.Left = LeftId != -1 && ventDictionary.TryGetValue(LeftId, out var leftVent)
                ? leftVent : null;
            vent.Center = CenterId != -1 && ventDictionary.TryGetValue(CenterId, out var centerVent)
                ? centerVent : null;
            vent.Right = RightId != -1 && ventDictionary.TryGetValue(RightId, out var rightVent)
                ? rightVent : null;
        }

        internal static VentData CreateFromVent(Vent vent)
        {
            return new VentData
            {
                Id = vent.Id,
                LeftId = vent.Left?.Id ?? -1,
                CenterId = vent.Center?.Id ?? -1,
                RightId = vent.Right?.Id ?? -1,
                Position = new Vector2(vent.transform.position.x, vent.transform.position.y)
            };
        }

        internal void Serialize(MessageWriter writer)
        {
            writer.Write(Id);
            writer.Write(LeftId);
            writer.Write(CenterId);
            writer.Write(RightId);
            writer.WriteVector2(Position);
        }

        internal static VentData Deserialize(MessageReader reader)
        {
            var data = new VentData
            {
                Id = reader.ReadInt32(),
                LeftId = reader.ReadInt32(),
                CenterId = reader.ReadInt32(),
                RightId = reader.ReadInt32(),
                Position = reader.ReadVector2()
            };
            return data;
        }
    }
}
