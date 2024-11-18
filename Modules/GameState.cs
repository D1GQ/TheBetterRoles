using AmongUs.GameOptions;
using TheBetterRoles.Helpers;
using TheBetterRoles.Patches;

namespace TheBetterRoles.Modules;

public static class GameState
{
    /**********Check Game Status***********/
    public static bool IsDev => Main.MyData.HasPermission(Items.MultiPermissionFlags.Dev);
    public static bool InGame => Main.AllPlayerControls.Any();
    public static bool IsNormalGame => GameOptionsManager.Instance.CurrentGameOptions.GameMode is GameModes.Normal or GameModes.NormalFools;
    public static bool IsHideNSeek => GameOptionsManager.Instance != null && GameOptionsManager.Instance.CurrentGameOptions.GameMode is GameModes.HideNSeek or GameModes.SeekFools;
    public static bool SkeldIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Skeld;
    public static bool MiraHQIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Mira;
    public static bool PolusIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Polus;
    public static bool DleksIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Dleks;
    public static bool AirshipIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Airship;
    public static bool FungleIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Fungle;
    public static bool SubmergedIsActive => SubmergedCompatibility.isSubmerged();
    public static bool CamouflageCommsIsActive => IsSystemActive(SystemTypes.Comms) && BetterGameSettings.CamouflageComms.GetBool();
    public static byte GetActiveMapId => GameOptionsManager.Instance.CurrentGameOptions.MapId;
    public static bool IsSystemActive(SystemTypes type)
    {
        if (IsHideNSeek || !ShipStatus.Instance.Systems.TryGetValue(type, out var system))
        {
            return false;
        }

        int mapId = GetActiveMapId;

        return type switch
        {
            SystemTypes.Electrical when mapId != 5 => !system.Cast<SwitchSystem>()?.IsActive == false,
            SystemTypes.Reactor when mapId != 2 => system.Cast<ReactorSystemType>()?.IsActive ?? false,
            SystemTypes.Laboratory when mapId == 2 => system.Cast<ReactorSystemType>()?.IsActive ?? false,
            SystemTypes.LifeSupp when mapId is 0 or 3 => system.Cast<LifeSuppSystemType>()?.IsActive ?? false,
            SystemTypes.HeliSabotage when mapId == 4 => system.Cast<HeliSabotageSystem>()?.IsActive ?? false,
            SystemTypes.Comms => system.TryCast<HudOverrideSystemType>()?.IsActive ?? system.TryCast<HqHudSystemType>()?.IsActive ?? false,
            SystemTypes.MushroomMixupSabotage when mapId == 5 || IsFreePlay => system.Cast<MushroomMixupSabotageSystem>()?.IsActive ?? false,
            _ => false
        };
    }
    public static bool IsCriticalSabotageActive
    {
        get
        {
            var deathSabotages = new[]
            {
            SystemTypes.Reactor,
            SystemTypes.Laboratory,
            SystemTypes.LifeSupp,
            SystemTypes.HeliSabotage,
            };

            return deathSabotages.Any(IsSystemActive);
        }
    }
    public static bool IsNoneCriticalSabotageActive
    {
        get
        {
            var noneDeathSabotages = new[]
            {
            SystemTypes.Electrical,
            SystemTypes.Comms,
            SystemTypes.MushroomMixupSabotage
            };

            return noneDeathSabotages.Any(IsSystemActive);
        }
    }
    public static bool IsAnySabotageActive
    {
        get
        {
            var allSabotages = new[]
            {
            SystemTypes.Electrical,
            SystemTypes.Reactor,
            SystemTypes.Laboratory,
            SystemTypes.LifeSupp,
            SystemTypes.HeliSabotage,
            SystemTypes.Comms,
            SystemTypes.MushroomMixupSabotage
            };

            return allSabotages.Any(IsSystemActive);
        }
    }
    public static bool IsInGame => InGame;
    public static bool IsLobby => AmongUsClient.Instance?.GameState == InnerNet.InnerNetClient.GameStates.Joined && InGame && !IsFreePlay;
    public static bool IsTBRLobby => IsHost || AmongUsClient.Instance?.GetHost()?.BetterData()?.HasMod == true;
    public static bool IsInIntro => IntroCutscene.Instance != null;
    public static bool IsInGamePlay => InGame && IsShip && !IsLobby && !IsInIntro || IsFreePlay;
    public static bool IsEnded => AmongUsClient.Instance?.GameState == InnerNet.InnerNetClient.GameStates.Ended;
    public static bool IsNotJoined => AmongUsClient.Instance?.GameState == InnerNet.InnerNetClient.GameStates.NotJoined;
    public static bool IsOnlineGame => AmongUsClient.Instance?.NetworkMode == NetworkModes.OnlineGame;
    public static bool IsVanillaServer
    {
        get
        {
            if (!IsOnlineGame) return false;

            string region = ServerManager.Instance.CurrentRegion.Name;
            return region == "North America" || region == "Europe" || region == "Asia";
        }
    }
    public static bool IsLocalGame => AmongUsClient.Instance?.NetworkMode == NetworkModes.LocalGame;
    public static bool IsFreePlay => AmongUsClient.Instance?.NetworkMode == NetworkModes.FreePlay;
    public static bool IsInTask => InGame && MeetingHud.Instance == null;
    public static bool IsMeeting => InGame && MeetingHud.Instance != null;
    public static bool IsVoting => IsMeeting && MeetingHud.Instance?.state is MeetingHud.VoteStates.Voted or MeetingHud.VoteStates.NotVoted;
    public static bool IsProceeding => IsMeeting && MeetingHud.Instance?.state is MeetingHud.VoteStates.Proceeding;
    public static bool IsExilling => ExileController.Instance != null && !(AirshipIsActive && Minigame.Instance != null && Minigame.Instance.isActiveAndEnabled);
    public static bool IsCountDown => GameStartManager.InstanceExists && GameStartManager.Instance.startState == GameStartManager.StartingStates.Countdown;
    public static bool IsShip => ShipStatus.Instance != null;
    public static bool IsHost => AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;
    public static bool IsCanMove => PlayerControl.LocalPlayer?.CanMove is true;
    public static bool IsDead => PlayerControl.LocalPlayer?.Data?.IsDead is true;
}
