using BepInEx.Unity.IL2CPP.Utils;
using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using System.Collections;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Attributes;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.Interfaces;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using UnityEngine;

namespace TheBetterRoles.Network.RPCs;

[RegisterCustomRpc((uint)ReactorRPCs.DirtyNetworkClass)]
internal class RpcDirtyNetworkClass(Main plugin, uint id) : PlayerCustomRpc<Main, RpcDirtyNetworkClass.Data>(plugin, id)
{
    public override SendOption SendOption => SendOption.Reliable;
    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public readonly struct Data(INetworkClass InetClass, uint netId = 0, uint syncedBits = 0, bool isSyncVar = false, MessageReader? reader = null)
    {
        public readonly MessageReader? Reader = reader;
        public readonly INetworkClass INetClass = InetClass;
        public readonly uint NetId = netId;
        public readonly uint SyncedBits = syncedBits;
        public readonly bool IsSyncVar = isSyncVar;
    }

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.IsSyncVar);
        writer.WritePacked(data.INetClass.NetworkId);
        if (!data.IsSyncVar)
        {
            writer.WritePacked(data.INetClass.DirtyBits);
        }

        INetworkClass netClass = NetworkClass.GetFromNetId(data.INetClass.NetworkId);
        netClass ??= NetworkClassMono.GetFromNetId(data.INetClass.NetworkId);

        if (!data.IsSyncVar)
        {
            netClass?.Serialize(writer);
        }
        else
        {
            SyncVarAttribute.SerializeAll(writer, netClass);
        }
    }

    public override Data Read(MessageReader reader)
    {
        var isSyncVar = reader.ReadBoolean();
        var netId = reader.ReadPackedUInt32();
        var syncedBits = !isSyncVar ? reader.ReadPackedUInt32() : 0;
        return new Data(null, netId, syncedBits, isSyncVar, MessageReader.Get(reader));
    }

    public override void Handle(PlayerControl player, Data data)
    {
        CoroutineManager.Scene.StartCoroutine(CoSyncNetClass(player, data));
    }

    private static IEnumerator CoSyncNetClass(PlayerControl sender, Data data)
    {
        const int MaxAttempts = 25;
        const float RetryInterval = 0.5f;

        for (int i = 1; i <= MaxAttempts; i++)
        {
            if (!GameState.IsInGame) yield break;

            INetworkClass? netClass = NetworkClass.GetFromNetId(data.NetId);
            netClass ??= NetworkClassMono.GetFromNetId(data.NetId);
            if (netClass != null)
            {
                if (sender.GetClientId() != netClass.OwnerId && !sender.IsHost())
                {
                    Logger.Warning($"NetClass({data.NetId}) serialized by {sender.Data.name}, Invalid owner, {sender.GetClientId()} - {netClass.OwnerId} - {sender.IsHost()}");
                    NetworkLogger.Warning($"NetClass({data.NetId}) serialized by {sender.Data.name}, Invalid owner, {sender.GetClientId()} - {netClass.OwnerId} - {sender.IsHost()}");
                    yield break;
                }

                if (!data.IsSyncVar)
                {
                    netClass.SyncedBits.SyncedDirtyBits = data.SyncedBits;
                    netClass.Deserialize(data.Reader);
                    data.Reader.Recycle();
                }
                else
                {
                    SyncVarAttribute.DeserializeAll(data.Reader, netClass);
                    data.Reader.Recycle();
                }
                NetworkLogger.LogReceive($"Found and deserialize NetClass({data.NetId}), SyncVars: {data.IsSyncVar}");
                yield break;
            }

            NetworkLogger.LogReceive($"Attempt {i}/{MaxAttempts}: NetClass({data.NetId}) not found. Retrying in {RetryInterval}s...");
            yield return new WaitForSeconds(RetryInterval);
        }

        data.Reader.Recycle();
        Logger.Warning($"Unable to find and deserialize NetClass - {data.NetId}");
        NetworkLogger.Warning($"Unable to find and deserialize NetClass - {data.NetId}");
    }
}
