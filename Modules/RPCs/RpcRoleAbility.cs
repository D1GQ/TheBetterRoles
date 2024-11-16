using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheBetterRoles.Helpers;
using TheBetterRoles.Managers;
using TheBetterRoles.Roles;

namespace TheBetterRoles.RPCs
{
    [RegisterCustomRpc((uint)ReactorRPCs.RoleAbility)]
    public class RpcRoleAbility : PlayerCustomRpc<Main, RpcRoleAbility.Data>
    {
        public override SendOption SendOption => SendOption.Reliable;
        public override RpcLocalHandling LocalHandling => RpcLocalHandling.Before;
        public RpcRoleAbility(Main plugin, uint id) : base(plugin, id)
        {
        }

        public readonly struct Data(ushort roleHash, int buttonId, int targetId, TargetType targetType, CustomRoleBehavior? role = null, MessageReader? reader = null)
        {
            public readonly CustomRoleBehavior? Role = role;
            public readonly MessageReader? Reader = reader;

            public readonly ushort RoleHash = roleHash;
            public readonly int ButtonId = buttonId;
            public readonly int TargetId = targetId;
            public readonly TargetType TargetType = targetType;
        }

        public override void Write(MessageWriter writer, Data data)
        {
            writer.Write(data.RoleHash);
            writer.WritePacked(data.ButtonId);
            writer.WritePacked(data.TargetId);
            writer.Write((byte)data.TargetType);
            data.Role.AbilityWriter(data.ButtonId, data.Role, ref writer);
        }

        public override Data Read(MessageReader reader)
        {
            var roleHash = reader.ReadUInt16();
            var buttonId = reader.ReadPackedInt32();
            var targetId = reader.ReadPackedInt32();
            var targetType = (TargetType)reader.ReadByte();
            var role = CustomRoleManager.GetActiveRoleFromPlayers(role => role.RoleHash == roleHash);

            return new Data(roleHash, buttonId, targetId, targetType, role, reader);
        }

        public override void Handle(PlayerControl player, Data data)
        {
            PlayerControl? target = data.TargetType == TargetType.Player ? Utils.PlayerFromPlayerId(data.TargetId) : null;
            Vent? vent = data.TargetType == TargetType.Vent ? ShipStatus.Instance.AllVents.FirstOrDefault(v => v.Id == data.TargetId) : null;
            DeadBody? body = data.TargetType == TargetType.Body ? Main.AllDeadBodys.FirstOrDefault(b => b.ParentId == data.TargetId) : null;

            Logger.LogMethodPrivate($"Checking Ability({data.ButtonId}) usage on {Enum.GetName(data.TargetType)}: {data.TargetId}", GetType());
            if (data.Role.CheckRoleAction(data.ButtonId, target, vent, body) == true)
            {
                Logger.LogMethodPrivate($"Using Ability({data.ButtonId}) on {Enum.GetName(data.TargetType)}: {data.TargetId}", GetType());
                data.Role.SetCooldownAndUse(data.ButtonId);
                data.Role.OnAbilityUse(data.ButtonId, target, vent, body, data.Reader, data.TargetType);
            }
        }
    }
}
