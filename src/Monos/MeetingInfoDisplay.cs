using BepInEx.Unity.IL2CPP.Utils;
using System.Text;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using TheBetterRoles.Roles;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Monos;

internal class MeetingInfoDisplay : PlayerInfoDisplay
{
    private PlayerVoteArea? _pva;
    private Vector3 _namePos;
    private Vector3 _infoPos;
    private Vector3 _TopPos;

    private readonly StringBuilder _sbTag = new(256);
    private readonly StringBuilder _sbInfo = new(256);
    private string _lastInfoText = "", _lastTopText = "";
    private int _lastUpdateFrame;
    private const int UPDATE_COOLDOWN = 5;

    private CachedTranslations _cachedTranslations = new();

    // Cached translations
    private class CachedTranslations
    {
        internal readonly string DisconnectLeft = Translator.GetString("DisconnectReasonMeeting.Left");
        internal readonly string DisconnectBanned = Translator.GetString("DisconnectReasonMeeting.Banned");
        internal readonly string DisconnectKicked = Translator.GetString("DisconnectReasonMeeting.Kicked");
        internal readonly string DisconnectDefault = Translator.GetString("DisconnectReasonMeeting.Disconnect");
    }

    internal void Init(PlayerControl? player, PlayerVoteArea pva)
    {
        _player = player;
        _pva = pva;
        this.StartCoroutine(CoGetData());

        _nameText = pva.NameText;
        _infoText = InstantiatePlayerInfoText("InfoText_Info_TMP", new Vector3(0f, 0.28f), pva.transform);
        _topText = InstantiatePlayerInfoText("InfoText_T_TMP", new Vector3(0f, 0.15f), pva.transform);
        _infoText.fontSize = 1.3f;
        _topText.fontSize = 1.3f;
        _namePos = _nameText.transform.localPosition - new Vector3(0f, 0.02f, 0f);
        _infoPos = _infoText.transform.localPosition;
        _TopPos = _topText.transform.localPosition;

        var PlayerLevel = pva.transform.Find("PlayerLevel");
        PlayerLevel.localPosition = new Vector3(PlayerLevel.localPosition.x, PlayerLevel.localPosition.y, -2f);
        var LevelDisplay = Instantiate(PlayerLevel, pva.transform);
        LevelDisplay.transform.SetSiblingIndex(pva.transform.Find("PlayerLevel").GetSiblingIndex() + 1);
        LevelDisplay.gameObject.name = "PlayerId";
        LevelDisplay.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 1f, 1f);
        var IdLabel = LevelDisplay.transform.Find("LevelLabel");
        var IdNumber = LevelDisplay.transform.Find("LevelNumber");
        IdLabel.gameObject.DestroyTextTranslators();
        IdLabel.GetComponent<TextMeshPro>().text = "ID";
        IdNumber.GetComponent<TextMeshPro>().text = pva.TargetPlayerId.ToString();
        IdLabel.name = "IdLabel";
        IdNumber.name = "IdNumber";
        PlayerLevel.transform.position += new Vector3(0.23f, 0f);
    }

    protected override void LateUpdate()
    {
        if (Time.frameCount - _lastUpdateFrame < UPDATE_COOLDOWN)
            return;

        if (_pva == null) return;

        _sbTag.Clear();
        _sbInfo.Clear();

        if (_player != null)
        {
            UpdateInfo();
        }
        else
        {
            UpdateDisconnect();
        }

        UpdateTextPositions();
        _pva.ColorBlindName.transform.localPosition = new Vector3(-0.91f, -0.19f, -0.05f);

        _lastUpdateFrame = Time.frameCount;
    }

    private void UpdateInfo()
    {
        if (_player?.Data == null || _player.ExtendedData() == null) return;

        UpdateRoleInfo();

        UpdateTextIfChanged(_infoText, _sbInfo, ref _lastInfoText);
        UpdateTextIfChanged(_topText, _sbTag, ref _lastTopText);

        UpdateTextPositions();
        _pva.NameText.SetText(Utils.FormatPlayerName(_player));
    }

    protected override void UpdateTextPositions()
    {
        bool hasInfoText = !string.IsNullOrEmpty(_infoText?.text);
        bool hasTopText = !string.IsNullOrEmpty(_topText?.text);

        Vector3 textPos;
        if (hasTopText && hasInfoText)
            textPos = new Vector3(_pva.NameText.transform.localPosition.x, -0.045f);
        else
            textPos = new Vector3(_pva.NameText.transform.localPosition.x, 0.015f);

        _pva.NameText.transform.localPosition = textPos;

        if (hasInfoText && hasTopText)
        {
            _nameText.transform.localPosition = _namePos + new Vector3(0f, -0.1f, 0f);
            _infoText.transform.localPosition = _infoPos + new Vector3(0f, -0.1f, 0f);
            _topText.transform.localPosition = _TopPos + new Vector3(0f, -0.1f, 0f);
        }
        else if (hasInfoText || hasTopText)
        {
            _nameText.transform.localPosition = _namePos;
            _infoText.transform.localPosition = _TopPos;
            _topText.transform.localPosition = _TopPos;
        }
        else
        {
            _nameText.transform.localPosition = _namePos;
            _infoText.transform.localPosition = _infoPos;
            _topText.transform.localPosition = _TopPos;
        }
    }

    private void UpdateRoleInfo()
    {
        if (_playerData == null) return;
        if (_player == null || _extendedPlayerData == null) return;

        string hexColor = "";

        bool hideInfo = PlayerControl.LocalPlayer.CheckAnyRoles(role => role.HidePlayerInfoOther(_player));

        bool canRevealRole = _player.IsLocalPlayer() || !PlayerControl.LocalPlayer.IsAlive(true) || _player.IsImpostorTeammate()
            || PlayerControl.LocalPlayer.CheckAnyRoles(role => role.RevealPlayerRole(_player));

        if (canRevealRole && !hideInfo && _player.Role()?.ShowRoleAboveName == true)
        {
            hexColor = _player.Role()?.RoleColorHex ?? "#FFFFFF";
            _sbTag.Append($"{_player.Role()?.RoleNameAndAbilityAmount}{_player.FormatTasksToText()}---");
        }

        bool canRevealAddons = _player.IsLocalPlayer() || !PlayerControl.LocalPlayer.IsAlive(true) || _player.IsImpostorTeammate()
            || PlayerControl.LocalPlayer.CheckAnyRoles(role => role.RevealPlayerAddons(_player));

        if (canRevealAddons && !hideInfo)
        {
            foreach (var addon in _extendedPlayerData.RoleInfo.Addons)
            {
                if (!addon.ShowRoleAboveName) continue;
                _sbTag.Append($"{addon.RoleNameAndAbilityAmount.Size(55f)}+++");
            }
        }

        bool canRevealDeath = (_player.IsLocalPlayer() && !_player.IsAlive() || !PlayerControl.LocalPlayer.IsAlive(true) && !PlayerControl.LocalPlayer.IsGhostRole() ||
            PlayerControl.LocalPlayer.CheckAnyRoles(role => role.RevealPlayerDeath(_player))) && !_player.IsAlive(true);

        if (canRevealDeath && !hideInfo)
        {
            var num = _sbTag.Length - 3;
            if (num > 0)
            {
                _sbTag.Remove(num, 3);
            }
            _sbTag.Append($"{_player.FormatDeathReason()}---");
        }

        if (!string.IsNullOrEmpty(_extendedPlayerData.NameColor))
        {
            hexColor = _extendedPlayerData.NameColor;
        }

        if (!string.IsNullOrEmpty(hexColor))
        {
            var color = Colors.HexToColor(hexColor);
            _pva.NameText.color = new Color(color.r, color.g, color.b, _pva.NameText.color.a);
        }
        else
        {
            _pva.NameText.color = new Color(1f, 1f, 1f, _pva.NameText.color.a);
        }
    }

    private void UpdateDisconnect()
    {
        string disconnectText = GetDisconnectText();

        if (disconnectText != _lastInfoText)
        {
            _infoText?.SetText($"<color=#6b6b6b>{disconnectText}</color>");
            _lastInfoText = disconnectText;
        }

        if (_lastTopText != string.Empty)
        {
            _topText?.SetText("");
            _lastTopText = string.Empty;
        }

        _pva.transform.Find("votePlayerBase")?.gameObject.SetActive(false);
        _pva.transform.Find("deadX_border")?.gameObject.SetActive(false);
        _pva.ClearForResults();
        _pva.SetDisabled();
    }

    private string GetDisconnectText()
    {
        var playerData = GameData.Instance.GetPlayerById(_pva.TargetPlayerId);
        var data = playerData?.ExtendedData();

        return data?.DisconnectReason switch
        {
            DisconnectReasons.ExitGame => _cachedTranslations.DisconnectLeft,
            DisconnectReasons.Kicked => _cachedTranslations.DisconnectKicked,
            DisconnectReasons.Banned => _cachedTranslations.DisconnectBanned,
            _ => _cachedTranslations.DisconnectDefault
        };
    }
}