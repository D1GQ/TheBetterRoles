
namespace TheBetterRoles.Commands
{
    public class AddonCommand : RoleCommand
    {
        public override string Name => "addon";
        public override bool IsAddonCommand => true;
    }
}
