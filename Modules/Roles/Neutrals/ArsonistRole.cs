using BepInEx.Unity.IL2CPP.Utils;
using Hazel;
using System.Collections;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles.Roles;

public class ArsonistRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 32;
    public override string RoleColor => "#ff8900";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Arsonist;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Neutral;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Killing;
    public override BetterOptionTab? SettingsTab => BetterTabs.NeutralRoles;

    public BetterOptionItem? DouseCooldown;
    public BetterOptionItem? DouseDistance;
    public BetterOptionItem? DousingDuration;
    public BetterOptionItem? DousingRange;

    public PlayerAbilityButton? DouseButton;
    public BaseAbilityButton? IgniteButton;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                DouseCooldown = new BetterOptionFloatItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Arsonist.Option.DouseCooldown"), [0f, 180f, 2.5f], 20f, "", "s", RoleOptionItem),
                DouseDistance = new BetterOptionStringItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Arsonist.Option.DouseDistance"),
                    [Translator.GetString("Role.Option.Distance.1"), Translator.GetString("Role.Option.Distance.2"), Translator.GetString("Role.Option.Distance.3")], 0, RoleOptionItem),
                DousingDuration = new BetterOptionFloatItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Arsonist.Option.DousingDuration"), [0f, 5f, 0.5f], 0f, "", "s", RoleOptionItem),
                DousingRange = new BetterOptionFloatItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Arsonist.Option.DousingRange"), [0f, 5f, 0.25f], 1.25f, "", "x", DousingDuration),
            ];
        }
    }

    public override void OnSetUpRole()
    {
        DouseButton = AddButton(new PlayerAbilityButton().Create(5, Translator.GetString("Role.Arsonist.Ability.1"), DouseCooldown.GetFloat(), DousingDuration.GetFloat(), 0, null, this, true, DouseDistance.GetValue()));
        DouseButton.CanCancelDuration = true;
        DouseButton.VisibleCondition = () => { return !Main.AllAlivePlayerControls.Where(pc => pc != _player).Select(pc => pc.Data).All(doused.Contains); };
        DouseButton.TargetCondition = (PlayerControl target) =>
        {
            return !doused.Contains(target.Data);
        };

        IgniteButton = AddButton(new BaseAbilityButton().Create(6, Translator.GetString("Role.Arsonist.Ability.2"), 0, 0, 1, null, this, true));
        IgniteButton.VisibleCondition = () => { return Main.AllAlivePlayerControls.Where(pc => pc != _player).Select(pc => pc.Data).All(doused.Contains); };
    }

    private List<NetworkedPlayerInfo> doused = [];
    private Coroutine? douseCoroutine;
    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 5:
                {
                    if (target != null)
                    {
                        if (DousingDuration.GetFloat() > 0f)
                        {
                            if (_player.IsLocalPlayer())
                            {
                                DouseButton?.SetDuration();
                                douseCoroutine = _player.BetterData().StartCoroutine(CoDousePlayer(target));
                            }
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

        DousePlayer(target, true);
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
                    player.BetterData().NameColor = string.Empty;
                }
            }
        }
    }

    private void DousePlayer(PlayerControl target, bool sync = false)
    {
        DouseButton?.SetCooldown(state: 0);
        doused.Add(target.Data);
        if (_player.IsLocalPlayer())
        {
            target.SetTrueVisorColor(Utils.HexToColor32("#59360d"));
            target.BetterData().NameColor = "#59360d";
        }

        if (sync)
        {
            SendRoleSync(0);
        }
    }

    private void Ignite()
    {
        DouseButton?.SetCooldown();
        foreach (var data in doused)
        {
            var player = data.Object;
            if (player != null && player.IsAlive() && player != _player)
            {
                _player.CustomMurderPlayer(player, false);
            }
        }
    }

    public override void OnSendRoleSync(int syncId, MessageWriter writer, object[]? additionalParams)
    {
        switch (syncId)
        {
            case 0:
                {
                    writer.Write(doused.Count);
                    foreach (var data in doused)
                    {
                        writer.Write(data.PlayerId);
                    }
                }
                break;
        }
    }

    public override void OnReceiveRoleSync(int syncId, MessageReader reader, PlayerControl sender)
    {
        switch (syncId)
        {
            case 0:
                int count = reader.ReadInt32();
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
                break;
        }
    }

}