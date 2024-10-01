
using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using UnityEngine;
using UnityEngine.UI;

namespace TheBetterRoles;

public enum TargetType
{
    None,
    Player,
    Body,
    Vent
}

public abstract class CustomRoleBehavior
{
    public PlayerControl? localPlayer => PlayerControl.LocalPlayer;
    public PlayerControl? _player;
    public NetworkedPlayerInfo? _data;
    public string RoleName => Translator.GetString($"Role.{Enum.GetName(RoleType)}");
    public virtual string RoleColor => Utils.GetCustomRoleTeamColor(RoleTeam);
    public bool IsCrewmate => RoleTeam == CustomRoleTeam.Crewmate;
    public bool IsImpostor => RoleTeam == CustomRoleTeam.Impostor;
    public bool IsNeutral => RoleTeam == CustomRoleTeam.Neutral;
    public int RoleId => 100000 + 200 * (int)RoleType;
    public virtual bool IsAddon => false;
    public abstract CustomRoleBehavior Role { get; }
    public abstract CustomRoles RoleType { get; }
    public abstract CustomRoleTeam RoleTeam { get; }
    public abstract CustomRoleCategory RoleCategory { get; }
    public abstract BetterOptionTab? SettingsTab { get; }
    public abstract BetterOptionItem[]? OptionItems { get; }
    public BetterOptionItem? RoleOptionItem { get; set; }
    public BetterOptionItem? AmountOptionItem { get; set; }
    public List<BaseButton> Buttons { get; set; } = [];
    public TargetButton? KillButton { get; set; }
    public SabotageButton? SabotageButton { get; set; }
    public VentButton? VentButton { get; set; }
    public List<NetworkedPlayerInfo> RecruitedPlayers { get; set; } = [];
    public bool InteractableTarget { get; set; } = true;
    public virtual float PlayerSpeed => _player.MyPhysics.Speed;
    public virtual float BaseSpeedMod => GameOptionsManager.Instance.currentNormalGameOptions.PlayerSpeedMod * 2.5f;
    public virtual bool HasTask => RoleTeam == CustomRoleTeam.Crewmate;
    public virtual bool CanKill => false;
    public virtual bool CanVent => false;
    public virtual bool CanMoveInVent => true;
    public virtual bool CanSabotage => false;
    public virtual bool CanMove => true;

    public bool Protected { get; set; }
    public NetworkedPlayerInfo? ProtectedBy { get; set; }

    public float GetChance() => GameStates.IsHost ? BetterDataManager.LoadFloatSetting(RoleId) : 0f;
    public int GetAmount() => GameStates.IsHost ? BetterDataManager.LoadIntSetting(RoleId + 5) : 0;

    public void Initialize(PlayerControl player)
    {
        if (player != null)
        {
            _player = player;
            _data = player.Data;
            if (!IsAddon)
            {
                player.BetterData().RoleInfo.Role = this;
                player.BetterData().RoleInfo.RoleType = RoleType;
                SetUpRole();
            }
            else
            {
                if (!player.BetterData().RoleInfo.Addons.Any(addon => addon.RoleType == RoleType))
                {
                    player.BetterData().RoleInfo.Addons.Add((CustomAddonBehavior)this);
                    SetUpRole();
                }
            }
        }
    }
    
    public void Deinitialize()
    {
        OnDeinitialize();

        // Remove Buttons
        foreach (var button in Buttons)
        {
            if (button?.Button?.gameObject != null)
            {
                UnityEngine.Object.Destroy(button.Button.gameObject);
            }
        }

        if (IsAddon)
        {
            _player.BetterData().RoleInfo.Addons.Remove((CustomAddonBehavior)this);
        }
    }

    public virtual bool WinCondition() => false;

    public virtual void OnDeinitialize() { }

    public virtual void Update() 
    {
    }

    // Run base if override
    public virtual void SetUpRole()
    {
        if (IsAddon) return;

        SabotageButton = AddButton(new SabotageButton().Create(1, Translator.GetString("Role.Ability.Sabotage"), this, true)) as SabotageButton;
        SabotageButton.VisibleCondition = () => { return VentButton.Role.CanSabotage; };

        KillButton = AddButton(new TargetButton().Create(2, Translator.GetString("Role.Ability.Kill"), GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown, 0, HudManager.Instance.KillButton.graphic.sprite, this, true, GameOptionsManager.Instance.currentNormalGameOptions.KillDistance + 1)) as TargetButton;
        KillButton.VisibleCondition = () => { return KillButton.Role.CanKill; };
        KillButton.TargetCondition = (PlayerControl target) =>
        {
            return !target.IsImpostorTeammate();
        };

        VentButton = AddButton(new VentButton().Create(3, Translator.GetString("Role.Ability.Vent"), 0, 0, this, null, false, true)) as VentButton;
        VentButton.VisibleCondition = () => { return VentButton.Role.CanVent; };
    }

    public void SetUpSettings()
    {
        RoleOptionItem = new BetterOptionPercentItem().Create(RoleId, SettingsTab, Utils.GetCustomRoleNameAndColor(RoleType, true), 0f);
        AmountOptionItem = new BetterOptionIntItem().Create(RoleId + 5, SettingsTab, "Amount", [1, 15, 1], 1, "", "", RoleOptionItem);
        OptionItems.Initialize();
    }

    public BaseButton AddButton(BaseButton button)
    {
        Buttons.Add(button);
        return button;
    }

    public void RemoveButton(BaseButton button)
    {
        button.RemoveButton();
        Buttons.Remove(button);
    }

    public void CheckAndUseAbility(int id, int targetId, TargetType type) 
    {
        if (GameStates.IsHost)
        {
            if (CheckRoleAction(id, type == TargetType.Player ? Utils.PlayerFromPlayerId(targetId) : null,
                            type == TargetType.Vent ? ShipStatus.Instance.AllVents.FirstOrDefault(v => v.Id == targetId) : null,
                            type == TargetType.Body ? Main.AllDeadBodys.FirstOrDefault(b => b.ParentId == targetId) : null) == true)
            {
                UseAbility(id, targetId, type);
            }
        }
        else
        {
            var writer = AmongUsClient.Instance.StartRpcImmediately(_player.NetId, (byte)CustomRPC.CheckRoleAction, SendOption.Reliable, AmongUsClient.Instance.GetHost().Id);
            writer.WriteNetObject(_player);
            writer.Write((int)RoleType);
            writer.Write(id);
            writer.Write(targetId);
            writer.Write((int)type);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }

    private void UseAbility(int id, int targetId, TargetType type)
    {
        if (GameStates.IsHost)
        {
            OnAbilityUse(id, type == TargetType.Player ? Utils.PlayerFromPlayerId(targetId) : null,
                            type == TargetType.Vent ? ShipStatus.Instance.AllVents.FirstOrDefault(v => v.Id == targetId) : null,
                            type == TargetType.Body ? Main.AllDeadBodys.FirstOrDefault(b => b.ParentId == targetId) : null);
            SetCooldownAndUse(id);

            var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RoleAction, SendOption.Reliable, -1);
            writer.WriteNetObject(_player);
            writer.Write((int)RoleType);
            writer.Write(id);
            writer.Write(targetId);
            writer.Write((int)type);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }

    public void SetCooldownAndUse(int id)
    {
        Buttons.FirstOrDefault(b => b.Id == id)?.SetCooldown();
        Buttons.FirstOrDefault(b => b.Id == id)?.RemoveUse();
    }

    public void HandleRpc(MessageReader reader, byte callId, PlayerControl player, PlayerControl realSender) 
    {
        _ = reader.ReadNetObject<PlayerControl>();
        _ = reader.ReadInt32();

        switch ((CustomRPC)callId)
        {
            case CustomRPC.CheckRoleAction:
                {
                    // Sender is Client, User is Client
                    if (GameStates.IsHost)
                    {
                        var id = reader.ReadInt32();
                        var targetId = reader.ReadInt32();
                        var type = (TargetType)reader.ReadInt32();

                        if (CheckRoleAction(id, type == TargetType.Player ? Utils.PlayerFromPlayerId(targetId) : null,
                            type == TargetType.Vent ? ShipStatus.Instance.AllVents.FirstOrDefault(v => v.Id == targetId) : null,
                            type == TargetType.Body ? Main.AllDeadBodys.FirstOrDefault(b => b.ParentId == targetId) : null) == true)
                        {
                            UseAbility(id, targetId, type);
                        }
                    }
                }
                break;
            case CustomRPC.RoleAction:
                {
                    // Sender is host, User is this role base player
                    if (realSender.IsHost())
                    {
                        var id = reader.ReadInt32();
                        var targetId = reader.ReadInt32();
                        var type = (TargetType)reader.ReadInt32();

                        OnAbilityUse(id, type == TargetType.Player ? Utils.PlayerFromPlayerId(targetId) : null,
                            type == TargetType.Vent ? ShipStatus.Instance.AllVents.FirstOrDefault(v => v.Id == targetId) : null,
                            type == TargetType.Body ? Main.AllDeadBodys.FirstOrDefault(b => b.ParentId == targetId) : null);
                        SetCooldownAndUse(id);
                    }
                }
                break;
        }
    }

    public virtual void OnAbilityUse(int id, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 1:
                {
                }
                break;
            case 2:
                {
                    if (target != null)
                    {
                        if (_player.IsLocalPlayer())
                        {
                            _player.MurderSync(target);
                        }
                    }
                }
                break;
            case 3:
                break;
            default:
                {
                    Main.AllPlayerControls.Where(pc => pc == _player || pc == target && pc.BetterData().RoleInfo.RoleAssigned).ToList()
                        .ForEach(pc => pc.BetterData().RoleInfo.Role.OnAbility(id, _player, target, vent));

                    Main.AllPlayerControls.Where(pc => pc.BetterData().RoleInfo.RoleAssigned).ToList()
                        .ForEach(pc => pc.BetterData().RoleInfo.Role.OnAbilityOther(id, _player, target, vent));
                }
                 break;
        }
    }

    public virtual bool CheckRoleAction(int id, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 1:
                {
                    if (!CanSabotage)
                    {
                        return false;
                    }
                }
                break;
            case 2:
                break;
            case 3:
                break;
                default:
                {
                    foreach (var player in Main.AllPlayerControls.Where(pc => pc == _player || pc == target))
                    {
                        if (!player.BetterData().RoleInfo.RoleAssigned) continue;

                        if (player.BetterData().RoleInfo.Role.CheckAbility(id, _player, target, vent) == false)
                        {
                            return false;
                        }
                    }
                    foreach (var player in Main.AllPlayerControls)
                    {
                        if (!player.BetterData().RoleInfo.RoleAssigned) continue;

                        if (player.BetterData().RoleInfo.Role.CheckAbilityOther(id, _player, target, vent) == false)
                        {
                            return false;
                        }
                    }
                }
                break;
        }

        return true;
    }

    public Sprite? LoadAbilitySprite(string name, float size = 115) => Utils.LoadSprite($"TheBetterRoles.Resources.Images.Ability.{name}.png", size);
    public virtual bool CheckMurderOther(PlayerControl killer, PlayerControl target, bool IsAbility) => true;
    public virtual bool CheckMurder(PlayerControl killer, PlayerControl target, bool IsAbility) => true;

    public virtual void OnMurderOther(PlayerControl killer, PlayerControl target, bool IsAbility) { }
    public virtual void OnMurder(PlayerControl killer, PlayerControl target, bool IsAbility) { }

    public virtual bool CheckAbilityOther(int Id, PlayerControl player, PlayerControl? target, Vent? vent) => true;
    public virtual bool CheckAbility(int Id, PlayerControl player, PlayerControl? target, Vent? vent) => true;

    public virtual void OnAbilityOther(int Id, PlayerControl player, PlayerControl? target, Vent? vent) { }
    public virtual void OnAbility(int Id, PlayerControl player, PlayerControl? target, Vent? vent) { }
    public virtual void OnAbilityDurationEnd(int Id) { }

    public virtual bool CheckBodyReportOther(PlayerControl reporter, NetworkedPlayerInfo? body, bool isButton) => true;
    public virtual bool CheckBodyReport(PlayerControl reporter, NetworkedPlayerInfo? body, bool isButton) => true;
    public virtual void OnBodyReportOther(PlayerControl reporter, NetworkedPlayerInfo? body, bool isButton) { }
    public virtual void OnBodyReport(PlayerControl reporter, NetworkedPlayerInfo? body, bool isButton) { }

    public virtual bool CheckVentOther(PlayerControl venter, int ventId, bool Exit) => true;
    public virtual bool CheckVent(PlayerControl venter, int ventId, bool Exit) => true;
    public virtual void OnVentOther(PlayerControl venter, int ventId, bool Exit) { }
    public virtual void OnVent(PlayerControl venter, int ventId, bool Exit) { }

    public virtual void OnPlayerPressOther(PlayerControl player, PlayerControl target) { }
    public virtual void OnPlayerPress(PlayerControl player, PlayerControl target) { }
}
