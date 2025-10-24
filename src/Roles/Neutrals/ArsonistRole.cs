using BepInEx.Unity.IL2CPP.Utils;
using Hazel;
using System.Collections;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Roles.Neutrals;

internal sealed class ArsonistRole : RoleClass, IRoleAbilityAction<PlayerControl>
{
    internal sealed override bool IsKillingRole => true;
    internal sealed override int RoleId => 32;
    internal sealed override string RoleColorHex => "#ff8900";
    internal sealed override RoleClass Role => this;
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Arsonist;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Neutral;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Killing;
    internal sealed override OptionTab? SettingsTab => TBRTabs.NeutralRoles;

    internal OptionItem? ArsonistType;
    internal OptionItem? MinimumDousesToIgnite;
    internal OptionItem? MaximumDouses;
    internal OptionItem? DouseCooldown;
    internal OptionItem? DouseDistance;
    internal OptionItem? DousingDuration;
    internal OptionItem? DousingRange;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                ArsonistType = OptionStringItem.Create(GetOptionUID(), SettingsTab, "Role.Arsonist.Option.ArsonistType", ["Role.Arsonist.ArsonistType.Original", "Role.Arsonist.ArsonistType.Modern"], 0, RoleOptions.RoleOptionItem),
                MinimumDousesToIgnite = OptionIntItem.Create(GetOptionUID(), SettingsTab, "Role.Arsonist.Option.MinimumDousesToIgnite", (1, 15, 1), 1, ("", ""), ArsonistType),

                MaximumDouses = OptionIntItem.Create(GetOptionUID(), SettingsTab, "Role.Arsonist.Option.MaximumDouses", (1, 15, 1), 3, ("", ""), ArsonistType),
                DouseCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Arsonist.Option.DouseCooldown", (0f, 180f, 2.5f), 20f, ("", "s"), RoleOptions.RoleOptionItem),
                DouseDistance = OptionStringItem.Create(GetOptionUID(), SettingsTab, "Role.Arsonist.Option.DouseDistance", ["Role.Option.Distance.1", "Role.Option.Distance.2", "Role.Option.Distance.3"], 0, RoleOptions.RoleOptionItem),

                DousingDuration = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Arsonist.Option.DousingDuration", (0f, 5f, 0.5f), 0f, ("", "s"), RoleOptions.RoleOptionItem),
                DousingRange = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Arsonist.Option.DousingRange", (0f, 5f, 0.25f), 1.25f, ("", "x"), DousingDuration),
            ];
        }
    }

    protected override void SetSettingsData()
    {
        MinimumDousesToIgnite.ShowCondition = () => ArsonistType.Is(1);
        MaximumDouses.ShowCondition = () => ArsonistType.Is(1);
    }

    private bool IsOriginal => ArsonistType.GetStringValue() == 0;
    private bool ShowDouseButton()
    {
        if (IsOriginal)
        {
            return !Main.AllAlivePlayerControls.Where(pc => pc != _player).Select(pc => pc.Data).All(doused.Contains); ;
        }
        else
        {
            return doused.Where(data => !data.IsDead && !data.Disconnected).ToArray().Length < MaximumDouses.GetInt();
        }
    }

    private bool ShowIgniteButton()
    {
        if (IsOriginal)
        {
            return Main.AllAlivePlayerControls.Where(pc => pc != _player).Select(pc => pc.Data).All(doused.Contains); ;
        }
        else
        {
            return doused.Where(data => !data.IsDead && !data.Disconnected).ToArray().Length >= MinimumDousesToIgnite.GetInt();
        }
    }

    internal PlayerAbilityButton? DouseButton;
    internal BaseAbilityButton? IgniteButton;
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            DouseButton = RoleButtons.AddButton(new PlayerAbilityButton().Create(5, Translator.GetString("Role.Arsonist.Ability.1"), DouseCooldown.GetFloat(), DousingDuration.GetFloat(), 0, null, this, true, DouseDistance.GetStringValue()));
            DouseButton.CanCancelDuration = true;
            DouseButton.VisibleCondition = ShowDouseButton;
            DouseButton.TargetCondition = (PlayerControl target) =>
            {
                return !doused.Contains(target.Data);
            };

            IgniteButton = RoleButtons.AddButton(new BaseAbilityButton().Create(6, Translator.GetString("Role.Arsonist.Ability.2"), DouseCooldown.GetFloat(), 0, IsOriginal ? 1 : 0, null, this, true));
            IgniteButton.VisibleCondition = ShowIgniteButton;
        }
    }

    internal sealed override void OnDeinitialize()
    {
        if (_player.IsLocalPlayer())
        {
            foreach (var data in doused)
            {
                var player = data.Object;
                if (player != null)
                {
                    player.SetTrueVisorColor(Palette.VisorColor);
                    player.ExtendedData().NameColor = string.Empty;
                }
            }
        }
    }

    private List<NetworkedPlayerInfo> doused = [];
    private Coroutine? douseCoroutine;
    void IRoleAbilityAction<PlayerControl>.OnAbility(int id, PlayerControl target)
    {
        switch (id)
        {
            case 5:
                {
                    if (DousingDuration.GetFloat() > 0f)
                    {
                        DouseButton?.SetDuration();
                        douseCoroutine = CoroutineManager.Instance.StartCoroutine(CoDousePlayer(target));
                    }
                    else
                    {
                        DousePlayer(target);
                    }
                }
                break;
        }
    }

    void IRoleAbilityAction.OnAbility(int id)
    {
        switch (id)
        {
            case 6:
                {
                    Ignite();
                }
                break;
        }
    }

    void IRoleAbilityAction.AbilityDurationEnd(int id, bool isTimeOut)
    {
        switch (id)
        {
            case 5:
                {
                    if (douseCoroutine != null && !isTimeOut)
                    {
                        DouseButton?.SetCooldown(0);
                        _player.StopCoroutine(douseCoroutine);
                    }
                }
                break;
        }
    }

    private IEnumerator CoDousePlayer(PlayerControl target)
    {
        float time = DousingDuration.GetFloat();
        float waitTime = 0.1f;
        float dousingRange = DousingRange.GetFloat();

        for (float elapsed = 0; elapsed < time; elapsed += waitTime)
        {
            Vector2 myPos = _player.GetTruePosition();
            Vector2 targetPos = target.GetTruePosition();

            if (Vector2.Distance(myPos, targetPos) > dousingRange)
            {
                DouseButton?.SetCooldown(0, 0);
                yield break;
            }

            yield return new WaitForSeconds(waitTime);
        }

        DousePlayer(target);
    }

    private void DousePlayer(PlayerControl target)
    {
        DouseButton?.SetCooldown(durationState: 0);
        doused.Add(target.Data);
        if (_player.IsLocalPlayer())
        {
            CustomSoundsManager.Instance.Play(Sounds.Douse, 2.5f);
            target.SetTrueVisorColor(RoleColor);
            target.ExtendedData().NameColor = "#59360d";
        }

        target.DirtyName();
        MarkDirty();
    }

    private void Ignite()
    {
        DouseButton?.SetCooldown();

        foreach (var data in doused)
        {
            var player = data.Object;
            if (player != null && player.IsAlive() && player != _player)
            {
                _player.SendRpcMurder(player, true, true, MultiMurderFlags.playSound | MultiMurderFlags.spawnBody | MultiMurderFlags.showAnimation);
            }
        }
    }

    public override void Serialize(MessageWriter writer)
    {
        writer.WriteFast(doused.Select(data => data.PlayerId));
        ClearDirtyBits();
    }

    public override void Deserialize(MessageReader reader)
    {
        var bytes = reader.ReadFast<byte[]>();

        doused.Clear();
        foreach (var playerId in bytes)
        {
            doused.Add(Utils.PlayerDataFromPlayerId(playerId));
        }
    }
}