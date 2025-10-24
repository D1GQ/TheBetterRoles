using TheBetterRoles.Items.Attributes;

namespace TheBetterRoles.Commands;

[RegisterCommand]
internal sealed class TestCommand : BaseCommand
{
    internal override CommandType Type => CommandType.Debug;
    protected override string CommandName => "Test";
    internal override uint ShortNamesAmount => 0;

    internal override void Run()
    {
    }
}