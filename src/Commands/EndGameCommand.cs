using Reactor.Networking.Rpc;
using TheBetterRoles.Items.Attributes;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Modules;
using TheBetterRoles.Network.RPCs;

namespace TheBetterRoles.Commands;

[RegisterCommand]
internal sealed class EndGameCommand : BaseCommand
{
    internal override CommandType Type => CommandType.Debug;
    protected override string CommandName => "EndGame";
    internal override uint ShortNamesAmount => 0;

    private StringArgument? ReasonArgument => (StringArgument)Arguments[0];
    private StringArgument? TeamArgument => (StringArgument)Arguments[1];
    internal EndGameCommand()
    {
        Arguments = [new StringArgument(this, "reason", ("[", "]")), new StringArgument(this, "team", ("[", "]"))];
        ReasonArgument.GetArgSuggestions = () => { return Enum.GetNames(typeof(EndGameReason)).ToList().Select(name => name.ToLower()).ToArray(); };
        TeamArgument.GetArgSuggestions = () => { return Enum.GetNames(typeof(RoleClassTeam)).ToList().Select(name => name.ToLower()).ToArray(); };
    }

    internal override bool ShowCommand() => GameState.IsHost && GameState.IsInGamePlay || GameState.IsFreePlay;
    internal override void Run()
    {
        if (!GameState.IsHost) return;

        var reasonNames = ReasonArgument.GetArgSuggestions.Invoke();
        int reasonIndex = Array.IndexOf(reasonNames, ReasonArgument.Arg.ToLower());
        EndGameReason reason = (EndGameReason)reasonIndex;

        var teamNames = TeamArgument.GetArgSuggestions.Invoke();
        int teamIndex = Array.IndexOf(teamNames, ReasonArgument.Arg.ToLower());
        RoleClassTeam team = (RoleClassTeam)teamIndex;

        Rpc<RpcEndGame>.Instance.Send(new(Main.AllPlayerControls.Select(pc => pc.PlayerId).ToList(), reason, team));
    }
}