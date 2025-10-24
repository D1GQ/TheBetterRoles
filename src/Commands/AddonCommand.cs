
using TheBetterRoles.Items.Attributes;

namespace TheBetterRoles.Commands;

[RegisterCommand]
internal sealed class AddonCommand : RoleCommand
{
    internal override bool IsAddonCommand => true;
}