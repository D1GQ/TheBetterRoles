using BepInEx.Unity.IL2CPP.Utils;
using Hazel;
using System.Collections;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using TheBetterRoles.RPCs;
using UnityEngine;

namespace TheBetterRoles.Roles;

public class ArsonistRole : CustomRoleBehavior
{
    // Role Info
    public override bool IsKillingRole => true;
    public override int RoleId => 32;
    public override string RoleColor => "#ff8900";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Arsonist;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Neutral;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Killing;
    public override TBROptionTab? SettingsTab => BetterTabs.NeutralRoles;

    public TBROptionItem? ArsonistType;
    public TBROptionItem? MinimumDousesToIgnite;
    public TBROptionItem? MaximumDouses;
    public TBROptionItem? DouseCooldown;
    public TBROptionItem? DouseDistance;
    public TBROptionItem? DousingDuration;
    public TBROptionItem? DousingRange;

    public PlayerAbilityButton? DouseButton;
    public BaseAbilityButton? IgniteButton;
    public override TBROptionItem[]? OptionItems
    {
        get
        {
            return
            [
                ArsonistType = new TBROptionStringItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Arsonist.Option.ArsonistType"),
                [Translator.GetString("Role.Arsonist.ArsonistType.Original"), Translator.GetString("Role.Arsonist.ArsonistType.Modern")], 0, RoleOptionItem),
                MinimumDousesToIgnite = new TBROptionIntItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Arsonist.Option.MinimumDousesToIgnite"), [1, 15, 1], 1, "", "", ArsonistType, () => { return ArsonistType.GetValue() == 1; }),
                MaximumDouses = new TBROptionIntItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Arsonist.Option.MaximumDouses"), [1, 15, 1], 3, "", "", ArsonistType, () => { return ArsonistType.GetValue() == 1; }),

                DouseCooldown = new TBROptionFloatItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Arsonist.Option.DouseCooldown"), [0f, 180f, 2.5f], 20f, "", "s", RoleOptionItem),
                DouseDistance = new TBROptionStringItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Arsonist.Option.DouseDistance"),
                    [Translator.GetString("Role.Option.Distance.1"), Translator.GetString("Role.Option.Distance.2"), Translator.GetString("Role.Option.Distance.3")], 0, RoleOptionItem),

                DousingDuration = new TBROptionFloatItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Arsonist.Option.DousingDuration"), [0f, 5f, 0.5f], 0f, "", "s", RoleOptionItem),
                DousingRange = new TBROptionFloatItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Arsonist.Option.DousingRange"), [0f, 5f, 0.25f], 1.25f, "", "x", DousingDuration),
            ];
        }
    }

    private bool IsOriginal => ArsonistType.GetValue() == 0;

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

    public override void OnSetUpRole()
    {
        DouseButton = AddButton(new PlayerAbilityButton().Create(5, Translator.GetString("Role.Arsonist.Ability.1"), DouseCooldown.GetFloat(), DousingDuration.GetFloat(), 0, null, this, true, DouseDistance.GetValue()));
        DouseButton.CanCancelDuration = true;
        DouseButton.VisibleCondition = ShowDouseButton;
        DouseButton.TargetCondition = (PlayerControl target) =>
        {
            return !doused.Contains(target.Data);
        };

        IgniteButton = AddButton(new BaseAbilityButton().Create(6, Translator.GetString("Role.Arsonist.Ability.2"), DouseCooldown.GetFloat(), 0, IsOriginal ? 1 : 0, null, this, true));
        IgniteButton.VisibleCondition = ShowIgniteButton;
    }

    public override void OnDeinitialize()
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
    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        if (_player.IsLocalPlayer())
        {
            switch (id)
            {
                case 5:
                    {
                        if (target != null)
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
                    }
                    break;
                case 6:
                    {
                        Ignite();
                    }
                    break;
            }
        }
    }

    public override void OnAbilityDurationEnd(int id, bool isTimeOut)
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
            CustomSoundsManager.Play("Douse", 2.5f);
            target.SetTrueVisorColor(RoleColor32);
            target.ExtendedData().NameColor = "#59360d";
        }

        IsDirty = true;
    }

    private void Ignite()
    {
        DouseButton?.SetCooldown();

        foreach (var data in doused)
        {
            var player = data.Object;
            if (player != null && player.IsAlive() && player != _player)
            {
                _player.SendRpcMurder(player, true, MultiMurderFlags.playSound | MultiMurderFlags.spawnBody | MultiMurderFlags.showAnimation);
            }
        }
    }

    public override void Serialize(MessageWriter writer)
    {
        writer.WritePacked(doused.Count);
        foreach (var data in doused)
        {
            writer.Write(data.PlayerId);
        }
    }

    public override void Deserialize(MessageReader reader)
    {
        int count = reader.ReadPackedInt32();
        doused.Clear();
        for (int i = 0; i < count; i++)
        {
            byte playerId = reader.ReadByte();
            var playerData = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(data => data.PlayerId == playerId);
            if (playerData != null)
            {
                doused.Add(playerData);
            }
        }
    }
}