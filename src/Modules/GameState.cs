using AmongUs.GameOptions;
using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Network;
using TheBetterRoles.Network.Configs;

namespace TheBetterRoles.Modules;

internal static class GameState
{
    /**********Check Game Status***********/
    internal static bool ShouldCheckWinConditions => !IsFreePlay && !IsExilling && IsInGamePlay && GameManager.Instance.GameHasStarted && !TBRGameSettings.NoGameEnd.GetBool();
    internal static bool IsDev => Main.MyData.IsDev();
    internal static bool InGame => Main.AllPlayerControls.Count > 0;
    internal static bool IsNormalGame => GameOptionsManager.Instance.CurrentGameOptions.GameMode is GameModes.Normal or GameModes.NormalFools;
    internal static bool IsHideNSeek => GameOptionsManager.Instance != null && GameOptionsManager.Instance.CurrentGameOptions.GameMode is GameModes.HideNSeek or GameModes.SeekFools;
    internal static bool SkeldIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Skeld;
    internal static bool MiraHQIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.MiraHQ;
    internal static bool PolusIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Polus;
    internal static bool DleksIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Dleks;
    internal static bool AirshipIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Airship;
    internal static bool FungleIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Fungle;
    internal static bool ModdedMapIsActive => !Enum.IsDefined(ShipStatus.Instance.Type);
    internal static bool CamouflageCommsIsActive => IsSystemActive(SystemTypes.Comms) && TBRGameSettings.CamouflageComms.GetBool();
    internal static byte GetActiveMapId => GameOptionsManager.Instance.CurrentGameOptions.MapId;
    internal static bool IsSystemActive(SystemTypes type)
    {
        if (IsHideNSeek || !ShipStatus.Instance.Systems.TryGetValue(type, out var system))
        {
            return false;
        }

        int mapId = GetActiveMapId;

        if (type == SystemTypes.Electrical)
        {
            return system.TryCast<SwitchSystem>()?.IsActive == true;
        }
        else if (type == SystemTypes.Reactor)
        {
            return system.TryCast<ReactorSystemType>()?.IsActive ?? false;
        }
        else if (type == SystemTypes.Laboratory)
        {
            return system.TryCast<ReactorSystemType>()?.IsActive ?? false;
        }
        else if (type == SystemTypes.LifeSupp)
        {
            return system.TryCast<LifeSuppSystemType>()?.IsActive ?? false;
        }
        else if (type == SystemTypes.HeliSabotage)
        {
            return system.TryCast<HeliSabotageSystem>()?.IsActive ?? false;
        }
        else if (type == SystemTypes.Comms)
        {
            return system.TryCast<HudOverrideSystemType>()?.IsActive
                   ?? system.TryCast<HqHudSystemType>()?.IsActive
                   ?? false;
        }
        else if (type == SystemTypes.MushroomMixupSabotage)
        {
            return system.TryCast<MushroomMixupSabotageSystem>()?.IsActive ?? false;
        }
        else if (type == CustomSystemTypes.Blackout)
        {
            return system.TryCast<BlackoutSabotageSystem>()?.IsActive ?? false;
        }
        else
        {
            return false;
        }
    }
    internal static bool IsCriticalSabotageActive
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
    internal static bool IsNoneCriticalSabotageActive
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
    internal static bool IsAnySabotageActive
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
                SystemTypes.MushroomMixupSabotage,
                CustomSystemTypes.Blackout
            };

            return allSabotages.Any(IsSystemActive);
        }
    }
    internal static bool IsInGame => InGame;
    internal static bool IsLobby => AmongUsClient.Instance?.GameState == InnerNet.InnerNetClient.GameStates.Joined && InGame && !IsFreePlay;
    internal static bool IsTBRLobby => IsHost || AmongUsClient.Instance?.GetHost()?.ExtendedData()?.HasMod == true;
    internal static bool IsInIntro => HudManager.InstanceExists && HudManager.Instance.IsIntroDisplayed;
    internal static bool IsInGamePlay => InGame && IsShip && !IsLobby && !IsInIntro || IsFreePlay;
    internal static bool IsEnded => AmongUsClient.Instance?.GameState == InnerNet.InnerNetClient.GameStates.Ended;
    internal static bool IsNotJoined => AmongUsClient.Instance?.GameState == InnerNet.InnerNetClient.GameStates.NotJoined;
    internal static bool IsOnlineGame => AmongUsClient.Instance?.NetworkMode == NetworkModes.OnlineGame;
    internal static bool IsVanillaServer
    {
        get
        {
            if (!IsOnlineGame) return false;

            string region = ServerManager.Instance.CurrentRegion.Name;
            return region == "North America" || region == "Europe" || region == "Asia";
        }
    }
    internal static bool IsLocalGame => AmongUsClient.Instance?.NetworkMode == NetworkModes.LocalGame;
    internal static bool IsFreePlay => AmongUsClient.Instance?.NetworkMode == NetworkModes.FreePlay;
    internal static bool IsInTask => InGame && MeetingHud.Instance == null;
    internal static bool IsMeeting => InGame && MeetingHud.Instance != null;
    internal static bool IsVoting => IsMeeting && MeetingHud.Instance?.state is MeetingHud.VoteStates.Voted or MeetingHud.VoteStates.NotVoted;
    internal static bool IsProceeding => IsMeeting && MeetingHud.Instance?.state is MeetingHud.VoteStates.Proceeding;
    internal static bool IsExilling => ExileController.Instance != null && !(AirshipIsActive && Minigame.Instance != null && Minigame.Instance.isActiveAndEnabled);
    internal static bool IsCountDown => GameStartManager.InstanceExists && GameStartManager.Instance.startState == GameStartManager.StartingStates.Countdown;
    internal static bool IsShip => ShipStatus.Instance != null;
    internal static bool IsHost => AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;
    internal static bool IsCanMove => PlayerControl.LocalPlayer?.CanMove is true;
    internal static bool IsDead => PlayerControl.LocalPlayer?.Data?.IsDead is true;
}
