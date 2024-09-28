using AmongUs.GameOptions;
using TheBetterRoles.Patches;
using InnerNet;
using TMPro;
using UnityEngine;

namespace TheBetterRoles;

static class ExtendedPlayerControl
{
    // Get players client
    public static ClientData? GetClient(this PlayerControl player)
    {
        try
        {
            var client = AmongUsClient.Instance.allClients.ToArray().FirstOrDefault(cd => cd.Character.PlayerId == player.PlayerId);
            return client;
        }
        catch
        {
            return null;
        }
    }
    // Get players client id
    public static int GetClientId(this PlayerControl player) => player?.GetClient()?.Id != null ? player.GetClient().Id : -1;
    // Get player name with outfit color
    public static string GetPlayerNameAndColor(this PlayerControl player)
    {
        if (player?.Data == null) return string.Empty;

        try
        {
            return $"<color={Utils.Color32ToHex(Palette.PlayerColors[player.Data.DefaultOutfit.ColorId])}>{player.Data.PlayerName}</color>";
        }
        catch
        {
            return player.Data.PlayerName;
        }
    }
    public static string GetRoleNameAndColor(this PlayerControl player) => $"<color={player.GetRoleColorHex()}>{player.GetRoleName()}</color>";
    public static string GetRoleName(this PlayerControl player) => Utils.GetCustomRoleName(player.BetterData().RoleInfo.RoleType);
    public static Color GetRoleColor(this PlayerControl player) => Utils.GetCustomRoleColor(player.BetterData().RoleInfo.RoleType);
    public static string GetRoleColorHex(this PlayerControl player) => Utils.GetCustomRoleColorHex(player.BetterData().RoleInfo.RoleType);
    public static string GetRoleInfo(this PlayerControl player, bool longInfo = false) => Utils.GetCustomRoleInfo(player.BetterData().RoleInfo.RoleType, longInfo);
    public static string GetRoleTeamName(this PlayerControl player) => Utils.GetCustomRoleTeamName(player.BetterData().RoleInfo.Role.RoleTeam);

    // Set players over head text
    public static void SetPlayerTextInfo(this PlayerControl player, string text, bool isBottom = false, bool isInfo = false)
    {
        if (player == null) return;

        string infoType = isBottom ? "InfoText_B_TMP" : "InfoText_T_TMP";

        if (isInfo)
        {
            infoType = "InfoText_Info_TMP";
            var topText = player.gameObject.transform.Find("Names/NameText_TMP/InfoText_T_TMP")?.GetComponent<TextMeshPro>();

            if (topText != null && string.IsNullOrEmpty(Utils.RemoveHtmlText(topText.text)))
            {
                text = "<voffset=-2.25em>" + text + "</voffset>";
            }
        }

        text = "<size=65%>" + text + "</size>";
        var textObj = player.gameObject.transform.Find($"Names/NameText_TMP/{infoType}")?.GetComponent<TextMeshPro>();

        if (textObj != null)
        {
            textObj.text = text;
        }
    }
    // Reset players over head text
    public static void ResetAllPlayerTextInfo(this PlayerControl player)
    {
        if (player == null) return;
        player.SetPlayerTextInfo("", isInfo: true);
        player.SetPlayerTextInfo("");
        player.SetPlayerTextInfo("", isBottom: true);
    }
    // Check if players character has been created and received from the Host
    public static bool DataIsCollected(this PlayerControl player)
    {
        if (player == null) return false;

        if (player.isDummy || GameStates.IsLocalGame || !GameStates.IsVanillaServer)
        {
            return true;
        }

        if (player.gameObject.transform.Find("Names/NameText_TMP").GetComponent<TextMeshPro>().text
        is "???" or "Player" or "<color=#b5b5b5>Loading</color>" or null
        || player.Data == null
        || string.IsNullOrEmpty(player.Data.Puid)
        || player.Data.PlayerLevel == uint.MaxValue
        || player.CurrentOutfit == null
        || player.CurrentOutfit.ColorId == -1)
        {
            return false;
        }
        return true;
    }
    // Kick player
    public static void Kick(this PlayerControl player, bool ban = false, string setReasonInfo = "", bool AntiCheatBan = false, bool bypassDataCheck = false)
    {
        var Ban = ban;

        if (!GameStates.IsHost || player.IsLocalPlayer() || !player.DataIsCollected() && !bypassDataCheck || player.IsHost() || player.isDummy)
        {
            return;
        }

        if (AntiCheatBan)
        {
            if (BetterGameSettings.WhenCheating.GetValue() == 0)
            {
                return;
            }

            Ban = Ban && BetterGameSettings.WhenCheating.GetValue() == 2;
        }

        if (setReasonInfo != "")
        {
            GameDataShowNotificationPatch.BetterShowNotification(player.Data, forceReasonText: string.Format(setReasonInfo, Ban ? Translator.GetString("AntiCheat.Ban").ToLower() : Translator.GetString("AntiCheat.Kick").ToLower()));
        }

        AmongUsClient.Instance.KickPlayer(player.GetClientId(), Ban);
    }
    // Set color outline on player
    public static void SetOutline(this PlayerControl player, bool active, Color? color = null)
    {
        player.cosmetics.currentBodySprite.BodySprite.material.SetFloat("_Outline", (float)(active ? 1 : 0));
        SpriteRenderer[] longModeParts = player.cosmetics.currentBodySprite.LongModeParts;
        for (int i = 0; i < longModeParts.Length; i++)
        {
            longModeParts[i].material.SetFloat("_Outline", (float)(active ? 1 : 0));
        }
        if (color != null)
        {
            player.cosmetics.currentBodySprite.BodySprite.material.SetColor("_OutlineColor", color.Value);
            longModeParts = player.cosmetics.currentBodySprite.LongModeParts;
            for (int i = 0; i < longModeParts.Length; i++)
            {
                longModeParts[i].material.SetColor("_OutlineColor", color.Value);
            }
        }
    }
    // Set color outline on player
    public static void SetOutlineByHex(this PlayerControl player, bool active, string hexColor = "")
    {
        Color color = Utils.HexToColor32(hexColor);
        player.cosmetics.currentBodySprite.BodySprite.material.SetFloat("_Outline", (float)(active ? 1 : 0));
        SpriteRenderer[] longModeParts = player.cosmetics.currentBodySprite.LongModeParts;
        for (int i = 0; i < longModeParts.Length; i++)
        {
            longModeParts[i].material.SetFloat("_Outline", (float)(active ? 1 : 0));
        }
        if (color != null)
        {
            player.cosmetics.currentBodySprite.BodySprite.material.SetColor("_OutlineColor", color);
            longModeParts = player.cosmetics.currentBodySprite.LongModeParts;
            for (int i = 0; i < longModeParts.Length; i++)
            {
                longModeParts[i].material.SetColor("_OutlineColor", color);
            }
        }
    }
    // Check if player is selecting room to spawn in, for Airship
    public static bool IsInRoomSelect(this PlayerControl player)
    {
        if (player == null) return false;
        return GameStates.AirshipIsActive && Vector2.Distance(player.GetTruePosition(), new(-25, 40)) < 5f;
    }
    // Check if player controller is self client
    public static bool IsLocalPlayer(this PlayerControl player) => player != null && PlayerControl.LocalPlayer != null && player == PlayerControl.LocalPlayer;
    public static bool IsOnLadder(this PlayerControl player) => player.onLadder || player.MyPhysics.Animations.IsPlayingAnyLadderAnimation();

    // Get vent Id that the player is in.
    public static int GetPlayerVentId(this PlayerControl player)
    {
        if (player == null) return -1;

        if (!(ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Ventilation, out var systemType) &&
              systemType.TryCast<VentilationSystem>() is VentilationSystem ventilationSystem))
            return 0;

        return ventilationSystem.PlayersInsideVents.TryGetValue(player.PlayerId, out var playerIdVentId) ? playerIdVentId : -1;
    }
    // Get true position
    public static Vector2 GetCustomPosition(this PlayerControl player) => new(player.transform.position.x, player.transform.position.y);
    // Check if player is a dev
    public static bool IsDev(this PlayerControl player) => player != null && Main.DevUser.Contains($"{Utils.GetHashPuid(player)}+{player.Data.FriendCode}");
    // Check if player is alive
    public static bool IsAlive(this PlayerControl player) => player?.Data != null && !player.Data.IsDead;
    // Check if player is in a vent
    public static bool IsInVent(this PlayerControl player) => player != null && (player.inVent || player.walkingToVent || player.MyPhysics?.Animations?.IsPlayingEnterVentAnimation() == true);
    // Check if player is Shapeshifting
    public static bool IsInShapeshift(this PlayerControl player) => player != null && (player.shapeshiftTargetPlayerId > -1 || player.shapeshifting);
    // Check if player is in vanish as Phantom
    public static bool IsInVanish(this PlayerControl player)
    {
        if (player != null && player.Data.Role is PhantomRole phantomRole)
        {
            return phantomRole.fading;
        }
        return false;
    }
    // Check if player can vent
    public static bool CanVent(this PlayerControl player) => player?.BetterData()?.RoleInfo?.Role?.CanVent == true;
    // Check if player can move in vents
    public static bool CanMoveInVent(this PlayerControl player) => player?.BetterData()?.RoleInfo?.Role?.CanMoveInVent == true;
    // Check if player can kill
    public static bool CanKill(this PlayerControl player) => player?.BetterData()?.RoleInfo?.Role?.CanKill == true;
    // Check if player can sabotage
    public static bool CanSabotage(this PlayerControl player) => player?.BetterData()?.RoleInfo?.Role?.CanSabotage == true;
    public static bool RoleAssigned(this PlayerControl player) => player?.BetterData()?.RoleInfo?.RoleAssigned == true;
    // Get hex color for team
    public static string GetTeamHexColor(this PlayerControl player) => Utils.GetCustomRoleTeamColor(player.BetterData().RoleInfo.Role.RoleTeam);
    public static Color GetTeamColor(this PlayerControl player) => Utils.HexToColor32(Utils.GetCustomRoleTeamColor(player.BetterData().RoleInfo.Role.RoleTeam));

    // Check if player is role type
    public static bool Is(this PlayerControl player, RoleTypes role) => player?.Data?.RoleType == role;
    public static bool Is(this PlayerControl player, CustomRoles role) => player?.BetterData()?.RoleInfo?.RoleType == role;
    public static bool Is(this PlayerControl player, CustomRoleTeam roleTeam) => player?.BetterData()?.RoleInfo?.Role?.RoleTeam == roleTeam;
    public static bool Is(this PlayerControl player, CustomRoleCategory roleCategory) => player?.BetterData()?.RoleInfo?.Role?.RoleCategory == roleCategory;
    // Check if player is Ghost role type
    public static bool IsGhostRole(this PlayerControl player) => player?.BetterData()?.RoleInfo?.Role.Role?.RoleCategory == CustomRoleCategory.Ghost;
    // Check if player is a imposter teammate
    public static bool IsImpostorTeammate(this PlayerControl player) =>
        player != null && PlayerControl.LocalPlayer != null &&
        ((player.IsLocalPlayer() && PlayerControl.LocalPlayer.Is(CustomRoleTeam.Impostor)) ||
        (PlayerControl.LocalPlayer.Is(CustomRoleTeam.Impostor) && player.Is(CustomRoleTeam.Impostor)));

    // Check if player is same team
    public static bool IsTeammate(this PlayerControl player) =>
        player != null && PlayerControl.LocalPlayer != null &&
        (player.IsLocalPlayer() || player.Is(PlayerControl.LocalPlayer.BetterData().RoleInfo.Role.RoleTeam) && !PlayerControl.LocalPlayer.Is(CustomRoleTeam.Neutral));
    // Check if player is the host
    public static bool IsHost(this PlayerControl player) => player?.Data != null && GameData.Instance?.GetHost() == player.Data;

    // Report player
    public static void ReportPlayer(this PlayerControl player, ReportReasons reason = ReportReasons.None)
    {
        if (player?.GetClient() != null)
        {
            if (!player.GetClient().HasBeenReported)
            {
                AmongUsClient.Instance.ReportPlayer(player.GetClientId(), reason);
            }
        }
    }
}
