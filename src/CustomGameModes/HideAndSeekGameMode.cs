using AmongUs.GameOptions;
using InnerNet;
using PowerTools;
using System.Collections;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.Interfaces;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using TheBetterRoles.Network.RPCs;
using UnityEngine;

namespace TheBetterRoles.CustomGameModes;

internal class HideAndSeekGameMode : NetworkClass, IGameMode
{
    public CustomGameMode gameMode => throw new NotImplementedException();

    internal HideAndSeekGameMode()
    {
        SetUpNetworkClass();
    }

    public void CheckAllWinConditions(bool initial = false)
    {
    }

    public bool CheckSabotageWin() => false;

    public IEnumerator CoAssignRoles()
    {
        foreach (var player in Main.AllPlayerControls)
        {
            player.SendRpcSetCustomRole(RoleClassTypes.Impostor);
        }
        RPC.SendRpcPlayIntro();
        yield break;
    }

    public IEnumerator CoPlayIntro(IntroCutscene introCutscene)
    {
        introCutscene.HideAndSeekPanels.SetActive(true);
        if (PlayerControl.LocalPlayer.Role().IsImpostor)
        {
            introCutscene.CrewmateRules.SetActive(false);
            introCutscene.ImpostorRules.SetActive(true);
        }
        else
        {
            introCutscene.CrewmateRules.SetActive(true);
            introCutscene.ImpostorRules.SetActive(false);
        }

        List<PlayerControl> seekers = Main.AllPlayerControls.Where(pc => pc.Role().IsImpostor).ToList();

        yield return CoSetUpInformation(introCutscene, seekers);

        if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
        {
            yield return CoHandleHider(introCutscene);
        }
        else
        {
            HandleSeekerHider(introCutscene, seekers);
        }

        ShipStatus.Instance.StartSFX();
        UnityEngine.Object.Destroy(introCutscene.gameObject);
    }

    private IEnumerator CoSetUpInformation(IntroCutscene introCutscene, List<PlayerControl> seekers)
    {
        for (int i = 0; i < seekers.Count; i++)
        {
            PlayerControl? seeker = seekers[i];
            GameManager.Instance.SetSpecialCosmetics(seeker);
            introCutscene.ImpostorName.gameObject.SetActive(true);
            introCutscene.ImpostorTitle.gameObject.SetActive(true);
            introCutscene.BackgroundBar.enabled = false;
            introCutscene.TeamTitle.gameObject.SetActive(false);
            if (seeker != null)
            {
                introCutscene.ImpostorName.text = seeker.Data.PlayerName;
            }
            else
            {
                introCutscene.ImpostorName.text = "???";
            }
            yield return new WaitForSecondsRealtime(0.1f);
            PoolablePlayer playerSlot = null;
            if (seeker != null)
            {
                playerSlot = introCutscene.CreatePlayer(1, 1, seeker.Data, false);
                playerSlot.SetBodyType(PlayerBodyTypes.Normal);
                playerSlot.SetFlipX(false);
                playerSlot.transform.localPosition = introCutscene.impostorPos + new Vector3(1f * i, 0f, 0f);
                playerSlot.transform.localScale = Vector3.one * introCutscene.impostorScale;
            }
            yield return ShipStatus.Instance.CosmeticsCache.PopulateFromPlayers();
            yield return new WaitForSecondsRealtime(6f);
            if (playerSlot != null)
            {
                playerSlot.gameObject.SetActive(false);
            }
            introCutscene.HideAndSeekPanels.SetActive(false);
            introCutscene.CrewmateRules.SetActive(false);
            introCutscene.ImpostorRules.SetActive(false);
            LogicHnSMusic logicHnSMusic = GameManager.Instance.GetLogicComponent<LogicHnSMusic>() as LogicHnSMusic;
            if (logicHnSMusic != null)
            {
                logicHnSMusic.StartMusicWithIntro();
            }
        }
    }

    private IEnumerator CoHandleHider(IntroCutscene introCutscene)
    {
        float crewmateLeadTime = 10;
        introCutscene.HideAndSeekTimerText.gameObject.SetActive(true);
        PoolablePlayer poolablePlayer;
        AnimationClip anim;
        if (AprilFoolsMode.ShouldHorseAround())
        {
            poolablePlayer = introCutscene.HorseWrangleVisualSuit;
            poolablePlayer.gameObject.SetActive(true);
            poolablePlayer.SetBodyType(PlayerBodyTypes.Seeker);
            anim = introCutscene.HnSSeekerSpawnHorseAnim;
            introCutscene.HorseWrangleVisualPlayer.SetBodyType(PlayerBodyTypes.Normal);
            introCutscene.HorseWrangleVisualPlayer.UpdateFromPlayerData(PlayerControl.LocalPlayer.Data, PlayerControl.LocalPlayer.CurrentOutfitType, PlayerMaterial.MaskType.None, false, null, false);
        }
        else if (AprilFoolsMode.ShouldLongAround())
        {
            poolablePlayer = introCutscene.HideAndSeekPlayerVisual;
            poolablePlayer.gameObject.SetActive(true);
            poolablePlayer.SetBodyType(PlayerBodyTypes.LongSeeker);
            anim = introCutscene.HnSSeekerSpawnLongAnim;
        }
        else
        {
            poolablePlayer = introCutscene.HideAndSeekPlayerVisual;
            poolablePlayer.gameObject.SetActive(true);
            poolablePlayer.SetBodyType(PlayerBodyTypes.Seeker);
            anim = introCutscene.HnSSeekerSpawnAnim;
        }
        poolablePlayer.SetBodyCosmeticsVisible(false);
        poolablePlayer.UpdateFromPlayerData(PlayerControl.LocalPlayer.Data, PlayerControl.LocalPlayer.CurrentOutfitType, PlayerMaterial.MaskType.None, false, null, false);
        SpriteAnim component = poolablePlayer.GetComponent<SpriteAnim>();
        poolablePlayer.gameObject.SetActive(true);
        poolablePlayer.ToggleName(false);
        component.Play(anim, 1f);
        while (crewmateLeadTime > 0f)
        {
            introCutscene.HideAndSeekTimerText.text = Mathf.RoundToInt(crewmateLeadTime).ToString();
            crewmateLeadTime -= Time.deltaTime;
            yield return null;
        }
    }

    private void HandleSeekerHider(IntroCutscene introCutscene, List<PlayerControl> seekers)
    {
        ShipStatus.Instance.HideCountdown = 10;

        foreach (var seeker in seekers)
        {
            if (seeker == null) continue;
            seeker.MyPhysics.SetBodyType(PlayerBodyTypes.Seeker);
            seeker.AnimateCustom(introCutscene.HnSSeekerSpawnAnim);
            seeker.cosmetics.SetBodyCosmeticsVisible(false);
        }
    }

    public void FixedUpdate()
    {

    }

    public void OnDisconnect(PlayerControl player)
    {
        throw new NotImplementedException();
    }

    public void OnGameEnd()
    {
        throw new NotImplementedException();
    }

    public void OnGameStart()
    {
        var logicOptions = GameManager.Instance.LogicOptions;
        GameManager.DestroyInstance();
        if (GameState.IsHost)
        {
            var gameManager = GameManagerCreator.CreateGameManager(GameModes.HideNSeek);
            AmongUsClient.Instance.Spawn(gameManager, -2, SpawnFlags.None);
            gameManager.LogicOptions = logicOptions;
        }
        // ShipStatus.Instance.BreakEmergencyButton();
    }

    public void OnPlayerDeath(PlayerControl player)
    {
        throw new NotImplementedException();
    }

    public void OnPlayerRevive(PlayerControl player)
    {
        throw new NotImplementedException();
    }

    public bool ReEnableGameplay()
    {
        throw new NotImplementedException();
    }

    public void SetUpOutro(EndGameManager endGameManager)
    {
        throw new NotImplementedException();
    }
}
