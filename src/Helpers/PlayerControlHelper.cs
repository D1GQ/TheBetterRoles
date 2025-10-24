using AmongUs.Data;
using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils;
using InnerNet;
using System.Collections;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Modules;
using TheBetterRoles.Monos;
using TheBetterRoles.Network;
using TheBetterRoles.Network.Configs;
using TheBetterRoles.Roles;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Interfaces;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Helpers;

/// <summary>
/// Provides extension methods for PlayerControl and NetworkedPlayerInfo to retrieve client data,
/// player details (such as name and color), role information, and perform actions like revive and exile.
/// </summary>
internal static class PlayerControlHelper
{
    /// <summary>
    /// Gets the ClientData associated with the given player.
    /// </summary>
    internal static ClientData? GetClient(this PlayerControl player)
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

    /// <summary>
    /// Gets the client ID of the given player.
    /// Returns -1 if no client is found.
    /// </summary>
    internal static int GetClientId(this PlayerControl player) => player?.GetClient()?.Id != null ? player.GetClient().Id : -1;

    /// <summary>
    /// Gets the player's name formatted with their outfit color.
    /// </summary>
    internal static string GetPlayerNameAndColor(this PlayerControl player)
    {
        if (player?.Data == null) return string.Empty;

        try
        {
            return $"<color={Colors.Color32ToHex(Palette.PlayerColors[player.Data.DefaultOutfit.ColorId])}>{player.Data.PlayerName}</color>";
        }
        catch
        {
            return player.Data.PlayerName;
        }
    }

    /// <summary>
    /// Gets the player's name formatted with their outfit color using NetworkedPlayerInfo.
    /// </summary>
    internal static string GetPlayerNameAndColor(this NetworkedPlayerInfo data)
    {
        if (data == null) return string.Empty;

        try
        {
            return $"<color={Colors.Color32ToHex(Palette.PlayerColors[data.DefaultOutfit.ColorId])}>{data.PlayerName}</color>";
        }
        catch
        {
            return data.PlayerName;
        }
    }

    /// <summary>
    /// Gets the player's outfit color.
    /// </summary>
    internal static Color GetPlayerColor(this PlayerControl player) => Palette.PlayerColors[player.Data.DefaultOutfit.ColorId];
    internal static Color GetPlayerColor(this NetworkedPlayerInfo data) => Palette.PlayerColors[data.DefaultOutfit.ColorId];

    /// <summary>
    /// Gets the player's role.
    /// </summary>
    internal static RoleClass? Role(this PlayerControl player) => player?.ExtendedData()?.RoleInfo?.Role;

    /// <summary>
    /// Gets the player's role.
    /// </summary>
    internal static RoleClass? Role(this NetworkedPlayerInfo data) => data?.ExtendedData()?.RoleInfo?.Role;

    /// <summary>
    /// Gets the player's role name formatted with its corresponding color.
    /// </summary>
    internal static string GetRoleNameAndColor(this PlayerControl player) => $"<color={player.GetRoleColorHex()}>{player.GetRoleName()}</color>";

    /// <summary>
    /// Gets the player's role name.
    /// </summary>
    internal static string GetRoleName(this PlayerControl player) => Utils.GetCustomRoleName(player.ExtendedData().RoleInfo.RoleType);

    /// <summary>
    /// Gets the player's role color.
    /// </summary>
    internal static Color GetRoleColor(this PlayerControl player) => Utils.GetCustomRoleColor(player.ExtendedData().RoleInfo.RoleType);

    /// <summary>
    /// Gets the player's role color in hexadecimal format.
    /// </summary>
    internal static string GetRoleColorHex(this PlayerControl player) => Utils.GetCustomRoleColorHex(player.ExtendedData().RoleInfo.RoleType);

    /// <summary>
    /// Gets the player's role information, optionally providing detailed info.
    /// </summary>
    internal static string GetRoleInfo(this PlayerControl player, bool longInfo = false) => Utils.GetCustomRoleInfo(player.ExtendedData().RoleInfo.RoleType, longInfo);

    /// <summary>
    /// Gets additional information about the player's role add-ons, optionally limiting the amount displayed.
    /// </summary>
    internal static string GetAddonInfo(this PlayerControl player, bool longInfo = false, float amount = float.MaxValue)
    {
        List<string> strs = [];
        int count = 0;
        foreach (var addon in player.ExtendedData().RoleInfo.Addons)
        {
            if (count >= amount) break;
            count++;
            strs.Add($"<{Utils.GetCustomRoleColorHex(addon.RoleType)}>{Utils.GetCustomRoleInfo(addon.RoleType, longInfo)}</color>");
        }
        return string.Join("\n", strs);
    }

    /// <summary>
    /// Gets the name of the player's role team.
    /// </summary>
    internal static string GetRoleTeamName(this PlayerControl player) => Utils.GetCustomRoleTeamName(player.Role()?.RoleTeam ?? RoleClassTeam.None);

    /// <summary>
    /// Gets the dead body associated with the player, if any.
    /// </summary>
    internal static DeadBody? DeadBody(this PlayerControl player) => Main.AllDeadBodys.FirstOrDefault(body => body.ParentId == player.PlayerId);
    internal static DeadBody? DeadBody(this NetworkedPlayerInfo data) => Main.AllDeadBodys.FirstOrDefault(body => body.ParentId == data.PlayerId);

    /// <summary>
    /// Revives the player, resetting their state to alive.
    /// </summary>
    internal static void CustomRevive(this PlayerControl player, bool SetData = true, bool setReason = true)
    {
        if (player.IsAlive(true)) return;

        if (SetData)
        {
            player.Data.IsDead = false;
        }
        if (setReason)
        {
            player.SetDeathReason(DeathReasons.None, Color.white);
        }
        player.gameObject.layer = LayerMask.NameToLayer("Players");
        player.MyPhysics.ResetMoveState(true);
        player.clickKillCollider.enabled = true;
        player.cosmetics.SetPetSource(player);
        player.cosmetics.SetNameMask(true);
        player.Visible = true;
        player.cosmetics.SetPhantomRoleAlpha(1f);
        if (player.IsLocalPlayer())
        {
            HudManager.Instance.Chat.ForceClosed();
            HudManager.Instance.Chat.SetVisible(GameState.IsLobby || GameState.IsMeeting);
            HudManager.Instance.ShadowQuad.gameObject.SetActive(true);
        }
        player.RawSetRole(RoleTypes.Crewmate);

        CatchedGameData.Instance?.CurrentGameMode?.OnPlayerRevive(player);

        Utils.DirtyAllNames();
    }

    /// <summary>
    /// Exiles the player, marking them as eliminated from the game.
    /// </summary>
    internal static void CustomExiled(this PlayerControl player, bool assignGhostRole = false, bool setReason = true)
    {
        if (!player.IsAlive()) return;
        RoleListener.InvokeRoles<IRoleDeathAction>(role => role.OnDeath(player, DeathReasons.Exiled), player: player);
        RoleListener.InvokeRoles<IRoleDeathAction>(role => role.OnDeathOther(player, DeathReasons.Exiled));

        player.ExtendedData().IsFakeDead = false;
        player.Die(DeathReason.Exile, assignGhostRole);
        if (player.IsLocalPlayer())
        {
            DataManager.Player.Stats.IncrementStat(StatID.TimesEjected);
        }
        if (setReason)
        {
            player.SetDeathReason(DeathReasons.Exiled, Utils.GetCustomRoleTeamColorHex(RoleClassTeam.Crewmate));
        }

        CatchedGameData.Instance?.CurrentGameMode?.OnPlayerDeath(player);

        Utils.DirtyAllNames();
    }

    /// <summary>
    /// Sets the death reason for a player with a specified color in hex format.
    /// </summary>
    internal static void SetDeathReason(this PlayerControl player, DeathReasons reason, string hexColor, bool setDirty = false)
    {
        player.SetDeathReason(reason, Colors.HexToColor(hexColor));
        if (setDirty) player.DirtyData();
    }

    /// <summary>
    /// Sets the death reason for a player with a specified color.
    /// </summary>
    internal static void SetDeathReason(this PlayerControl player, DeathReasons reason, Color color)
    {
        if (player?.ExtendedData()?.DeathReason != null)
        {
            player.ExtendedData().DeathReasonColor = color;
            player.ExtendedData().DeathReason = reason;
        }
    }

    /// <summary>
    /// Formats the player's death reason into a readable string.
    /// </summary>
    internal static string FormatDeathReason(this PlayerControl player)
    {
        var data = player?.ExtendedData();
        if (data?.DeathReason != null && data.DeathReason != DeathReasons.None)
        {
            return $"《{Utils.FormatDeathReason(data.DeathReason, data.DeathReasonColor)}》";
        }

        return string.Empty;
    }

    /// <summary>
    /// Formats the death reason for a networked player into a readable string.
    /// </summary>
    internal static string FormatDeathReason(this NetworkedPlayerInfo Data)
    {
        var data = Data?.ExtendedData();
        if (data?.DeathReason != null && data.DeathReason != DeathReasons.None)
        {
            return $"《{Utils.FormatDeathReason(data.DeathReason, data.DeathReasonColor)}》";
        }

        return string.Empty;
    }

    /// <summary>
    /// Updates the position of the colorblind text indicator for the player.
    /// </summary>
    internal static void UpdateColorBlindTextPosition(this PlayerControl player)
    {
        var text = player.cosmetics.colorBlindText;
        if (!text.enabled) return;
        if (!player.onLadder && !player.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
        {
            text.transform.localPosition = new Vector3(0f, -1.5f, 0.4999f);
        }
        else
        {
            text.transform.localPosition = new Vector3(0f, -1.75f, 0.4999f);
        }
    }

    /// <summary>
    /// Toggles the player's camouflage effect.
    /// </summary>
    internal static void SetCamouflage(this PlayerControl player, bool active)
    {
        if (player.ExtendedPC().CamouflagedQueue)
        {
            player.SetCosmeticsActive(false);
            player.SetPlayerTextActive(false);
            player.ExtendedPC().CamouflageBackToColor = player.cosmetics.bodyMatProperties.ColorId;
            player.RawSetColor(CustomColors.CamouflageId);
            var pet = player.cosmetics.GetPet();
            if (pet != null)
            {
                pet.targetPlayer = null;
                pet.SetScared();
            }
        }

        player.ExtendedPC().CamouflagedQueue.Add(active);
        active = !player.ExtendedPC().CamouflagedQueue;

        if (!active)
        {
            player.RawSetColor(player.ExtendedPC().CamouflageBackToColor);
            player.SetPlayerTextActive(true);
            player.SetCosmeticsActive(true);
            var pet = player.cosmetics.GetPet();
            if (pet != null)
            {
                if (player.IsAlive())
                {
                    pet.SetIdle();
                    pet.targetPlayer = player;
                    pet.Visible = !player.IsInVent();
                }
                else
                {
                    pet.SetIdle();
                }
            }
        }
    }

    /// <summary>
    /// Toggles the player's cosmetic visibility.
    /// </summary>
    internal static void SetCosmeticsActive(this PlayerControl player, bool active)
    {
        player.ExtendedPC()?.CosmeticsActiveQueue.Add(!active);
    }

    /// <summary>
    /// Toggles the player's name and text visibility.
    /// </summary>
    internal static void SetPlayerTextActive(this PlayerControl player, bool active)
    {
        player.ExtendedPC()?.PlayerTextActiveQueue.Add(!active);
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
    internal static void CustomMurderPlayer(this PlayerControl killer, PlayerControl target, bool snapToTarget = true, bool spawnBody = true, bool showAnimation = true, bool playSound = true, bool assignGhostRole = true)
    {
        RoleListener.InvokeRoles<IRoleDeathAction>(role => role.OnDeath(target, DeathReasons.Killed), player: target);
        RoleListener.InvokeRoles<IRoleDeathAction>(role => role.OnDeathOther(target, DeathReasons.Killed));

        if (killer.IsLocalPlayer())
        {
            if (GameManager.Instance.IsHideAndSeek())
            {
                DataManager.Player.Stats.IncrementStat(StatID.HideAndSeek_ImpostorKills);
            }
            else
            {
                DataManager.Player.Stats.IncrementStat(StatID.ImpostorKills);
            }
            if (killer.CurrentOutfitType == PlayerOutfitType.Shapeshifted)
            {
                DataManager.Player.Stats.IncrementStat(StatID.Role_Shapeshifter_ShiftedKills);
            }
            if (Constants.ShouldPlaySfx() && playSound)
            {
                SoundManager.Instance.PlaySound(killer.KillSfx, false, 0.8f, null);
            }
        }
        if (target.IsLocalPlayer())
        {
            DataManager.Player.Stats.IncrementStat(StatID.TimesMurdered);
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

            if (showAnimation) HudManager.Instance.KillOverlay.ShowKillAnimation(killer.Data, target.Data);
            target.cosmetics.SetNameMask(false);
            target.RpcSetScanner(false);
        }

        target.SetDeathReason(DeathReasons.Killed, Utils.GetCustomRoleTeamColorHex(RoleClassTeam.Impostor));
        KillAnimation randomKillAnimation = killer.KillAnimations.Random();
        killer.MyPhysics.StartCoroutine(CoNewMurder(killer, target, randomKillAnimation, snapToTarget, spawnBody, showAnimation, assignGhostRole));
    }
    private static IEnumerator CoNewMurder(PlayerControl killer, PlayerControl target, KillAnimation killAnimation, bool snapToTarget, bool spawnBody, bool showAnimation, bool assignGhostRole)
    {
        FollowerCamera cam = Camera.main.GetComponent<FollowerCamera>();
        bool isParticipant = killer.IsLocalPlayer() || target.IsLocalPlayer();
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
            deadBody = Utils.SpawnBody(target.transform.position, target.PlayerId, null, null, killAnimation.BodyOffset);
            RoleListener.InvokeRoles<IRoleMurderAction>(role => role.DeadBodyDrop(killer, deadBody), player: target);
            RoleListener.InvokeRoles<IRoleMurderAction>(role => role.DeadBodyDropOther(killer, deadBody));
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
        target.Die(DeathReason.Kill, assignGhostRole);

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

        CatchedGameData.Instance?.CurrentGameMode?.OnPlayerDeath(target);

        Utils.DirtyAllNames();
        yield break;
    }

    /// <summary>
    /// Plays the shield break animation for the player with an optional color change.
    /// </summary>
    internal static void ShieldBreakAnimation(this PlayerControl player, Color? color = null)
    {
        RoleEffectAnimation roleEffectAnimation = UnityEngine.Object.Instantiate(RoleManager.Instance.protectAnim, player.gameObject.transform);
        roleEffectAnimation.SetMaskLayerBasedOnWhoShouldSee(true);
        if (color != null)
        {
            roleEffectAnimation.Renderer.material.shader = AssetBundles.GrayscaleShader;
            roleEffectAnimation.Renderer.material.SetColor("_Color", (Color)color);
        }
        roleEffectAnimation.Play(player, null, player.cosmetics.FlipX, RoleEffectAnimation.SoundType.Global, 0f, true, 0f);
    }

    /// <summary>
    /// Plays the shield break animation for the player using a hexadecimal string for the color.
    /// </summary>
    internal static void ShieldBreakAnimation(this PlayerControl player, string color)
    {
        player.ShieldBreakAnimation(Colors.HexToColor(color));
    }

    /// <summary>
    /// Checks if the player can be teleported based on various game states and conditions.
    /// </summary>
    internal static bool CanBeTeleported(this PlayerControl player)
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

    /// <summary>
    /// Checks if the player can be interacted with based on various game states and conditions.
    /// </summary>
    internal static bool CanBeInteracted(this PlayerControl player)
    {
        if (player.Data == null
            || GameState.IsMeeting
            || !player.IsAlive()
            || player.inVent
            || player.inMovingPlat // Moving Platform on Airhip and Zipline on Fungle
            || player.onLadder || player.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Sets the player's overhead text, allowing customization for top, bottom, or info text.
    /// </summary>
    internal static void SetPlayerTextInfo(this PlayerControl player, string text, bool isBottom = false, bool isInfo = false)
    {
        if (player == null) return;

        var textTop = player.ExtendedPC().InfoTextTop;
        var textBottom = player.ExtendedPC().InfoTextBottom;
        var textInfo = player.ExtendedPC().InfoTextInfo;

        var targetText = isBottom ? textBottom : textTop;
        if (isInfo)
        {
            targetText = textInfo;

            if (string.IsNullOrEmpty(Utils.RemoveHtmlText(textTop.text)))
            {
                text = "<voffset=-2.25em>" + text + "</voffset>";
            }
        }

        text = text.Size(65f);
        if (targetText != null)
        {
            targetText.text = text;
        }
    }

    /// <summary>
    /// Resets all of the player's overhead text (top, bottom, and info).
    /// </summary>
    internal static void ResetAllPlayerTextInfo(this PlayerControl player)
    {
        if (player == null) return;
        player.SetPlayerTextInfo("", isInfo: true);
        player.SetPlayerTextInfo("");
        player.SetPlayerTextInfo("", isBottom: true);
    }

    /// <summary>
    /// Checks if the player's character data is valid and has been properly received from the host.
    /// </summary>
    internal static bool DataIsCollected(this PlayerControl player)
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

    /// <summary>
    /// Kicks the player from the game, optionally banning them.
    /// </summary>
    internal static void Kick(this PlayerControl player, bool ban = false)
    {
        AmongUsClient.Instance.KickPlayer(player.GetClientId(), ban);
    }

    /// <summary>
    /// Sets the color outline on the player's character.
    /// </summary>
    internal static void SetOutline(this PlayerControl player, bool active, Color? color = null)
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

    /// <summary>
    /// Sets the color outline on the player's character using a hexadecimal color string.
    /// </summary>
    internal static void SetOutlineByHex(this PlayerControl player, bool active, string hexColor = "")
    {
        Color? color = Colors.HexToColor(hexColor);
        player.cosmetics.currentBodySprite.BodySprite.material.SetFloat("_Outline", active ? 1 : 0);
        SpriteRenderer[] longModeParts = player.cosmetics.currentBodySprite.LongModeParts;
        for (int i = 0; i < longModeParts.Length; i++)
        {
            longModeParts[i].material.SetFloat("_Outline", active ? 1 : 0);
        }
        if (color != null)
        {
            player.cosmetics.currentBodySprite.BodySprite.material.SetColor("_OutlineColor", (Color)color);
            longModeParts = player.cosmetics.currentBodySprite.LongModeParts;
            for (int i = 0; i < longModeParts.Length; i++)
            {
                longModeParts[i].material.SetColor("_OutlineColor", (Color)color);
            }
        }
    }

    /// <summary>
    /// Sets the true visor color on the player's character.
    /// </summary>
    internal static void SetTrueVisorColor(this PlayerControl player, Color color)
    {
        player?.cosmetics?.bodySprites[0]?.BodySprite?.material?.SetColor(PlayerMaterial.VisorColor, color);
        var sprite = player?.cosmetics?.visor?.GetComponent<SpriteRenderer>();
        color = color != Palette.VisorColor ? color : Color.white;
        if (sprite != null)
        {
            sprite.color = color;
        }
    }

    /// <summary>
    /// Sets the player's role directly, overriding any previous role.
    /// </summary>
    internal static void RawSetRole(this PlayerControl player, RoleTypes role) => RoleManager.Instance.SetRole(player, role);

    /// <summary>
    /// Checks if the player is in the room selection phase for the Airship map.
    /// </summary>
    internal static bool IsInRoomSelect(this PlayerControl player)
    {
        if (player == null) return false;
        return GameState.AirshipIsActive && Vector2.Distance(player.GetTruePosition(), new(-25, 40)) < 5f;
    }

    /// <summary>
    /// Checks if the player is the local player.
    /// </summary>
    internal static bool IsLocalPlayer(this PlayerControl player) => player == PlayerControl.LocalPlayer;

    /// <summary>
    /// Checks if the player's data belongs to the local player.
    /// </summary>
    internal static bool IsLocalData(this NetworkedPlayerInfo data) => data?.Object?.IsLocalPlayer() ?? data?.ExtendedData()?.IsLocalData ?? false;

    /// <summary>
    /// Checks if the player is currently on a ladder.
    /// </summary>
    internal static bool IsOnLadder(this PlayerControl player) => player.onLadder || player.MyPhysics.Animations.IsPlayingAnyLadderAnimation();

    /// <summary>
    /// Retrieves the vent ID the player is currently in.
    /// </summary>
    internal static int GetPlayerVentId(this PlayerControl player)
    {
        if (player == null) return -1;

        if (!(ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Ventilation, out var systemType) &&
              systemType.TryCast<VentilationSystem>() is VentilationSystem ventilationSystem))
            return 0;

        return ventilationSystem.PlayersInsideVents.TryGetValue(player.PlayerId, out var playerIdVentId) ? playerIdVentId : -1;
    }

    /// <summary>
    /// Retrieves the player's custom position in the world.
    /// </summary>
    internal static Vector2 GetCustomPosition(this PlayerControl player) => new(player.transform.position.x, player.transform.position.y);

    /// <summary>
    /// Retrieves the player's hashed PUID.
    /// </summary>
    internal static string GetHashPuid(this PlayerControl player)
    {
        return player.Data.GetHashPuid() ?? "";
    }

    /// <summary>
    /// Retrieves the hashed PUID from networked player data.
    /// </summary>
    internal static string GetHashPuid(this NetworkedPlayerInfo data)
    {
        if (data?.Puid == null) return "";
        return Utils.GetHashStr(data.Puid);
    }

    /// <summary>
    /// Retrieves the player's hashed Friendcode.
    /// </summary>
    internal static string GetHashFriendcode(this PlayerControl player)
    {
        return player.Data.GetHashFriendcode() ?? "";
    }

    /// <summary>
    /// Retrieves the hashed Friendcode from networked player data.
    /// </summary>
    internal static string GetHashFriendcode(this NetworkedPlayerInfo data)
    {
        if (data?.FriendCode == null) return "";
        return Utils.GetHashStr(data.FriendCode);
    }

    /// <summary>
    /// Checks if the player is a developer.
    /// </summary>
    internal static bool IsDev(this PlayerControl player) => player.ExtendedData().MyUserData.IsDev();

    /// <summary>
    /// Checks if the player is alive.
    /// </summary>
    internal static bool IsAlive(this PlayerControl player, bool CheckFake = false) => IsAlive(player.Data, CheckFake);

    /// <summary>
    /// Checks if the networked player data indicates that the player is alive.
    /// </summary>
    internal static bool IsAlive(this NetworkedPlayerInfo data, bool CheckFake = false)
    {
        if (data == null)
            return false;

        bool isDead = data.IsDead;
        if (!CheckFake)
            return !isDead;

        var extendedData = data.ExtendedData();
        return !isDead && extendedData?.IsFakeDead == false;
    }

    // Check if player is in a vent
    /// <summary>
    /// Checks if the player is currently in a vent, either by being inside, walking to one, or playing the enter vent animation.
    /// </summary>
    internal static bool IsInVent(this PlayerControl player) => player != null && (player.inVent || player.walkingToVent || player.MyPhysics?.Animations?.IsPlayingEnterVentAnimation() == true);

    // Check if player can vent
    /// <summary>
    /// Determines if the player has the ability to vent based on their role's permissions.
    /// </summary>
    internal static bool CanVent(this PlayerControl player) => player.CheckAnyRoles(role => role.CanVent) == true;

    // Check if player can kill
    /// <summary>
    /// Determines if the player has the ability to kill based on their role's permissions.
    /// </summary>
    internal static bool CanKill(this PlayerControl player) => player.CheckAnyRoles(role => role.CanKill) == true;

    // Check if player can sabotage
    /// <summary>
    /// Determines if the player has the ability to sabotage based on their role's permissions.
    /// </summary>
    internal static bool CanSabotage(this PlayerControl player) => player.CheckAnyRoles(role => role.CanSabotage) == true;

    // Check if player's role has been assigned
    /// <summary>
    /// Checks if the player's role has been assigned based on their extended data.
    /// </summary>
    internal static bool RoleAssigned(this PlayerControl player)
    {
        if (player == null) return false;

        var extendedData = player.ExtendedData();
        if (extendedData == null || extendedData.RoleInfo == null)
            return false;

        return extendedData.RoleInfo.RoleAssigned;
    }

    // Get hex color for team
    /// <summary>
    /// Retrieves the hex color associated with the player's team based on their role's team.
    /// </summary>
    internal static string GetTeamHexColor(this PlayerControl player) => Utils.GetCustomRoleTeamColorHex(player.Role()?.RoleTeam ?? RoleClassTeam.None);

    // Get color for team
    /// <summary>
    /// Retrieves the color associated with the player's team based on their role's team.
    /// </summary>
    internal static Color GetTeamColor(this PlayerControl player) => Colors.HexToColor(Utils.GetCustomRoleTeamColorHex(player.Role()?.RoleTeam ?? RoleClassTeam.None));

    // Check if player is of a specific role type
    /// <summary>
    /// Checks if the player has the specified role type.
    /// </summary>
    internal static bool Is(this PlayerControl player, RoleClassTypes role) => player?.ExtendedData()?.RoleInfo?.RoleType == role;

    // Check if player has a specific addon role
    /// <summary>
    /// Checks if the player has the specified addon role.
    /// </summary>
    internal static bool Has(this PlayerControl player, RoleClassTypes addon) => player?.ExtendedData()?.RoleInfo?.Addons.Any(a => a.RoleType == addon) ?? false;

    // Check if player is of a specific team role type
    /// <summary>
    /// Checks if the player is on a specific role team or has overridden the team role.
    /// </summary>
    internal static bool Is(this PlayerControl player, RoleClassTeam roleTeam) => player?.Role()?.RoleTeam == roleTeam && player?.Role()?.OverrideTeam == RoleClassTeam.None
        || player?.Role()?.OverrideTeam == roleTeam;

    // Check if player is of a specific category role type
    /// <summary>
    /// Checks if the player belongs to a specific role category.
    /// </summary>
    internal static bool Is(this PlayerControl player, RoleClassCategory roleCategory) => player?.Role()?.RoleCategory == roleCategory;

    // Check if player has a Ghost role type
    /// <summary>
    /// Checks if the player has a Ghost role type.
    /// </summary>
    internal static bool IsGhostRole(this PlayerControl player) => player?.Role()?.IsGhostRole == true;

    // Check if player is an impostor teammate
    /// <summary>
    /// Determines if the player is an impostor teammate, either by being the local player in the impostor team or being in the same team as the local impostor player.
    /// </summary>
    internal static bool IsImpostorTeammate(this PlayerControl player) =>
        player != null && PlayerControl.LocalPlayer != null &&
        (player.IsLocalPlayer() && PlayerControl.LocalPlayer.Is(RoleClassTeam.Impostor) ||
        PlayerControl.LocalPlayer.Is(RoleClassTeam.Impostor) && player.Is(RoleClassTeam.Impostor));

    // Check if player is a teammate
    /// <summary>
    /// Checks if the player is on the same team as the local player, considering specific team and role exclusions.
    /// </summary>
    internal static bool IsTeammate(this PlayerControl player) =>
        player != null && PlayerControl.LocalPlayer != null &&
        (player.IsLocalPlayer() || player.Is(PlayerControl.LocalPlayer.Role()?.RoleTeam ?? RoleClassTeam.None) && !PlayerControl.LocalPlayer.Is(RoleClassTeam.Neutral));

    // Check if player is the host
    /// <summary>
    /// Checks if the player is the host of the current game session.
    /// </summary>
    internal static bool IsHost(this PlayerControl player) => player?.Data != null && GameData.Instance?.GetHost() == player.Data;
}
