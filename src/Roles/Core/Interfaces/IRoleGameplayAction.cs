namespace TheBetterRoles.Roles.Core.Interfaces;

internal interface IRoleGameplayAction : IRoleAction
{
    /// <summary>
    /// This method is called at the end of the intro cutscene.
    /// </summary>
    void IntroCutsceneEnd() { }

    /// <summary>
    /// Determines the win condition for the role. This can be overridden by roles that have special win conditions.
    /// </summary>
    bool WinCondition() => false;

    /// <summary>
    /// This method is called at the end of the game to process the winning players.
    /// </summary>
    void GameEnd() { }

    /// <summary>
    /// This method is called at the end of the game if a player won.
    /// </summary>
    void OnWinOther(PlayerControl player) { }

    /// <summary>
    /// This method is called at the end of the game if the local player won.
    /// </summary>
    void OnWin() { }
}
