using AmongUs.Data;
using InnerNet;
using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using TheBetterRoles.Patches.Game.Ship;
using TheBetterRoles.Patches.UI.Chat;
using TheBetterRoles.Roles;
using TheBetterRoles.Roles.Core;
using UnityEngine;

namespace TheBetterRoles.Helpers;

internal static class Utils
{
    /// <summary>
    /// Adjusts the size of the provided string by wrapping it with a 
    /// <size> tag using the specified percentage value.
    /// This allows you to modify text size in Unity-based environments.
    /// </summary>
    /// <param name="str">The string to be resized.</param>
    /// <param name="size">The size percentage to apply (e.g., 150 for 150% size).</param>
    /// <returns>A string wrapped in a size tag, displaying the text at the specified size.</returns>
    internal static string Size(this string str, float size) => $"<size={size}%>{str}</size>";

    // Convert a string to a colored string using a hexadecimal color code
    /// <summary>
    /// Converts the provided string into a colored string by wrapping it with a 
    /// <color> tag using the provided hexadecimal color code (e.g., "#FF5733").
    /// This allows you to apply color formatting to the string in Unity-based environments.
    /// </summary>
    /// <param name="str">The string to be colored.</param>
    /// <param name="hexColor">The hexadecimal color code to apply (e.g., "#FF5733").</param>
    /// <returns>A string wrapped in a color tag, displaying the string in the specified color.</returns>
    internal static string ToColor(this string str, string hexColor) => $"<{hexColor}>{str}</color>";

    // Get player data from client ID
    /// <summary>
    /// Retrieves the client data for a given client ID by searching through the list of all clients.
    /// This method is used to retrieve information about a specific client in a multiplayer game environment.
    /// </summary>
    /// <param name="clientId">The ID of the client whose data is being retrieved.</param>
    /// <returns>The client data associated with the provided client ID, or null if no client is found.</returns>
    internal static ClientData? ClientFromClientId(int clientId)
        => AmongUsClient.Instance.allClients.ToArray().FirstOrDefault(cd => cd.Id == clientId);

    // Get player data from player ID
    /// <summary>
    /// Retrieves the player data for a given player ID by searching through the list of all players.
    /// This method is useful for retrieving information about a specific player using their player ID in the game.
    /// </summary>
    /// <param name="playerId">The ID of the player whose data is being retrieved.</param>
    /// <returns>The player data associated with the provided player ID, or null if no player is found.</returns>
    internal static NetworkedPlayerInfo? PlayerDataFromPlayerId(int playerId)
        => GameData.Instance.AllPlayers.ToArray().FirstOrDefault(data => data.PlayerId == playerId);

    // Get player data from client ID
    /// <summary>
    /// Retrieves the player data for a given client ID by searching through the list of all players.
    /// This is particularly useful when you need to look up player information based on their client connection ID.
    /// </summary>
    /// <param name="clientId">The client ID of the player whose data is being retrieved.</param>
    /// <returns>The player data associated with the provided client ID, or null if no player is found.</returns>
    internal static NetworkedPlayerInfo? PlayerDataFromClientId(int clientId)
        => GameData.Instance.AllPlayers.ToArray().FirstOrDefault(data => data.ClientId == clientId);

    // Get player data from friend code
    /// <summary>
    /// Retrieves the player data for a given friend code, which is a unique identifier assigned to players for friend connections.
    /// This method is useful for fetching data related to a player using their friend code in a multiplayer environment.
    /// </summary>
    /// <param name="friendCode">The friend code of the player whose data is being retrieved.</param>
    /// <returns>The player data associated with the provided friend code, or null if no player is found.</returns>
    internal static NetworkedPlayerInfo? PlayerDataFromFriendCode(string friendCode)
        => GameData.Instance.AllPlayers.ToArray().FirstOrDefault(data => data.FriendCode == friendCode);

    // Get player from player ID
    /// <summary>
    /// Retrieves the PlayerControl object for a given player ID. The PlayerControl object represents 
    /// the player in the game, and this method can be used to access the player's control features.
    /// </summary>
    /// <param name="playerId">The ID of the player whose PlayerControl object is being retrieved.</param>
    /// <returns>The PlayerControl object for the player with the specified ID, or null if no player is found.</returns>
    internal static PlayerControl? PlayerFromPlayerId(int playerId)
        => Main.AllPlayerControls.FirstOrDefault(player => player.PlayerId == playerId);

    // Get player from client ID
    /// <summary>
    /// Retrieves the PlayerControl object for a given client ID. This method is used to access a player's 
    /// control object based on their client connection, useful for managing player actions in a networked environment.
    /// </summary>
    /// <param name="clientId">The client ID of the player whose PlayerControl object is being retrieved.</param>
    /// <returns>The PlayerControl object for the player with the specified client ID, or null if no player is found.</returns>
    internal static PlayerControl? PlayerFromClientId(int clientId)
        => Main.AllPlayerControls.FirstOrDefault(player => player.GetClientId() == clientId);

    // Get player from network ID
    /// <summary>
    /// Retrieves the PlayerControl object for a given network ID. This is useful in multiplayer games 
    /// where players are identified by a network ID, allowing the system to find the specific player based on this ID.
    /// </summary>
    /// <param name="netId">The network ID of the player whose PlayerControl object is being retrieved.</param>
    /// <returns>The PlayerControl object for the player with the specified network ID, or null if no player is found.</returns>
    internal static PlayerControl? PlayerFromNetId(uint netId)
        => Main.AllPlayerControls.FirstOrDefault(player => player.NetId == netId);


    // Attempt to fix a sabotage in the system based on the sabotage type
    /// <summary>
    /// Attempts to fix a specific sabotage by identifying the system type and applying the appropriate fix.
    /// Each system type is handled differently, with various behaviors based on the system affected.
    /// This method requires that the player be the host of the game, otherwise, it will not attempt to fix the sabotage.
    /// </summary>
    /// <param name="type">The type of sabotage system to fix.</param>
    internal static void TryFixSabotage(SystemTypes type)
    {
        if (!GameState.IsHost || !ShipStatus.Instance.Systems.TryGetValue(type, out var system)) return;

        if (type == SystemTypes.Electrical)
        {
            var switchSystem = system.TryCast<SwitchSystem>();
            if (switchSystem != null && switchSystem.IsActive)
            {
                switchSystem.ActualSwitches = switchSystem.ExpectedSwitches;
                switchSystem.IsDirty = true;
            }
        }
        else if (type == SystemTypes.Reactor)
        {
            var reactorSystem = system.TryCast<ReactorSystemType>();
            if (reactorSystem != null && reactorSystem.IsActive)
            {
                reactorSystem.ClearSabotage();
                reactorSystem.IsDirty = true;
            }
        }
        else if (type == SystemTypes.Laboratory)
        {
            var laboratorySystem = system.TryCast<ReactorSystemType>();
            if (laboratorySystem != null && laboratorySystem.IsActive)
            {
                laboratorySystem.ClearSabotage();
                laboratorySystem.IsDirty = true;
            }
        }
        else if (type == SystemTypes.LifeSupp)
        {
            var lifeSuppSystem = system.TryCast<LifeSuppSystemType>();
            if (lifeSuppSystem != null && lifeSuppSystem.IsActive)
            {
                lifeSuppSystem.Countdown = 10000f;
                lifeSuppSystem.IsDirty = true;
            }
        }
        else if (type == SystemTypes.HeliSabotage)
        {
            var heliSabotageSystem = system.TryCast<HeliSabotageSystem>();
            if (heliSabotageSystem != null && heliSabotageSystem.IsActive)
            {
                heliSabotageSystem.ClearSabotage();
                heliSabotageSystem.IsDirty = true;
            }
        }
        else if (type == SystemTypes.Comms)
        {
            var commsSystem = system.TryCast<HudOverrideSystemType>();
            if (commsSystem != null && commsSystem.IsActive)
            {
                SystemPatch.CamouflageComms(system, commsSystem.IsActive);
                commsSystem.IsActive = false;
                commsSystem.IsDirty = true;
            }
            var commsSystem2 = system.TryCast<HqHudSystemType>();
            if (commsSystem2 != null && commsSystem2.IsActive)
            {
                commsSystem2.CompletedConsoles = new();
                SystemPatch.CamouflageComms(system, commsSystem2.IsActive);
                commsSystem2.CompletedConsoles.Add(1);
                commsSystem2.CompletedConsoles.Add(2);
                commsSystem2.IsDirty = true;
            }
        }
        else if (type == SystemTypes.MushroomMixupSabotage)
        {
            var mushroomMixupSabotage = system.TryCast<MushroomMixupSabotageSystem>();
            if (mushroomMixupSabotage != null && mushroomMixupSabotage.IsActive)
            {
                mushroomMixupSabotage.currentSecondsUntilHeal = 0.1f;
                mushroomMixupSabotage.IsDirty = true;
            }
        }
        else if (type == CustomSystemTypes.Blackout)
        {
            var blackoutSabotage = system.TryCast<BlackoutSabotageSystem>();
            if (blackoutSabotage != null && blackoutSabotage.IsActive)
            {
                blackoutSabotage.ClearSabotage();
            }
        }
    }

    // Clears all known sabotages
    /// <summary>
    /// Attempts to fix all known sabotages by iterating through all defined sabotage types and invoking the 
    /// <see cref="TryFixSabotage(SystemTypes)"/> method for each one.
    /// This includes:
    /// - Electrical, Reactor, Laboratory, Life Support, Helicopter Sabotage, Comms, Mushroom Mixup Sabotage, and Blackout.
    /// The method ensures that any sabotage can be fixed when necessary, regardless of the type.
    /// </summary>
    internal static void ClearAllSabotages()
    {
        List<SystemTypes> allSabotages =
        [
            SystemTypes.Electrical,
            SystemTypes.Reactor,
            SystemTypes.Laboratory,
            SystemTypes.LifeSupp,
            SystemTypes.HeliSabotage,
            SystemTypes.Comms,
            SystemTypes.MushroomMixupSabotage,
            CustomSystemTypes.Blackout
        ];

        allSabotages.ForEach(TryFixSabotage);
    }


    /// <summary>
    /// Marks all player names as dirty by incrementing the DirtyName field for each player
    /// based on the number of times their ExtendedData() is referenced.
    /// </summary>
    internal static void DirtyAllNames()
    {
        foreach (var player in Main.AllPlayerControls)
        {
            if (player?.Data == null) continue;

            var extendedData = player.ExtendedData();
            if (extendedData != null)
            {
                extendedData.DirtyName += (byte)Main.AllPlayerControls.Where(pc => player.ExtendedData() == extendedData).Count();
            }
        }
    }

    /// <summary>
    /// Formats the death reason for a player, converting it into a localized string with color.
    /// </summary>
    /// <param name="reason">The reason for the player's death.</param>
    /// <param name="color">The color to apply to the formatted string.</param>
    /// <returns>A formatted string representing the death reason, or an empty string if no reason is provided.</returns>
    internal static string FormatDeathReason(DeathReasons? reason, Color color)
    {
        if (reason != null && reason != DeathReasons.None)
        {
            return $"<{Colors.Color32ToHex(color)}>{Translator.GetString($"DeathReason.{Enum.GetName(typeof(DeathReasons), reason)}")}</color>";
        }

        return string.Empty;
    }

    /// <summary>
    /// Formats the player name, optionally bypassing any disguise for the player.
    /// </summary>
    /// <param name="player">The player whose name is being formatted.</param>
    /// <param name="bypassDisguise">Whether to bypass the disguise and show the player's true name.</param>
    /// <returns>The formatted player name, or an empty string if no name is available.</returns>
    internal static string FormatPlayerName(PlayerControl player, bool bypassDisguise = false)
    {
        if (player == null) return string.Empty;

        var playerData = player.ExtendedData();
        var roleInfo = playerData?.RoleInfo;
        var role = roleInfo?.Role;
        PlayerControl target = player;

        if (role != null)
        {
            if (!bypassDisguise && role.IsDisguised == true && role.DisguisedTargetId >= 0)
            {
                foreach (var players in Main.AllPlayerControls)
                {
                    if (player == null) continue;

                    if (players.PlayerId == role.DisguisedTargetId)
                    {
                        target = players;
                        break;
                    }
                }
            }
        }

        StringBuilder nameBuilder = new StringBuilder();
        string? nameColor = target.ExtendedData().NameColor;

        if (string.IsNullOrEmpty(nameColor))
        {
            nameBuilder.Append(target.Data.PlayerName);
        }
        else
        {
            nameBuilder.Append($"<{nameColor}>{target.Data.PlayerName}</color>");
        }

        nameBuilder.Append(CustomRoleManager.GetRoleMarks(target));
        return nameBuilder.ToString();
    }

    /// <summary>
    /// Formats the player's task progress into a string representation, showing the number of completed tasks and total tasks.
    /// </summary>
    /// <param name="player">The player whose task progress is being formatted.</param>
    /// <returns>A string representing the player's task progress, or an empty string if no tasks are present.</returns>
    internal static string FormatTasksToText(this PlayerControl player)
    {
        if (player.CheckAnyRoles(role => role.HasTask) || player.CheckAnyRoles(role => role.HasSelfTask))
        {
            return $" <#ffff2b>({player.Data.Tasks.ToArray().Where(task => task.Complete).Count()}/{player.Data.Tasks.Count})</color>";
        }
        else
        {
            return string.Empty;
        }
    }

    // Add msg to chat
    /// <summary>
    /// Adds a private chat message to the chat system. The message can be customized with an optional override name and 
    /// alignment to the right. If the message is sent to the right, the position of the name and chat text is adjusted.
    /// This method also handles playing a sound and triggering notifications if the chat is closed or opening.
    /// </summary>
    /// <param name="text">The message text to display.</param>
    /// <param name="overrideName">Optional parameter to override the default system name.</param>
    /// <param name="setRight">Optional flag to set message alignment to the right.</param>
    internal static void AddChatPrivate(string text, string overrideName = "", bool setRight = false)
    {
        ChatController chat = HudManager.Instance.Chat;
        NetworkedPlayerInfo data = PlayerControl.LocalPlayer.Data;
        ChatBubble pooledBubble = chat.GetPooledBubble();
        string MsgName = $"<color=#ffffff><b>(<color=#00ff44>{Translator.GetString("SystemMessage")}</color>)</b>";
        if (overrideName != "")
            MsgName = overrideName;
        try
        {
            pooledBubble.transform.SetParent(chat.scroller.Inner);
            pooledBubble.transform.localScale = Vector3.one;
            pooledBubble.SetCosmetics(data);
            pooledBubble.gameObject.transform.Find("PoolablePlayer").gameObject.SetActive(false);
            pooledBubble.ColorBlindName.gameObject.SetActive(false);
            if (!setRight)
            {
                pooledBubble.SetLeft();
                pooledBubble.gameObject.transform.Find("NameText (TMP)").transform.localPosition += new Vector3(-0.7f, 0f);
                pooledBubble.gameObject.transform.Find("ChatText (TMP)").transform.localPosition += new Vector3(-0.7f, 0f);
            }
            else
            {
                pooledBubble.SetRight();
            }
            chat.SetChatBubbleName(pooledBubble, data, false, false, PlayerNameColor.Get(data), null);
            pooledBubble.SetText(text);
            pooledBubble.AlignChildren();
            chat.AlignAllBubbles();
            pooledBubble.NameText.text = MsgName;
            if (!chat.IsOpenOrOpening && chat.notificationRoutine == null)
            {
                chat.notificationRoutine = chat.StartCoroutine(chat.BounceDot());
            }
            SoundManager.Instance.PlaySound(chat.messageSound, false, 1f, null).pitch = 0.5f + data.PlayerId / 15f;
            ChatControllerPatch.SetChatPoolTheme(pooledBubble);
        }
        catch (Exception ex)
        {
            chat.chatBubblePool.Reclaim(pooledBubble);
            Logger.Error(ex);
            throw;
        }
    }

    // Creates a new IList of a specified type
    /// <summary>
    /// Creates and returns a new instance of a list of a specified type. This method dynamically determines the type of the list 
    /// based on the provided type and returns it as an IList. The type of elements in the list is determined at runtime.
    /// </summary>
    /// <param name="myType">The type of elements that the list will contain.</param>
    /// <returns>An IList containing elements of the specified type.</returns>
    internal static IList? createIList(Type myType)
    {
        Type genericListType = typeof(List<>).MakeGenericType(myType);
        return (IList)Activator.CreateInstance(genericListType);
    }

    // Determines if a system type is related to sabotage
    /// <summary>
    /// Determines if the provided system type is a sabotage-related system. This includes systems such as Reactor, 
    /// Laboratory, Comms, Life Support, Mushroom Mixup Sabotage, Helicopter Sabotage, and Electrical.
    /// </summary>
    /// <param name="type">The system type to check.</param>
    /// <returns>True if the system type is related to sabotage, false otherwise.</returns>
    internal static bool SystemTypeIsSabotage(SystemTypes type) => type is SystemTypes.Reactor
                    or SystemTypes.Laboratory
                    or SystemTypes.Comms
                    or SystemTypes.LifeSupp
                    or SystemTypes.MushroomMixupSabotage
                    or SystemTypes.HeliSabotage
                    or SystemTypes.Electrical;

    // Determines if a system type is related to sabotage based on its integer representation
    /// <summary>
    /// Determines if a system type, represented by its integer value, is related to sabotage. This method is used when the system 
    /// type is provided as an integer rather than a typed enum.
    /// </summary>
    /// <param name="typeNum">The integer value representing the system type to check.</param>
    /// <returns>True if the system type is related to sabotage, false otherwise.</returns>
    internal static bool SystemTypeIsSabotage(int typeNum) => (SystemTypes)typeNum is SystemTypes.Reactor
                or SystemTypes.Laboratory
                or SystemTypes.Comms
                or SystemTypes.LifeSupp
                or SystemTypes.MushroomMixupSabotage
                or SystemTypes.HeliSabotage
                or SystemTypes.Electrical;

    // Retrieves the SystemTypes associated with an ISystemType instance
    /// <summary>
    /// Determines and returns the associated SystemTypes value for the given system. This method checks the system's type 
    /// and maps it to the appropriate enumeration value. If the system type doesn't match any predefined categories, null is returned.
    /// </summary>
    /// <param name="system">The system whose type is being checked.</param>
    /// <returns>The associated SystemTypes value or null if no match is found.</returns>
    internal static SystemTypes? GetSystemTypes(this ISystemType system)
    {
        return system switch
        {
            _ when CastHelper.TryCast<SwitchSystem>(system) => SystemTypes.Electrical,
            _ when CastHelper.TryCast<ReactorSystemType>(system) => SystemTypes.Reactor,
            _ when CastHelper.TryCast<LifeSuppSystemType>(system) => SystemTypes.LifeSupp,
            _ when CastHelper.TryCast<HeliSabotageSystem>(system) => SystemTypes.HeliSabotage,
            _ when CastHelper.TryCast<HqHudSystemType>(system) => SystemTypes.Comms,
            _ when CastHelper.TryCast<HudOverrideSystemType>(system) => SystemTypes.Comms,
            _ when CastHelper.TryCast<MushroomMixupSabotageSystem>(system) => SystemTypes.MushroomMixupSabotage,
            _ => null
        };
    }

    // Creates a screen flash effect with customizable fade-in and fade-out durations
    /// <summary>
    /// Creates a visual screen flash effect using a specified color and customizable fade-in, fade-out, and effect durations.
    /// This method can be used to notify the player of specific events or triggers by flashing the screen.
    /// </summary>
    /// <param name="name">The name of the effect, used for identification.</param>
    /// <param name="color">The color of the flash effect.</param>
    /// <param name="fadeInDuration">The duration of the fade-in effect.</param>
    /// <param name="fadeOutDuration">The duration of the fade-out effect.</param>
    /// <param name="effectDuration">The total duration of the flash effect.</param>
    /// <param name="Override">Optional flag to override the default behavior.</param>
    internal static void FlashScreen(string name, Color color, float fadeInDuration = 0.25f, float fadeOutDuration = 0.25f, float effectDuration = 1f, bool Override = false, bool fullColor = false)
    {
        new ScreenFlash().Create(name, color, fadeInDuration, fadeOutDuration, effectDuration, Override, fullColor);
    }

    // Creates a screen flash effect with a hex color input
    /// <summary>
    /// Creates a visual screen flash effect using a specified hex color string and customizable fade-in, fade-out, and effect durations.
    /// This method is an overload of the standard FlashScreen method, accepting a hex color value instead of a Color object.
    /// </summary>
    /// <param name="name">The name of the effect, used for identification.</param>
    /// <param name="hex">The hex string representing the color for the flash effect.</param>
    /// <param name="fadeInDuration">The duration of the fade-in effect.</param>
    /// <param name="fadeOutDuration">The duration of the fade-out effect.</param>
    /// <param name="effectDuration">The total duration of the flash effect.</param>
    /// <param name="Override">Optional flag to override the default behavior.</param>
    /// <param name="fullColor">Optional flag to use the full color for the effect.</param>
    internal static void FlashScreen(string name, string hex, float fadeInDuration = 0.25f, float fadeOutDuration = 0.25f, float effectDuration = 1f, bool Override = false, bool fullColor = false)
    {
        Color color = Colors.HexToColor(hex);
        FlashScreen(name, color, fadeInDuration, fadeOutDuration, effectDuration, Override, fullColor);
    }

    // Spawns a dead body at a specific position with optional customization for player IDs, skin, and position offset
    /// <summary>
    /// Spawns a dead body at the specified position with additional options to customize the parent ID, target player ID, 
    /// player skin ID, and position offset. The body will be instantiated with materials matching the player's skin and the 
    /// appropriate rotation (flipX) based on the player's physics properties.
    /// </summary>
    /// <param name="pos">The position to spawn the body.</param>
    /// <param name="parentId">The ID of the player who the body belongs to.</param>
    /// <param name="playerTargetId">The ID of the target player (optional).</param>
    /// <param name="playerSkinId">The ID of the player skin (optional).</param>
    /// <param name="offset">Optional offset for positioning the body.</param>
    /// <returns>The instantiated dead body object.</returns>
    internal static DeadBody SpawnBody(Vector2 pos, byte parentId, byte? playerTargetId = null, byte? playerSkinId = null, Vector2? offset = null)
    {
        offset ??= Vector2.zero;
        playerTargetId ??= parentId;
        playerSkinId ??= parentId;

        PlayerControl target = PlayerFromPlayerId((byte)playerTargetId);
        PlayerControl targetSkin = PlayerFromPlayerId((byte)playerSkinId);
        DeadBody deadBody = UnityEngine.Object.Instantiate(GameManager.Instance.deadBodyPrefab[0]);
        deadBody.enabled = false;
        deadBody.ParentId = parentId;
        deadBody.bodyRenderers.ToList().ForEach(targetSkin.SetPlayerMaterialColors);
        deadBody.bodyRenderers.ToList().ForEach(body => body.flipX = !target.MyPhysics.FlipX);
        targetSkin.SetPlayerMaterialColors(deadBody.bloodSplatter);
        Vector3 vector = (Vector3)pos + (Vector3)offset;
        vector.z = vector.y / 1000f;
        deadBody.transform.position = vector;
        deadBody.name = $"{target.Data.PlayerName}Body({Main.AllDeadBodys.Where(b => b.ParentId == deadBody.ParentId).ToArray().Length})";
        deadBody.SetObjHash(GetHashUInt16($"{parentId}+{playerTargetId}+{playerSkinId}"));
        return deadBody;
    }

    // Sets the outline for a dead body, optionally showing or hiding it and adjusting the outline color
    /// <summary>
    /// Adjusts the outline of the dead body by enabling or disabling the outline effect and changing its color. The method 
    /// iterates through the body's renderers to apply the changes to each sprite. The outline effect can be toggled based on 
    /// the "show" parameter, and the color of the outline can be customized.
    /// </summary>
    /// <param name="body">The dead body to modify.</param>
    /// <param name="show">Flag to determine if the outline should be shown or hidden.</param>
    /// <param name="color">The color of the outline.</param>
    internal static void SetOutline(this DeadBody body, bool show, Color color)
    {
        if (body == null) return;

        foreach (var sprite in body.bodyRenderers)
        {
            float spriteAlpha = sprite.color.a;
            sprite.material.SetFloat("_Outline", show ? 1 : 0);
            Color outlineColor = new Color(color.r, color.g, color.b, spriteAlpha);
            sprite.material.SetColor("_OutlineColor", outlineColor);
            sprite.material.SetColor("_AddColor", show ? outlineColor : Color.clear);
        }
    }

    // Removes a dead body from the game
    /// <summary>
    /// Removes the specified dead body from the game by destroying the object and cleaning up related references, 
    /// including any names that may be associated with the body.
    /// </summary>
    /// <param name="body">The dead body to remove.</param>
    internal static void Remove(this DeadBody body)
    {
        body.DestroyObj();
        DirtyAllNames();
    }

    // Synchronizes the settings change notification across the network
    /// <summary>
    /// This method synchronizes the settings change notification across the network, updating any existing notification or 
    /// creating a new one if necessary. The message is updated or a new notification is created to reflect the changes. 
    /// A sound can also be played to notify players.
    /// </summary>
    /// <param name="Id">The ID of the setting being changed.</param>
    /// <param name="text">The message text describing the change.</param>
    /// <param name="playSound">Flag to determine if a sound should be played during the notification.</param>
    internal static void SettingsChangeNotifier(int Id, string text, bool playSound = true)
    {
        var Notifier = HudManager.Instance.Notifier;
        if (Notifier.lastMessageKey == Id && Notifier.activeMessages.Count > 0)
        {
            Notifier.activeMessages[Notifier.activeMessages.Count - 1].UpdateMessage(text);
        }
        else
        {
            Notifier.lastMessageKey = Id;
            LobbyNotificationMessage newMessage = UnityEngine.Object.Instantiate(Notifier.notificationMessageOrigin, Vector3.zero, Quaternion.identity, Notifier.transform);
            newMessage.transform.localPosition = new Vector3(0f, 0f, -2f);
            newMessage.SetUp(text, Notifier.settingsChangeSprite, Notifier.settingsChangeColor, (Action)(() =>
            {
                Notifier.OnMessageDestroy(newMessage);
            }));
            Notifier.ShiftMessages();
            Notifier.AddMessageToQueue(newMessage);
        }
        if (playSound)
        {
            SoundManager.Instance.PlaySoundImmediate(Notifier.settingsChangeSound, false, 1f, 1f, null);
        }
    }

    /// <summary>
    /// Generates a hash string (first 5 and last 4 characters) using SHA256 from the input string.
    /// </summary>
    /// <param name="str">The input string to hash.</param>
    /// <returns>A shortened hash string.</returns>
    internal static string GetHashStr(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";

        using SHA256 sha256 = SHA256.Create();
        byte[] sha256Bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(str));
        string sha256Hash = BitConverter.ToString(sha256Bytes).Replace("-", "").ToLower();
        return sha256Hash.Substring(0, 5) + sha256Hash.Substring(sha256Hash.Length - 4);
    }

    /// <summary>
    /// Generates a hash value as a ushort (16-bit unsigned integer) from the input string using SHA256.
    /// </summary>
    /// <param name="input">The input string to hash.</param>
    /// <returns>A ushort hash value.</returns>
    internal static ushort GetHashUInt16(string input)
    {
        if (string.IsNullOrEmpty(input)) return 0;
        return BitConverter.ToUInt16(SHA256.HashData(Encoding.UTF8.GetBytes(input)), 0);
    }

    /// <summary>
    /// Gets the formatted custom role name and color as a string with optional large text size.
    /// </summary>
    /// <param name="role">The role to retrieve the name and color for.</param>
    /// <param name="bigText">Optional flag to increase text size.</param>
    /// <returns>A formatted string with the role name and color.</returns>
    internal static string GetCustomRoleNameAndColor(RoleClassTypes role, bool bigText = false)
    {
        if (!bigText)
        {
            return $"<{GetCustomRoleColorHex(role)}>{GetCustomRoleName(role)}</color>";
        }
        else
        {
            return $"<size=125%><{GetCustomRoleColorHex(role)}>{GetCustomRoleName(role)}</color></size>";
        }
    }

    /// <summary>
    /// Retrieves the custom role class based on the specified role type.
    /// </summary>
    /// <param name="role">The role type to retrieve the class for.</param>
    /// <returns>The custom role class associated with the role type, if found.</returns>
    internal static RoleClass? GetCustomRoleClass(RoleClassTypes role) => CustomRoleManager.RolePrefabs.FirstOrDefault(r => r.RoleType == role);

    /// <summary>
    /// Retrieves the custom role name based on the specified role type.
    /// </summary>
    /// <param name="role">The role type to retrieve the name for.</param>
    /// <returns>The name of the custom role.</returns>
    internal static string GetCustomRoleName(RoleClassTypes role) => CustomRoleManager.RolePrefabs.FirstOrDefault(r => r.RoleType == role).RoleName;

    /// <summary>
    /// Retrieves the hexadecimal color of the custom role based on the specified role type.
    /// </summary>
    /// <param name="role">The role type to retrieve the color for.</param>
    /// <returns>The hexadecimal color of the custom role.</returns>
    internal static string GetCustomRoleColorHex(RoleClassTypes role) => CustomRoleManager.RolePrefabs.FirstOrDefault(r => r.RoleType == role).RoleColorHex;

    /// <summary>
    /// Converts the hexadecimal color of the custom role to a Color32 object.
    /// </summary>
    /// <param name="role">The role type to convert the color for.</param>
    /// <returns>The Color32 object representing the custom role color.</returns>
    internal static Color GetCustomRoleColor(RoleClassTypes role) => Colors.HexToColor(CustomRoleManager.RolePrefabs.FirstOrDefault(r => r.RoleType == role)?.RoleColorHex ?? "#FFFFFF");

    /// <summary>
    /// Retrieves the team color for the specified role class team.
    /// </summary>
    /// <param name="roleTeam">The role class team to retrieve the color for.</param>
    /// <returns>The hexadecimal color for the team.</returns>
    internal static string GetCustomRoleTeamColorHex(RoleClassTeam roleTeam) => Colors.Color32ToHex(GetCustomRoleTeamColor(roleTeam));

    /// <summary>
    /// Converts the hexadecimal team color for the specified role class team to a Color32 object.
    /// </summary>
    /// <param name="roleTeam">The role class team to convert the color for.</param>
    /// <returns>The Color32 object representing the team color.</returns>
    internal static Color GetCustomRoleTeamColor(RoleClassTeam roleTeam)
    {
        switch (roleTeam)
        {
            case RoleClassTeam.Crewmate:
                return Colors.CrewmateBlue;
            case RoleClassTeam.Impostor:
                return Colors.ImpostorRed;
            case RoleClassTeam.Neutral:
                return Colors.NeutralGray;
            case RoleClassTeam.Apocalypse:
                return Colors.ApocalypseGray;
            default:
                return Color.white;
        }
    }

    /// <summary>
    /// Retrieves the name of the custom role team.
    /// </summary>
    /// <param name="roleTeam">The role class team to retrieve the name for.</param>
    /// <returns>The translated name of the role team.</returns>
    internal static string GetCustomRoleTeamName(RoleClassTeam roleTeam) => Translator.GetString($"Role.Team.{Enum.GetName(roleTeam)}");

    /// <summary>
    /// Retrieves the name of the custom role category.
    /// </summary>
    /// <param name="roleCategory">The role class category to retrieve the name for.</param>
    /// <returns>The translated name of the role category.</returns>
    internal static string GetCustomRoleCategoryName(RoleClassCategory roleCategory) => Translator.GetString($"Role.Category.{Enum.GetName(roleCategory)}");

    /// <summary>
    /// Retrieves the custom role information based on the role type, with an option for more detailed information.
    /// </summary>
    /// <param name="role">The role type to retrieve the information for.</param>
    /// <param name="longInfo">Optional flag to retrieve detailed information.</param>
    /// <returns>The custom role information string.</returns>
    internal static string GetCustomRoleInfo(RoleClassTypes role, bool longInfo = false)
    {
        string str;
        if (!longInfo)
        {
            str = Translator.GetString($"Role.{Enum.GetName(typeof(RoleClassTypes), role)}.Info");
            var roleBehavior = PlayerControl.LocalPlayer?.ExtendedData()?.RoleInfo.AllRoles.FirstOrDefault(r => r.RoleType == role);
            roleBehavior ??= GetCustomRoleClass(role);
            roleBehavior?.FormatRoleInfo(ref str, longInfo);
            return str;
        }
        else
        {
            str = Translator.GetString($"Role.{Enum.GetName(typeof(RoleClassTypes), role)}.LongInfo");
            var roleBehavior = PlayerControl.LocalPlayer?.ExtendedData()?.RoleInfo.AllRoles.FirstOrDefault(r => r.RoleType == role);
            roleBehavior ??= GetCustomRoleClass(role);
            roleBehavior?.FormatRoleInfo(ref str, longInfo);
            return str;
        }
    }

    /// <summary>
    /// Removes HTML tags from the input string using a template-based method.
    /// </summary>
    /// <param name="str">The input string with HTML tags to remove.</param>
    /// <returns>The cleaned string without HTML tags.</returns>
    internal static string RemoveHtmlTagsTemplate(string str) => Regex.Replace(str, "", "");

    /// <summary>
    /// Removes HTML tags (like <div>, <b>, etc.) and custom tags (like {tag}) from the input string.
    /// </summary>
    /// <param name="text">The input string to clean from HTML and custom tags.</param>
    /// <returns>The raw string with tags removed.</returns>
    internal static string RemoveHtmlText(string text)
    {
        text = Regex.Replace(text, "<[^>]*>", "");
        text = Regex.Replace(text, "{[^}]*}", "");
        text = text.Replace("\n", " ").Replace("\r", " ");
        text = text.Trim();

        return text;
    }

    /// <summary>
    /// Removes HTML size tags (like <size=100%>) and custom tags (like {tag}) from the input string.
    /// </summary>
    /// <param name="text">The input string to clean from size HTML tags and custom tags.</param>
    /// <returns>The raw string with size tags removed.</returns>
    internal static string RemoveSizeHtmlText(string text)
    {
        text = Regex.Replace(text, "<size=[^>]*>", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "</size>", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "{[^}]*}", "");
        text = text.Replace("\n", " ").Replace("\r", " ").Trim();

        return text;
    }

    /// <summary>
    /// Checks if the input string contains any HTML or custom tags.
    /// </summary>
    /// <param name="text">The input string to check for HTML or custom tags.</param>
    /// <returns>True if the string contains HTML or custom tags, otherwise false.</returns>
    internal static bool IsHtmlText(string text)
    {
        if (Regex.IsMatch(text, "<[^>]*>"))
        {
            return true;
        }
        if (Regex.IsMatch(text, "{[^}]*}"))
        {
            return true;
        }
        if (text.Contains("\n") || text.Contains("\r"))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Formats a StringBuilder by inserting " + " or " - " between parts, based on the input string content.
    /// </summary>
    /// <param name="source">The source StringBuilder to format.</param>
    /// <returns>A formatted StringBuilder.</returns>
    internal static StringBuilder FormatStringBuilder(StringBuilder source)
    {
        var sb = new StringBuilder();
        if (source.Length > 0)
        {
            string text = source.ToString();
            bool isPlus = text.Contains("+++");
            string[] parts;

            if (isPlus)
            {
                parts = text.Split(new[] { "+++" }, StringSplitOptions.None);
            }
            else
            {
                parts = text.Split(new[] { "---" }, StringSplitOptions.None);
            }

            for (int i = 0; i < parts.Length; i++)
            {
                if (!string.IsNullOrEmpty(parts[i]))
                {
                    sb.Append(parts[i]);

                    if (i != parts.Length - 2)
                    {
                        sb.Append(isPlus ? " + " : " - ");
                    }
                }
            }
        }

        return sb;
    }

    /// <summary>
    /// Disconnects the player's account from the online service and updates the UI accordingly.
    /// </summary>
    /// <param name="apiError">Whether an API error occurred, affecting the popup message.</param>
    internal static void DisconnectAccountFromOnline(bool apiError = false)
    {
        if (GameState.IsInGame)
        {
            AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
        }

        DataManager.Player.Account.LoginStatus = EOSManager.AccountLoginStatus.Offline;
        DataManager.Player.Save();
        AccountManager.Instance?.accountTab?.signInStatusComponent?.friendsButton?.SetActive(false);
        var MainMenu = UnityEngine.Object.FindAnyObjectByType<MainMenuManager>();
        if (MainMenu != null)
        {
            MainMenu.PlayOnlineButton.SetButtonEnableState(false);
        }

        if (apiError)
        {
            ShowPopUp(Translator.GetString("DataBaseConnect.InitFailure"), true);
        }
    }

    /// <summary>
    /// Disconnects the client from the game with a specified reason and optionally shows a reason popup.
    /// </summary>
    /// <param name="reason">The reason for the disconnection.</param>
    /// <param name="showReason">Whether to display the reason to the user.</param>
    internal static void DisconnectSelf(string reason, bool showReason = true)
    {
        AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
        _ = new LateTask(() =>
        {
            SceneChanger.ChangeScene("MainMenu");
            if (showReason)
            {
                _ = new LateTask(() =>
                {
                    var lines = "<color=#ebbd34>----------------------------------------------------------------------------------------------</color>";
                    ShowPopUp($"{lines}\n\n\n<size=150%>{reason}</size>\n\n\n{lines}");
                }, 0.1f, "DisconnectSelf 2");
            }
        }, 0.2f, "DisconnectSelf 1");
    }

    /// <summary>
    /// Displays a popup with a disconnect message.
    /// </summary>
    /// <param name="text">The text to display in the popup.</param>
    /// <param name="enableWordWrapping">Whether to enable word wrapping for the text.</param>
    internal static void ShowPopUp(string text, bool enableWordWrapping = false)
    {
        DisconnectPopup.Instance.gameObject.SetActive(true);
        DisconnectPopup.Instance._textArea.enableWordWrapping = enableWordWrapping;
        DisconnectPopup.Instance._textArea.text = text;
    }

    internal static IEnumerator LoadAudioClip(string clipName, Action<AudioClip> callback)
    {
        string path = $"TheBetterRoles.Resources.Sounds.{clipName}";
        Assembly assembly = Assembly.GetExecutingAssembly();
        using Stream stream = assembly.GetManifestResourceStream(path);
        if (stream == null)
        {
            Logger.Error("Error: Could not find resource: " + path);
            yield break;
        }

        byte[] audioBytes = new byte[stream.Length];
        stream.Read(audioBytes, 0, audioBytes.Length);

        if (!TryParseWAV(audioBytes, out var samples, out var sampleRate, out var channels))
        {
            Logger.Error("Failed to parse WAV file.");
            yield break;
        }

        int trimCount = 200;
        if (samples.Length > trimCount)
        {
            Array.Resize(ref samples, samples.Length - trimCount);
        }

        AudioClip clip = AudioClip.Create(clipName, samples.Length / channels, channels, sampleRate, false);
        clip.SetData(samples, 0);
        clip.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;

        callback?.Invoke(clip);
    }

    // Audacity settings
    // File → Export
    // Save as type → WAV(Microsoft) signed 16-bit PCM
    // Encoding → Signed 16-bit PCM
    // Sample rate → 48000 Hz
    // Channels → Stereo(2 channels)
    private static bool TryParseWAV(byte[] wavFile, out float[] samples, out int sampleRate, out int channels)
    {
        samples = null;
        sampleRate = 0;
        channels = 0;

        try
        {
            // WAV Header Offsets
            int sampleRateOffset = 24; // Sample Rate at byte 24
            int channelsOffset = 22;   // Number of Channels at byte 22
            int dataOffset = 44;       // PCM Data starts here

            // Read Sample Rate and Channel Count
            sampleRate = BitConverter.ToInt32(wavFile, sampleRateOffset);
            channels = BitConverter.ToInt16(wavFile, channelsOffset);

            // Ensure file is valid
            if (wavFile.Length <= dataOffset)
            {
                Logger.Error("Invalid WAV file: too small.");
                return false;
            }

            // Calculate usable audio data size
            int dataSize = wavFile.Length - dataOffset;
            if (dataSize % 2 != 0)
            {
                Logger.Warning("WAV file size is misaligned. Trimming last byte.");
                dataSize--; // Ensure an even byte count
            }

            int sampleCount = dataSize / 2; // 16-bit PCM = 2 bytes per sample
            samples = new float[sampleCount];

            // Convert 16-bit PCM samples to normalized float values
            for (int i = 0, offset = dataOffset; i < sampleCount; i++, offset += 2)
            {
                short sample = BitConverter.ToInt16(wavFile, offset); // Read as signed 16-bit
                samples[i] = sample / 32768f; // Normalize to float range [-1,1]
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Error parsing WAV file: " + ex);
            return false;
        }
    }

    internal static Dictionary<string, Sprite> CachedSprites = [];

    /// <summary>
    /// Loads a texture from disk by reading a file at the specified path.
    /// </summary>
    /// <param name="path">The file path of the texture to load.</param>
    /// <returns>A Texture2D object if the texture was successfully loaded, or null if it failed.</returns>
    internal static Texture2D? loadTextureFromDisk(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                byte[] byteTexture = File.ReadAllBytes(path);

                bool isLoaded = texture.LoadImage(byteTexture, false);

                if (isLoaded)
                    return texture;
                else
                    Logger.Error("Failed to load image data into texture.");
            }
            else
            {
                Logger.Error("File does not exist: " + path);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Exception while loading texture: " + ex);
        }
        return null;
    }

    /// <summary>
    /// Loads a sprite from a given path, optionally specifying pixels per unit.
    /// Caches the sprite for future use.
    /// </summary>
    /// <param name="path">The file path of the texture to create the sprite from.</param>
    /// <param name="pixelsPerUnit">The number of pixels per unit for the sprite.</param>
    /// <returns>A Sprite object if successful, or null if it fails to load the sprite.</returns>
    internal static Sprite? LoadSprite(string path, float pixelsPerUnit = 1f)
    {
        try
        {
            if (CachedSprites.TryGetValue(path + pixelsPerUnit, out var sprite))
                return sprite;

            var texture = LoadTextureFromResources(path);
            if (texture == null)
                return null;

            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;

            return CachedSprites[path + pixelsPerUnit] = sprite;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            return null;
        }
    }

    /// <summary>
    /// Loads a texture from embedded resources in the application's assembly.
    /// </summary>
    /// <param name="path">The path to the texture resource.</param>
    /// <returns>A Texture2D object if the texture was loaded successfully, or null if it failed.</returns>
    internal static Texture2D? LoadTextureFromResources(string path)
    {
        try
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            if (stream == null)
                return null;

            var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                if (!texture.LoadImage(ms.ToArray(), false))
                    return null;
            }

            return texture;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            return null;
        }
    }

    /// <summary>
    /// Loads a sprite for a cosmetic item from disk, given its parent path, folder, and file name.
    /// Throws an exception if the file does not exist.
    /// </summary>
    /// <param name="parentPath">The parent directory containing the cosmetic data.</param>
    /// <param name="folderName">The folder containing the cosmetic sprites.</param>
    /// <param name="fileName">The file name of the sprite to load.</param>
    /// <returns>A Sprite object if successful, or null if loading the sprite fails.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist on disk.</exception>
    internal static Sprite? LoadCosmeticSprite(string parentPath, string folderName, string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return null;
        }

        if (!File.Exists(Path.Combine(parentPath, folderName, "sprites", fileName)))
        {
            throw new FileNotFoundException($"{Path.Combine(parentPath, folderName, "sprites", fileName)} not downloaded yet!");
        }

        var texture = loadTextureFromDisk(Path.Combine(parentPath, folderName, "sprites", fileName));
        if (texture == null) return null;
        var sprite = Sprite.Create(texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.53f, 0.575f),
            texture.width * 0.375f);
        if (sprite == null) return null;
        texture.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
        sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
        return sprite;
    }

    /// <summary>
    /// Replaces the main texture of a given SpriteRenderer with a new texture.
    /// This method creates a MaterialPropertyBlock to modify the texture without altering the actual material.
    /// </summary>
    internal static void ReplaceTexture(SpriteRenderer spriteRenderer, Texture2D newTexture)
    {
        if (spriteRenderer != null)
        {
            MaterialPropertyBlock block = new();
            spriteRenderer.GetPropertyBlock(block);
            block.SetTexture("_MainTex", newTexture);
            spriteRenderer.SetPropertyBlock(block);
        }
    }

    /// <summary>
    /// Retrieves the platform name for a player based on their platform data.
    /// Optionally includes the platform tag (e.g., "PC", "Mobile", "Console") in the returned string.
    /// </summary>
    /// <param name="player">The player for whom to retrieve the platform name.</param>
    /// <param name="useTag">Whether to include the platform tag (e.g., "PC", "Mobile") in the return value.</param>
    /// <returns>A string representing the platform name, possibly prefixed by the platform tag.</returns>
    internal static string GetPlatformName(PlayerControl player, bool useTag = false)
    {
        if (player == null) return string.Empty;
        if (player.GetClient() == null) return string.Empty;

        string PlatformName = string.Empty;
        string Tag = string.Empty;

        Platforms platform = player.GetClient().PlatformData.Platform;

        switch (platform)
        {
            case Platforms.StandaloneSteamPC:
                PlatformName = "Steam";
                Tag = "PC";
                break;
            case Platforms.StandaloneEpicPC:
                PlatformName = "Epic Games";
                Tag = "PC";
                break;
            case Platforms.StandaloneWin10:
                PlatformName = "Microsoft Store";
                Tag = "PC";
                break;
            case Platforms.StandaloneMac:
                PlatformName = "Mac OS";
                Tag = "PC";
                break;
            case Platforms.StandaloneItch:
                PlatformName = "Itch.io";
                Tag = "PC";
                break;
            case Platforms.Xbox:
                PlatformName = "Xbox";
                Tag = "Console";
                break;
            case Platforms.Playstation:
                PlatformName = "Playstation";
                Tag = "Console";
                break;
            case Platforms.Switch:
                PlatformName = "Switch";
                Tag = "Console";
                break;
            case Platforms.Android:
                PlatformName = "Android";
                Tag = "Mobile";
                break;
            case Platforms.IPhone:
                PlatformName = "IPhone";
                Tag = "Mobile";
                break;
            case Platforms p when !Enum.IsDefined(p):
            case Platforms.Unknown:
                PlatformName = "Unknown";
                useTag = false;
                break;
            default:
                PlatformName = "None";
                useTag = false;
                break;
        }

        if (useTag == false)
            return PlatformName;

        return $"{Tag}: {PlatformName}";
    }

    /// <summary>
    /// Retrieves the platform name based on a specific platform enum value.
    /// Optionally includes the platform tag (e.g., "PC", "Mobile", "Console") in the returned string.
    /// </summary>
    /// <param name="platform">The platform enum value for which to retrieve the platform name.</param>
    /// <param name="useTag">Whether to include the platform tag (e.g., "PC", "Mobile") in the return value.</param>
    /// <returns>A string representing the platform name, possibly prefixed by the platform tag.</returns>
    internal static string GetPlatformName(Platforms platform, bool useTag = false)
    {
        string Tag = string.Empty;

        string PlatformName;
        switch (platform)
        {
            case Platforms.StandaloneSteamPC:
                PlatformName = "Steam";
                Tag = "PC";
                break;
            case Platforms.StandaloneEpicPC:
                PlatformName = "Epic Games";
                Tag = "PC";
                break;
            case Platforms.StandaloneWin10:
                PlatformName = "Microsoft Store";
                Tag = "PC";
                break;
            case Platforms.StandaloneMac:
                PlatformName = "Mac OS";
                Tag = "PC";
                break;
            case Platforms.StandaloneItch:
                PlatformName = "Itch.io";
                Tag = "PC";
                break;
            case Platforms.Xbox:
                PlatformName = "Xbox";
                Tag = "Console";
                break;
            case Platforms.Playstation:
                PlatformName = "Playstation";
                Tag = "Console";
                break;
            case Platforms.Switch:
                PlatformName = "Switch";
                Tag = "Console";
                break;
            case Platforms.Android:
                PlatformName = "Android";
                Tag = "Mobile";
                break;
            case Platforms.IPhone:
                PlatformName = "IPhone";
                Tag = "Mobile";
                break;
            case Platforms.Unknown:
                PlatformName = "None";
                break;
            default:
                return string.Empty;
        }

        if (useTag == false)
            return PlatformName;

        return $"{Tag}: {PlatformName}";
    }
}