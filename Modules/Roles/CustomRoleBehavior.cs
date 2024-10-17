
using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using UnityEngine;

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
    /// Set role color. This returns the custom color for the role's team, using the team’s color configuration.
    /// </summary>
    public virtual string RoleColor => Utils.GetCustomRoleTeamColor(RoleTeam);

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
    /// Determines whether the vote-out message is always shown for this role.
    /// </summary>
    public virtual bool AlwaysShowVoteOutMsg => false;

    /// <summary>
    /// Get automatically generated role ID based on the role type. Each role is assigned a unique ID derived from its type.
    /// </summary>
    public int RoleId => 100000 + 200 * (int)RoleType;

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
    public abstract CustomRoles RoleType { get; }

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
    public abstract BetterOptionTab? SettingsTab { get; }

    /// <summary>
    /// Array of setting options for the role. These can be initialized later to provide customization for the role.
    /// </summary>
    public abstract BetterOptionItem[]? OptionItems { get; }

    /// <summary>
    /// The role's specific chance option in the game settings. This allows the chance of this role appearing to be configured.
    /// </summary>
    public BetterOptionItem? RoleOptionItem { get; set; }

    /// <summary>
    /// The role's amount option in the game settings. This allows setting how many players can have this role in a game.
    /// </summary>
    public BetterOptionItem? AmountOptionItem { get; set; }

    /// <summary>
    /// The option that determines whether players can use vents while playing this role. 
    /// </summary>
    public BetterOptionItem? CanVentOptionItem { get; set; }

    /// <summary>
    /// The option that determines if the normal task amount is overrated.
    /// </summary>
    public BetterOptionItem? OverrideTasksOptionItem { get; set; }

    /// <summary>
    /// The option that determines the amount of common tasks assigned to this role.
    /// </summary>
    public BetterOptionItem? CommonTasksOptionItem { get; set; }

    /// <summary>
    /// The option that determines the amount of long tasks assigned to this role when using vents.
    /// </summary>
    public BetterOptionItem? LongTasksOptionItem { get; set; }

    /// <summary>
    /// The option that determines the amount of short tasks assigned to this role.
    /// </summary>
    public BetterOptionItem? ShortTasksOptionItem { get; set; }

    /// <summary>
    /// List of all local ability buttons available to the player for this role. This can include things like kill, sabotage, or vent buttons.
    /// </summary>
    public List<BaseButton> Buttons { get; set; } = [];

    /// <summary>
    /// The kill button for the role, allowing the player to perform kills if applicable.
    /// </summary>
    public TargetButton? KillButton { get; set; }

    /// <summary>
    /// The sabotage button for the role, allowing the player to perform sabotage actions.
    /// </summary>
    public SabotageButton? SabotageButton { get; set; }

    /// <summary>
    /// The vent button for the role, allowing the player to use vents if they have that ability.
    /// </summary>
    public VentButton? VentButton { get; set; }

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
    public float BaseVisionMod => GameOptionsManager.Instance.CurrentGameOptions.GetFloat(!IsImpostor ? FloatOptionNames.CrewLightMod : FloatOptionNames.ImpostorLightMod);

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
    public virtual bool CanKill => false;

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
    public virtual bool CanSabotage => false;

    /// <summary>
    /// Set if the role is allowed to move, for example, if a certain condition freezes movement.
    /// </summary>
    public virtual bool CanMove => true;

    public float GetChance() => GameStates.IsHost && CanBeAssigned ? BetterDataManager.LoadFloatSetting(RoleId) : 0f;
    public int GetAmount() => GameStates.IsHost && CanBeAssigned ? BetterDataManager.LoadIntSetting(RoleId + 1) : 0;

    private int TempOptionNum = 0;
    public int GenerateOptionId(bool firstOption = false)
    {
        if (firstOption)
        {
            TempOptionNum = 1;
        }

        var num = TempOptionNum;
        TempOptionNum++;
        return RoleId + 10 + 5 * num;
    }

    public CustomRoleBehavior Initialize(PlayerControl player)
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

            if (!GameStates.IsFreePlay)
            {
                SetAllCooldowns();
            }
        }

        return this;
    }

    public void Deinitialize()
    {
        OnDeinitialize();
        OnResetAbilityState();

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
            _player.BetterData().RoleInfo.Addons.Remove((CustomAddonBehavior)this);
        }
    }

    /// <summary>
    /// Sets up the role by initializing the option items and calling any additional setup logic from <see cref="OnSetUpRole"/>.
    /// Do not override this method.
    /// </summary>
    protected virtual void SetUpRole()
    {
        if (_player != null)
        {
            SabotageButton = AddButton(new SabotageButton().Create(1, Translator.GetString(StringNames.SabotageLabel), this, true));
            SabotageButton.VisibleCondition = () => { return SabotageButton.Role.CanSabotage; };

            KillButton = AddButton(new TargetButton().Create(2, Translator.GetString(StringNames.KillLabel), GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown, 0, HudManager.Instance.KillButton.graphic.sprite, this, true, GameOptionsManager.Instance.currentNormalGameOptions.KillDistance));
            KillButton.VisibleCondition = () => { return KillButton.Role.CanKill; };
            KillButton.TargetCondition = (PlayerControl target) =>
            {
                return !target.IsImpostorTeammate();
            };

            VentButton = AddButton(new VentButton().Create(3, Translator.GetString(StringNames.VentLabel), 0, 0, this, null, false, true));
            VentButton.VisibleCondition = () => { return CustomRoleManager.RoleChecksAny(_player, role => role.CanVent, false); };
        }

        SetUpSettings();
        OptionItems.Initialize();
        OnSetUpRole();
    }

    public void LoadSettings()
    {
        SetUpSettings();
    }

    protected virtual void SetUpSettings()
    {
        RoleOptionItem = new BetterOptionPercentItem().Create(RoleId, SettingsTab, Utils.GetCustomRoleNameAndColor(RoleType, true), 0f);
        AmountOptionItem = new BetterOptionIntItem().Create(RoleId + 1, SettingsTab, Translator.GetString("Role.Option.Amount"), [1, 15, 1], 1, "", "", RoleOptionItem);
        if (!IsCrewmate && !VentReliantRole)
            CanVentOptionItem = new BetterOptionCheckboxItem().Create(RoleId + 2, SettingsTab, Translator.GetString("Role.Ability.CanVent"), IsImpostor, RoleOptionItem);
        if (TaskReliantRole)
        {
            OverrideTasksOptionItem = new BetterOptionCheckboxItem().Create(RoleId + 3, SettingsTab, Translator.GetString("Role.Option.OverrideTasks"), false, RoleOptionItem);
            CommonTasksOptionItem = new BetterOptionIntItem().Create(RoleId + 4, SettingsTab, Translator.GetString("Role.Option.CommonTasks"), [0, 10, 1], 2, "", "", OverrideTasksOptionItem);
            LongTasksOptionItem = new BetterOptionIntItem().Create(RoleId + 5, SettingsTab, Translator.GetString("Role.Option.LongTasks"), [0, 10, 1], 2, "", "", OverrideTasksOptionItem);
            ShortTasksOptionItem = new BetterOptionIntItem().Create(RoleId + 6, SettingsTab, Translator.GetString("Role.Option.ShortTasks"), [0, 10, 1], 4, "", "", OverrideTasksOptionItem);
        }

        OptionItems.Initialize();
    }

    protected T AddButton<T>(T button) where T : BaseButton
    {
        Buttons.Add(button);
        return button;
    }

    protected void RemoveButton(BaseButton button)
    {
        button.RemoveButton();
        Buttons.Remove(button);
    }

    public void CheckAndUseAbility(int id, int targetId, TargetType type)
    {
        if (CheckRoleAction(id, type == TargetType.Player ? Utils.PlayerFromPlayerId(targetId) : null,
                        type == TargetType.Vent ? ShipStatus.Instance.AllVents.FirstOrDefault(v => v.Id == targetId) : null,
                        type == TargetType.Body ? Main.AllDeadBodys.FirstOrDefault(b => b.ParentId == targetId) : null) == true)
        {
            UseAbility(id, targetId, type);
        }
    }

    private void UseAbility(int id, int targetId, TargetType type)
    {
        OnAbilityUse(id, type == TargetType.Player ? Utils.PlayerFromPlayerId(targetId) : null,
                        type == TargetType.Vent ? ShipStatus.Instance.AllVents.FirstOrDefault(v => v.Id == targetId) : null,
                        type == TargetType.Body ? Main.AllDeadBodys.FirstOrDefault(b => b.ParentId == targetId) : null, null, type);
        SetCooldownAndUse(id);

        var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RoleAction, SendOption.Reliable, -1);
        writer.WriteNetObject(_player);
        writer.Write((int)RoleType);
        writer.Write(id);
        writer.Write(targetId);
        writer.Write((int)type);
        AbilityWriter(id, this, ref writer);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    private void SetCooldownAndUse(int id)
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
            case CustomRPC.RoleAction:
                {
                    var id = reader.ReadInt32();
                    var targetId = reader.ReadInt32();
                    var type = (TargetType)reader.ReadInt32();

                    OnAbilityUse(id, type == TargetType.Player ? Utils.PlayerFromPlayerId(targetId) : null,
                        type == TargetType.Vent ? ShipStatus.Instance.AllVents.FirstOrDefault(v => v.Id == targetId) : null,
                        type == TargetType.Body ? Main.AllDeadBodys.FirstOrDefault(b => b.ParentId == targetId) : null, reader, type);
                    SetCooldownAndUse(id);
                }
                break;
        }
    }

    private void OnAbilityUse(int id, PlayerControl? target, Vent? vent, DeadBody? body, MessageReader? reader, TargetType type)
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
                    CustomRoleManager.RoleListener(_player, role => role.OnAbility(id, reader, this, target, vent, body), role => role == this);
                    CustomRoleManager.RoleListenerOther(role => role.OnAbilityOther(id, reader, this, target, vent, body));
                }
                break;
        }
    }

    private bool CheckRoleAction(int id, PlayerControl? target, Vent? vent, DeadBody? body)
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

    public void TryOverrideTasks()
    {
        if (GameStates.IsHost && TaskReliantRole && OverrideTasksOptionItem != null && OverrideTasksOptionItem.GetBool())
        {
            _player.SetNewTasks(LongTasksOptionItem.GetInt(), ShortTasksOptionItem.GetInt(), CommonTasksOptionItem.GetInt());
        }
    }

    /// <summary>
    /// Sets the cooldown for all ability buttons associated with the current role.
    /// This loops through all local ability buttons and applies the appropriate cooldowns.
    /// </summary>
    public void SetAllCooldowns()
    {
        foreach (var button in Buttons)
        {
            button.SetCooldown();
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
    /// Called once per frame to update the state of the role or perform actions.
    /// Override this method to implement any per-frame logic, such as checking conditions, updating timers, or managing abilities.
    /// </summary>
    public virtual void Update() { }

    /// <summary>
    /// Determines the win condition for the role. This can be overridden by roles that have special win conditions.
    /// </summary>
    public virtual bool WinCondition() => false;

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
    /// Returns icon next to player name.
    /// </summary>
    public virtual string SetNameMark(PlayerControl target) => string.Empty;

    /// <summary>
    /// Host-side check for the ability to murder another player. Returns false if the murder should be prevented.
    /// The host checks if the action is valid based on the killer and target and if it was triggered by an ability.
    /// </summary>
    public virtual bool CheckMurderOther(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility) => true;

    /// <summary>
    /// Host-side check for the local player attempting to murder. This checks if the murder action is allowed.
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
    /// Host-side check for an ability being used by another player. This checks if the action is allowed before execution.
    /// </summary>
    public virtual bool CheckAbilityOther(int id, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body) => true;

    /// <summary>
    /// Host-side check for an ability being used by the local player. The host validates the ability before it is executed.
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
    /// Host-side check when another player attempts to report a body. If the check fails, the report action will be canceled.
    /// </summary>
    public virtual bool CheckBodyReportOther(PlayerControl reporter, NetworkedPlayerInfo? body, bool isButton) => true;

    /// <summary>
    /// Host-side check when the local player attempts to report a body. If the check fails, the report action will be canceled.
    /// </summary>
    public virtual bool CheckBodyReport(PlayerControl reporter, NetworkedPlayerInfo? body, bool isButton) => true;

    /// <summary>
    /// Called after the host has allowed another player to report a body. This executes the logic after the report is approved.
    /// </summary>
    public virtual void OnBodyReportOther(PlayerControl reporter, NetworkedPlayerInfo? body, bool isButton) { }

    /// <summary>
    /// Called after the host has allowed the local player to report a body. This executes the logic after the report is approved.
    /// </summary>
    public virtual void OnBodyReport(PlayerControl reporter, NetworkedPlayerInfo? body, bool isButton) { }

    /// <summary>
    /// Host-side check when another player attempts to use or exit a vent. This checks if the action is allowed before execution.
    /// </summary>
    public virtual bool CheckVentOther(PlayerControl venter, int ventId, bool Exit) => true;

    /// <summary>
    /// Host-side check when the local player attempts to use or exit a vent. This checks if the action is allowed.
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
    /// Called after a meeting has ended, determining what happens after the discussion or vote.
    /// </summary>
    public virtual void OnVotingComplete(MeetingHud.VoterState[] states, NetworkedPlayerInfo? exiled, bool tie) { }

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
    /// Menu will be null if not ran by local player!
    /// </summary>
    public virtual void OnPlayerMenu(int Id, PlayerControl? target, NetworkedPlayerInfo? targetData, PlayerMenu? menu, ShapeshifterPanel? playerPanel) { }

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
    public virtual void OnResetAbilityState(bool isTimeOut = false) { }

    /// <summary>
    /// This method is called at the end of the game to process the winning players.
    /// </summary>
    /// <param name="WinnerIds">A reference to the list containing the IDs of the winning players.</param>
    public virtual void OnGameEnd(ref List<byte> WinnerIds) { }
}
