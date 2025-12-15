using AmongUs.GameOptions;
using TheBetterRoles.CustomGameModes;
using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Patches.Game.Ship;
using TheBetterRoles.Roles.Core.Interfaces;
using TheBetterRoles.Roles.Core.RoleBase;
using UnityEngine;

namespace TheBetterRoles.Roles.Core;

internal abstract class RoleClass : NetworkClass
{
    internal bool HasSetup { get; set; } = false;
    internal bool HasDeinitialize { get; set; } = false;

    /// <summary>
    /// Local player. Refers to the player that is controlled locally on the client.
    /// </summary>
    internal PlayerControl? localPlayer => PlayerControl.LocalPlayer;

    /// <summary>
    /// Player Base for role. This refers to the `PlayerControl` instance that represents the player 
    /// this role is attached to.
    /// </summary>
    internal PlayerControl? _player;

    /// <summary>
    /// Player Base data for role. Holds additional information about the player in a `NetworkedPlayerInfo` object, 
    /// which includes networked data like the player's ID, status, or other properties that might be synced across clients.
    /// </summary>
    internal NetworkedPlayerInfo? _data;

    /// <summary>
    /// The MonoBehavior that contains the role class.
    /// </summary>
    internal RoleMono? _roleMono;

    internal virtual AudioClip? IntroSound => IsImpostor ? Prefab.GetCachedPrefab<ImpostorRole>().IntroSound :
        IsKillingRole && IsNeutral ? Prefab.GetCachedPrefab<ShapeshifterRole>().IntroSound :
        IsCrewmate ? DestroyableSingleton<RoleBehaviour>.Instance.IntroSound :
        Prefab.GetCachedPrefab<TrackerRole>().IntroSound;

    /// <summary>
    /// Get role name. This returns a translated string for the role's name based on its type using a translator utility.
    /// </summary>
    internal string RoleName
    {
        get
        {
            try
            {
                return Translator.GetString($"Role.{Enum.GetName(RoleType)}");
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// Get role name with color.
    /// </summary>
    internal string RoleNameAndColor
    {
        get
        {
            return RoleName.ToColor(RoleColorHex);
        }
    }

    /// <summary>
    /// Get role name and ability amount. This returns a translated string for the role's name and the amount of ability uses based on its type using a translator utility.
    /// </summary>
    internal RoleNameAndAbilityAmountText RoleNameAndAbilityAmountText { get; private set; }

    /// <summary>
    /// Set role color. This returns the custom color for the role's team, using the team’s color configuration.
    /// </summary>
    internal virtual string RoleColorHex => Utils.GetCustomRoleTeamColorHex(RoleTeam);

    internal Color RoleColor => RoleColorHex.HexToColor();

    /// <summary>
    /// Checks if the role belongs to the Crewmate team.
    /// </summary>
    internal bool IsCrewmate => RoleTeam == RoleClassTeam.Crewmate && OverrideTeam == RoleClassTeam.None
                || OverrideTeam == RoleClassTeam.Crewmate;

    /// <summary>
    /// Checks if the role belongs to the Impostor team.
    /// </summary>
    internal bool IsImpostor => RoleTeam == RoleClassTeam.Impostor && OverrideTeam == RoleClassTeam.None
                || OverrideTeam == RoleClassTeam.Impostor;

    /// <summary>
    /// Checks if the role belongs to the Neutral team.
    /// </summary>
    internal bool IsNeutral => RoleTeam == RoleClassTeam.Neutral && OverrideTeam == RoleClassTeam.None
        || OverrideTeam == RoleClassTeam.Neutral;

    /// <summary>
    /// Checks if the role belongs to the Apocalypse team.
    /// </summary>
    internal bool IsApocalypse => RoleTeam == RoleClassTeam.Apocalypse && OverrideTeam == RoleClassTeam.None
        || OverrideTeam == RoleClassTeam.Apocalypse;

    /// <summary>
    /// Checks if the role is a killing role.
    /// </summary>
    internal virtual bool IsKillingRole => RoleCategory == RoleClassCategory.Killing || CanKill;

    /// <summary>
    /// Checks if a killing role is considered benign at the moment, if true they will be counted as a non killing neutral.
    /// </summary>
    internal virtual bool IsBenign => RoleCategory == RoleClassCategory.Benign;

    /// <summary>
    /// Determines whether the vote-out message is always shown for this role.
    /// </summary>
    internal virtual bool AlwaysShowVoteOutMsg => false;

    /// <summary>
    /// <code>Use id 54 for the next role!</code>
    /// Each role is assigned a unique ID.
    /// </summary>
    internal abstract int RoleId { get; }

    /// <summary>
    /// Get automatically generated role UID based on the role type. Each role is assigned a unique identifier derived from its type.
    /// </summary>
    internal int RoleUID => 100000 + 200 * RoleId;

    /// <summary>
    /// Get automatically generated role Hash based on the role and player.
    /// </summary>
    internal ushort RoleHash => Utils.GetHashUInt16($"{(int)RoleType}{RoleId}{RoleUID}{_data?.NetId ?? uint.MaxValue}");

    /// <summary>
    /// Determines whether the role can be assigned during the initial role assignment at the start of the game. 
    /// </summary>
    internal virtual bool CanBeAssigned => true;

    /// <summary>
    /// Check if the role is an addon, meaning an additional or modified role. This should never be overridden in subclasses.
    /// </summary>
    internal bool IsAddon => this is AddonClass;

    /// <summary>
    /// Check if the role is a ghost role.
    /// </summary>
    internal bool IsGhostRole => this is GhostRoleClass;

    /// <summary>
    /// Get the type of the role. This is an abstract method that returns the enum representing the role type.
    /// </summary>
    internal abstract RoleClassTypes RoleType { get; }

    /// <summary>
    /// Set the role's team. This determines whether the role is part of the Crewmates, Impostors, or Neutrals.
    /// </summary>
    internal abstract RoleClassTeam RoleTeam { get; }

    /// <summary>
    /// Set the role's sub team. This determines whether the role is part of the Crewmates, Impostors, or Neutrals.
    /// </summary>
    internal RoleClassTeam OverrideTeam { get; set; } = RoleClassTeam.None;

    /// <summary>
    /// Set the role's category. This organizes roles into specific categories for better management and classification.
    /// </summary>
    internal abstract RoleClassCategory RoleCategory { get; }

    /// <summary>
    /// Defines the settings tab that this role will automatically be placed into. 
    /// This helps in organizing the role's configuration in the UI.
    /// </summary>
    internal abstract OptionTab? SettingsTab { get; }

    /// <summary>
    /// Array of setting options for the role. These can be initialized later to provide customization for the role.
    /// </summary>
    internal abstract OptionItem[]? OptionItems { get; }

    internal RoleOptions RoleOptions { get; } = new();

    internal RoleButtons RoleButtons { get; } = new();

    internal RoleNetworked Networked { get; } = new();

    /// <summary>
    /// The role's amount option interval in the game settings.
    /// </summary>
    internal virtual int AmountSize => 1;

    /// <summary>
    /// Additional options for CanVent
    /// </summary>
    internal virtual OptionAttributes? AdditionalVentOptions => null;

    /// <summary>
    /// The option that determines whether players can use vents while playing this role. 
    /// </summary>
    internal virtual bool DefaultVentOption => IsImpostor;

    /// <summary>
    /// The option that determines whether players can callm meetings while playing this role. 
    /// </summary>
    internal virtual bool DefaultCanCallMeetingOption => false;

    /// <summary>
    /// The current speed of the player, which is often influenced by the role's special properties and physics settings.
    /// </summary>
    internal virtual float _PlayerSpeed => _player.MyPhysics.Speed;

    /// <summary>
    /// The base speed modification factor for the player, derived from game settings. This value affects overall movement speed.
    /// </summary>
    internal virtual float BaseSpeedMod => GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.PlayerSpeedMod) * 2.5f;

    /// <summary>
    /// The base vision modification factor for the player, derived from game settings.
    /// </summary>
    internal float BaseVisionMod => GameOptionsManager.Instance.CurrentGameOptions.GetFloat(!HasImpostorVision ? FloatOptionNames.CrewLightMod : FloatOptionNames.ImpostorLightMod);

    /// <summary>
    /// The bool that determines whether players has Impostor vision.
    /// </summary>
    internal virtual bool HasImpostorVision => IsImpostor || RoleOptions.HasImpostorVisionOption?.GetBool() == true;

    /// <summary>
    /// Determines if the player is accounted for when checking for endgame based off player amount.
    /// </summary>
    internal virtual bool CountToPlayerAmount => true;

    /// <summary>
    /// Set if player appearance is disguised.
    /// </summary>
    internal bool IsDisguised { get; set; } = false;

    /// <summary>
    /// Set target id for disguised player.
    /// </summary>
    internal int DisguisedTargetId { get; set; } = -1;

    /// <summary>
    /// Sets if the role has tasks assigned. This is typical for Crewmate-aligned roles.
    /// </summary>
    internal virtual bool HasTask => RoleTeam == RoleClassTeam.Crewmate;

    /// <summary>
    /// Sets if the role has tasks assigned for them self.
    /// </summary>
    internal virtual bool HasSelfTask => false;

    /// <summary>
    /// Checks if the role can perform kills. This is typically overridden by roles that are allowed to kill, such as Impostors.
    /// </summary>
    internal virtual bool CanKill => IsImpostor;

    /// <summary>
    /// Indicates whether this role is reliant on using vents. 
    /// This property can be overridden by specific roles that require vent usage, 
    /// </summary>
    internal virtual bool VentReliantRole => false;

    /// <summary>
    /// Indicates whether this role is reliant on tasks. 
    /// This property can be overridden by specific roles that require task usage, 
    /// </summary>
    internal virtual bool TaskReliantRole => false;

    /// <summary>
    /// Indicates whether this role is reliant on guessing. 
    /// This property can be overridden by specific roles that require guessing, 
    /// </summary>
    internal virtual bool GuessReliantRole => false;

    /// <summary>
    /// Indicates whether the role Name is shown above the player's name. 
    /// </summary>
    internal virtual bool ShowRoleAboveName => true;

    /// <summary>
    /// Indicates whether the role Name is shown in endgame outro if they are the winner. 
    /// </summary>
    internal virtual bool ShowRoleInOutro => true;

    /// <summary>
    /// Indicates whether this role is reliant on meetings. 
    /// This property can be overridden by specific roles that require meetings, 
    /// </summary>
    internal virtual bool MeetingReliantRole => false;

    /// <summary>
    /// Checks if the role can use vents. This is typically overridden by roles like Impostors or other vent-using roles.
    /// </summary>
    internal virtual bool CanVent => RoleOptions.CanVentOptionItem?.GetBool() == true || VentReliantRole;

    /// <summary>
    /// Checks if the role can move to other vents while inside a vent.
    /// This is often set to true for roles that can use vents as a movement system, like Impostors.
    /// </summary>
    internal virtual bool CanMoveInVents => true;

    /// <summary>
    /// Checks if the role can perform sabotage actions. This is typically overridden by roles that have the ability to sabotage, such as Impostors.
    /// </summary>
    internal virtual bool CanSabotage => IsImpostor;

    /// <summary>
    /// Check if the role is Enabled.
    /// </summary>
    internal bool IsEnabled => GetChance() > 0f;

    internal float GetChance() => CanBeAssigned ? TBRDataManager.LoadSetting<float>(RoleUID) : 0f;
    internal int GetAmount() => CanBeAssigned ? TBRDataManager.LoadSetting<int>(RoleUID + 1) / AmountSize : 0;

    private int tempOptionNum = 0;
    protected int GetOptionUID()
    {
        var num = tempOptionNum;
        tempOptionNum++;
        return RoleUID + 50 + 5 * num;
    }

    private int tempBaseOptionNum = 0;
    /// <summary>
    /// Do not use this method.
    /// </summary>
    protected int GetBaseOptionID()
    {
        var num = tempBaseOptionNum;
        tempBaseOptionNum++;
        return RoleUID + num;
    }

    /// <summary>
    /// Do not use this method.
    /// </summary>
    protected void ResetOptionIDs()
    {
        tempOptionNum = 0;
        tempBaseOptionNum = 0;
    }

    internal RoleClass Initialize(PlayerControl player, RoleMono roleMono, bool isAssigned = false)
    {
        if (player != null)
        {
            RoleNameAndAbilityAmountText = new(this);
            Networked.Initialize(this);
            Networked.OnReceiveRoleSync += OnReceiveRoleSync;
            _player = player;
            _data = player.Data;
            _roleMono = roleMono;
            roleMono.Setup(this);
            var extendedData = player.ExtendedData();

            SetUpNetworkClass(RoleHash, _player.Data.ClientId);
            CustomRoleManager.AllActiveRoles.Add(this);
            RoleListener.AddRole(this);

            if (!IsAddon)
            {
                extendedData.RoleInfo.Role = this;
                extendedData.RoleInfo.RoleType = RoleType;
                if (isAssigned)
                {
                    extendedData.RoleInfo.RoleHistory.Clear();
                    OnRoleAssigned();
                    SetUpRole();
                }
                else
                {
                    SetUpRole();
                    SetUpRoleAsHost();
                }
                extendedData.RoleInfo.RoleHistory.Add(RoleType);
            }
            else
            {
                if (!player.ExtendedData().RoleInfo.Addons.Any(addon => addon.RoleType == RoleType))
                {
                    extendedData.RoleInfo.Addons.Add((AddonClass)this);
                    if (isAssigned)
                    {
                        OnRoleAssigned();
                        SetUpRole();
                    }
                    else
                    {
                        SetUpRole();
                        SetUpRoleAsHost();
                    }
                }
            }

            extendedData.RoleInfo.DirtyRolesCache();

            if (!GameState.IsFreePlay)
            {
                SetAllCooldownsHalf();
            }
        }

        return this;
    }

    internal void Deinitialize()
    {
        if (HasDeinitialize) return;
        HasDeinitialize = true;
        _roleMono?.Deinitialize();
        CustomRoleManager.AllActiveRoles.Remove(this);
        RoleListener.RemoveRole(this);
        DisposeNetworkClass();

        try
        {
            Logger.LogMethodPrivate("Deinitialize Role Base!", GetType());
            try
            {
                if (this is IRoleAbilityAction action)
                {
                    action.OnResetAbilityState(false);
                }
            }
            catch (Exception ex) { Logger.LogMethodPrivate(ex.ToString(), GetType()); }

            try { OnDeinitialize(); }
            catch (Exception ex) { Logger.LogMethodPrivate(ex.ToString(), GetType()); }
            try
            {
                RoleListener.InvokeRoles<IRoleOtherAction>(role => role.DeinitializeOther(this), role => !role.HasDeinitialize);
            }
            catch (Exception ex) { Logger.LogMethodPrivate(ex.ToString(), GetType()); }

            Logger.LogPrivate($"Finished deinitialize Role Base, now deinitialize Role({RoleName})!");

            // Remove Buttons
            foreach (var button in RoleButtons.Buttons)
            {
                if (button?.Button?.gameObject != null)
                {
                    button.RemoveButton();
                }
            }

            if (!IsAddon)
            {
                if (_player?.ExtendedData()?.RoleInfo != null)
                {
                    _player.ExtendedData().RoleInfo.Role = null;
                }
            }
            else
            {
                _player.ExtendedData().RoleInfo.Addons.Remove((AddonClass)this);
            }

            _player?.ExtendedData()?.RoleInfo?.DirtyRolesCache();
            Utils.DirtyAllNames();

            try { Logger.LogPrivate($"Finished deinitialize Role({RoleName})!"); }
            catch { }
        }
        catch { }
    }

    internal virtual void CleanUp()
    {

    }

    /// <summary>
    /// Sets up the role on the host side to sync<see cref="OnSetUpRole"/>.
    /// </summary>
    internal virtual void SetUpRoleAsHost() { }

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

        if (IsGhostRole)
        {
            if (_player.IsAlive())
            {
                _player.CustomExiled();
                _player.SetDeathReason(DeathReasons.Destroyed, RoleColorHex);
            }

            if (_player.IsLocalPlayer())
            {
                CustomSoundsManager.Instance.StopSound(Prefab.GetCachedPrefab<GuardianAngelRole>().UseSound);
                CustomSoundsManager.Instance.PlaySound(Prefab.GetCachedPrefab<GuardianAngelRole>().UseSound, false);
            }
        }

        if (_player.IsLocalPlayer() && _player.inVent && !CanVent)
        {
            _player.SendRpcBootFromVent(_player.GetPlayerVentId());
        }

        if (_player.IsLocalPlayer())
        {
            if (_player != null)
            {
                RoleButtons.SetUpHauntButton(this);
                RoleButtons.SetUpReportButton(this);
                RoleButtons.SetUpSabotageButton(this);
                RoleButtons.SetUpKillButton(this);
                RoleButtons.SetUpVentButton(this);
            }

            if (RoleOptions.CanCallMeetingOptionItem?.GetBool() != false)
            {
                if (ShipStatus.Instance?.EmergencyButton != null)
                {
                    ShipStatus.Instance.EmergencyButton.Image.sprite = ShipStatusPatch.CatchedMeetingButtonSprite;
                    ShipStatus.Instance.EmergencyButton.enabled = true;
                }
            }
            else
            {
                if (ShipStatus.Instance?.EmergencyButton != null)
                {
                    ShipStatus.Instance.EmergencyButton.Image.sprite = ShipStatus.Instance.BrokenEmergencyButton;
                    ShipStatus.Instance.EmergencyButton.enabled = false;
                }
            }
        }

        Logger.LogPrivate($"Finished setting up Role Base, now setting up Role({RoleName})!");

        OnSetUpRole();
        RoleListener.InvokeRoles<IRoleOtherAction>(role => role.SetUpRoleOther(_player, this));

        Logger.LogPrivate($"Finished setting up Role({RoleName})!");

        _player.UpdateName();
    }

    internal void LoadSettings()
    {
        SetUpSettings();
    }

    protected virtual void SetUpSettings()
    {
        ResetOptionIDs();
        RoleOptions.RoleOptionItem = OptionPercentItem.Create(GetBaseOptionID(), SettingsTab, Utils.GetCustomRoleNameAndColor(RoleType, true), 0f);
        RoleOptions.RoleOptionItem.CreateDescriptionButton(Utils.GetCustomRoleInfo(RoleType, true));
        RoleOptions.AmountOptionItem = OptionIntItem.Create(GetBaseOptionID(), SettingsTab, "Role.Option.Amount", (AmountSize, 15 / AmountSize * AmountSize, AmountSize), AmountSize, ("", ""), RoleOptions.RoleOptionItem);

        if ((IsNeutral || IsApocalypse) && !IsGhostRole)
        {
            RoleOptions.HasImpostorVisionOption = OptionCheckboxItem.Create(GetBaseOptionID(), SettingsTab, "Role.Ability.HasImpostorVision", false, RoleOptions.RoleOptionItem);
        }

        OptionItems.Initialize();
        SetupOptionItems();

        if (MeetingReliantRole)
        {
            RoleOptions.CanCallMeetingOptionItem = OptionCheckboxItem.Create(GetBaseOptionID(), SettingsTab, "Role.Ability.CanCallMeeting", DefaultCanCallMeetingOption, RoleOptions.RoleOptionItem); ;
        }

        bool ventFlag = !IsCrewmate && !VentReliantRole && !IsGhostRole;
        if (ventFlag)
        {
            RoleOptions.CanVentOptionItem = OptionCheckboxItem.Create(GetBaseOptionID(), SettingsTab, "Role.Ability.CanVent", DefaultVentOption, RoleOptions.RoleOptionItem);
        }
        if (AdditionalVentOptions != null)
        {
            RoleOptions.VentCooldownOptionItem = OptionFloatItem.Create(GetBaseOptionID(), SettingsTab, "Role.Ability.VentCooldown", (0f, 180f, 2.5f), AdditionalVentOptions.Cooldown, ("", "s"),
                ventFlag ? RoleOptions.CanVentOptionItem : RoleOptions.RoleOptionItem);
            RoleOptions.VentDurationOptionItem = OptionFloatItem.Create(GetBaseOptionID(), SettingsTab, "Role.Ability.VentDuration", (0f, 180f, 2.5f), AdditionalVentOptions.Duration, ("", "s"),
                ventFlag ? RoleOptions.CanVentOptionItem : RoleOptions.RoleOptionItem, canBeInfinite: true);
        }

        if (TaskReliantRole)
        {
            RoleOptions.OverrideTasksOptionItem = OptionCheckboxItem.Create(GetBaseOptionID(), SettingsTab, "Role.Option.OverrideTasks", false, RoleOptions.RoleOptionItem);
            RoleOptions.CommonTasksOptionItem = OptionIntItem.Create(GetBaseOptionID(), SettingsTab, "Role.Option.CommonTasks", (0, 10, 1), 2, ("", ""), RoleOptions.OverrideTasksOptionItem);
            RoleOptions.LongTasksOptionItem = OptionIntItem.Create(GetBaseOptionID(), SettingsTab, "Role.Option.LongTasks", (0, 10, 1), 2, ("", ""), RoleOptions.OverrideTasksOptionItem);
            RoleOptions.ShortTasksOptionItem = OptionIntItem.Create(GetBaseOptionID(), SettingsTab, "Role.Option.ShortTasks", (0, 10, 1), 4, ("", ""), RoleOptions.OverrideTasksOptionItem);
        }
    }

    protected virtual void SetupOptionItems() { }

    /// <summary>
    /// Checks custom objective win condition for NormalGameMode.
    /// </summary>
    protected static void CheckWinCondition()
    {
        var gamemode = CatchedGameData.Instance?.CurrentGameMode;
        if (gamemode is NormalGameMode normalGameMode)
        {
            normalGameMode.CheckCustomObjectiveWin(forceEnd: true);
        }
    }

    internal void BaseUpdate()
    {
        if (this is IRoleUpdateAction action)
        {
            action.Update();
        }
    }

    internal void BaseFixedUpdate()
    {
        if (this is IRoleUpdateAction action)
        {
            action.FixedUpdate();
        }
    }

    internal void BaseLateUpdate()
    {
        if (this is IRoleUpdateAction action)
        {
            action.LateUpdate();
        }
    }

    internal void SetCooldownAndUse(int id)
    {
        RoleButtons.Buttons.FirstOrDefault(b => b.Id == id)?.SetCooldown();
        RoleButtons.Buttons.FirstOrDefault(b => b.Id == id)?.RemoveUse();
    }

    internal void CheckAndUseAbility(int id, MonoBehaviour? target, TargetType type)
    {
        switch (id)
        {
            case 1:
            case 2:
            case 3:
                break;
            default:
                {
                    if (RoleListener.CheckAllRoles<IRoleAbilityAction>(role => role.CheckAbilityOther(id, this, type, target)) == false)
                    {
                        return;
                    }

                    switch (type)
                    {
                        case TargetType.Player:
                            if (this is IRoleAbilityAction<PlayerControl> actionP)
                            {
                                if (actionP.CheckAbility(id, (PlayerControl)target))
                                {
                                    if (target is PlayerControl playerTarget)
                                    {
                                        RoleListener.InvokeRoles<IRoleInteractedAction>(role => role.PlayerInteracted(_player, playerTarget), role => role == this, _player);
                                        RoleListener.InvokeRoles<IRoleInteractedAction>(role => role.PlayerInteracted(_player, playerTarget), role => role == this, playerTarget);
                                        RoleListener.InvokeRoles<IRoleInteractedAction>(role => role.PlayerInteractedOther(_player, playerTarget), role => role == this);
                                    }

                                    actionP.OnAbility(id, (PlayerControl)target);
                                    SetCooldownAndUse(id);
                                }
                            }
                            break;
                        case TargetType.Body:
                            if (this is IRoleAbilityAction<DeadBody> actionB)
                            {
                                if (actionB.CheckAbility(id, (DeadBody)target))
                                {
                                    actionB.OnAbility(id, (DeadBody)target);
                                    SetCooldownAndUse(id);
                                }
                            }
                            break;
                        case TargetType.Vent:
                            if (this is IRoleAbilityAction<Vent> actionV)
                            {
                                if (actionV.CheckAbility(id, (Vent)target))
                                {
                                    actionV.OnAbility(id, (Vent)target);
                                    SetCooldownAndUse(id);
                                }
                            }
                            break;
                        case TargetType.None:
                            if (this is IRoleAbilityAction actionN)
                            {
                                if (actionN.CheckAbility(id))
                                {
                                    actionN.OnAbility(id);
                                    SetCooldownAndUse(id);
                                }
                            }
                            break;
                    }
                }
                break;
        }
    }

    internal void DurationEnd(int id, bool isTimeOut)
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
    /// Called upon receiving role synchronization data.
    /// BE SURE TO USE ReadFast!!!
    /// </summary>
    internal virtual void OnReceiveRoleSync(RoleNetworked.Data data) { }

    internal void TryOverrideTasks(bool overideOldTask = false)
    {
        Logger.LogMethodPrivate($"Overriding tasks for {GetType().Name}", GetType(), true);

        if (GameState.IsHost && TaskReliantRole && RoleOptions.OverrideTasksOptionItem != null && RoleOptions.OverrideTasksOptionItem.GetBool())
        {
            _player.SetNewTasks(RoleOptions.LongTasksOptionItem.GetInt(), RoleOptions.ShortTasksOptionItem.GetInt(), RoleOptions.CommonTasksOptionItem.GetInt());
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
    internal void SetAllCooldowns()
    {
        Logger.LogMethodPrivate("Setting all button cooldowns", GetType());

        foreach (var button in RoleButtons.Buttons)
        {
            button.SetCooldown(durationState: 0);
        }
    }

    /// <summary>
    /// Sets the cooldown to half for all ability buttons associated with the current role.
    /// This loops through all local ability buttons and applies the appropriate cooldowns.
    /// </summary>
    internal void SetAllCooldownsHalf()
    {
        Logger.LogMethodPrivate("Setting all button cooldowns", GetType());

        foreach (var button in RoleButtons.Buttons)
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
    internal Sprite? LoadAbilitySprite(string name, float size = 115) => Utils.LoadSprite($"TheBetterRoles.Resources.Images.Ability.{name}.png", size);

    /// <summary>
    /// Additional logic for the role on assigned.
    /// This method is called at the end of role assignment to allow customization of role-specific behavior.
    /// </summary>
    internal virtual void OnRoleAssigned() { }

    /// <summary>
    /// Additional setup logic for the role.
    /// This method is called at the end of <see cref="SetUpRole"/> to allow customization of role-specific behavior.
    /// </summary>
    internal virtual void OnSetUpRole() { }

    /// <summary>
    /// Called when the role is de-initialized or removed. Can be used to clean up resources or reset states.
    /// </summary>
    internal virtual void OnDeinitialize() { }

    /// <summary>
    /// Allows info/hint for a role to be formatted with extra information.
    /// </summary>
    internal virtual void FormatRoleInfo(ref string info, bool isLongInfo) { }

    /// <summary>
    /// Returns icon next to player name.
    /// </summary>
    internal virtual string SetNameMark(PlayerControl target) => string.Empty;

    /// <summary>
    /// Add ability counter next to role.
    /// </summary>
    internal virtual void SetAbilityAmountText(ref int maxAmount, ref int currentAmount) { }

    /// <summary>
    /// Determines whether a player's death reason should be revealed to this role.
    /// </summary>
    /// <param name="target">The player whose role is being checked for reveal.</param>
    /// <returns>A boolean value indicating whether the role should be revealed (true) or not (false).</returns>
    internal virtual bool RevealPlayerDeath(PlayerControl target) => false;

    /// <summary>
    /// Determines whether a player's role should be revealed to this role.
    /// </summary>
    /// <param name="target">The player whose role is being checked for reveal.</param>
    /// <returns>A boolean value indicating whether the role should be revealed (true) or not (false).</returns>
    internal virtual bool RevealPlayerRole(PlayerControl target) => false;

    /// <summary>
    /// Determines whether a player's addons should be revealed to this role.
    /// </summary>
    /// <param name="target">The player whose role is being checked for reveal.</param>
    /// <returns>A boolean value indicating whether the role should be revealed (true) or not (false).</returns>
    internal virtual bool RevealPlayerAddons(PlayerControl target) => false;

    /// <summary>
    /// Determines whether a player's info: role, addons, death, etc should be hidden.
    /// </summary>
    /// <param name="target">The player whose role is being checked for reveal.</param>
    /// <returns>A boolean value indicating whether the role should be revealed (true) or not (false).</returns>
    internal virtual bool HidePlayerInfoOther(PlayerControl target) => false;
}