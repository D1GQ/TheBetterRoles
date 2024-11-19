using AmongUs.GameOptions;
using Hazel;
using Reactor.Networking.Rpc;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using TheBetterRoles.RPCs;
using UnityEngine;

namespace TheBetterRoles.Roles;

public enum TargetType
{
    None,
    Player,
    Body,
    Vent
}

public class OptionAttributes
{
    public int Amount = 0;
    public float Cooldown = 0f;
    public float Duration = 0f;
}

public abstract class CustomRoleBehavior
{
    protected bool HasSetup { get; set; } = false;
    protected bool HasDeinitialize { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the object's data has changed and needs to be synchronized.
    /// When set to true, the data is marked as "dirty," triggering serialization and synchronization.
    /// </summary>
    protected bool IsDirty { get; set; } = false;

    /// <summary>
    /// A dictionary representing players recruited by this role to win together. 
    /// The key is the recruiter, and the value is a list of players they recruited.
    /// </summary>
    public static Dictionary<byte, List<byte>> SubTeam { get; set; } = [];

    /// <summary>
    /// Adds a recruited player to the recruiter's sub-team. 
    /// If the recruiter is not already in the dictionary, a new entry is created for them.
    /// </summary>
    /// <param name="Recruiter">The player who is recruiting another player.</param>
    /// <param name="Recruited">The player being recruited.</param>
    public static void AddSubTeam(NetworkedPlayerInfo Recruiter, NetworkedPlayerInfo Recruited)
    {
        if (!SubTeam.ContainsKey(Recruiter.PlayerId))
        {
            SubTeam[Recruiter.PlayerId] = [];
        }
        SubTeam[Recruiter.PlayerId].Add(Recruited.PlayerId);
    }

    /// <summary>
    /// Removes a recruited player from the recruiter's sub-team. 
    /// If the recruiter or the recruited player is not present, no action is taken.
    /// </summary>
    /// <param name="Recruiter">The player who recruited another player.</param>
    /// <param name="Recruited">The player being removed from the recruiter's sub-team.</param>
    public static void RemoveSubTeam(NetworkedPlayerInfo Recruiter, NetworkedPlayerInfo Recruited)
    {
        if (SubTeam.ContainsKey(Recruiter.PlayerId) && SubTeam[Recruiter.PlayerId].Contains(Recruited.PlayerId))
        {
            SubTeam[Recruiter.PlayerId].Remove(Recruited.PlayerId);

            if (SubTeam[Recruiter.PlayerId].Count == 0)
            {
                SubTeam.Remove(Recruiter.PlayerId);
            }
        }
    }

    /// <summary>
    /// Local player. Refers to the player that is controlled locally on the client.
    /// </summary>
    public PlayerControl? localPlayer => PlayerControl.LocalPlayer;

    /// <summary>
    /// Player Base for role. This refers to the `PlayerControl` instance that represents the player 
    /// this role is attached to.
    /// </summary>
    public PlayerControl? _player;

    /// <summary>
    /// Player Base data for role. Holds additional information about the player in a `NetworkedPlayerInfo` object, 
    /// which includes networked data like the player's ID, status, or other properties that might be synced across clients.
    /// </summary>
    public NetworkedPlayerInfo? _data;

    /// <summary>
    /// Get role name. This returns a translated string for the role's name based on its type using a translator utility.
    /// </summary>
    public string RoleName => Translator.GetString($"Role.{Enum.GetName(RoleType)}");

    /// <summary>
    /// Get role name and ability amount. This returns a translated string for the role's name and the amount of ability uses based on its type using a translator utility.
    /// </summary>
    public string RoleNameAndAbilityAmount
    {
        get
        {
            string str = string.Empty;
            int max = -1, current = -1;
            SetAbilityAmountText(ref max, ref current);
            if (max > -1 || current > -1)
            {
                string hex = Utils.Color32ToHex(Utils.HexToColor32(RoleColor) - new Color(0.15f, 0.15f, 0.15f));
                str = $" <{hex}>(</color>{(current > -1 ? current.ToString() : string.Empty)}{(max > -1 && current > -1 ? $"<{hex}>/</color>" : string.Empty)}{(max > -1 ? max.ToString() : string.Empty)}<{hex}>)</color>";
            }

            return $"<{RoleColor}>{RoleName + str}</color>";
        }
    }

    /// <summary>
    /// Set role color. This returns the custom color for the role's team, using the team’s color configuration.
    /// </summary>
    public virtual string RoleColor => Utils.GetCustomRoleTeamColor(RoleTeam);

    public Color RoleColor32 => Utils.HexToColor32(RoleColor);

    /// <summary>
    /// Checks if the role belongs to the Crewmate team.
    /// </summary>
    public bool IsCrewmate => RoleTeam == CustomRoleTeam.Crewmate;

    /// <summary>
    /// Checks if the role belongs to the Impostor team.
    /// </summary>
    public bool IsImpostor => RoleTeam == CustomRoleTeam.Impostor;

    /// <summary>
    /// Checks if the role belongs to the Neutral team.
    /// </summary>
    public bool IsNeutral => RoleTeam == CustomRoleTeam.Neutral;

    /// <summary>
    /// Checks if the role is a killing role.
    /// </summary>
    public virtual bool IsKillingRole =>
        RoleCategory == CustomRoleCategory.Killing ||
        Utils.GetCustomRoleClass(RoleType).CanKill;

    /// <summary>
    /// Determines whether the vote-out message is always shown for this role.
    /// </summary>
    public virtual bool AlwaysShowVoteOutMsg => false;

    /// <summary>
    /// <code>Use id 39 for the next role!</code>
    /// Each role is assigned a unique ID.
    /// </summary>
    public abstract int RoleId { get; }

    /// <summary>
    /// Get automatically generated role UID based on the role type. Each role is assigned a unique identifier derived from its type.
    /// </summary>
    public int RoleUID => 100000 + 200 * (int)RoleType;

    /// <summary>
    /// Get automatically generated role Hash based on the role and player.
    /// </summary>
    public ushort RoleHash => Utils.GetHashUInt16($"{(int)RoleType}{RoleId}{RoleUID}{_player?.PlayerId ?? 255}");

    /// <summary>
    /// Determines whether the role can be assigned during the initial role assignment at the start of the game. 
    /// </summary>
    public virtual bool CanBeAssigned => true;

    /// <summary>
    /// Check if the role is an addon, meaning an additional or modified role. This should never be overridden in subclasses.
    /// </summary>
    public virtual bool IsAddon => false;

    /// <summary>
    /// Check if the role is an ghost role.
    /// </summary>
    public virtual bool IsGhostRole => false;

    /// <summary>
    /// Get the role class. This is an abstract method that will return the custom behavior associated with this role.
    /// </summary>
    public abstract CustomRoleBehavior Role { get; }

    /// <summary>
    /// Get the type of the role. This is an abstract method that returns the enum representing the role type.
    /// </summary>
    public abstract CustomRoleType RoleType { get; }

    /// <summary>
    /// Set the role's team. This determines whether the role is part of the Crewmates, Impostors, or Neutrals.
    /// </summary>
    public abstract CustomRoleTeam RoleTeam { get; }

    /// <summary>
    /// Set the role's category. This organizes roles into specific categories for better management and classification.
    /// </summary>
    public abstract CustomRoleCategory RoleCategory { get; }

    /// <summary>
    /// Defines the settings tab that this role will automatically be placed into. 
    /// This helps in organizing the role's configuration in the UI.
    /// </summary>
    public abstract TBROptionTab? SettingsTab { get; }

    /// <summary>
    /// Array of setting options for the role. These can be initialized later to provide customization for the role.
    /// </summary>
    public abstract TBROptionItem[]? OptionItems { get; }

    /// <summary>
    /// The role's specific chance option in the game settings. This allows the chance of this role appearing to be configured.
    /// </summary>
    public TBROptionItem? RoleOptionItem { get; set; }

    /// <summary>
    /// The role's amount option in the game settings. This allows setting how many players can have this role in a game.
    /// </summary>
    public TBROptionItem? AmountOptionItem { get; set; }

    /// <summary>
    /// The option that determines whether players can use vents while playing this role. 
    /// </summary>
    public TBROptionItem? CanVentOptionItem { get; set; }

    /// <summary>
    /// The option that sets the cooldown time between vent uses for players in this role.
    /// </summary>
    public TBROptionItem? VentCooldownOptionItem { get; set; }

    /// <summary>
    /// The option that sets the duration a player can stay in a vent while playing this role.
    /// </summary>
    public TBROptionItem? VentDurationOptionItem { get; set; }

    /// <summary>
    /// Additional options for CanVent
    /// </summary>
    public virtual OptionAttributes? AdditionalVentOptions => null;

    /// <summary>
    /// The option that determines whether players can use vents while playing this role. 
    /// </summary>
    public virtual bool DefaultVentOption => IsImpostor;

    /// <summary>
    /// The option that determines whether players can callm meetings while playing this role. 
    /// </summary>
    public virtual bool DefaultCanCallMeetingOption => false;

    /// <summary>
    /// The option that determines whether players can use call an emergency meeting.
    /// </summary>
    public TBROptionItem? CanCallMeetingOptionItem { get; set; }

    /// <summary>
    /// The option that determines if the normal task amount is overrated.
    /// </summary>
    public TBROptionItem? OverrideTasksOptionItem { get; set; }

    /// <summary>
    /// The option that determines the amount of common tasks assigned to this role.
    /// </summary>
    public TBROptionItem? CommonTasksOptionItem { get; set; }

    /// <summary>
    /// The option that determines the amount of long tasks assigned to this role when using vents.
    /// </summary>
    public TBROptionItem? LongTasksOptionItem { get; set; }

    /// <summary>
    /// The option that determines the amount of short tasks assigned to this role.
    /// </summary>
    public TBROptionItem? ShortTasksOptionItem { get; set; }

    /// <summary>
    /// List of all local ability buttons available to the player for this role. This can include things like kill, sabotage, or vent buttons.
    /// </summary>
    public List<BaseButton> Buttons { get; set; } = [];

    /// <summary>
    /// The report button for the role, allowing the player to report a body.
    /// </summary>
    public DeadBodyAbilityButton? ReportButton => RolePatch.ReportButton;

    /// <summary>
    /// The kill button for the role, allowing the player to perform kills if applicable.
    /// </summary>
    public PlayerAbilityButton? KillButton { get; set; }

    /// <summary>
    /// The sabotage button for the role, allowing the player to perform sabotage actions.
    /// </summary>
    public BaseAbilityButton? SabotageButton { get; set; }

    /// <summary>
    /// The vent button for the role, allowing the player to use vents if they have that ability.
    /// </summary>
    public VentAbilityButton? VentButton { get; set; }

    /// <summary>
    /// Set if the player is interactable with a target ability button, determining whether they can be targeted by abilities.
    /// </summary>
    public bool InteractableTarget { get; set; } = true;

    /// <summary>
    /// The current speed of the player, which is often influenced by the role's special properties and physics settings.
    /// </summary>
    public virtual float PlayerSpeed => _player.MyPhysics.Speed;

    /// <summary>
    /// The base speed modification factor for the player, derived from game settings. This value affects overall movement speed.
    /// </summary>
    public virtual float BaseSpeedMod => GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.PlayerSpeedMod) * 2.5f;

    /// <summary>
    /// The base vision modification factor for the player, derived from game settings.
    /// </summary>
    public float BaseVisionMod => GameOptionsManager.Instance.CurrentGameOptions.GetFloat(!HasImpostorVision ? FloatOptionNames.CrewLightMod : FloatOptionNames.ImpostorLightMod);

    /// <summary>
    /// The option that determines whether players has Impostor vision.
    /// </summary>
    protected TBROptionItem? HasImpostorVisionOption { get; set; }

    /// <summary>
    /// The bool that determines whether players has Impostor vision.
    /// </summary>
    public virtual bool HasImpostorVision => IsImpostor || HasImpostorVisionOption?.GetBool() == true;

    /// <summary>
    /// Set if player appearance is disguised.
    /// </summary>
    public bool IsDisguised { get; set; } = false;

    /// <summary>
    /// Set target id for disguised player.
    /// </summary>
    public int DisguisedTargetId { get; set; } = -1;

    /// <summary>
    /// Sets if the role has tasks assigned. This is typical for Crewmate-aligned roles.
    /// </summary>
    public virtual bool HasTask => RoleTeam == CustomRoleTeam.Crewmate;

    /// <summary>
    /// Sets if the role has tasks assigned for them self.
    /// </summary>
    public virtual bool HasSelfTask => false;

    /// <summary>
    /// Checks if the role can perform kills. This is typically overridden by roles that are allowed to kill, such as Impostors.
    /// </summary>
    public virtual bool CanKill => IsImpostor;

    /// <summary>
    /// Indicates whether this role is reliant on using vents. 
    /// This property can be overridden by specific roles that require vent usage, 
    /// </summary>
    public virtual bool VentReliantRole => false;

    /// <summary>
    /// Indicates whether this role is reliant on tasks. 
    /// This property can be overridden by specific roles that require task usage, 
    /// </summary>
    public virtual bool TaskReliantRole => false;

    /// <summary>
    /// Indicates whether this role is reliant on guessing. 
    /// This property can be overridden by specific roles that require guessing, 
    /// </summary>
    public virtual bool GuessReliantRole => false;

    /// <summary>
    /// Indicates whether this role is reliant on meetings. 
    /// This property can be overridden by specific roles that require meetings, 
    /// </summary>
    public virtual bool MeetingReliantRole => false;

    /// <summary>
    /// Checks if the role can use vents. This is typically overridden by roles like Impostors or other vent-using roles.
    /// </summary>
    public virtual bool CanVent => CanVentOptionItem?.GetBool() == true || VentReliantRole;

    /// <summary>
    /// Checks if the role can move to other vents while inside a vent.
    /// This is often set to true for roles that can use vents as a movement system, like Impostors.
    /// </summary>
    public virtual bool CanMoveInVents => true;

    /// <summary>
    /// Checks if the role can perform sabotage actions. This is typically overridden by roles that have the ability to sabotage, such as Impostors.
    /// </summary>
    public virtual bool CanSabotage => IsImpostor;

    /// <summary>
    /// Set if the role is allowed to move, for example, if a certain condition freezes movement.
    /// </summary>
    public virtual bool CanMove => true;

    public float GetChance() => GameState.IsHost && CanBeAssigned ? TBRDataManager.LoadFloatSetting(RoleUID) : 0f;
    public int GetAmount() => GameState.IsHost && CanBeAssigned ? TBRDataManager.LoadIntSetting(RoleUID + 1) : 0;

    private int tempOptionNum = 0;
    protected int GetOptionUID(bool firstOption = false)
    {
        if (firstOption)
        {
            tempOptionNum = 1;
        }

        var num = tempOptionNum;
        tempOptionNum++;
        return RoleUID + 50 + (5 * num);
    }

    private int tempBaseOptionNum = 0;
    private int GetBaseOptionID()
    {
        var num = tempBaseOptionNum;
        tempBaseOptionNum++;
        return RoleUID + num;
    }

    public CustomRoleBehavior Initialize(PlayerControl player, bool isAssigned = false)
    {
        if (player != null)
        {
            _player = player;
            _data = player.Data;

            if (!IsAddon)
            {
                player.ExtendedData().RoleInfo.Role = this;
                player.ExtendedData().RoleInfo.RoleType = RoleType;
                player.ExtendedData().RoleInfo.RoleHistory.Add(RoleType);
                if (isAssigned) OnRoleAssigned();
                SetUpRole();
            }
            else
            {
                if (!player.ExtendedData().RoleInfo.Addons.Any(addon => addon.RoleType == RoleType))
                {
                    player.ExtendedData().RoleInfo.Addons.Add((CustomAddonBehavior)this);
                    if (isAssigned) OnRoleAssigned();
                    SetUpRole();
                }
            }

            if (!GameState.IsFreePlay)
            {
                SetAllCooldownsHalf();
            }
        }

        return this;
    }

    public void Deinitialize()
    {
        if (HasDeinitialize) return;
        HasDeinitialize = true;

        Logger.LogMethodPrivate("Deinitialize Role Base!", GetType());

        try { OnResetAbilityState(false); }
        catch (Exception ex) { Logger.LogMethodPrivate(ex.ToString(), GetType()); }

        Logger.LogPrivate($"Finished deinitialize Role Base, now deinitialize Role({RoleName})!");
        OnDeinitialize();

        // Remove Buttons
        foreach (var button in Buttons)
        {
            if (button?.Button?.gameObject != null)
            {
                button.RemoveButton();
            }
        }

        if (IsAddon)
        {
            _player.ExtendedData().RoleInfo.Addons.Remove((CustomAddonBehavior)this);
        }

        Utils.DirtyAllNames();

        Logger.LogPrivate($"Finished deinitialize Role({RoleName})!");
    }

    /// <summary>
    /// Sets up the role by initializing the option items and calling any additional setup logic from <see cref="OnSetUpRole"/>.
    /// Do not override this method.
    /// </summary>
    protected virtual void SetUpRole()
    {
        if (HasSetup) return;
        HasSetup = true;

        Logger.LogMethodPrivate("Setting up Role Base!", GetType());

        SetUpSettings();

        if (_player != null)
        {
            SetUpSabotageButton();
            SetUpKillButton();
            SetUpVentButton();
        }

        if (_player.IsLocalPlayer())
        {
            if (CanCallMeetingOptionItem?.GetBool() != false)
            {
                if (ShipStatus.Instance != null)
                {
                    ShipStatus.Instance.EmergencyButton.Image.sprite = ShipStatusPatch.CatchedMeetingButtonSprite;
                    ShipStatus.Instance.EmergencyButton.enabled = true;
                }
            }
            else
            {
                if (ShipStatus.Instance != null)
                {
                    ShipStatus.Instance.EmergencyButton.Image.sprite = ShipStatus.Instance.BrokenEmergencyButton;
                    ShipStatus.Instance.EmergencyButton.enabled = false;
                }
            }
        }

        Logger.LogPrivate($"Finished setting up Role Base, now setting up Role({RoleName})!");

        OnSetUpRole();

        Logger.LogPrivate($"Finished setting up Role({RoleName})!");

        _player.DirtyName();
    }

    public void SetUpSabotageButton()
    {
        SabotageButton = AddButton(new BaseAbilityButton().Create(1, Translator.GetString(StringNames.SabotageLabel), 0f, 0f, 0, HudManager._instance.SabotageButton.graphic.sprite, this, true));
        SabotageButton.UseAsDead = true;
        SabotageButton.VisibleCondition = () => { return SabotageButton.Role.CanSabotage; };
        SabotageButton.OnClick = () =>
        {
            if (SabotageButton.ActionButton.canInteract)
            {
                if (Role.CanSabotage)
                {
                    DestroyableSingleton<HudManager>.Instance.ToggleMapVisible(new MapOptions
                    {
                        Mode = MapOptions.Modes.Sabotage
                    });
                }
            }
        };
    }

    public void SetUpKillButton()
    {
        KillButton = AddButton(new PlayerAbilityButton().Create(2, Translator.GetString(StringNames.KillLabel), VanillaGameSettings.KillCooldown.GetFloat(), 0, 0, HudManager.Instance.KillButton.graphic.sprite, this, true, VanillaGameSettings.KillDistance.GetValue()));
        KillButton.VisibleCondition = () => { return KillButton.Role.CanKill; };
        KillButton.TargetCondition = (PlayerControl target) =>
        {
            return !IsImpostor || !target.IsImpostorTeammate();
        };
        KillButton.OnClick = () =>
        {
            if (KillButton.lastTarget != null)
            {
                localPlayer.SendRpcMurder(KillButton.lastTarget);
                KillButton.SetCooldown();
            }
        };
    }

    public void SetUpVentButton()
    {
        VentButton = AddButton(new VentAbilityButton().Create(3, Translator.GetString(StringNames.VentLabel), VentCooldownOptionItem?.GetFloat() ?? 0, VentDurationOptionItem?.GetFloat() ?? 0, 0, this, null, false, true));
        VentButton.VisibleCondition = () => { return CustomRoleManager.RoleChecksAny(_player, role => role.CanVent, false); };
        VentButton.CanCancelDuration = true;
    }

    public void LoadSettings()
    {
        SetUpSettings();
    }

    protected virtual void SetUpSettings()
    {
        tempBaseOptionNum = 0;
        RoleOptionItem = new TBROptionPercentItem().Create(GetBaseOptionID(), SettingsTab, Utils.GetCustomRoleNameAndColor(RoleType, true), 0f, this);
        AmountOptionItem = new TBROptionIntItem().Create(GetBaseOptionID(), SettingsTab, Translator.GetString("Role.Option.Amount"), [1, 15, 1], 1, "", "", RoleOptionItem);

        OptionItems.Initialize();

        if (IsNeutral && !IsGhostRole)
        {
            HasImpostorVisionOption = new TBROptionCheckboxItem().Create(GetBaseOptionID(), SettingsTab, Translator.GetString("Role.Ability.HasImpostorVision"), false, RoleOptionItem);
        }

        if (MeetingReliantRole)
        {
            CanCallMeetingOptionItem = new TBROptionCheckboxItem().Create(GetBaseOptionID(), SettingsTab, Translator.GetString("Role.Ability.CanCallMeeting"), DefaultCanCallMeetingOption, RoleOptionItem); ;
        }

        bool ventFlag = !IsCrewmate && !VentReliantRole && !IsGhostRole;
        if (ventFlag)
        {
            CanVentOptionItem = new TBROptionCheckboxItem().Create(GetBaseOptionID(), SettingsTab, Translator.GetString("Role.Ability.CanVent"), DefaultVentOption, RoleOptionItem);
        }
        if (AdditionalVentOptions != null)
        {
            if (AdditionalVentOptions.Cooldown > 0f) VentCooldownOptionItem = new TBROptionFloatItem().Create(GetBaseOptionID(), SettingsTab, Translator.GetString("Role.Ability.VentCooldown"), [0f, 180f, 2.5f], AdditionalVentOptions.Cooldown, "", "s", ventFlag ? CanVentOptionItem : null);
            if (AdditionalVentOptions.Duration > 0f) VentDurationOptionItem = new TBROptionFloatItem().Create(GetBaseOptionID(), SettingsTab, Translator.GetString("Role.Ability.VentDuration"), [0f, 180f, 2.5f], AdditionalVentOptions.Duration, "", "s", ventFlag ? CanVentOptionItem : null);
        }

        if (TaskReliantRole)
        {
            OverrideTasksOptionItem = new TBROptionCheckboxItem().Create(GetBaseOptionID(), SettingsTab, Translator.GetString("Role.Option.OverrideTasks"), false, RoleOptionItem);
            CommonTasksOptionItem = new TBROptionIntItem().Create(GetBaseOptionID(), SettingsTab, Translator.GetString("Role.Option.CommonTasks"), [0, 10, 1], 2, "", "", OverrideTasksOptionItem);
            LongTasksOptionItem = new TBROptionIntItem().Create(GetBaseOptionID(), SettingsTab, Translator.GetString("Role.Option.LongTasks"), [0, 10, 1], 2, "", "", OverrideTasksOptionItem);
            ShortTasksOptionItem = new TBROptionIntItem().Create(GetBaseOptionID(), SettingsTab, Translator.GetString("Role.Option.ShortTasks"), [0, 10, 1], 4, "", "", OverrideTasksOptionItem);
        }
    }

    public void BaseUpdate()
    {
        Update();
    }

    public void BaseFixedUpdate()
    {
        if (IsDirty)
        {
            if (_player.IsLocalPlayer())
            {
                Rpc<RpcDirtyRole>.Instance.Send(_player, new(RoleHash));
            }

            IsDirty = false;
        }

        FixedUpdate();
    }

    protected T AddButton<T>(T button) where T : BaseButton
    {
        Buttons.Add(button);
        return button;
    }

    protected void RemoveButton(BaseButton button)
    {
        if (button != null)
        {
            button.RemoveButton();
            Buttons.Remove(button);
        }
    }

    public void CheckAndUseAbility(int id, int targetId, TargetType type)
    {
        Rpc<RpcRoleAbility>.Instance.Send(_player, new(RoleHash, id, targetId, type, this));
    }

    public void SetCooldownAndUse(int id)
    {
        Buttons.FirstOrDefault(b => b.Id == id)?.SetCooldown();
        Buttons.FirstOrDefault(b => b.Id == id)?.RemoveUse();
    }

    public void OnAbilityUse(int id, PlayerControl? target, Vent? vent, DeadBody? body, MessageReader? reader, TargetType type)
    {
        if (target != null && type == TargetType.Player)
        {
            CustomRoleManager.RoleListener(_player, role => role.OnPlayerInteracted(_player, target), role => role == this);
            CustomRoleManager.RoleListener(target, role => role.OnPlayerInteracted(_player, target), role => role == this);
            CustomRoleManager.RoleListenerOther(role => role.OnPlayerInteractedOther(_player, target));
        }

        switch (id)
        {
            case 1:
            case 2:
            case 3:
                break;
            default:
                {
                    OnAbility(id, reader, this, target, vent, body);
                    CustomRoleManager.RoleListenerOther(role => role.OnAbilityOther(id, reader, this, target, vent, body));
                }
                break;
        }

        Utils.DirtyAllNames();
    }

    public bool CheckRoleAction(int id, PlayerControl? target, Vent? vent, DeadBody? body)
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
                    if (CustomRoleManager.RoleChecks(_player, role => role.CheckAbility(id, this, target, vent, body), targetRole: this) == false)
                    {
                        return false;
                    }
                    if (CustomRoleManager.RoleChecksOther(role => role.CheckAbilityOther(id, this, target, vent, body)) == false)
                    {
                        return false;
                    }
                }
                break;
        }

        return true;
    }

    public void OnDurationEnd(int id, bool isTimeOut)
    {
        switch (id)
        {
            case 3:
                {
                    if (isTimeOut)
                    {
                        _player.SendRpcVent(_player.GetPlayerVentId(), true);
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Sends a synchronization message for role abilities.
    /// </summary>
    /// <param name="syncId">The synchronization identifier for the ability.</param>
    /// <param name="additionalParams">Optional additional parameters for the ability.</param>
    protected void SendRoleSync(int syncId = 0, object[]? additionalParams = null)
    {
        Rpc<RpcSyncRole>.Instance.Send(_player, new(syncId, RoleHash, additionalParams));
    }

    /// <summary>
    /// Called when sending role ability synchronization data.
    /// </summary>
    /// <param name="syncId">The synchronization identifier for the ability.</param>
    /// <param name="writer">The message writer used to send data.</param>
    /// <param name="additionalParams">Optional additional parameters for the ability.</param>
    public virtual void OnSendRoleSync(int syncId, MessageWriter writer, object[]? additionalParams) { }

    /// <summary>
    /// Called upon receiving role ability synchronization data.
    /// </summary>
    /// <param name="syncId">The synchronization identifier for the ability.</param>
    /// <param name="reader">The message reader containing the received data.</param>
    /// <param name="sender">The player who sent the synchronization message.</param>
    public virtual void OnReceiveRoleSync(int syncId, MessageReader reader, PlayerControl sender) { }

    public void TryOverrideTasks(bool overideOldTask = false)
    {
        Logger.LogMethodPrivate($"Overriding tasks for {GetType().Name}", GetType(), true);

        if (GameState.IsHost && TaskReliantRole && OverrideTasksOptionItem != null && OverrideTasksOptionItem.GetBool())
        {
            _player.SetNewTasks(LongTasksOptionItem.GetInt(), ShortTasksOptionItem.GetInt(), CommonTasksOptionItem.GetInt());
        }
        else if (GameState.IsHost && overideOldTask)
        {
            _player.SetNewTasks();
        }
    }

    /// <summary>
    /// Sets the cooldown for all ability buttons associated with the current role.
    /// This loops through all local ability buttons and applies the appropriate cooldowns.
    /// </summary>
    public void SetAllCooldowns()
    {
        Logger.LogMethodPrivate("Setting all button cooldowns", GetType());

        foreach (var button in Buttons)
        {
            button.SetCooldown(durationState: 0);
        }
    }

    /// <summary>
    /// Sets the cooldown to half for all ability buttons associated with the current role.
    /// This loops through all local ability buttons and applies the appropriate cooldowns.
    /// </summary>
    public void SetAllCooldownsHalf()
    {
        Logger.LogMethodPrivate("Setting all button cooldowns", GetType());

        foreach (var button in Buttons)
        {
            button.SetCooldown(button.Cooldown * 0.5f);
        }
    }

    /// <summary>
    /// Loads a sprite for a specific ability by its name. The sprite is fetched from the embedded resources
    /// and resized according to the specified size.
    /// </summary>
    /// <param name="name">The name of the ability, used to locate the corresponding image file.</param>
    /// <param name="size">The size to scale the loaded sprite to, with a default value of 115.</param>
    /// <returns>A <see cref="Sprite"/> representing the ability icon, or null if the sprite could not be loaded.</returns>
    public Sprite? LoadAbilitySprite(string name, float size = 115) => Utils.LoadSprite($"TheBetterRoles.Resources.Images.Ability.{name}.png", size);

    /// <summary>
    /// Serializes the object's data into a <see cref="MessageWriter"/> for network transmission 
    /// when the role is marked as "dirty," indicating data changes that need to be synchronized.
    /// </summary>
    /// <param name="writer">The <see cref="MessageWriter"/> to which data will be written for synchronization.</param>
    public virtual void Serialize(MessageWriter writer) { }

    /// <summary>
    /// Deserializes object data from a <see cref="MessageReader"/> to update the object's state
    /// when synchronizing data marked as "dirty" on the receiving end.
    /// </summary>
    /// <param name="reader">The <see cref="MessageReader"/> from which data will be read to update the object state.</param>
    public virtual void Deserialize(MessageReader reader) { }

    /// <summary>
    /// Called once per frame to update the state of the role or perform actions.
    /// Override this method to implement any per-frame logic, such as checking conditions, updating timers, or managing abilities.
    /// </summary>
    public virtual void Update() { }

    /// <summary>
    /// Called once every frame to update the state of the role or perform actions.
    /// Override this method to implement any per-frame logic, such as checking conditions, updating timers, or managing abilities.
    /// </summary>
    public virtual void FixedUpdate() { }

    /// <summary>
    /// Determines the win condition for the role. This can be overridden by roles that have special win conditions.
    /// </summary>
    public virtual bool WinCondition() => false;

    /// <summary>
    /// A virtual method that can be overridden to include additional logic for the role on assigned.
    /// This method is called at the end of role assignment to allow customization of role-specific behavior.
    /// </summary>
    public virtual void OnRoleAssigned() { }

    /// <summary>
    /// A virtual method that can be overridden to include additional setup logic for the role.
    /// This method is called at the end of <see cref="SetUpRole"/> to allow customization of role-specific behavior.
    /// </summary>
    public virtual void OnSetUpRole() { }

    /// <summary>
    /// Called when the role is de-initialized or removed. Can be used to clean up resources or reset states.
    /// </summary>
    public virtual void OnDeinitialize() { }

    /// <summary>
    /// Called when a meeting is called. Only really used to create meeting ability buttons.
    /// </summary>
    public virtual void OnMeetingStart(MeetingHud meetingHud) { }

    /// <summary>
    /// Returns icon next to player name.
    /// </summary>
    public virtual string SetNameMark(PlayerControl target) => string.Empty;

    /// <summary>
    /// Returns icon next to player name.
    /// </summary>
    public virtual void SetAbilityAmountText(ref int maxAmount, ref int currentAmount) { }

    /// <summary>
    /// Returns text that will appear on the meeting hud on start.
    /// Priority is a output parameter representing the priority of this text entry, where higher values indicate a higher display priority.
    /// </summary>
    public virtual string AddMeetingText(ref CustomClip? clip, out uint priority)
    {
        priority = uint.MinValue;
        return string.Empty;
    }

    /// <summary>
    /// Check for a player guessing another player's role.
    /// This method can handle special conditions or roles.
    /// </summary>
    public virtual bool CheckGuessOther(PlayerControl guesser, PlayerControl target, CustomRoleType role) => true;

    /// <summary>
    /// Check for the local player attempting to guess another player's role.
    /// If the guess is invalid, the action will be canceled.
    /// </summary>
    public virtual bool CheckGuess(PlayerControl guesser, PlayerControl target, CustomRoleType role) => true;

    /// <summary>
    /// Executes when a player has made a guess about another player's role.
    /// Custom logic for handling the result of a guess can be added here.
    /// </summary>
    public virtual void OnGuess(PlayerControl guesser, PlayerControl target, CustomRoleType role) { }

    /// <summary>
    /// Executes when a player has made a guess about another player's role using a method intended for handling special cases or conditions.
    /// Custom logic for processing the result of such guesses can be added here.
    /// </summary>
    public virtual void OnGuessOther(PlayerControl guesser, PlayerControl target, CustomRoleType role) { }

    /// <summary>
    /// Check for the ability to murder another player. Returns false if the murder should be prevented.
    /// The host checks if the action is valid based on the killer and target and if it was triggered by an ability.
    /// </summary>
    public virtual bool CheckMurderOther(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility) => true;

    /// <summary>
    /// Check for the local player attempting to murder. This checks if the murder action is allowed.
    /// If the check fails, the action will be canceled.
    /// </summary>
    public virtual bool CheckMurder(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility) => true;

    /// <summary>
    /// Executes when the host has allowed another player (not the local player) to successfully murder a target.
    /// This is where post-murder logic can be added, such as applying cooldowns or effects.
    /// </summary>
    public virtual void OnMurderOther(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility) { }

    /// <summary>
    /// Executes when the host has allowed the local player to successfully murder a target.
    /// Custom logic for what happens after the murder action is validated by the host can be placed here.
    /// </summary>
    public virtual void OnMurder(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility) { }

    /// <summary>
    /// Triggered when the local player dies, regardless of the cause.
    /// This method allows for custom logic to handle player death events, including deaths from
    /// murders, abilities, or other game mechanics.
    /// </summary>
    public virtual void OnDeath(PlayerControl player, DeathReasons reason) { }

    /// <summary>
    /// Triggered when a player dies, regardless of the cause.
    /// This method allows for custom logic to handle player death events, including deaths from
    /// murders, abilities, or other game mechanics.
    /// </summary>
    public virtual void OnDeathOther(PlayerControl player, DeathReasons reason) { }

    /// <summary>
    /// Executes when a player is successfully murdered.
    /// Custom logic for what happens to the body.
    /// </summary>
    public virtual void OnDeadBodyDropOther(PlayerControl killer, DeadBody body) { }

    /// <summary>
    /// Executes when the player is successfully murdered.
    /// Custom logic for what happens to the body.
    /// </summary>
    public virtual void OnDeadBodyDrop(PlayerControl killer, DeadBody myBody) { }

    /// <summary>
    /// Executes when a player disconnects.
    /// Custom logic for handling disconnections.
    /// </summary>
    public virtual void OnDisconnect(PlayerControl target, DisconnectReasons reason) { }

    /// <summary>
    /// Check for an ability being used by another player. This checks if the action is allowed before execution.
    /// </summary>
    public virtual bool CheckAbilityOther(int id, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body) => true;

    /// <summary>
    /// Check for an ability being used by the local player. The host validates the ability before it is executed.
    /// </summary>
    public virtual bool CheckAbility(int id, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body) => true;

    /// <summary>
    /// Called after the host has approved the ability action for another player. Executes custom logic once the ability is allowed.
    /// </summary>
    public virtual void OnAbilityOther(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body) { }

    /// <summary>
    /// Called after the host has approved the ability action for the local player. This runs the logic after the ability is allowed.
    /// </summary>
    public virtual void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body) { }

    /// <summary>
    /// Check when another player attempts to use or exit a vent. This checks if the action is allowed before execution.
    /// </summary>
    public virtual bool CheckVentOther(PlayerControl venter, int ventId, bool Exit) => true;

    /// <summary>
    /// Check when the local player attempts to use or exit a vent. This checks if the action is allowed.
    /// </summary>
    public virtual bool CheckVent(PlayerControl venter, int ventId, bool Exit) => true;

    /// <summary>
    /// Called after the host has allowed another player to vent. This handles the logic once the vent action is approved.
    /// </summary>
    public virtual void OnVentOther(PlayerControl venter, int ventId, bool Exit) { }

    /// <summary>
    /// Called after the host has allowed the local player to vent. This handles the logic once the vent action is approved.
    /// </summary>
    public virtual void OnVent(PlayerControl venter, int ventId, bool Exit) { }

    /// <summary>
    /// Provides additional data related to the ability when sending it over the network.
    /// This method is meant to be overridden in subclasses to add custom ability-specific information.
    /// </summary>
    public virtual void AbilityWriter(int id, CustomRoleBehavior role, ref MessageWriter writer) { }

    /// <summary>
    /// Called when the duration of an ability ends, typically to clean up or reset states related to the ability.
    /// </summary>
    public virtual void OnAbilityDurationEnd(int id, bool isTimeOut) { }

    /// <summary>
    /// Check when another player attempts to report a body. If the check fails, the report action will be canceled.
    /// </summary>
    public virtual bool CheckBodyReportOther(PlayerControl reporter, NetworkedPlayerInfo? bodyData, bool isButton) => true;

    /// <summary>
    /// Check when the local player attempts to report a body. If the check fails, the report action will be canceled.
    /// </summary>
    public virtual bool CheckBodyReport(PlayerControl reporter, NetworkedPlayerInfo? bodyData, bool isButton) => true;

    /// <summary>
    /// Check when another player attempts to report a body. If the check fails, the report action will be canceled.
    /// This code is only ran by the local client!
    /// </summary>
    public virtual bool CheckBodyOther(DeadBody body) => true;

    /// <summary>
    /// Check when the local player attempts to report a body. If the check fails, the report action will be canceled.
    /// This code is only ran by the local client!
    /// </summary>
    public virtual bool CheckBody(DeadBody body) => true;

    /// <summary>
    /// Called after the host has allowed another player to report a body. This executes the logic after the report is approved.
    /// </summary>
    public virtual void OnBodyReportOther(PlayerControl reporter, NetworkedPlayerInfo? body, bool isButton) { }

    /// <summary>
    /// Called after the host has allowed the local player to report a body. This executes the logic after the report is approved.
    /// </summary>
    public virtual void OnBodyReport(PlayerControl reporter, NetworkedPlayerInfo? body, bool isButton) { }

    /// <summary>
    /// Called on meeting end, add additional votes to votedFor.
    /// This code is only ran by the host!
    /// </summary>
    public virtual int AddVotes(MeetingHud meetingHud, PlayerVoteArea pva) => 0;

    /// <summary>
    /// Called on meeting end, add visual votes on players vote area.
    /// </summary>
    public virtual void AddVisualVotes(MeetingHud meetingHud, PlayerVoteArea votedFor, ref List<MeetingHud.VoterState> states) { }

    /// <summary>
    /// Called on meeting end, add additional votes for calculation of exiled.
    /// This code is only ran by the host!
    /// </summary>
    public virtual void AddAdditionalVotes(MeetingHud meetingHud, ref Dictionary<byte, int> votes) { }

    /// <summary>
    /// Called after a meeting has ended, converting PlayerVoteArea info to VoterState.
    /// This code is only ran by the host!
    /// </summary>
    public virtual void OnEndVoting(MeetingHud meetingHud) { }

    /// <summary>
    /// Called after an exile has concluded, handling the logic for the player who was exiled.
    /// </summary>
    public virtual void OnExileEnd(PlayerControl? exiled, NetworkedPlayerInfo? exiledData) { }

    /// <summary>
    /// Called when a player disguises, handling the logic of what happens when the player changes their appearance.
    /// </summary>
    public virtual void OnDisguise(PlayerControl player) { }

    /// <summary>
    /// Called when a player removes their disguise, handling the logic of what happens when they return to their original form.
    /// </summary>
    public virtual void OnUndisguise(PlayerControl player) { }

    /// <summary>
    /// Called when another player presses an action button by directly clicking on a player with the mouse.
    /// </summary>
    public virtual void OnPlayerPressOther(PlayerControl player, PlayerControl target) { }

    /// <summary>
    /// Called when the local player presses an action button by directly clicking on another player with the mouse.
    /// </summary>
    public virtual void OnPlayerPress(PlayerControl player, PlayerControl target) { }

    /// <summary>
    /// Called when the player chooses an target in player list menu.
    /// Only ran by the local player!
    /// </summary>
    public virtual void OnPlayerMenu(int id, PlayerControl? target, NetworkedPlayerInfo? targetData, PlayerMenu? menu, ShapeshifterPanel? playerPanel, bool close) { }

    /// <summary>
    /// Called when another player interacts or gets interaction with a Target button.
    /// </summary>
    public virtual void OnPlayerInteractedOther(PlayerControl player, PlayerControl target) { }

    /// <summary>
    /// Called when the local player interacts or gets interaction with a Target button.
    /// </summary>
    public virtual void OnPlayerInteracted(PlayerControl player, PlayerControl target) { }

    /// <summary>
    /// Called when another player completes a task.
    /// </summary>
    public virtual void OnTaskCompleteOther(PlayerControl player, uint taskId) { }

    /// <summary>
    /// Called when the local player completes a task.
    /// </summary>
    public virtual void OnTaskComplete(PlayerControl player, uint taskId) { }

    /// <summary>
    /// Called when a Sabotage is called.
    /// </summary>
    public virtual void OnSabotage(ISystemType system, SystemTypes? systemType) { }

    /// <summary>
    /// Determines whether a player's death reason should be revealed to this role.
    /// </summary>
    /// <param name="target">The player whose role is being checked for reveal.</param>
    /// <returns>A boolean value indicating whether the role should be revealed (true) or not (false).</returns>
    public virtual bool RevealPlayerDeath(PlayerControl target) => false;

    /// <summary>
    /// Determines whether a player's role should be revealed to this role.
    /// </summary>
    /// <param name="target">The player whose role is being checked for reveal.</param>
    /// <returns>A boolean value indicating whether the role should be revealed (true) or not (false).</returns>
    public virtual bool RevealPlayerRole(PlayerControl target) => false;

    /// <summary>
    /// Determines whether a player's addons should be revealed to this role.
    /// </summary>
    /// <param name="target">The player whose role is being checked for reveal.</param>
    /// <returns>A boolean value indicating whether the role should be revealed (true) or not (false).</returns>
    public virtual bool RevealPlayerAddons(PlayerControl target) => false;

    /// <summary>
    /// Resets the state of any ability-related cooldowns or flags for this role.
    /// </summary>
    public virtual void OnResetAbilityState(bool isTimeOut) { }

    /// <summary>
    /// This method is called at the end of the intro cutscene.
    /// </summary>
    public virtual void OnIntroCutsceneEnd() { }

    /// <summary>
    /// Override base vision calculation, always return false if overridden!
    /// </summary>
    public virtual bool CalculateLightRadius(ShipStatus shipStatus, out float radius)
    {
        radius = 0f;
        return true;
    }

    /// <summary>
    /// This method is called at the end of the game to process the winning players.
    /// </summary>
    /// <param name="WinnerIds">A reference to the list containing the IDs of the winning players.</param>
    public virtual void OnGameEnd(ref List<byte> WinnerIds) { }
}