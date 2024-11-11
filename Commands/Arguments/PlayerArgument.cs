using TheBetterRoles.Modules;

namespace TheBetterRoles.Commands;

public class PlayerArgument(BaseCommand? command) : BaseArgument(command)
{
    public override string Suggestion => "{Id}";
    public override T? TryGetTarget<T>() where T : class
    {
        var digits = Arg.Where(char.IsDigit).ToArray();
        bool isDigitFlag = digits.Any();
        bool playerFound = false;

        if (isDigitFlag)
        {
            if (int.TryParse(new string(digits), out var playerId))
            {
                playerFound = Main.AllPlayerControls.Any(player => !player.isDummy && player.Data.PlayerId == playerId);
            }
        }

        if (playerFound && byte.TryParse(Arg, out var num))
        {
            return Utils.PlayerFromPlayerId(num) as T;
        }
        else
        {
            if (!isDigitFlag)
            {
                BaseCommand.CommandErrorText($"Improper syntax!");
            }
            else if (!playerFound)
            {
                BaseCommand.CommandErrorText($"Player not found!");
            }
        }

        return null;
    }
}