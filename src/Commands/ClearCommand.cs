using TheBetterRoles.Items.Attributes;
using TheBetterRoles.Patches.UI.Chat;

namespace TheBetterRoles.Commands;

[RegisterCommand]
internal sealed class ClearCommand : BaseCommand
{
    internal override CommandType Type => CommandType.Normal;
    protected override string CommandName => "Clear";
    internal override uint ShortNamesAmount => 0;

    internal override void Run()
    {
        ChatControllerPatch.ClearChat();
    }
}
