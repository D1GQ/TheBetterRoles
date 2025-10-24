using BepInEx.Unity.IL2CPP.Utils;
using Il2CppInterop.Runtime.Attributes;
using Reactor.Utilities.Extensions;
using System.Collections;
using TheBetterRoles.CustomGameModes;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.Interfaces;
using TheBetterRoles.Modules;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheBetterRoles.Network;

internal class CatchedGameData : MonoBehaviour
{
    internal static CatchedGameData? Instance { get; private set; }

    internal static float lobbyTimer;
    internal static string lobbyTimerText = "";
    internal bool GameHasStarted = false;
    internal bool GameHasEnded = false;
    internal static NetworkedPlayerInfo[] CatchedPlayerData => FindObjectsOfType<NetworkedPlayerInfo>();
    internal IGameMode CurrentGameMode;
    internal readonly List<byte> CatchedWinners = [];
    internal readonly List<byte> CatchedSubWinners = [];
    internal EndGameReason CatchedGameEndReason;
    internal RoleClassTeam CatchedWinTeam;

    private void Update()
    {
        if (GameState.IsLobby && GameState.IsVanillaServer && lobbyTimer > 0)
        {
            lobbyTimer = Mathf.Max(0f, lobbyTimer -= Time.deltaTime);
            int minutes = (int)lobbyTimer / 60;
            int seconds = (int)lobbyTimer % 60;
            lobbyTimerText = $"{minutes:00}:{seconds:00}";
        }

        if (GameState.IsInGamePlay)
        {
            CurrentGameMode?.FixedUpdate();
        }
    }

    private void Awake()
    {
        if (!Instance.IsDestroyedOrNull() && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        CurrentGameMode = new NormalGameMode();
    }

    internal void OnGameStart()
    {
        GameHasStarted = true;
    }

    internal void OnGameEnd()
    {
        StorePlayerData();
    }

    private void StorePlayerData()
    {
        gameObject.DontDestroyOnLoad();
        foreach (var data in GameData.Instance.AllPlayers)
        {
            DontDestroyOnLoad(data.gameObject);
        }
        this.StartCoroutine(CoStorePlayerData());
    }

    [HideFromIl2Cpp]
    private IEnumerator CoStorePlayerData()
    {
        yield return new WaitForSeconds(0.6f);

        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
        foreach (var data in GameData.Instance.AllPlayers)
        {
            SceneManager.MoveGameObjectToScene(data.gameObject, SceneManager.GetActiveScene());
            data.transform.SetParent(gameObject.transform);
        }
    }
}
