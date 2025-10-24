using System.Collections;
using TheBetterRoles.Items.Enums;

namespace TheBetterRoles.Items.Interfaces;

/// <summary>
/// Defines the core structure for custom game modes, providing essential methods
/// that dictate the flow and behavior of the game from start to finish.
/// </summary>
internal interface IGameMode
{
    /// <summary>
    /// Gets the specific type of custom game mode being implemented.
    /// </summary>
    CustomGameMode gameMode { get; }

    /// <summary>
    /// Handles the assignment of roles to players at the beginning of the game.
    /// This method should define how roles are distributed among players.
    /// </summary>
    IEnumerator CoAssignRoles();

    /// <summary>
    /// Executes logic in a fixedupdate
    /// </summary>
    void FixedUpdate();

    /// <summary>
    /// Executes logic when the game starts, initializing necessary components
    /// and setting up the game environment.
    /// </summary>
    void OnGameStart();

    /// <summary>
    /// Executes logic to try to end the game.
    /// </summary>
    void CheckAllWinConditions(bool initial = false);

    /// <summary>
    /// Determines rather if gameplay should be in re-enabled after a meeting.
    /// </summary>
    bool ReEnableGameplay();

    /// <summary>
    /// Executes logic to check if the game should end by sabotage.
    /// </summary>
    bool CheckSabotageWin();

    /// <summary>
    /// Executes logic when a player disconnects.
    /// </summary>
    void OnDisconnect(PlayerControl player);

    /// <summary>
    /// Executes logic when a player dies.
    /// </summary>
    void OnPlayerDeath(PlayerControl player);

    /// <summary>
    /// Executes logic when a player gets revived.
    /// </summary>
    void OnPlayerRevive(PlayerControl player);

    /// <summary>
    /// Executes logic when the game ends, cleaning up resources and handling
    /// post-game operations.
    /// </summary>
    void OnGameEnd();

    /// <summary>
    /// Plays the introduction cutscene at the start of the game, allowing for any
    /// setup or storytelling elements to be presented to the players.
    /// </summary>
    /// <param name="introCutscene">The cutscene to be played at the start of the game.</param>
    IEnumerator CoPlayIntro(IntroCutscene introCutscene);

    /// <summary>
    /// Sets up the outro sequence when the game concludes, using the provided end game manager
    /// to handle any final cutscenes, statistics, or summary screens.
    /// </summary>
    /// <param name="endGameManager">Manager responsible for handling the end game sequence.</param>
    void SetUpOutro(EndGameManager endGameManager);
}