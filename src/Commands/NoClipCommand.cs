using TheBetterRoles.Data;
using TheBetterRoles.Items.Attributes;
using TheBetterRoles.Modules;
using TheBetterRoles.Network.Configs;

namespace TheBetterRoles.Commands;

[RegisterCommand]
internal sealed class NoClipCommand : BaseCommand
{
    internal override CommandType Type => CommandType.Debug;
    protected override string CommandName => "NoClip";
    internal override uint ShortNamesAmount => 0;

    internal override bool ShowCommand() => (GameState.IsFreePlay || TBRGameSettings.Debugging.GetBool()) && Main.MyData.HasAll() && Main.MyData.IsVerified();

    internal override void Run()
    {
        PlayerControl.LocalPlayer.Collider.enabled = !PlayerControl.LocalPlayer.Collider.enabled;
        CommandResultText($"Player Collision set to <#7F7F7F>{PlayerControl.LocalPlayer.Collider.enabled}</color>");
    }
}
