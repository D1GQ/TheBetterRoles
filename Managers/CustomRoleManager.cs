using AmongUs.GameOptions;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.Collections;
using System.Reflection;
using TheBetterRoles.Helpers;
using TheBetterRoles.Helpers.Random;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using TheBetterRoles.Roles;
using TheBetterRoles.RPCs;
using UnityEngine;

namespace TheBetterRoles.Managers;

public class RoleAssignmentData
{
    public CustomRoleBehavior? _role;
    public bool CanKill => _role.CanKill;
    public CustomRoles RoleType => _role.RoleType;
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
        .OrderBy(role => (int)role.RoleType)
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

    public static CustomRoleBehavior? GetActiveRoleFromPlayers(Func<CustomRoleBehavior, bool> selector) => Main.AllPlayerControls
        .Where(player => player.BetterData()?.RoleInfo?.AllRoles != null)
        .SelectMany(player => player.BetterData().RoleInfo.AllRoles)
        .FirstOrDefault(selector);

    public static int GetRNGAmount(int min, int max)
    {
        if (min >= max)
        {
            return max;
        }
        else
        {
            return IRandom.Instance.Next(min, max);
        }
    }


    public static IEnumerator AssignRolesCoroutine()
    {
        if (!GameState.IsHost) yield break;

        int ImposterAmount = BetterGameSettings.ImpostorAmount.GetInt();
        int BenignNeutralAmount = GetRNGAmount(BetterGameSettings.MinimumBenignNeutralAmount.GetInt(), BetterGameSettings.MaximumBenignNeutralAmount.GetInt());
        int KillingNeutralAmount = GetRNGAmount(BetterGameSettings.MinimumKillingNeutralAmount.GetInt(), BetterGameSettings.MaximumKillingNeutralAmount.GetInt());

        AdjustImposterAmount(ref ImposterAmount);

        var availableRoles = GatherRoles(false);
        var availableAddons = GatherRoles(true);

        List<PlayerControl> players = Main.AllPlayerControls.Shuffle().ToList();

        Dictionary<PlayerControl, RoleAssignmentData?> playerRoleAssignments = new Dictionary<PlayerControl, RoleAssignmentData?>();

        foreach (var player in players)
        {
            if (player == null) continue;

            var selectedRole = SelectRole(ref availableRoles, ref ImposterAmount, ref BenignNeutralAmount, ref KillingNeutralAmount);
            playerRoleAssignments[player] = selectedRole;

            var selectedAddons = AssignAddons(ref availableAddons, selectedRole, player);

            yield return SyncPlayerRoleCoroutine(player, selectedRole, selectedAddons);
            yield return SyncPlayerRoleCoroutine(player, selectedRole, selectedAddons);
            yield return SyncPlayerRoleCoroutine(player, selectedRole, selectedAddons);
        }

        LogAllAssignments(playerRoleAssignments);

        yield return new WaitForSeconds(0.5f);

        RPC.SendRpcPlayIntro(PlayerControl.LocalPlayer);
    }

    private static void AdjustImposterAmount(ref int ImposterAmount)
    {
        var impostorLimits = new Dictionary<int, int>
        {
            { 3, 1 },
            { 5, 2 },
            { 7, 3 }
        };

        foreach (var limit in impostorLimits)
        {
            if (Main.AllPlayerControls.Length <= limit.Key)
            {
                ImposterAmount = Math.Min(ImposterAmount, limit.Value);
                break;
            }
        }
    }

    private static List<RoleAssignmentData> GatherRoles(bool includeAddons)
    {
        List<RoleAssignmentData> roles = [];

        foreach (var role in allRoles)
        {
            if (role != null && role.GetChance() > 0 && role.CanBeAssigned)
            {
                if (role.IsAddon == includeAddons && !role.IsGhostRole)
                {
                    roles.Add(new RoleAssignmentData { _role = role, Amount = role.GetAmount() });
                }
            }
        }

        return roles.Shuffle().ToList();
    }

    private static RoleAssignmentData? SelectRole(ref List<RoleAssignmentData> availableRoles, ref int ImposterAmount, ref int BenignNeutralAmount, ref int KillingNeutralAmount)
    {
        List<CustomRoles> validRoleTypes = [];
        RoleAssignmentData? lowestRngRole = null;
        int lowestRngValue = int.MaxValue;

        // Filter valid roles and track roles that fail RNG but are close
        foreach (var role in availableRoles)
        {
            if (role.Amount <= 0) continue;

            if (role._role.IsImpostor && ImposterAmount <= 0)
            {
                continue;
            }
            else if (role._role.IsImpostor)
            {
                validRoleTypes.Add(role.RoleType);
                continue;
            }

            if (role._role.IsNeutral)
            {
                if (role._role.IsKillingRole && KillingNeutralAmount <= 0) continue;
                if (!role._role.IsKillingRole && BenignNeutralAmount <= 0) continue;
            }

            validRoleTypes.Add(role.RoleType);
        }

        if (validRoleTypes.Count == 0) return GetFallbackRole(ref ImposterAmount);

        foreach (var roleData in availableRoles.Where(role => validRoleTypes.Contains(role.RoleType)))
        {
            int rng = IRandom.Instance.Next(100);

            if (rng <= roleData._role.GetChance())
            {
                roleData.Amount--;
                UpdateRoleAmounts(roleData, ref ImposterAmount, ref BenignNeutralAmount, ref KillingNeutralAmount);
                return roleData;
            }

            if (rng < lowestRngValue)
            {
                lowestRngRole = roleData;
                lowestRngValue = rng;
            }
        }

        // Step 3: If no role met the RNG condition, assign the one with the lowest RNG
        if (lowestRngRole != null)
        {
            lowestRngRole.Amount--;
            UpdateRoleAmounts(lowestRngRole, ref ImposterAmount, ref BenignNeutralAmount, ref KillingNeutralAmount);
            return lowestRngRole;
        }

        // If no role is selected, return fallback
        return GetFallbackRole(ref ImposterAmount);
    }

    private static void UpdateRoleAmounts(RoleAssignmentData roleData, ref int ImposterAmount, ref int BenignNeutralAmount, ref int KillingNeutralAmount)
    {
        if (roleData._role.IsImpostor) ImposterAmount--;
        else if (roleData._role.IsNeutral)
        {
            if (roleData._role.CanKill) KillingNeutralAmount--;
            else BenignNeutralAmount--;
        }
    }


    private static RoleAssignmentData? GetFallbackRole(ref int ImposterAmount)
    {
        if (ImposterAmount > 0)
        {
            ImposterAmount--;
            return new RoleAssignmentData { _role = Utils.GetCustomRoleClass(CustomRoles.Impostor), Amount = 1, };
        }

        return new RoleAssignmentData { _role = Utils.GetCustomRoleClass(CustomRoles.Crewmate), Amount = 1 };
    }

    private static List<RoleAssignmentData> AssignAddons(ref List<RoleAssignmentData> availableAddons, RoleAssignmentData? assignedRole, PlayerControl player)
    {
        List<CustomRoles> selectedAddons = [];
        int addonAmount = GetRNGAmount(BetterGameSettings.MinimumAddonAmount.GetInt(), BetterGameSettings.MaximumAddonAmount.GetInt());
        int safeAttempts = 0;

        List<CustomRoles> validAddons = availableAddons
            .Where(addonData => addonData.Amount > 0
                && addonData._role is CustomAddonBehavior addon
                && addon.AssignmentCondition(assignedRole._role)
                && addon.CanBeAssignedWithTeam(assignedRole._role.RoleTeam)
                && addon.GetChance() > 0)
            .Shuffle().Select(data => data.RoleType).ToList();


        if (validAddons.Count < addonAmount)
        {
            addonAmount = validAddons.Count;
        }

        while (selectedAddons.Count < addonAmount && safeAttempts < 50)
        {
            foreach (var addonData in availableAddons.Where(role => validAddons.Contains(role.RoleType)))
            {
                if (!selectedAddons.Contains(addonData.RoleType))
                {
                    int rng = IRandom.Instance.Next(100);
                    if (rng <= addonData._role.GetChance())
                    {
                        selectedAddons.Add(addonData.RoleType);
                        addonData.Amount--;
                        safeAttempts = 0;
                        if (selectedAddons.Count >= addonAmount)
                        {
                            break;
                        }
                    }
                }
            }
            safeAttempts++;
        }

        return availableAddons.Where(role => selectedAddons.Contains(role.RoleType)).ToList();
    }

    private static IEnumerator SyncPlayerRoleCoroutine(PlayerControl player, RoleAssignmentData? selectedRole, List<RoleAssignmentData> selectedAddons)
    {
        if (selectedRole?._role != null)
        {
            Logger.Log($"{player.Data.PlayerName} -> {selectedRole._role.RoleName}");
            player.SendRpcSetCustomRole(selectedRole._role.RoleType, isAssigned: true);
            yield return new WaitForSeconds(0.05f);
        }

        foreach (var addon in selectedAddons)
        {
            if (addon?._role != null)
            {
                Logger.Log($"{player.Data.PlayerName} -> {addon._role.RoleName}");
                player.SendRpcSetCustomRole(addon._role.RoleType, isAssigned: true);
                yield return new WaitForSeconds(0.05f);
            }
        }
    }

    private static void LogAllAssignments(Dictionary<PlayerControl, RoleAssignmentData?> assignments)
    {
        foreach (var assignment in assignments)
        {
            Logger.LogPrivate($"Set Role: {assignment.Key.Data.PlayerName} -> {assignment.Value?._role.RoleName}");
        }
    }


    public static List<RoleAssignmentData> availableGhostRoles = [];
    public static void AssignGhostRoleOnDeath(PlayerControl player)
    {
        if (!GameState.IsHost || GameState.IsFreePlay) return;

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
                player.SendRpcSetCustomRole(selectedGhostRole._role.RoleType, isAssigned: true);
                Logger.LogPrivate($"Set Role: {player.Data.PlayerName} -> {selectedGhostRole._role.RoleName}");
            }
        }, 2.5f, shoudLog: false);
    }

    public static void SetNewTasks(this PlayerControl player, int longTasks = -1, int shortTasks = -1, int commonTasks = -1)
    {
        if (!GameState.IsHost) return;

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

        return IRandom.Instance.Next(0, list.Count);
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

    public static void RoleListener(PlayerControl player, Action<CustomRoleBehavior> action, Func<CustomRoleBehavior, bool>? filter = null)
    {
        foreach (var role in player.BetterData().RoleInfo.AllRoles)
        {
            if (role == null || filter != null && !filter(role)) continue;

            action(role);
        }
    }

    public static void RoleListenerOther(Action<CustomRoleBehavior> action, Func<CustomRoleBehavior, bool>? filter = null)
    {
        foreach (var player in Main.AllPlayerControls)
        {
            if (player == null) continue;

            foreach (var role in player.BetterData().RoleInfo.AllRoles)
            {
                if (role == null || filter != null && !filter(role)) continue;

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
                    // if (log) Logger.LogMethodPrivate($"RoleChecks check failed in {role.GetType().Name}.cs for player: {player.Data.PlayerName}", typeof(CustomRoleManager));
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
                    // if (log) Logger.LogMethodPrivate($"RoleChecksOther check failed in {role.GetType().Name}.cs for player: {player.Data.PlayerName}", typeof(CustomRoleManager));
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
                if (role == null || targetRole != null && targetRole != role || filter != null && !filter(role)) continue;

                if (predicate(role))
                {
                    // if (log) Logger.LogMethodPrivate($"RoleChecksAny check passed in {role.GetType().Name} for player: {player.Data.PlayerName}", typeof(CustomRoleManager));
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
                if (role == null || filter != null && !filter(role)) continue;

                if (predicate(role))
                {
                    // if (log) Logger.LogMethodPrivate($"RoleChecksOtherAny check passed in {role.GetType().Name} for player: {player.Data.PlayerName}", typeof(CustomRoleManager));
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
                player.SendRpcSetCustomRole(addon.RoleType, true);
            }
        }
    }

    public static void ClearAddons(this PlayerControl player)
    {
        if (player == null) return;

        var Addons = player.BetterData().RoleInfo.Addons;
        if (Addons.Count > 0)
        {
            var addonsCopy = Addons.ToList();
            foreach (var addon in addonsCopy)
            {
                RemoveAddon(player, addon.RoleType);
            }
        }
    }

    public static CustomRoleBehavior? SetCustomRole(PlayerControl player, CustomRoles role, bool isAssigned = false)
    {
        if (player == null || player?.BetterData()?.RoleInfo?.Role.RoleType == role) return null;

        player?.BetterData()?.RoleInfo?.Role?.Deinitialize();

        player.RawSetRole(RoleTypes.Crewmate);

        CustomRoleBehavior? newRole = CreateNewRoleInstance(r => r.RoleType == role);
        newRole?.Initialize(player, isAssigned);

        return newRole;
    }

    public static CustomRoleBehavior? AddAddon(PlayerControl player, CustomRoles role, bool isAssigned = false)
    {
        if (player == null) return null;

        CustomRoleBehavior? roleClass = allRoles.FirstOrDefault(r => r.RoleType == role);

        if (roleClass != null)
        {
            if (roleClass.IsAddon)
            {
                if (!player.BetterData().RoleInfo.Addons.Any(addon => addon.RoleType == role))
                {
                    CustomRoleBehavior? newRole = CreateNewRoleInstance(r => r.RoleType == role);
                    newRole?.Initialize(player, isAssigned);
                    return newRole;
                }
                else
                {
                    return player.BetterData().RoleInfo.Addons.FirstOrDefault(addon => addon.RoleType == role);
                }
            }
        }

        return null;
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

    public static void PlayIntro()
    {
        if (!DestroyableSingleton<TutorialManager>.InstanceExists)
        {
            if (!GameManager.Instance.GameHasStarted)
            {
                if (GameState.IsHost) Main.AllPlayerControls.ToList().ForEach(player => player.Data.RpcSetTasks(new Il2CppStructArray<byte>(0)));
                Main.AllPlayerControls.ToList().ForEach(PlayerNameColor.Set);
                PlayerControl.LocalPlayer.StopAllCoroutines();
                DestroyableSingleton<HudManager>.Instance.StartCoroutine(DestroyableSingleton<HudManager>.Instance.CoShowIntro());
                DestroyableSingleton<HudManager>.Instance.HideGameLoader();
            }
        }
    }

    public static int RNG(int input) => IRandom.Instance.Next(input);
}

public enum CustomRoles
{
    // Crewmates
    Crewmate,
    Altruist,
    Investigator,
    Mayor,
    Medic,
    Sheriff,
    Snitch,
    Swapper,
    Transporter,
    Veteran,

    // Impostors
    Impostor,
    Blackmailer,
    Janitor,
    Miner,
    Morphling,
    Swooper,
    Undertaker,

    // Neutrals
    Arsonist,
    Amnesiac,
    Glitch,
    Jester,
    Mole,
    Opportunist,
    Pestillence,
    Phantom,
    Plaguebearer,

    // Addons
    Bait,
    ButtonBerry,
    Glow,
    Giant,
    Drunk,
    Lantern,
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