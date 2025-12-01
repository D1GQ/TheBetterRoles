using BepInEx.Unity.IL2CPP.Utils;
using System.Collections;
using System.Text;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using TheBetterRoles.Roles;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Monos;

internal class PlayerInfoDisplay : MonoBehaviour
{
    protected PlayerControl? _player;
    protected NetworkedPlayerInfo? _playerData;
    protected ExtendedPlayerInfo? _extendedPlayerData;

    protected TextMeshPro? _nameText;

    protected TextMeshPro? _infoText;
    protected TextMeshPro? _topText;
    protected TextMeshPro? _bottomText;
    private Vector3 _namePos;
    private Vector3 _infoPos;
    private Vector3 _TopPos;

    private readonly StringBuilder _sbTag = new(256);
    private readonly StringBuilder _sbTagTop = new(256);
    private readonly StringBuilder _sbTagBottom = new(256);
    private string _lastTopText = "", _lastBottomText = "", _lastInfoText = "";
    private int _lastUpdateFrame;
    private const int UPDATE_COOLDOWN = 10;

    internal void Init(PlayerControl player)
    {
        _player = player;
        this.StartCoroutine(CoGetData());

        var nameTextTransform = player.gameObject.transform.Find("Names/NameText_TMP");
        _nameText = nameTextTransform?.GetComponent<TextMeshPro>();

        _infoText = InstantiatePlayerInfoText("InfoText_Info_TMP", new Vector3(0f, 0.25f), nameTextTransform);
        _topText = InstantiatePlayerInfoText("InfoText_T_TMP", new Vector3(0f, 0.15f), nameTextTransform);
        _bottomText = InstantiatePlayerInfoText("InfoText_B_TMP", new Vector3(0f, -0.15f), nameTextTransform);
        _namePos = _nameText.transform.localPosition;
        _infoPos = _infoText.transform.localPosition;
        _TopPos = _topText.transform.localPosition;
        _infoText.fontSize = 1.3f;
        _topText.fontSize = 1.3f;
        _bottomText.fontSize = 1.3f;
    }

    protected IEnumerator CoGetData()
    {
        while (_player?.Data == null)
        {
            yield return null;
        }

        _playerData = _player.Data;
        _extendedPlayerData = _playerData.ExtendedData();
    }

    protected TextMeshPro InstantiatePlayerInfoText(string name, Vector3 positionOffset, Transform parent)
    {
        var newTextObject = Instantiate(_nameText, parent);
        newTextObject.name = name;
        newTextObject.transform.DestroyChildren();
        newTextObject.transform.position += positionOffset;

        var textMesh = newTextObject.GetComponent<TextMeshPro>();
        textMesh.text = string.Empty;
        newTextObject.gameObject.SetActive(true);

        return textMesh;
    }

    private void ResetText()
    {
        _infoText?.SetText(string.Empty);
        _topText?.SetText(string.Empty);
        _bottomText?.SetText(string.Empty);
    }

    protected virtual void LateUpdate()
    {
        if (Time.frameCount - _lastUpdateFrame < UPDATE_COOLDOWN)
            return;

        _sbTag.Clear();
        _sbTagTop.Clear();
        _sbTagBottom.Clear();

        bool isLobbyState = GameState.IsLobby && !GameState.IsFreePlay;

        if (isLobbyState)
        {
            UpdateLobbyInfo();
        }
        else
        {
            UpdatePlayerInfo();
        }

        _player.RawSetName(Utils.FormatPlayerName(_player));
        UpdateTextIfChanged(_topText, _sbTagTop, ref _lastTopText);
        UpdateTextIfChanged(_bottomText, _sbTagBottom, ref _lastBottomText);
        UpdateTextIfChanged(_infoText, _sbTag, ref _lastInfoText);
        UpdateTextPositions();

        UpdateColorBlindTextPosition();
        _nameText.transform.parent.localPosition = new Vector3(0f, 0.8f, -0.5f);

        _lastUpdateFrame = Time.frameCount;
    }

    protected virtual void UpdateTextPositions()
    {
        bool hasInfoText = !string.IsNullOrEmpty(_infoText?.text);
        bool hasTopText = !string.IsNullOrEmpty(_topText?.text);

        if (hasInfoText && hasTopText)
        {
            _nameText.transform.localPosition = _namePos;
            _infoText.transform.localPosition = _infoPos;
            _topText.transform.localPosition = _TopPos;
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

    private void UpdateLobbyInfo()
    {
        if (_extendedPlayerData == null) return;
        var userData = _extendedPlayerData.MyUserData;
        var cosmetics = _player.cosmetics;

        bool isLocalPlayer = _player.IsLocalPlayer();

        if (userData != null)
        {
            var playerTag = !string.IsNullOrEmpty(userData.OverheadColor) ? $"<{userData.OverheadColor}>{userData.OverheadTag}</color>" : userData.OverheadTag;
            if (!string.IsNullOrEmpty(playerTag))
            {
                _sbTagTop.Append($"{playerTag}---");
            }
        }
        cosmetics.nameText.color = _extendedPlayerData.HasMod || isLocalPlayer
            ? new Color(0.47f, 1f, 0.95f, 1f)
            : Color.white;
    }

    private void UpdatePlayerInfo()
    {
        if (_extendedPlayerData == null) return;
        var cosmetics = _player.cosmetics;

        bool isLocalPlayerAlive = PlayerControl.LocalPlayer.IsAlive();
        bool isLocalPlayer = _player.IsLocalPlayer();

        bool hideInfo = PlayerControl.LocalPlayer.CheckAnyRoles(role => role.HidePlayerInfoOther(_player));

        bool canRevealDeath = (!isLocalPlayerAlive && !PlayerControl.LocalPlayer.IsGhostRole() ||
                 PlayerControl.LocalPlayer.CheckAnyRoles(role => role.RevealPlayerDeath(_player))) && !_player.IsAlive();

        if (canRevealDeath && !hideInfo)
        {
            _sbTagBottom.Append($"{_player.FormatDeathReason()}---");
        }

        bool canRevealRole = isLocalPlayer || !isLocalPlayerAlive || _player.IsImpostorTeammate() ||
                             PlayerControl.LocalPlayer.CheckAnyRoles(role => role.RevealPlayerRole(_player));

        string hexColor = "";

        if (canRevealRole && !hideInfo && _player.Role() != null && _player.Role().ShowRoleAboveName)
        {
            hexColor = _player.Role()?.RoleColorHex ?? "#FFFFFF";
            _sbTag.Append($"{_player.Role()?.RoleNameAndAbilityAmountText.GetText()}{_player.FormatTasksToText()}---");
        }

        bool canRevealAddons = isLocalPlayer || !isLocalPlayerAlive || _player.IsImpostorTeammate() ||
                               PlayerControl.LocalPlayer.CheckAllRoles(role => role.RevealPlayerAddons(_player));

        if (canRevealAddons && !hideInfo)
        {
            foreach (var addon in _extendedPlayerData.RoleInfo.Addons)
            {
                if (!addon.ShowRoleAboveName) continue;
                _sbTagTop.Append($"{addon.RoleNameAndAbilityAmountText.GetText().Size(75f)}</size>+++");
            }
        }

        if (!string.IsNullOrEmpty(_extendedPlayerData.NameColor))
        {
            hexColor = _extendedPlayerData.NameColor;
        }

        if (!string.IsNullOrEmpty(hexColor))
        {
            var color = Colors.HexToColor(hexColor);
            cosmetics.nameText.color = new Color(color.r, color.g, color.b, cosmetics.nameText.color.a);
        }
        else
        {
            cosmetics.nameText.color = new Color(1f, 1f, 1f, cosmetics.nameText.color.a);
        }
    }

    protected static void UpdateTextIfChanged(TextMeshPro textMesh, StringBuilder sb, ref string lastValue)
    {
        if (textMesh == null) return;

        string newText = Utils.FormatStringBuilder(sb).ToString();
        if (newText != lastValue)
        {
            textMesh?.SetText(newText);
            lastValue = newText;
        }
    }

    private void UpdateColorBlindTextPosition()
    {
        var text = _player.cosmetics.colorBlindText;
        if (!text.enabled) return;
        if (!_player.onLadder && !_player.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
        {
            text.transform.localPosition = new Vector3(0f, -1.3f, 0.4999f);
        }
        else
        {
            text.transform.localPosition = new Vector3(0f, -1.5f, 0.4999f);
        }
    }
}