using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils;
using InnerNet;
using System.Collections;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Roles;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Helpers;

static class PlayerControlHelper
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
    public static CustomRoleBehavior? Role(this PlayerControl player) => player?.BetterData()?.RoleInfo?.Role;
    public static string GetRoleNameAndColor(this PlayerControl player) => $"<color={player.GetRoleColorHex()}>{player.GetRoleName()}</color>";
    public static string GetRoleName(this PlayerControl player) => Utils.GetCustomRoleName(player.BetterData().RoleInfo.RoleType);
    public static Color GetRoleColor(this PlayerControl player) => Utils.GetCustomRoleColor(player.BetterData().RoleInfo.RoleType);
    public static string GetRoleColorHex(this PlayerControl player) => Utils.GetCustomRoleColorHex(player.BetterData().RoleInfo.RoleType);
    public static string GetRoleInfo(this PlayerControl player, bool longInfo = false) => Utils.GetCustomRoleInfo(player.BetterData().RoleInfo.RoleType, longInfo);
    public static string GetRoleTeamName(this PlayerControl player) => Utils.GetCustomRoleTeamName(player.BetterData().RoleInfo.Role.RoleTeam);

    public static void CustomRevive(this PlayerControl player, bool SetData = true)
    {
        if (SetData) player.Data.IsDead = false;
        player.gameObject.layer = LayerMask.NameToLayer("Players");
        player.MyPhysics.ResetMoveState(true);
        player.clickKillCollider.enabled = true;
        player.cosmetics.SetPetSource(player);
        player.cosmetics.SetNameMask(true);
        if (player.IsLocalPlayer())
        {
            DestroyableSingleton<HudManager>.Instance.ShadowQuad.gameObject.SetActive(true);
            DestroyableSingleton<HudManager>.Instance.AbilityButton.ToggleVisible(false);
            // DestroyableSingleton<HudManager>.Instance.KillButton.ToggleVisible(player.Data.Role.IsImpostor);
            // DestroyableSingleton<HudManager>.Instance.AdminButton.ToggleVisible(player.Data.Role.IsImpostor);
            // DestroyableSingleton<HudManager>.Instance.SabotageButton.ToggleVisible(player.Data.Role.IsImpostor);
            // DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(player.Data.Role.IsImpostor);
            DestroyableSingleton<HudManager>.Instance.Chat.ForceClosed();
            DestroyableSingleton<HudManager>.Instance.Chat.SetVisible(false);
        }
    }

    /// <summary>
    /// Only call this directly if it's a 100% guarantee kill without any checks and is synced to all clients!
    /// </summary>
    /// <param name="killer">Set the killer.</param>
    /// <param name="target">Set the victim.</param>
    /// <param name="snapToTarget">Set if the killer should snap to the victim.</param>
    /// <param name="spawnBody">Set if a body should Spawn from the murder.</param>
    /// <param name="showAnimation">Set if the kill animation should play for the victim.</param>
    /// <param name="playSound">Set if the kill sound should play for the Killer.</param>
    public static void CustomMurderPlayer(this PlayerControl killer, PlayerControl target, bool snapToTarget = true, bool spawnBody = true, bool showAnimation = true, bool playSound = true)
    {
        if (killer.IsLocalPlayer())
        {
            if (GameManager.Instance.IsHideAndSeek())
            {
                StatsManager.Instance.IncrementStat(StringNames.StatsImpostorKills_HideAndSeek);
            }
            else
            {
                StatsManager.Instance.IncrementStat(StringNames.StatsImpostorKills);
            }
            if (killer.CurrentOutfitType == PlayerOutfitType.Shapeshifted)
            {
                StatsManager.Instance.IncrementStat(StringNames.StatsShapeshifterShiftedKills);
            }
            if (Constants.ShouldPlaySfx() && playSound)
            {
                SoundManager.Instance.PlaySound(killer.KillSfx, false, 0.8f, null);
            }
        }
        if (target.IsLocalPlayer())
        {
            StatsManager.Instance.IncrementStat(StringNames.StatsTimesMurdered);
            if (Minigame.Instance)
            {
                try
                {
                    Minigame.Instance.Close();
                    Minigame.Instance.Close();
                }
                catch
                {
                }
            }

            if (showAnimation) DestroyableSingleton<HudManager>.Instance.KillOverlay.ShowKillAnimation(killer.Data, target.Data);
            target.cosmetics.SetNameMask(false);
            target.RpcSetScanner(false);
        }

        KillAnimation[] killAnimationsArray = killer.KillAnimations.ToArray();
        KillAnimation randomKillAnimation = killAnimationsArray[UnityEngine.Random.Range(0, killAnimationsArray.Length)];
        killer.MyPhysics.StartCoroutine(CoNewMurder(killer, target, randomKillAnimation, snapToTarget, spawnBody, showAnimation));
    }
    private static IEnumerator CoNewMurder(PlayerControl killer, PlayerControl target, KillAnimation killAnimation, bool snapToTarget, bool spawnBody, bool showAnimation)
    {
        FollowerCamera cam = Camera.main.GetComponent<FollowerCamera>();
        bool isParticipant = PlayerControl.LocalPlayer == killer || PlayerControl.LocalPlayer == target;
        PlayerPhysics sourcePhys = killer.MyPhysics;
        if (showAnimation)
        {
            if (snapToTarget) KillAnimation.SetMovement(killer, false);
            KillAnimation.SetMovement(target, false);
        }
        if (isParticipant)
        {
            PlayerControl.LocalPlayer.isKilling = true;
            killer.isKilling = true;
        }
        DeadBody? deadBody = null;
        if (spawnBody)
        {
            deadBody = UnityEngine.Object.Instantiate(GameManager.Instance.DeadBodyPrefab);
            deadBody.enabled = false;
            deadBody.ParentId = target.PlayerId;
            deadBody.bodyRenderers.ToList().ForEach(target.SetPlayerMaterialColors);
            target.SetPlayerMaterialColors(deadBody.bloodSplatter);
            Vector3 vector = target.transform.position + killAnimation.BodyOffset;
            vector.z = vector.y / 1000f;
            deadBody.transform.position = vector;
        }
        if (isParticipant)
        {
            cam.Locked = true;
            ConsoleJoystick.SetMode_Task();
            if (PlayerControl.LocalPlayer.AmOwner)
            {
                PlayerControl.LocalPlayer.MyPhysics.inputHandler.enabled = true;
            }
        }
        target.Die(DeathReason.Kill, true);

        if (snapToTarget)
        {
            yield return killer.MyPhysics.Animations.CoPlayCustomAnimation(killAnimation.BlurAnim);
            killer.NetTransform.SnapTo(target.transform.position);
            sourcePhys.Animations.PlayIdleAnimation();
        }

        if (showAnimation)
        {
            if (snapToTarget) KillAnimation.SetMovement(killer, true);
            KillAnimation.SetMovement(target, true);
        }
        if (deadBody != null) deadBody.enabled = true;
        if (isParticipant)
        {
            cam.Locked = false;
            PlayerControl.LocalPlayer.isKilling = false;
            killer.isKilling = false;
        }
        yield break;
    }

    public static void ShieldBreakAnimation(this PlayerControl player, Color? color = null)
    {
        RoleEffectAnimation roleEffectAnimation = UnityEngine.Object.Instantiate(DestroyableSingleton<RoleManager>.Instance.protectAnim, player.gameObject.transform);
        roleEffectAnimation.SetMaskLayerBasedOnWhoShouldSee(true);
        if (color != null)
        {
            roleEffectAnimation.Renderer.color = (Color)color;
        }
        roleEffectAnimation.Play(player, null, player.cosmetics.FlipX, RoleEffectAnimation.SoundType.Global, 0f, true, 0f);
    }

    public static void ShieldBreakAnimation(this PlayerControl player, string color)
    {
        player.ShieldBreakAnimation(Utils.HexToColor32(color));
    }

    public static bool CanBeTeleported(this PlayerControl player)
    {
        if (player.Data == null
            || GameState.IsMeeting
            || !player.IsAlive()
            || player.inVent
            || player.walkingToVent
            || player.inMovingPlat // Moving Platform on Airhip and Zipline on Fungle
            || player.MyPhysics.Animations.IsPlayingEnterVentAnimation()
            || player.onLadder || player.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
        {
            return false;
        }
        return true;
    }

    // Set players over head text
    public static void SetPlayerTextInfo(this PlayerControl player, string text, bool isBottom = false, bool isInfo = false)
    {
        if (player == null) return;

        string infoType = isBottom ? "InfoText_B_TMP" : "InfoText_T_TMP";

        if (isInfo)
        {
            infoType = "InfoText_Info_TMP";
            var topText = player.cosmetics.nameText.transform.Find("InfoText_T_TMP")?.GetComponent<TextMeshPro>();

            if (topText != null && string.IsNullOrEmpty(Utils.RemoveHtmlText(topText.text)))
            {
                text = "<voffset=-2.25em>" + text + "</voffset>";
            }
        }

        text = "<size=65%>" + text + "</size>";
        var textObj = player.cosmetics.nameText.transform.Find($"{infoType}")?.GetComponent<TextMeshPro>();

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

        if (player.isDummy || GameState.IsLocalGame || !GameState.IsVanillaServer)
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
    public static void Kick(this PlayerControl player, bool ban = false)
    {
        AmongUsClient.Instance.KickPlayer(player.GetClientId(), ban);
    }
    // Set color outline on player
    public static void SetOutline(this PlayerControl player, bool active, Color? color = null)
    {
        player.cosmetics.currentBodySprite.BodySprite.material.SetFloat("_Outline", active ? 1 : 0);
        SpriteRenderer[] longModeParts = player.cosmetics.currentBodySprite.LongModeParts;
        for (int i = 0; i < longModeParts.Length; i++)
        {
            longModeParts[i].material.SetFloat("_Outline", active ? 1 : 0);
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
        Color? color = Utils.HexToColor32(hexColor);
        player.cosmetics.currentBodySprite.BodySprite.material.SetFloat("_Outline", active ? 1 : 0);
        SpriteRenderer[] longModeParts = player.cosmetics.currentBodySprite.LongModeParts;
        for (int i = 0; i < longModeParts.Length; i++)
        {
            longModeParts[i].material.SetFloat("_Outline", active ? 1 : 0);
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

    public static void SetTrueVisorColor(this PlayerControl player, Color color)
    {
        player?.cosmetics?.bodySprites[0]?.BodySprite?.material?.SetColor(PlayerMaterial.VisorColor, color);
        var sprite = player.cosmetics.visor.GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            sprite.color = color;
        }
    }

    public static void RawSetRole(this PlayerControl player, RoleTypes role) => DestroyableSingleton<RoleManager>.Instance.SetRole(player, role);
    // Check if player is selecting room to spawn in, for Airship
    public static bool IsInRoomSelect(this PlayerControl player)
    {
        if (player == null) return false;
        return GameState.AirshipIsActive && Vector2.Distance(player.GetTruePosition(), new(-25, 40)) < 5f;
    }
    // Check if player controller is self client
    public static bool IsLocalPlayer(this PlayerControl player) => player != null && PlayerControl.LocalPlayer != null && player == PlayerControl.LocalPlayer;
    public static bool IsLocalData(this NetworkedPlayerInfo data) => data != null && PlayerControl.LocalPlayer != null && data.PlayerId == PlayerControl.LocalPlayer.PlayerId;
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
    public static bool IsDev(this PlayerControl player) => player != null && Main.DevUser.Contains($"{Utils.GetHashPuid(player)}+{Utils.GetHashPuid(player.Data.FriendCode)}");
    // Check if player is alive
    public static bool IsAlive(this PlayerControl player, bool CheckFakeAlive = false) => player?.Data?.IsDead == false || CheckFakeAlive && player.BetterData()?.IsFakeAlive == true;
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
    public static bool CanVent(this PlayerControl player) => player.RoleChecksAny(role => role.CanKill, false) == true;
    // Check if player can kill
    public static bool CanKill(this PlayerControl player) => player.RoleChecksAny(role => role.CanKill, false) == true;
    // Check if player can sabotage
    public static bool CanSabotage(this PlayerControl player) => player.RoleChecksAny(role => role.CanSabotage, false) == true;
    public static bool RoleAssigned(this PlayerControl player)
    {
        if (player == null) return false;

        var betterData = player.BetterData();
        if (betterData == null || betterData.RoleInfo == null)
            return false;

        return betterData.RoleInfo.RoleAssigned;
    }

    // Get hex color for team
    public static string GetTeamHexColor(this PlayerControl player) => Utils.GetCustomRoleTeamColor(player.BetterData().RoleInfo.Role.RoleTeam);
    public static Color GetTeamColor(this PlayerControl player) => Utils.HexToColor32(Utils.GetCustomRoleTeamColor(player.BetterData().RoleInfo.Role.RoleTeam));

    // Check if player is role type
    public static bool Is(this PlayerControl player, RoleTypes role) => player?.Data?.RoleType == role;
    public static bool Is(this PlayerControl player, CustomRoles role) => player?.BetterData()?.RoleInfo?.RoleType == role;
    public static bool Is(this PlayerControl player, CustomRoleTeam roleTeam) => player?.Role()?.RoleTeam == roleTeam;
    public static bool Is(this PlayerControl player, CustomRoleCategory roleCategory) => player?.BetterData()?.RoleInfo?.Role?.RoleCategory == roleCategory;
    // Check if player is Ghost role type
    public static bool IsGhostRole(this PlayerControl player) => player?.BetterData()?.RoleInfo?.Role.Role?.RoleCategory == CustomRoleCategory.Ghost;
    // Check if player is a imposter teammate
    public static bool IsImpostorTeammate(this PlayerControl player) =>
        player != null && PlayerControl.LocalPlayer != null &&
        (player.IsLocalPlayer() && PlayerControl.LocalPlayer.Is(CustomRoleTeam.Impostor) ||
        PlayerControl.LocalPlayer.Is(CustomRoleTeam.Impostor) && player.Is(CustomRoleTeam.Impostor));

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
