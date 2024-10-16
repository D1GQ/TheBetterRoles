
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.Reflection;
using TheBetterRoles.Patches;

namespace TheBetterRoles;

public class RoleAssignmentData
{
    public CustomRoleBehavior? _role;
    public bool CanKill => _role.CanKill;
    public CustomRoleTeam RoleTeam => _role.RoleTeam;
    public bool IsGhostRole => _role.RoleCategory == CustomRoleCategory.Ghost;
    public bool IsAddon => _role.IsAddon;
    public int Chance => (int)_role.GetChance();
    public int Amount;
}

public static class CustomRoleManager
{
    public static readonly CustomRoleBehavior[] allRoles = GetAllCustomRoleInstances();

    public static CustomRoleBehavior[] GetAllCustomRoleInstances() => Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(t => t.IsSubclassOf(typeof(CustomRoleBehavior)) && !t.IsAbstract)
        .Select(t => (CustomRoleBehavior)Activator.CreateInstance(t))
        .OrderBy(role => role.RoleId)
        .ToArray();

    public static T? GetRoleInstance<T>() where T : CustomRoleBehavior
    {
        var foundRole = allRoles.FirstOrDefault(role => role.GetType() == typeof(T));

        if (foundRole is T roleInstance)
        {
            return roleInstance;
        }
        return null;
    }

    public static CustomRoleBehavior? CreateNewRoleInstance(Func<CustomRoleBehavior, bool> selector)
    {
        Type selectedType = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(CustomRoleBehavior)) && !t.IsAbstract)
            .FirstOrDefault(t =>
            {
                var instance = (CustomRoleBehavior)Activator.CreateInstance(t);
                return selector(instance);
            });

        if (selectedType != null)
        {
            return Activator.CreateInstance(selectedType) as CustomRoleBehavior;
        }

        return null;
    }

    public static int GetRNGAmount(int min, int max)
    {
        if (min >= max || max <= min)
        {
            return max;
        }
        else
        {
            return IRandom.Instance.Next(min, max);
        }
    }

    public static void AssignRoles()
    {
        if (!GameStates.IsHost) return;

        try
        {
            IRandom.SetInstanceById(0);

            // Define role amounts
            int ImposterAmount = BetterGameSettings.ImposterAmount.GetInt();
            int BenignNeutralAmount = GetRNGAmount(BetterGameSettings.MinimumBenignNeutralAmount.GetInt(), BetterGameSettings.MaximumBenignNeutralAmount.GetInt());
            int KillingNeutralAmount = GetRNGAmount(BetterGameSettings.MinimumKillingNeutralAmount.GetInt(), BetterGameSettings.MaximumKillingNeutralAmount.GetInt());

            var impostorLimits = new Dictionary<int, int>
            {
                { 3, 1 },
                { 5, 2 },
                { 7, 3 }
            };

            // Adjust ImposterAmount based on player count
            foreach (var limit in impostorLimits)
            {
                if (Main.AllPlayerControls.Length <= limit.Key)
                {
                    ImposterAmount = Math.Min(ImposterAmount, limit.Value);
                    break;
                }
            }

            // Gather all available roles dynamically
            List<RoleAssignmentData> availableRoles = [];
            foreach (var role in allRoles)
            {
                if (role != null && role.GetChance() > 0 && !role.IsAddon && !role.IsGhostRole && role.CanBeAssigned)
                {
                    availableRoles.Add(new RoleAssignmentData { _role = role, Amount = role.GetAmount() });
                }
            }

            List<RoleAssignmentData> availableAddons = [];
            foreach (var role in allRoles)
            {
                if (role != null && role.GetChance() > 0 && role.IsAddon && !role.IsGhostRole && role.CanBeAssigned)
                {
                    availableAddons.Add(new RoleAssignmentData { _role = role, Amount = role.GetAmount() });
                }
            }

            int shuffleCount = 25;

            // Shuffle the available roles to randomize selection multiple times
            for (int s = 0; s < shuffleCount; s++)
            {
                for (int i = availableAddons.Count - 1; i > 0; i--)
                {
                    int j = IRandom.Instance.Next(i + 1);

                    var temp = availableAddons[i];
                    availableAddons[i] = availableAddons[j];
                    availableAddons[j] = temp;
                }
            }

            for (int s = 0; s < shuffleCount; s++)
            {
                for (int i = availableRoles.Count - 1; i > 0; i--)
                {
                    int j = IRandom.Instance.Next(i + 1);

                    var temp = availableRoles[i];
                    availableRoles[i] = availableRoles[j];
                    availableRoles[j] = temp;
                }
            }


            // Shuffle players multiple times
            List<PlayerControl> players = new(Main.AllPlayerControls);
            for (int s = 0; s < shuffleCount; s++)
            {
                for (int i = players.Count - 1; i > 0; i--)
                {
                    int j = IRandom.Instance.Next(i + 1);
                    (players[i], players[j]) = (players[j], players[i]);
                }
            }

            // Prepare a dictionary to store player-to-role assignments
            Dictionary<PlayerControl, RoleAssignmentData?> playerRoleAssignments = [];

            foreach (var player in players)
            {
                if (player == null) continue;

                List<RoleAssignmentData> validRoles = [];

                // Filter valid roles based on multiple conditions in one loop
                foreach (var role in availableRoles)
                {
                    if (role.Amount > 0)
                    {
                        if (role._role.IsImpostor)
                        {
                            if (ImposterAmount <= 0) continue;
                        }

                        if (role._role.IsNeutral && !role._role.CanKill)
                        {
                            if (BenignNeutralAmount <= 0) continue;
                        }

                        if (role._role.IsNeutral && role._role.CanKill)
                        {
                            if (KillingNeutralAmount <= 0) continue;
                        }

                        validRoles.Add(role);
                    }
                }

                RoleAssignmentData? selectedRole = null;

                // If there are valid roles, randomly select one based on chance
                if (validRoles.Count > 0)
                {
                    foreach (var roleData in validRoles)
                    {
                        int rng = IRandom.Instance.Next(100);
                        if (rng <= roleData._role.GetChance())
                        {
                            selectedRole = roleData;
                            break;
                        }
                    }
                }

                // If no role could be selected, fallback to default roles (Impostor or Crewmate)
                if (selectedRole == null)
                {
                    if (ImposterAmount > 0)
                    {
                        selectedRole = new RoleAssignmentData
                        {
                            _role = Utils.GetCustomRoleClass(CustomRoles.Impostor),
                            Amount = 1
                        };
                    }
                    else
                    {
                        selectedRole = new RoleAssignmentData
                        {
                            _role = Utils.GetCustomRoleClass(CustomRoles.Crewmate),
                            Amount = 1
                        };
                    }
                }

                playerRoleAssignments[player] = selectedRole;
                selectedRole.Amount--;

                if (selectedRole._role.IsImpostor) ImposterAmount--;
                if (selectedRole._role.IsNeutral && selectedRole.CanKill) KillingNeutralAmount--;
                if (selectedRole._role.IsNeutral && !selectedRole.CanKill) BenignNeutralAmount--;

                // Set Addons
                {
                    int safeAttempts = 0;
                    int addonAmount = GetRNGAmount(BetterGameSettings.MinimumAddonAmount.GetInt(), BetterGameSettings.MaximumAddonAmount.GetInt());
                    List<RoleAssignmentData> selectedAddons = [];

                    while (addonAmount > 0 && safeAttempts < 50)
                    {
                        foreach (var roleData in availableAddons)
                        {
                            if (roleData.Amount <= 0) continue;
                            if (roleData._role is CustomAddonBehavior addon)
                            {
                                if (!addon.AssignmentCondition(playerRoleAssignments[player]._role)) continue;
                                if (!addon.CanBeAssignedWithTeam(playerRoleAssignments[player]._role.RoleTeam)) continue;
                                if (addon.RoleCategory == CustomRoleCategory.EvilAddon && playerRoleAssignments[player]._role.IsCrewmate) continue;
                                if (addon.RoleCategory == CustomRoleCategory.GoodAddon && !playerRoleAssignments[player]._role.IsCrewmate) continue;

                                int rng = IRandom.Instance.Next(100);
                                if (!selectedAddons.Contains(roleData) && roleData.Amount > 0 && rng <= addon.GetChance())
                                {
                                    selectedAddons.Add(roleData);
                                    addonAmount--;
                                    roleData.Amount--;
                                    safeAttempts = 0;
                                    break;
                                }
                            }
                        }

                        safeAttempts++;
                    }

                    foreach (var roleData in selectedAddons)
                    {
                        Logger.Log($"{player.Data.PlayerName} -> {roleData._role.RoleName}");
                        player.SetRoleSync(roleData._role.RoleType);
                    }
                }
            }

            foreach (var assignment in playerRoleAssignments)
            {
                Logger.Log($"{assignment.Key.Data.PlayerName} -> {assignment.Value._role.RoleName}");
                assignment.Key.SetRoleSync(assignment.Value._role.RoleType);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }

    public static List<RoleAssignmentData> availableGhostRoles = [];
    public static void AssignGhostRoleOnDeath(PlayerControl player)
    {
        if (!GameStates.IsHost || GameStates.IsFreePlay) return;

        IRandom.SetInstanceById(0);

        // Gather all available roles dynamically
        if (!availableGhostRoles.Any())
        {
            foreach (var role in allRoles)
            {
                if (role != null && role.GetChance() > 0 && !role.IsAddon && role.IsGhostRole && role.CanBeAssigned)
                {
                    if (role.RoleTeam != CustomRoleTeam.Neutral && role.RoleTeam != player.Role().RoleTeam) continue;

                    availableGhostRoles.Add(new RoleAssignmentData { _role = role, Amount = role.GetAmount() });
                }
            }
        }

        int shuffleCount = 25;

        for (int s = 0; s < shuffleCount; s++)
        {
            for (int i = availableGhostRoles.Count - 1; i > 0; i--)
            {
                int j = IRandom.Instance.Next(i + 1);

                var temp = availableGhostRoles[i];
                availableGhostRoles[i] = availableGhostRoles[j];
                availableGhostRoles[j] = temp;
            }
        }

        RoleAssignmentData? selectedGhostRole = null;

        // If there are valid roles, randomly select one based on chance
        if (availableGhostRoles.Count > 0)
        {
            foreach (var roleData in availableGhostRoles)
            {
                if (roleData.Amount <= 0) return;
                int rng = IRandom.Instance.Next(100);
                if (rng <= roleData._role.GetChance())
                {
                    selectedGhostRole = roleData;
                    break;
                }
            }
        }

        _ = new LateTask(() =>
        {
            if (selectedGhostRole != null)
            {
                selectedGhostRole.Amount--;
                player.ClearAddonsSync();
                player.SetRoleSync(selectedGhostRole._role.RoleType);
            }
        }, 2.5f, shoudLog: false);
    }

    public static void SetNewTasks(this PlayerControl player, int longTasks = -1, int shortTasks = -1, int commonTasks = -1)
    {
        if (!GameStates.IsHost) return;

        if (player != null)
        {
            if (player?.BetterData()?.RoleInfo != null)
            {
                player.BetterData().RoleInfo.OverrideLongTasks = longTasks;
                player.BetterData().RoleInfo.OverrideShortTasks = shortTasks;
                player.BetterData().RoleInfo.OverrideCommonTasks = commonTasks;
            }
            player.Data.RpcSetTasks(new Il2CppStructArray<byte>(0));
        }
    }

    public static int GetRandomIndex<T>(Il2CppSystem.Collections.Generic.List<T> list)
    {
        if (list == null || list.Count == 0)
            throw new ArgumentException("List is null or empty!");

        System.Random random = new System.Random();
        return random.Next(0, list.Count); // Returns a random index from 0 to list.Count - 1
    }


    public static void RoleUpdate(PlayerControl player)
    {
        var betterData = player?.BetterData();
        if (betterData?.RoleInfo == null || betterData.RoleInfo.AllRoles == null) return;

        if (betterData.RoleInfo.AllRoles.Any())
        {
            foreach (var role in betterData.RoleInfo.AllRoles)
            {
                role?.Update();

                if (player.IsLocalPlayer())
                {
                    if (role?.Buttons != null)
                    {
                        foreach (var button in role.Buttons)
                        {
                            if (button?.ActionButton == null) continue;

                            button.Update();
                        }
                    }
                }
            }
        }
    }

    public static string GetRoleMarks(PlayerControl target)
    {
        string Marks = "";

        if (target != null)
        {
            foreach (var player in Main.AllPlayerControls)
            {
                if (player == null) continue;

                foreach (var role in player.BetterData().RoleInfo.AllRoles)
                {
                    if (role == null || role.SetNameMark(target) == string.Empty) continue;

                    Marks += role.SetNameMark(target);
                }
            }
        }

        return Marks;
    }

    public static void RoleListener(PlayerControl player, Action<CustomRoleBehavior> action, CustomRoleBehavior? targetRole = null)
    {
        foreach (var role in player.BetterData().RoleInfo.AllRoles)
        {
            if (role == null || targetRole != null && targetRole != role) continue;

            action(role);
        }
    }

    public static void RoleListenerOther(Action<CustomRoleBehavior> action)
    {
        foreach (var player in Main.AllPlayerControls)
        {
            if (player == null) continue;

            foreach (var role in player.BetterData().RoleInfo.AllRoles)
            {
                if (role == null) continue;

                action(role);
            }
        }
    }

    public static bool RoleChecks(this PlayerControl player, Func<CustomRoleBehavior, bool> predicate, bool log = true,
        CustomRoleBehavior? targetRole = null, Func<CustomRoleBehavior, bool>? filter = null)
    {
        if (player.RoleAssigned() && player != null)
        {
            foreach (var role in player.BetterData().RoleInfo.AllRoles)
            {
                if (role == null || targetRole != null && targetRole != role || filter != null && !filter(role)) continue;

                if (!predicate(role))
                {
                    if (log) Logger.Log($"Role check failed in {role.GetType().Name}.cs {predicate.GetType().Name} for player: {player.Data.PlayerName}", "CustomRoleManager");
                    return false;
                }
            }
        }

        return true;
    }

    public static bool RoleChecksOther(Func<CustomRoleBehavior, bool> predicate, bool log = true,
         Func<CustomRoleBehavior, bool>? filter = null)
    {
        foreach (var player in Main.AllPlayerControls)
        {
            if (player == null) continue;

            foreach (var role in player.BetterData().RoleInfo.AllRoles)
            {
                if (role == null || filter != null && !filter(role)) continue;

                if (!predicate(role))
                {
                    if (log) Logger.Log($"Role check failed in {role.GetType().Name}.cs {predicate.GetType().Name} for player: {player.Data.PlayerName}", "CustomRoleManager");
                    return false;
                }
            }
        }

        return true;
    }

    public static bool RoleChecksAny(this PlayerControl player, Func<CustomRoleBehavior, bool> predicate, bool log = true,
        CustomRoleBehavior? targetRole = null, Func<CustomRoleBehavior, bool>? filter = null)
    {
        if (player.RoleAssigned() && player != null)
        {
            foreach (var role in player.BetterData().RoleInfo.AllRoles)
            {
                if (role == null || (targetRole != null && targetRole != role) || (filter != null && !filter(role))) continue;

                if (predicate(role))
                {
                    if (log) Logger.Log($"Role check passed in {role.GetType().Name} for player: {player.Data.PlayerName}", "CustomRoleManager");
                    return true;
                }
            }
        }

        return false;
    }


    public static bool RoleChecksOtherAny(Func<CustomRoleBehavior, bool> predicate, bool log = true,
        Func<CustomRoleBehavior, bool>? filter = null)
    {
        foreach (var player in Main.AllPlayerControls)
        {
            if (player == null) continue;

            foreach (var role in player.BetterData().RoleInfo.AllRoles)
            {
                if (role == null || (filter != null && !filter(role))) continue;

                if (predicate(role))
                {
                    if (log) Logger.Log($"Role check passed in {role.GetType().Name} for player: {player.Data.PlayerName}", "CustomRoleManager");
                    return true;
                }
            }
        }

        return false;
    }

    public static void ClearRoles(this PlayerControl player)
    {
        if (player == null) return;

        var Role = player.BetterData().RoleInfo.Role;
        if (Role != null)
        {
            Role?.Deinitialize();
        }

        var Addons = player.BetterData().RoleInfo.Addons;
        if (Addons.Count > 0)
        {
            var addonsCopy = Addons.ToList();
            foreach (var addon in addonsCopy)
            {
                addon.Deinitialize();
            }
        }
    }

    public static void ClearAddonsSync(this PlayerControl player)
    {
        if (player == null) return;

        var Addons = player.BetterData().RoleInfo.Addons;
        if (Addons.Count > 0)
        {
            var addonsCopy = Addons.ToList();
            foreach (var addon in addonsCopy)
            {
                player.SetRoleSync(addon.RoleType, true);
            }
        }
    }


    public static void SetCustomRole(PlayerControl player, CustomRoles role)
    {
        if (player == null) return;

        player.RawSetRole(RoleTypes.Crewmate);

        player?.BetterData()?.RoleInfo?.Role?.Deinitialize();

        CustomRoleBehavior? newRole = CreateNewRoleInstance(r => r.RoleType == role);
        newRole?.Initialize(player);

        PlayIntro();
    }

    public static void PlayIntro()
    {
        if (!DestroyableSingleton<TutorialManager>.InstanceExists)
        {
            if (!GameManager.Instance.GameHasStarted && Main.AllPlayerControls.All(pc => pc.Data != null && pc.BetterData().RoleInfo.RoleAssigned || pc.Data.Disconnected))
            {
                if (GameStates.IsHost) Main.AllPlayerControls.ToList().ForEach(player => player.Data.RpcSetTasks(new Il2CppStructArray<byte>(0)));
                Main.AllPlayerControls.ToList().ForEach(PlayerNameColor.Set);
                PlayerControl.LocalPlayer.StopAllCoroutines();
                DestroyableSingleton<HudManager>.Instance.StartCoroutine(DestroyableSingleton<HudManager>.Instance.CoShowIntro());
                DestroyableSingleton<HudManager>.Instance.HideGameLoader();
            }
        }
    }

    public static void AddAddon(PlayerControl player, CustomRoles role)
    {
        if (player == null) return;

        CustomRoleBehavior? roleClass = allRoles.FirstOrDefault(r => r.RoleType == role);

        if (roleClass != null)
        {
            if (roleClass.IsAddon && !player.BetterData().RoleInfo.Addons.Contains(roleClass))
            {
                CustomRoleBehavior? newRole = CreateNewRoleInstance(r => r.RoleType == role);
                newRole?.Initialize(player);
            }
        }
    }

    public static void RemoveAddon(PlayerControl player, CustomRoles role)
    {
        if (player == null) return;

        if (player.BetterData().RoleInfo.Addons.Any(ad => ad.RoleType == role))
        {
            var Role = player.BetterData().RoleInfo.Addons.FirstOrDefault(ad => ad.RoleType == role);
            if (Role != null)
            {
                Role.Deinitialize();
            }
        }
    }

    public static int RNG(int input) => IRandom.Instance.Next(input);
}

public enum CustomRoles
{
    // Crewmates
    Altruist,
    Crewmate,
    Medic,
    Sheriff,
    Snitch,
    Veteran,
    Investigator,

    // Impostors
    Impostor,
    Janitor,
    Miner,
    Morphling,
    Swooper,

    // Neutrals
    Jester,
    Mole,
    Opportunist,
    Pestillence,
    Plaguebearer,
    Phantom,
    Amnesiac,

    // Addons
    Bait,
    ButtonBerry,
    Giant,
    NoiseMaker,
    Swift,
    Tracker,
}

public enum CustomRoleTeam
{
    Crewmate,
    Impostor,
    Neutral,
    None
}

public enum CustomRoleCategory
{
    // Roles
    Vanilla,

    Information,
    Benign,
    Evil,
    Killing,
    Support,
    Chaos,
    Ghost,

    // Addons
    GeneralAddon,
    GoodAddon,
    EvilAddon,
    AbilityAddon,
    HelpfulAddon,
    HarmfulAddon,
}