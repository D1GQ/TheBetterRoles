using AmongUs.Data;
using InnerNet;
using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using TheBetterRoles.Roles;
using UnityEngine;

namespace TheBetterRoles.Helpers;

public static class Utils
{
    // Get player by client id
    public static ClientData? ClientFromClientId(int clientId)
        => AmongUsClient.Instance.allClients.ToArray().FirstOrDefault(cd => cd.Id == clientId);

    // Get player data from player id
    public static NetworkedPlayerInfo? PlayerDataFromPlayerId(int playerId)
        => GameData.Instance.AllPlayers.ToArray().FirstOrDefault(data => data.PlayerId == playerId);

    // Get player data from client id
    public static NetworkedPlayerInfo? PlayerDataFromClientId(int clientId)
        => GameData.Instance.AllPlayers.ToArray().FirstOrDefault(data => data.ClientId == clientId);

    // Get player data from friend code
    public static NetworkedPlayerInfo? PlayerDataFromFriendCode(string friendCode)
        => GameData.Instance.AllPlayers.ToArray().FirstOrDefault(data => data.FriendCode == friendCode);

    // Get player from player id
    public static PlayerControl? PlayerFromPlayerId(int playerId)
        => Main.AllPlayerControls.FirstOrDefault(player => player.PlayerId == playerId);

    // Get player from client id
    public static PlayerControl? PlayerFromClientId(int clientId)
        => Main.AllPlayerControls.FirstOrDefault(player => player.GetClientId() == clientId);

    // Get player from net id
    public static PlayerControl? PlayerFromNetId(uint netId)
        => Main.AllPlayerControls.FirstOrDefault(player => player.NetId == netId);

    public static void DirtyAllNames()
    {
        foreach (var player in Main.AllPlayerControls)
        {
            var betterData = player.ExtendedData();
            if (betterData != null)
            {
                betterData.DirtyName = true;
            }
        }
    }

    // Set player name
    public static string FormatPlayerName(NetworkedPlayerInfo player, bool bypassDisguise = false)
    {
        if (player == null || !(player.ExtendedData()?.RoleInfo?.RoleAssigned == true))
            return string.Empty;

        var playerData = player.ExtendedData();
        var roleInfo = playerData.RoleInfo;
        var role = roleInfo?.Role;
        NetworkedPlayerInfo target = player;

        if (!bypassDisguise && role?.IsDisguised == true && role.DisguisedTargetId >= 0)
        {
            foreach (var data in GameData.Instance.AllPlayers)
            {
                if (data.PlayerId == role.DisguisedTargetId && data.Object != null)
                {
                    target = data;
                    break;
                }
            }
        }

        StringBuilder nameBuilder = new StringBuilder();
        string? nameColor = target.ExtendedData().NameColor;

        if (string.IsNullOrEmpty(nameColor))
        {
            nameBuilder.Append(target.PlayerName);
        }
        else
        {
            nameBuilder.Append($"<{nameColor}>{target.PlayerName}</color>");
        }

        nameBuilder.Append(CustomRoleManager.GetRoleMarks(target.Object));
        return nameBuilder.ToString();
    }

    public static string FormatTasksToText(this PlayerControl player)
    {
        if (player.RoleChecksAny(role => role.HasTask, false) || player.RoleChecksAny(role => role.HasSelfTask, false))
        {
            return $" <#ffff2b>({player.Data.Tasks.ToArray().Where(task => task.Complete).Count()}/{player.Data.Tasks.Count})</color>";
        }
        else
        {
            return string.Empty;
        }
    }

    public static string SetColor(this string str, string hexColor)
    {
        return $"<{hexColor}>{str}</color>";
    }

    public static string SetColor(this string str, Color color)
    {
        return SetColor(str, Color32ToHex(color));
    }

    // Add msg to chat
    public static void AddChatPrivate(string text, string overrideName = "", bool setRight = false)
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
            chat.SetChatBubbleName(pooledBubble, data, data.IsDead, false, PlayerNameColor.Get(data), null);
            pooledBubble.SetText(text);
            pooledBubble.AlignChildren();
            chat.AlignAllBubbles();
            pooledBubble.NameText.text = MsgName;
            if (!chat.IsOpenOrOpening && chat.notificationRoutine == null)
            {
                chat.notificationRoutine = chat.StartCoroutine(chat.BounceDot());
            }
            SoundManager.Instance.PlaySound(chat.messageSound, false, 1f, null).pitch = 0.5f + data.PlayerId / 15f;
            ChatPatch.ChatControllerPatch.SetChatPoolTheme(pooledBubble);
        }
        catch (Exception ex)
        {
            chat.chatBubblePool.Reclaim(pooledBubble);
            Logger.Error(ex);
            throw;
        }
    }

    public static IList? createIList(Type myType)
    {
        Type genericListType = typeof(List<>).MakeGenericType(myType);
        return (IList)Activator.CreateInstance(genericListType);
    }

    public static bool SystemTypeIsSabotage(SystemTypes type) => type is SystemTypes.Reactor
                    or SystemTypes.Laboratory
                    or SystemTypes.Comms
                    or SystemTypes.LifeSupp
                    or SystemTypes.MushroomMixupSabotage
                    or SystemTypes.HeliSabotage
                    or SystemTypes.Electrical;
    public static bool SystemTypeIsSabotage(int typeNum) => (SystemTypes)typeNum is SystemTypes.Reactor
                or SystemTypes.Laboratory
                or SystemTypes.Comms
                or SystemTypes.LifeSupp
                or SystemTypes.MushroomMixupSabotage
                or SystemTypes.HeliSabotage
                or SystemTypes.Electrical;

    public static SystemTypes? GetSystemTypes(this ISystemType system)
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

    // Flash screen color with custom fade in, fade out, and total duration (max alpha 50% for transparency)
    private static Coroutine? activeFadeCoroutine;
    public static void FlashScreen(Color color, float fadeInDuration = 0.25f, float fadeOutDuration = 0.25f, float effectDuration = 1f, bool Override = false, bool fullColor = false)
    {
        var hud = DestroyableSingleton<HudManager>.Instance;
        if (hud.FullScreen == null) return;

        var obj = hud.transform.Find("FlashColor_FullScreen")?.gameObject;
        if (obj == null)
        {
            obj = UnityEngine.Object.Instantiate(hud.FullScreen.gameObject, hud.transform);
            obj.name = "FlashColor_FullScreen";
        }

        float totalDuration = fadeInDuration + fadeOutDuration;
        fadeInDuration = Mathf.Clamp(fadeInDuration, 0, effectDuration);
        fadeOutDuration = Mathf.Clamp(fadeOutDuration, 0, effectDuration - fadeInDuration);

        if (Override && activeFadeCoroutine != null)
        {
            hud.StopCoroutine(activeFadeCoroutine);
            obj.SetActive(false);
        }

        activeFadeCoroutine = hud.StartCoroutine(Effects.Lerp(effectDuration, new Action<float>((t) =>
        {
            obj.SetActive(t != 1f);

            float alpha;
            if (t < fadeInDuration / effectDuration)
            {
                alpha = fullColor
                    ? Mathf.Lerp(0, color.a, t / (fadeInDuration / effectDuration))
                    : Mathf.Lerp(0, color.a / 2, t / (fadeInDuration / effectDuration));
            }
            else if (t > (effectDuration - fadeOutDuration) / effectDuration)
            {
                alpha = fullColor
                    ? Mathf.Lerp(color.a, 0, (t - (effectDuration - fadeOutDuration) / effectDuration) / (fadeOutDuration / effectDuration))
                    : Mathf.Lerp(color.a / 2, 0, (t - (effectDuration - fadeOutDuration) / effectDuration) / (fadeOutDuration / effectDuration));
            }
            else
            {
                alpha = fullColor ? color.a : color.a / 2;
            }

            obj.GetComponent<SpriteRenderer>().color = new Color(color.r, color.g, color.b, alpha);
        })));
    }


    // Flash screen hex color with custom fade in, fade out, and total duration (max alpha 50%)
    public static void FlashScreen(string hex, float fadeInDuration = 0.25f, float fadeOutDuration = 0.25f, float effectDuration = 1f, bool Override = false, bool fullColor = false)
    {
        Color color = HexToColor32(hex);
        FlashScreen(color, fadeInDuration, fadeOutDuration, effectDuration, Override, fullColor);
    }

    // Set Out line on vent
    public static void SetOutline(this Vent vent, Color color, bool showOutline, bool showMain)
    {
        if (vent == null) return;
        vent.myRend.material.SetFloat("_Outline", showOutline ? 1 : 0);
        vent.myRend.material.SetColor("_OutlineColor", color);
        vent.myRend.material.SetColor("_AddColor", showMain ? color : Color.clear);
    }

    public static bool IsEnabled(this Vent vent) => vent?.enabled == true;

    public static void SetEnabled(this Vent vent, bool @bool)
    {
        if (vent != null)
        {
            vent.enabled = @bool;

            if (ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Ventilation, out var system))
            {
                VentilationSystem ventilationSystem = system.Cast<VentilationSystem>();
                ventilationSystem?.UpdateVentArrows();
            }
        }
    }

    public static void SetOutline(this DeadBody body, bool show, Color color)
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

    public static string SettingsChangeNotifier(int Id, string info, bool playSound = true)
    {
        var option = TBROptionItem.BetterOptionItems.FirstOrDefault(op => op.Id == Id);
        if (option != null)
        {
            string Name = option.Name ?? "???";
            List<string> names = [Name];
            TBROptionItem tempOption = option;

            while (tempOption.ThisParent != null)
            {
                names.Add(tempOption.ThisParent.Name);
                tempOption = tempOption.ThisParent;
            }

            Name = string.Join("<color=#868686>/</color>", names.AsEnumerable().Reverse());


            string msg = $"<font=\"Barlow-Black SDF\" material=\"Barlow-Black Outline\">{RemoveSizeHtmlText(Name)} " +
                $"<color=#868686><size=85%>{Translator.GetString("BetterSetting.SetTo")}</size></color> {info}";

            SettingsChangeNotifierSync(Id, msg, playSound);

            return msg;
        }

        return "";
    }

    public static void SettingsChangeNotifierSync(int Id, string text, bool playSound = true)
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

    // Get Hash string
    public static string GetHashStr(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";

        using SHA256 sha256 = SHA256.Create();
        byte[] sha256Bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(str));
        string sha256Hash = BitConverter.ToString(sha256Bytes).Replace("-", "").ToLower();
        return sha256Hash.Substring(0, 5) + sha256Hash.Substring(sha256Hash.Length - 4);
    }

    public static ushort GetHashUInt16(string input)
    {
        if (string.IsNullOrEmpty(input)) return 0;

        return (ushort)(BitConverter.ToUInt16(SHA256.HashData(Encoding.UTF8.GetBytes(input)), 0) % 65536);
    }

    public static string GetCustomRoleNameAndColor(CustomRoles role, bool bigText = false)
    {
        if (!bigText)
        {
            return $"<{GetCustomRoleColorHex(role)}>{GetCustomRoleName(role)}</color>";
        }
        else
        {
            return $"<size=150%><{GetCustomRoleColorHex(role)}>{GetCustomRoleName(role)}</color></size>";
        }
    }
    public static CustomRoleBehavior? GetCustomRoleClass(CustomRoles role) => CustomRoleManager.allRoles.FirstOrDefault(r => r.RoleType == role);
    public static string GetCustomRoleName(CustomRoles role) => CustomRoleManager.allRoles.FirstOrDefault(r => r.RoleType == role).RoleName;
    public static string GetCustomRoleColorHex(CustomRoles role) => CustomRoleManager.allRoles.FirstOrDefault(r => r.RoleType == role).RoleColor;
    public static Color GetCustomRoleColor(CustomRoles role) => HexToColor32(CustomRoleManager.allRoles.FirstOrDefault(r => r.RoleType == role).RoleColor);
    public static string GetCustomRoleTeamColor(CustomRoleTeam roleTeam)
    {
        switch (roleTeam)
        {
            case CustomRoleTeam.Crewmate:
                return "#8cffff";
            case CustomRoleTeam.Impostor:
                return "#f00202";
            case CustomRoleTeam.Neutral:
            case CustomRoleTeam.Apocalypse:
                return "#949494";
            default:
                return "#ffffff";
        }
    }
    public static string GetCustomRoleTeamName(CustomRoleTeam roleTeam) => Translator.GetString($"Role.Team.{Enum.GetName(roleTeam)}");
    public static string GetCustomRoleCategoryName(CustomRoleCategory roleCategory) => Translator.GetString($"Role.Category.{Enum.GetName(roleCategory)}");
    public static string GetCustomRoleInfo(CustomRoles role, bool longInfo = false)
    {
        if (!longInfo)
        {
            return Translator.GetString($"Role.{Enum.GetName(typeof(CustomRoles), role)}.Info");
        }
        else
        {
            return Translator.GetString($"Role.{Enum.GetName(typeof(CustomRoles), role)}.LongInfo");
        }
    }
    // Remove Html Tags Template
    public static string RemoveHtmlTagsTemplate(string str) => Regex.Replace(str, "", "");
    // Get raw text
    public static string RemoveHtmlText(string text)
    {
        text = Regex.Replace(text, "<[^>]*>", "");
        text = Regex.Replace(text, "{[^}]*}", "");
        text = text.Replace("\n", " ").Replace("\r", " ");
        text = text.Trim();

        return text;
    }

    public static string RemoveSizeHtmlText(string text)
    {
        text = Regex.Replace(text, "<size=[^>]*>", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "</size>", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "{[^}]*}", "");
        text = text.Replace("\n", " ").Replace("\r", " ").Trim();

        return text;
    }

    public static bool IsHtmlText(string text)
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
    public static string Color32ToHex(Color32 color) => $"#{color.r:X2}{color.g:X2}{color.b:X2}{255:X2}";

    public static Color HexToColor32(string hex)
    {
        if (hex.StartsWith("#"))
        {
            hex = hex.Substring(1);
        }

        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

        return new Color32(r, g, b, 255);
    }

    // Put +++ or --- at the end of each tag
    public static StringBuilder FormatStringBuilder(StringBuilder source)
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

    public static void DisconnectAccountFromOnline(bool apiError = false)
    {
        if (GameState.IsInGame)
        {
            AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
        }

        DataManager.Player.Account.LoginStatus = EOSManager.AccountLoginStatus.Offline;
        DataManager.Player.Save();
        if (apiError)
        {
            ShowPopUp(Translator.GetString("DataBaseConnect.InitFailure"), true);
        }
    }

    // Disconnect client
    public static void DisconnectSelf(string reason, bool showReason = true)
    {
        AmongUsClient.Instance.ExitGame(0);
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
    // Show dc pop up with text
    public static void ShowPopUp(string text, bool enableWordWrapping = false)
    {
        DisconnectPopup.Instance.gameObject.SetActive(true);
        DisconnectPopup.Instance._textArea.enableWordWrapping = enableWordWrapping;
        DisconnectPopup.Instance._textArea.text = text;
    }

    public static AudioClip? loadAudioClipFromResources(string path, string clipName = "UNNAMED_TOR_AUDIO_CLIP")
    {
        // must be "raw (headerless) 2-channel signed 32 bit pcm (le)" (can e.g. use Audacity® to export)
        try
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream? stream = assembly.GetManifestResourceStream(path);
            var byteAudio = new byte[stream.Length];
            _ = stream.Read(byteAudio, 0, (int)stream.Length);
            float[] samples = new float[byteAudio.Length / 4]; // 4 bytes per sample
            int offset;
            for (int i = 0; i < samples.Length; i++)
            {
                offset = i * 4;
                samples[i] = (float)BitConverter.ToInt32(byteAudio, offset) / int.MaxValue;
            }
            int channels = 2;
            int sampleRate = 48000;
            AudioClip audioClip = AudioClip.Create(clipName, samples.Length / 2, channels, sampleRate, false);
            audioClip.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
            audioClip.SetData(samples, 0);
            return audioClip;
        }
        catch
        {
            System.Console.WriteLine("Error loading AudioClip from resources: " + path);
        }
        return null;

        /* Usage example:
        AudioClip exampleClip = Helpers.loadAudioClipFromResources("TheBetterRoles.Resources.exampleClip.raw");
        if (Constants.ShouldPlaySfx()) SoundManager.Instance.PlaySound(exampleClip, false, 0.8f);
        */
    }

    public static Dictionary<string, Sprite> CachedSprites = [];

    public static Texture2D? loadTextureFromDisk(string path)
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

    public static Sprite? LoadSprite(string path, float pixelsPerUnit = 1f)
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

            // Logger.Log($"Successfully loaded sprite from {path}");
            return CachedSprites[path + pixelsPerUnit] = sprite;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            return null;
        }
    }

    public static Texture2D? LoadTextureFromResources(string path)
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

            // Logger.Log($"Successfully loaded texture from {path}");
            return texture;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            return null;
        }
    }

    // Get platform name
    public static string GetPlatformName(PlayerControl player, bool useTag = false)
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

    public static string GetPlatformName(Platforms platform, bool useTag = false)
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