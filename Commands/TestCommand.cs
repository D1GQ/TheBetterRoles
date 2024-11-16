using TheBetterRoles.Helpers;

namespace TheBetterRoles.Commands;

public class TestCommand : BaseCommand
{
    public override CommandType Type => CommandType.Debug;
    public override string Name => "test";
    public override string Description => "Test Command";
    public override void Run()
    {
        PlayerControl.LocalPlayer.SetCamouflage(PlayerControl.LocalPlayer.BetterData().CamouflagedQueue);
    }
}
