using TheBetterRoles.Helpers;

namespace TheBetterRoles.Commands;

internal class PlayerArgument(BaseCommand? command, string argInfoTranStr, (string prefix, string postfix) prefix_postfix) : BaseArgument(command, argInfoTranStr, prefix_postfix)
{
    protected override string[] ArgSuggestions => Main.AllPlayerControls
        .OrderBy(pc => pc.IsLocalPlayer() ? 0 : 1)
        .Select(pc => pc.Data.PlayerName.ToLower()
        .Replace(' ', '_')).ToArray();

    internal PlayerControl? TryGetTarget(bool showError = true)
    {
        var player = Main.AllPlayerControls.FirstOrDefault(pc => pc.Data.PlayerName.ToLower().Replace(' ', '_') == Arg.ToLower());

        if (player == null && !string.IsNullOrEmpty(Arg) && showError)
        {
            BaseCommand.CommandErrorText($"Player not found!");
        }

        return player;
    }
}