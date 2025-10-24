using Reactor.Networking.Rpc;
using System.Collections;
using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.Interfaces;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Monos;
using TheBetterRoles.Network;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Roles.Interfaces;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.CustomGameModes;

internal class NormalGameMode : NetworkClass, IGameMode
{
    internal NormalGameMode()
    {
        SetUpNetworkClass();
    }

    public CustomGameMode gameMode => CustomGameMode.Normal;

    public IEnumerator CoAssignRoles()
    {
        yield return CustomRoleManager.CoAssignRoles();
    }

    public IEnumerator CoPlayIntro(IntroCutscene introCutscene)
    {
        introCutscene.HideAndSeekPanels.SetActive(false);
        introCutscene.CrewmateRules.SetActive(false);
        introCutscene.ImpostorRules.SetActive(false);
        introCutscene.ImpostorName.gameObject.SetActive(false);
        introCutscene.ImpostorTitle.gameObject.SetActive(false);
        List<PlayerControl> teamToShow = PlayerControl.LocalPlayer.Is(RoleClassTeam.Crewmate) || PlayerControl.LocalPlayer.Is(RoleClassTeam.Apocalypse) ? Main.AllPlayerControls.ToList() : Main.AllPlayerControls.Where(p => p.IsTeammate()).ToList();
        teamToShow = teamToShow.OrderBy(player => player.IsLocalPlayer() ? 0 : 1).ToList();
        SoundManager.Instance.PlaySound(introCutscene.IntroStinger, false, 1f, null);
        yield return CoShowTeam(introCutscene, teamToShow, 3f);
        yield return CoShowRole(introCutscene);
        ShipStatus.Instance.StartSFX();
        CatchedGameData.Instance?.CurrentGameMode?.CheckAllWinConditions(true);
        introCutscene.DestroyObj();
        yield break;
    }

    private static IEnumerator CoShowTeam(IntroCutscene introCutscene, List<PlayerControl> teamToShow, float duration)
    {
        if (introCutscene.overlayHandle == null)
        {
            introCutscene.overlayHandle = DualshockLightManager.Instance.AllocateLight();
        }
        yield return ShipStatus.Instance.CosmeticsCache.PopulateFromPlayers();

        Begin(introCutscene, teamToShow);

        Color c = introCutscene.TeamTitle.color;
        Color fade = Color.black;
        Color impColor = Color.white;
        Vector3 titlePos = introCutscene.TeamTitle.transform.localPosition;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float num = Mathf.Min(1f, timer / duration);
            introCutscene.Foreground.material.SetFloat("_Rad", introCutscene.ForegroundRadius.ExpOutLerp(num * 2f));
            fade.a = Mathf.Lerp(1f, 0f, num * 3f);
            introCutscene.FrontMost.color = fade;
            c.a = Mathf.Clamp(FloatRange.ExpOutLerp(num, 0f, 1f), 0f, 1f);
            introCutscene.TeamTitle.color = c;
            introCutscene.RoleText.color = c;
            impColor.a = Mathf.Lerp(0f, 1f, (num - 0.3f) * 3f);
            introCutscene.ImpostorText.color = impColor;
            titlePos.y = 2.7f - num * 0.3f;
            introCutscene.TeamTitle.transform.localPosition = titlePos;
            introCutscene.overlayHandle.color.SetAlpha(Mathf.Min(1f, timer * 2f));
            yield return null;
        }
        timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime;
            float num2 = timer / 1f;
            fade.a = Mathf.Lerp(0f, 1f, num2 * 3f);
            introCutscene.FrontMost.color = fade;
            introCutscene.overlayHandle.color.SetAlpha(1f - fade.a);
            yield return null;
        }
        yield break;
    }

    public static void Begin(IntroCutscene introCutscene, List<PlayerControl> teamToDisplay)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        Vector3 position = introCutscene.BackgroundBar.transform.position;
        position.y -= 0.25f;
        introCutscene.BackgroundBar.transform.position = position;
        introCutscene.BackgroundBar.material.SetColor("_Color", localPlayer.GetTeamColor());
        introCutscene.TeamTitle.DestroyTextTranslators();
        introCutscene.TeamTitle.text = localPlayer.GetRoleTeamName();
        introCutscene.TeamTitle.color = localPlayer.GetTeamColor();
        var flag = !localPlayer.Is(RoleClassTeam.Impostor);
        introCutscene.ImpostorText.gameObject.SetActive(flag);

        if (flag)
        {
            int impsCount = Main.AllPlayerControls.Where(pc => pc.Is(RoleClassTeam.Impostor)).Count();
            introCutscene.ImpostorText.DestroyTextTranslators();

            if (impsCount == 1)
            {
                introCutscene.ImpostorText.text = Translator.GetString(StringNames.NumImpostorsS);
            }
            else
            {
                introCutscene.ImpostorText.text = Translator.GetString(StringNames.NumImpostorsP, [impsCount.ToString()]);
            }
        }

        int maxDepth = Mathf.CeilToInt(7.5f);
        for (int i = 0; i < teamToDisplay.Count; i++)
        {
            PlayerControl playerControl = teamToDisplay[i];
            if (playerControl)
            {
                NetworkedPlayerInfo data = playerControl.Data;
                if (!(data == null))
                {
                    PoolablePlayer poolablePlayer = introCutscene.CreatePlayer(i, maxDepth, data, !localPlayer.Is(RoleClassTeam.Crewmate) && !localPlayer.Is(RoleClassTeam.Apocalypse));
                    if (i == 0 && data.PlayerId == localPlayer.PlayerId)
                    {
                        introCutscene.ourCrewmate = poolablePlayer;
                    }
                }
            }
        }
    }

    private static IEnumerator CoShowRole(IntroCutscene __instance)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        PlayIntroSound();

        Color RoleColor = localPlayer.GetRoleColor();

        __instance.ourCrewmate.ToggleName(false);
        __instance.RoleText.text = localPlayer.GetRoleName();

        var addonText = UnityEngine.Object.Instantiate(__instance.RoleBlurbText, __instance.RoleBlurbText.transform.parent);
        if (addonText != null)
        {
            addonText.name = "Addons(TMP)";
            addonText.color = Color.white;
            addonText.text = "";
            addonText.transform.position += new Vector3(0f, 2.8f, 0f);
            bool first = true;
            foreach (var addon in localPlayer.ExtendedData().RoleInfo.Addons)
            {
                if (addon == null) continue;

                if (!first) addonText.text += " + ";
                addonText.text += $"<color={addon.RoleColorHex}>{addon.RoleName}</color>";
                first = false;
            }
        }

        string addons = "";
        if (localPlayer.ExtendedData().RoleInfo.Addons.Any())
        {
            addons = "\n" + localPlayer.GetAddonInfo(amount: 2);
        }
        __instance.RoleBlurbText.text = localPlayer.GetRoleInfo() + addons;
        __instance.ImpostorText.gameObject.SetActive(false);
        __instance.TeamTitle.gameObject.SetActive(false);
        __instance.BackgroundBar.material.color = RoleColor;
        __instance.BackgroundBar.transform.SetLocalZ(-15);
        __instance.transform.Find("BackgroundLayer").transform.SetLocalZ(-15);
        __instance.YouAreText.color = RoleColor;
        __instance.RoleText.color = RoleColor;
        __instance.RoleBlurbText.color = RoleColor;

        HudManager.Instance?.MapButton?.gameObject?.SetActive(true);
        HudManager.Instance?.MapButton?.SelectButton(false);

        __instance.YouAreText.gameObject.SetActive(true);
        __instance.RoleText.gameObject.SetActive(true);
        __instance.RoleBlurbText.gameObject.SetActive(true);
        if (__instance.ourCrewmate == null)
        {
            __instance.ourCrewmate = __instance.CreatePlayer(0, 1, localPlayer.Data, false);
            __instance.ourCrewmate.gameObject.SetActive(false);
        }
        __instance.ourCrewmate.gameObject.SetActive(true);
        __instance.ourCrewmate.transform.localPosition = new Vector3(0f, -1.05f, -18f);
        __instance.ourCrewmate.transform.localScale = new Vector3(1f, 1f, 1f);
        __instance.ourCrewmate.ToggleName(false);
        yield return new WaitForSeconds(2.5f);
        __instance.YouAreText.gameObject.SetActive(false);
        __instance.RoleText.gameObject.SetActive(false);
        __instance.RoleBlurbText.gameObject.SetActive(false);
        __instance.ourCrewmate.gameObject.SetActive(false);
        yield break;
    }

    private static void PlayIntroSound()
    {
        SoundManager.Instance.StopSound(DestroyableSingleton<RoleBehaviour>.Instance.IntroSound);
        SoundManager.Instance.PlaySound(PlayerControl.LocalPlayer?.Role()?.IntroSound, false, 1f, null);
    }

    public void FixedUpdate()
    {
        CheckSabotageWin();
    }

    public void OnGameStart()
    {
        /*
        GameManager.DestroyInstance();
        if (GameState.IsHost)
        {
            var gameManager = GameManagerCreator.CreateGameManager(GameModes.Normal);
            AmongUsClient.Instance.Spawn(gameManager, -2, SpawnFlags.None);
        }
        */
    }

    public void OnGameEnd() { }

    public void CheckAllWinConditions(bool initial = false)
    {
        if (initial)
        {
            _ = CheckAllWinConditionsDelay();
            return;
        }

        if (CheckCustomObjectiveWin()) return;
        if (CheckPlayerAmountWin()) return;
        if (CheckSabotageWin()) return;
    }

    private async Task CheckAllWinConditionsDelay()
    {
        await Task.Delay(500); // Wait 0.5s so intro can end
        CheckPlayerAmountWin();
        CheckCustomObjectiveWin();
        CheckSabotageWin();
    }

    public bool ReEnableGameplay()
    {
        if (CheckPlayerAmountWin(true)) return false;
        if (CheckCustomObjectiveWin(true)) return false;

        return true;
    }

    public void OnDisconnect(PlayerControl player)
    {
        CheckPlayerAmountWin();
    }

    public void OnPlayerDeath(PlayerControl player)
    {
        CheckPlayerAmountWin();
    }

    public void OnPlayerRevive(PlayerControl player)
    {
        CheckPlayerAmountWin();
    }

    public static List<NetworkedPlayerInfo> GetPlayersFromTeam(RoleClassTeam team)
    {
        List<NetworkedPlayerInfo> players = [];
        foreach (var data in GameData.Instance.AllPlayers)
        {
            if (data?.ExtendedData()?.RoleInfo?.Role?.RoleTeam != team) continue;
            players.Add(data);
        }

        return players;
    }

    public static List<byte> GetPlayerIdsFromTeam(RoleClassTeam team)
    {
        List<byte> players = [];

        foreach (var data in GameData.Instance.AllPlayers)
        {
            if (data?.ExtendedData()?.RoleInfo?.Role?.RoleTeam != team)
                continue;
            if (data?.ExtendedData()?.RoleInfo?.Role?.CountToPlayerAmount == false)
                continue;

            players.Add(data.PlayerId);
        }

        return players;
    }

    private readonly List<byte> crewmatePlayers = [];
    private readonly List<byte> impostorPlayers = [];
    private readonly List<byte> killingNeutralPlayers = [];
    private readonly List<byte> nonKillingNeutralPlayers = [];
    private readonly List<byte> apocalypsePlayers = [];

    public bool CheckPlayerAmountWin(bool checkEnableGameplay = false)
    {
        if (!GameState.IsShip || GameState.IsFreePlay) return false;
        if (checkEnableGameplay && GameState.IsFreePlay) return false;
        if (CatchedGameData.Instance.GameHasEnded || TBRGameSettings.NoGameEnd.GetBool()) return false;
        if (!CustomGameManager.ShouldCheckWinConditions && !checkEnableGameplay) return false;
        if (!checkEnableGameplay && !GameState.IsHost) return false;
        bool shouldEndGame = CustomGameManager.ShouldCheckWinConditions || (checkEnableGameplay && !GameState.IsFreePlay);

        crewmatePlayers.Clear();
        impostorPlayers.Clear();
        killingNeutralPlayers.Clear();
        nonKillingNeutralPlayers.Clear();
        apocalypsePlayers.Clear();

        foreach (var pc in Main.AllPlayerControls)
        {
            if (!pc.IsAlive()) continue;
            var role = pc.Role();
            if (role == null || !role.CountToPlayerAmount || pc.ExtendedPC().IsFake) continue;

            byte playerId = pc.PlayerId;

            if (role.IsCrewmate)
            {
                crewmatePlayers.Add(playerId);
            }
            else if (role.IsImpostor)
            {
                if (!role.IsBenign)
                {
                    impostorPlayers.Add(playerId);
                }
                else
                {
                    nonKillingNeutralPlayers.Add(playerId);
                }
            }
            else if (role.IsNeutral || (role.IsApocalypse && role.IsKillingRole))
            {
                if (role.IsKillingRole)
                {
                    if (!role.IsBenign)
                    {
                        killingNeutralPlayers.Add(playerId);
                    }
                    else
                    {
                        nonKillingNeutralPlayers.Add(playerId);
                    }
                }
                else
                {
                    nonKillingNeutralPlayers.Add(playerId);
                }
            }
            else if (role.IsApocalypse)
            {
                if (!role.IsBenign)
                {
                    apocalypsePlayers.Add(playerId);
                }
                else
                {
                    nonKillingNeutralPlayers.Add(playerId);
                }
            }
        }

        if (crewmatePlayers.Count == 0 && impostorPlayers.Count == 0 && killingNeutralPlayers.Count == 0)
        {
            if (checkEnableGameplay && !GameState.IsHost) return true;

            if (shouldEndGame)
            {
                Logger.Log($"Ending Game As Host: Draw");
                Rpc<RpcEndGame>.Instance.Send(new([], EndGameReason.Draw, RoleClassTeam.None), true);
                CatchedGameData.Instance.GameHasEnded = true;
            }
            return true;
        }

        if (impostorPlayers.Count == 0 && killingNeutralPlayers.Count == 0 && crewmatePlayers.Count <= apocalypsePlayers.Count)
        {
            if (checkEnableGameplay && !GameState.IsHost) return true;

            if (shouldEndGame)
            {
                Logger.Log($"Ending Game As Host: {Utils.GetCustomRoleTeamName(RoleClassTeam.Crewmate)} Outnumbered");
                Rpc<RpcEndGame>.Instance.Send(new(crewmatePlayers, EndGameReason.Outnumbered, RoleClassTeam.Crewmate), true);
                CatchedGameData.Instance.GameHasEnded = true;
            }
            return true;
        }

        if (killingNeutralPlayers.Count == 0 && apocalypsePlayers.Count == 0)
        {
            if (impostorPlayers.Count >= (crewmatePlayers.Count + nonKillingNeutralPlayers.Count))
            {
                if (checkEnableGameplay && !GameState.IsHost) return true;

                if (shouldEndGame)
                {
                    Logger.Log($"Ending Game As Host: {Utils.GetCustomRoleTeamName(RoleClassTeam.Impostor)} Outnumbered");
                    Rpc<RpcEndGame>.Instance.Send(new(impostorPlayers, EndGameReason.Outnumbered, RoleClassTeam.Impostor), true);
                    CatchedGameData.Instance.GameHasEnded = true;
                }
                return true;
            }
        }

        if (impostorPlayers.Count == 0 && apocalypsePlayers.Count == 0)
        {
            if ((crewmatePlayers.Count + nonKillingNeutralPlayers.Count) <= 1 && killingNeutralPlayers.Count == 1)
            {
                if (checkEnableGameplay && !GameState.IsHost) return true;

                if (shouldEndGame)
                {
                    Logger.Log($"Ending Game As Host: {Utils.GetCustomRoleTeamName(RoleClassTeam.Neutral)} Outnumbered");
                    Rpc<RpcEndGame>.Instance.Send(new([killingNeutralPlayers[0]], EndGameReason.Outnumbered, RoleClassTeam.Neutral), true);
                    CatchedGameData.Instance.GameHasEnded = true;
                }
                return true;
            }
        }

        return false;
    }

    public bool CheckCustomObjectiveWin(bool checkEnableGameplay = false, bool forceEnd = false)
    {
        if (checkEnableGameplay && GameState.IsFreePlay) return false;
        if (CatchedGameData.Instance.GameHasEnded || TBRGameSettings.NoGameEnd.GetBool()) return false;
        if (!CustomGameManager.ShouldCheckWinConditions && !checkEnableGameplay && !forceEnd) return false;
        if (!checkEnableGameplay && !GameState.IsHost) return false;
        bool shouldEndGame = CustomGameManager.ShouldCheckWinConditions || (checkEnableGameplay && !GameState.IsFreePlay) || forceEnd;

        foreach (var player in Main.AllRealPlayerControls)
        {
            if (player.Role() is not IRoleGameplayAction action) continue;
            if (action.WinCondition())
            {
                if (checkEnableGameplay && !GameState.IsHost) return true;

                if (shouldEndGame)
                {
                    if (player.Role().IsNeutral)
                    {
                        var team = player.Role().RoleTeam;
                        Logger.Log($"Ending Game As Host: {player.Data.PlayerName} Role -> {player.GetRoleName()} Win Condition Met");
                        Rpc<RpcEndGame>.Instance.Send(new([player.PlayerId], EndGameReason.CustomFromRole, team), true);
                        CatchedGameData.Instance.GameHasEnded = true;
                    }
                    else if (player.Role().IsApocalypse)
                    {
                        var Apocalypse = GetPlayerIdsFromTeam(RoleClassTeam.Apocalypse);
                        Logger.Log($"Ending Game As Host: {player.Data.PlayerName} Role -> {player.GetRoleName()} Win Condition Met");
                        Rpc<RpcEndGame>.Instance.Send(new(Apocalypse, EndGameReason.CustomFromRole, RoleClassTeam.Apocalypse), true);
                        CatchedGameData.Instance.GameHasEnded = true;
                    }
                }
                return true;
            }
        }

        return false;
    }

    public bool CheckSabotageWin()
    {
        if (CatchedGameData.Instance.GameHasEnded || TBRGameSettings.NoGameEnd.GetBool()) return false;
        if (!CustomGameManager.ShouldCheckWinConditions) return false;
        if (!GameState.IsHost) return false;

        void EndGame()
        {
            var Impostors = GetPlayerIdsFromTeam(RoleClassTeam.Impostor);
            Logger.Log("Ending Game As Host: Critical Sabotage");
            Rpc<RpcEndGame>.Instance.Send(new(Impostors, EndGameReason.Sabotage, RoleClassTeam.Impostor), true);
            CatchedGameData.Instance.GameHasEnded = true;
        }

        ISystemType systemType;
        if (ShipStatus.Instance.Systems.TryGetValue(SystemTypes.LifeSupp, out systemType))
        {
            LifeSuppSystemType lifeSuppSystemType = systemType.Cast<LifeSuppSystemType>();
            if (lifeSuppSystemType.Countdown < 0f)
            {
                lifeSuppSystemType.Countdown = 10000f;
                EndGame();
                return true;
            }
        }
        foreach (ISystemType systemType2 in ShipStatus.Instance.Systems.Values)
        {
            ICriticalSabotage? criticalSabotage = systemType2.TryCast<ICriticalSabotage>();
            if (criticalSabotage != null && criticalSabotage.Countdown < 0f)
            {
                criticalSabotage.ClearSabotage();
                EndGame();
                return true;
            }
        }

        return false;
    }

    public void SetUpOutro(EndGameManager endGameManager)
    {
        GameSummaryManager.SetupGameSummary(endGameManager);

        List<NetworkedPlayerInfo> players = CatchedGameData.CatchedPlayerData
            .Where(CustomGameManager.CheckWinner)
            .OrderBy(data => data.PlayerId)
            .ThenBy(data => data.ExtendedData()?.IsLocalData == true ? 0 : 1)
            .ToList();

        bool hasWinners = players.Any();
        NetworkedPlayerInfo? firstWinner = hasWinners ? players.First() : null;

        var role = firstWinner?.Role()?.RoleType ?? RoleClassTypes.Crewmate;
        var team = CatchedGameData.Instance.CatchedWinTeam;

        Color teamColor = team != RoleClassTeam.Neutral
            ? Colors.HexToColor(Utils.GetCustomRoleTeamColorHex(team))
            : firstWinner?.Role()?.RoleColor ?? Color.gray;

        UpdateWinText(endGameManager, team, role, ref teamColor, players);

        PlayEndGameSound(endGameManager, players, hasWinners);

        DisplayEndGamePlayers(endGameManager, players);

        endGameManager.BackgroundBar.material.SetColor("_Color", teamColor);
    }

    private static void UpdateWinText(EndGameManager __instance, RoleClassTeam team, RoleClassTypes role, ref Color teamColor, List<NetworkedPlayerInfo> players)
    {
        __instance.WinText.alignment = TextAlignmentOptions.Right;
        __instance.WinText.text = $"<color={Colors.Color32ToHex(teamColor)}>";

        switch (team)
        {
            case RoleClassTeam.Impostor:
                __instance.WinText.text += $"{Translator.GetString(StringNames.ImpostorsCategory)}\n<size=75%>";
                LogGameEnd("Impostors", players);
                break;

            case RoleClassTeam.Crewmate:
                __instance.WinText.text += $"{Translator.GetString(StringNames.Crewmates)}\n<size=75%>";
                LogGameEnd("Crewmates", players);
                break;

            case RoleClassTeam.Apocalypse:
                __instance.WinText.text += $"{Utils.GetCustomRoleTeamName(RoleClassTeam.Apocalypse)}\n<size=75%>";
                LogGameEnd("Apocalypse", players);
                break;

            case RoleClassTeam.Neutral:
                __instance.WinText.text += $"{Utils.GetCustomRoleName(role)}\n<size=75%>";
                LogGameEnd("Neutral", players);
                break;

            case RoleClassTeam.None:
                HandleNoWinningTeam(__instance, ref teamColor);
                return;

            default:
                HandleCriticalError(__instance, ref teamColor);
                break;
        }

        HighlightSubWinners(__instance, players, teamColor);
    }

    private static void HandleNoWinningTeam(EndGameManager __instance, ref Color teamColor)
    {
        __instance.WinText.alignment = TextAlignmentOptions.Center;

        if (CatchedGameData.Instance.CatchedGameEndReason == EndGameReason.Draw)
        {
            __instance.WinText.text = Translator.GetString("Game.Summary.Draw");
            __instance.WinText.color = teamColor = new Color(1f, 1f, 1f);
            Logger.Log("Game Has Ended: Draw");
        }
        else if (CatchedGameData.Instance.CatchedGameEndReason == EndGameReason.ByHost)
        {
            __instance.WinText.text = Translator.GetString("Game.Summary.Abandoned");
            __instance.WinText.color = teamColor = new Color(0f, 0.5f, 1f);
            Logger.Log("Game Has Ended: By Host");
        }
        else if (CatchedGameData.Instance.CatchedGameEndReason == EndGameReason.CriticalError)
        {
            HandleCriticalError(__instance, ref teamColor);
        }
    }

    private static void HandleCriticalError(EndGameManager __instance, ref Color teamColor)
    {
        __instance.WinText.alignment = TextAlignmentOptions.Center;
        __instance.WinText.text = Translator.GetString("Game.Summary.CriticalError");
        __instance.WinText.color = teamColor = Color.red;
        Logger.Log("Game Has Ended: Error");
    }

    private static void HighlightSubWinners(EndGameManager __instance, List<NetworkedPlayerInfo> players, Color teamColor)
    {
        List<RoleClassTypes> rolesShown = [];

        foreach (var playerId in CatchedGameData.Instance.CatchedSubWinners)
        {
            if (CatchedGameData.Instance.CatchedWinners.Contains(playerId)) continue;

            var player = Utils.PlayerDataFromPlayerId(playerId);
            players.Add(player);

            bool shouldSkipOuterLoop = false;
            foreach (var addon in player.ExtendedData().RoleInfo.Addons.Where(ad => ad?.ShowRoleInOutro == true))
            {
                if (rolesShown.Contains(addon.RoleType)) return;

                __instance.WinText.text += $"<size=50%><color=#FFFFFF>+</color> " + addon.RoleNameAndColor + "\n";
                rolesShown.Add(addon.RoleType);

                shouldSkipOuterLoop = true;
                break;
            }

            if (shouldSkipOuterLoop) continue;

            if (player.Role()?.ShowRoleInOutro == true && player.Role()?.RoleTeam != CatchedGameData.Instance.CatchedWinTeam)
            {
                __instance.WinText.text += $"<size=50%><color=#FFFFFF>+</color> " + player.Role().RoleNameAndColor + "\n";
                rolesShown.Add(player.Role().RoleType);
            }
        }

        if (CatchedGameData.Instance.CatchedWinTeam != RoleClassTeam.None)
        {
            __instance.WinText.text += $" {Translator.GetString("Game.Summary.Won")}</color>";
        }
    }

    private static void PlayEndGameSound(EndGameManager __instance, List<NetworkedPlayerInfo> players, bool hasWinners)
    {
        bool isLocalPlayerWinner = players.Any(data => data.ExtendedData().IsLocalData);

        if (CatchedGameData.Instance.CatchedGameEndReason == EndGameReason.Draw)
        {
            SoundManager.Instance.PlaySound(DestroyableSingleton<EndGameManager>.Instance.DisconnectStinger, false);
            return;
        }

        if (!hasWinners)
        {
            SoundManager.Instance.PlaySound(DestroyableSingleton<EndGameManager>.Instance.DisconnectStinger, false);
        }
        else if (isLocalPlayerWinner)
        {
            SoundManager.Instance.PlaySound(__instance.CrewStinger, false);
        }
        else
        {
            SoundManager.Instance.PlaySound(__instance.ImpostorStinger, false);
        }
    }

    private static void DisplayEndGamePlayers(EndGameManager __instance, List<NetworkedPlayerInfo> players)
    {
        if (CatchedGameData.Instance.CatchedGameEndReason == EndGameReason.Draw)
        {
            players = [.. CatchedGameData.CatchedPlayerData];
        }

        players = players.OrderBy(data => data.ExtendedData()?.IsLocalData == true ? 0 : 1).ToList();

        int num = Mathf.CeilToInt(7.5f);
        for (int i = 0; i < players.Count; i++)
        {
            NetworkedPlayerInfo cachedPlayerData = players[i];
            int num2 = i % 2 == 0 ? -1 : 1;
            int num3 = (i + 1) / 2;
            float num4 = num3 / num;
            float num5 = Mathf.Lerp(1f, 0.75f, num4);
            float num6 = i == 0 ? -8 : -1;
            PoolablePlayer poolablePlayer = UnityEngine.Object.Instantiate(__instance.PlayerPrefab, __instance.transform);
            poolablePlayer.transform.localPosition = new Vector3(1f * num2 * num3 * num5, FloatRange.SpreadToEdges(-1.125f, 0f, num3, num), num6 + num3 * 0.01f) * 0.9f;
            float num7 = Mathf.Lerp(1f, 0.65f, num4) * 0.9f;
            Vector3 vector = new Vector3(num7, num7, 1f);
            poolablePlayer.transform.localScale = vector;
            if (cachedPlayerData.IsDead)
            {
                poolablePlayer.SetBodyAsGhost();
                poolablePlayer.SetDeadFlipX(i % 2 == 0);
            }
            else
            {
                poolablePlayer.SetFlipX(i % 2 == 0);
            }
            poolablePlayer.UpdateFromPlayerOutfit(cachedPlayerData.DefaultOutfit, PlayerMaterial.MaskType.None, cachedPlayerData.IsDead, true, null, false);

            poolablePlayer.SetName(cachedPlayerData.PlayerName + "\n" + cachedPlayerData.Role()?.RoleNameAndColor.Size(75f) ?? "", vector.Inv(), Color.white, -15f);
            Vector3 namePosition = new Vector3(0f, -1.31f, -0.5f);
            poolablePlayer.SetNamePosition(namePosition);
            if (AprilFoolsMode.ShouldHorseAround() && GameOptionsManager.Instance.CurrentGameOptions.GameMode == AmongUs.GameOptions.GameModes.HideNSeek)
            {
                poolablePlayer.SetBodyType(PlayerBodyTypes.Normal);
                poolablePlayer.SetFlipX(false);
            }
        }
    }

    private static void LogGameEnd(string teamName, List<NetworkedPlayerInfo> players)
    {
        Logger.Log($"Game Has Ended: Team -> {teamName}, Reason: {Enum.GetName(CatchedGameData.Instance.CatchedGameEndReason)}, Players: {string.Join(" - ", players.Select(p => p.PlayerName))}");
    }
}
