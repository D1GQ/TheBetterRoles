using TheBetterRoles.Items.Enums;

namespace TheBetterRoles.Roles.Interfaces;

internal interface IRoleGuessAction : IRoleAction
{
    /// <summary>
    /// Check for a player guessing another player's role.
    /// If this method returns false, it will cancel the designated action.
    /// </summary>
    bool CheckGuessOther(PlayerControl guesser, PlayerControl target, RoleClassTypes role) => true;

    /// <summary>
    /// Check for the local player attempting to guess another player's role.
    /// If this method returns false, it will cancel the designated action.
    /// </summary>
    bool CheckGuess(PlayerControl guesser, PlayerControl target, RoleClassTypes role) => true;

    /// <summary>
    /// Executes when a player has made a guess about another player's role.
    /// Custom logic for handling the result of a guess can be added here.
    /// </summary>
    void Guess(PlayerControl guesser, PlayerControl target, RoleClassTypes role) { }

    /// <summary>
    /// Executes when a player has made a guess about another player's role using a method intended for handling special cases or conditions.
    /// Custom logic for processing the result of such guesses can be added here.
    /// </summary>
    void GuessOther(PlayerControl guesser, PlayerControl target, RoleClassTypes role) { }
}
