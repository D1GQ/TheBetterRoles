using AmongUs.GameOptions;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.Collections;
using System.Reflection;
using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Helpers.Random;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Roles;
using TheBetterRoles.Roles.Core;
using UnityEngine;

namespace TheBetterRoles.Managers;

internal class RoleAssignmentData
{
    internal RoleClass? _role;
    internal bool CanKill => _role.CanKill;
    internal RoleClassTypes RoleType => _role.RoleType;
    internal RoleClassTeam RoleTeam => _role.RoleTeam;
    internal bool IsGhostRole => _role.RoleCategory == RoleClassCategory.Ghost;
    internal bool IsAddon => _role.IsAddon;
    internal int Chance => (int)_role.GetChance();
    internal int Amount;
}

internal static class CustomRoleManager
{
    internal static HashSet<RoleClass> AllActiveRoles = [];
    internal static readonly Dictionary<Type, HashSet<RoleClass>> RoleListenerMap = [];
    internal static readonly RoleClass[] RolePrefabs = [.. GetAllCustomRoleInstances()];

    internal static RoleClass[] GetAllCustomRoleInstances() => Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(t => t.IsSubclassOf(typeof(RoleClass)) && !typeof(LobbyBehaviorRole).IsAssignableFrom(t) && !t.IsAbstract)
        .Select(Activator.CreateInstance)
        .Cast<RoleClass>()
        .OrderBy(role => (int)role.RoleType)
        .ToArray();

    internal static T? GetRolePrefab<T>() where T : RoleClass
    {
        var foundRole = RolePrefabs.FirstOrDefault(role => role.GetType() == typeof(T));

        if (foundRole is T roleInstance)
        {
            return roleInstance;
        }
        return null;
    }

    internal static RoleClass? CreateNewRoleInstance(Func<RoleClass, bool> selector)
    {
        Type selectedType = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(RoleClass)) && !t.IsAbstract)
            .FirstOrDefault(t =>
            {
                var instance = (RoleClass)Activator.CreateInstance(t);
                return selector(instance);
            });

        if (selectedType != null)
        {
            return Activator.CreateInstance(selectedType) as RoleClass;
        }

        return null;
    }

    internal static RoleClass? GetActiveRoleFromPlayers(Func<RoleClass, bool> selector)
    {
        foreach (var player in Main.AllPlayerControls)
        {
            if (player == null) continue;

            var extendedData = player.ExtendedData();
            if (extendedData == null) continue;

            var roleInfo = extendedData.RoleInfo;
            if (roleInfo?.RoleAssigned != true) continue;

            foreach (var role in roleInfo.AllRoles)
            {
                if (selector(role))
                    return role;
            }
        }
        return null;
    }

    internal static void CleanUpRoles()
    {
        foreach (var role in RolePrefabs)
        {
            role.CleanUp();
        }
    }

    internal static int GetRNGAmount(int min, int max)
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

    internal static Dictionary<NetworkedPlayerInfo, RoleClassTypes> QueuedRoles = [];
    internal static Dictionary<NetworkedPlayerInfo, List<RoleClassTypes>> QueuedAddons = [];

    internal static IEnumerator CoAssignRoles()
    {
        if (!GameState.IsHost) yield break;

        int ImpostorAmount = TBRGameSettings.ImpostorAmount.GetInt();
        int BenignNeutralAmount = GetRNGAmount(TBRGameSettings.MinimumBenignNeutralAmount.GetInt(), TBRGameSettings.MaximumBenignNeutralAmount.GetInt());
        int KillingNeutralAmount = GetRNGAmount(TBRGameSettings.MinimumKillingNeutralAmount.GetInt(), TBRGameSettings.MaximumKillingNeutralAmount.GetInt());
        int ApocalypseAmount = GetRNGAmount(TBRGameSettings.MinimumApocalypseAmount.GetInt(), TBRGameSettings.MaximumApocalypseAmount.GetInt());

        Logger.LogPrivate($"Original Assignment > Impostors: {ImpostorAmount}, Benign Neutrals: {BenignNeutralAmount}, Killing Neutrals: {KillingNeutralAmount}");

        AdjustAmountByPlayers(ref ImpostorAmount);
        AdjustAmountByPlayers(ref BenignNeutralAmount);
        AdjustAmountByPlayers(ref KillingNeutralAmount);
        AdjustAmountByPlayers(ref ApocalypseAmount);

        Logger.LogPrivate($"Player Adjusted Assignment > Impostors: {ImpostorAmount}, Benign Neutrals: {BenignNeutralAmount}, Killing Neutrals: {KillingNeutralAmount}");

        var availableRoles = GatherRoles(false);
        var availableAddons = GatherRoles(true);

        List<PlayerControl> players =
        [
            .. Main.AllPlayerControls.Shuffle().OrderBy(pc => QueuedRoles.ContainsKey(pc.Data) || QueuedAddons.ContainsKey(pc.Data) ? 0 : 1),
        ];

        Dictionary<PlayerControl, RoleAssignmentData?> playerRoleAssignments = [];

        int totalPlayers = players.Count;
        int processedCount = 1;

        foreach (var player in players)
        {
            if (player == null) continue;

            float percentage = (float)processedCount / totalPlayers * 100;
            processedCount++;
            CustomLoadingBarManager.SetLoadingPercent(percentage, "Assigning Roles");
            RPC.SendRpcSetLoadingBar(percentage, "Loading", false);

            RoleAssignmentData selectedRole = null;
            if (!QueuedRoles.ContainsKey(player.Data))
            {
                selectedRole = SelectRole(ref availableRoles, ref ImpostorAmount, ref BenignNeutralAmount, ref KillingNeutralAmount, ref ApocalypseAmount);
            }
            else
            {
                RoleAssignmentData roleData = availableRoles.FirstOrDefault(data => data.RoleType == QueuedRoles[player.Data] && data.Amount > 0) ?? new() { Amount = 1, _role = Utils.GetCustomRoleClass(QueuedRoles[player.Data]) };
                selectedRole = roleData;
                roleData.Amount--;
                UpdateRoleAmounts(roleData, ref ImpostorAmount, ref BenignNeutralAmount, ref KillingNeutralAmount, ref ApocalypseAmount);
            }
            playerRoleAssignments[player] = selectedRole;

            var selectedAddons = AssignAddons(ref availableAddons, selectedRole, player);

            if (QueuedAddons.ContainsKey(player.Data))
            {
                foreach (var addon in QueuedAddons[player.Data])
                {
                    RoleAssignmentData addonData = availableAddons.FirstOrDefault(data => data.RoleType == addon && data.Amount > 0) ?? new() { Amount = 1, _role = Utils.GetCustomRoleClass(addon) };
                    if (!selectedAddons.Contains(addonData))
                    {
                        addonData.Amount--;
                        selectedAddons.Add(addonData);
                    }
                }
            }

            yield return CoSyncPlayerRole(player, selectedRole, selectedAddons);
            LogAllAssignments(player, selectedRole, selectedAddons);

            yield return new WaitForSeconds(0.2f);
        }

        QueuedRoles.Clear();
        QueuedAddons.Clear();

        yield return new WaitForSeconds(0.25f);
        Main.AllPlayerControls.ForEach(player => player.InvokeRoles(role => role.SetUpRoleAsHost()));
        yield return new WaitForSeconds(0.25f);

        RPC.SendRpcPlayIntro();
    }

    private static void AdjustAmountByPlayers(ref int RoleTeamAmount)
    {
        if (RoleTeamAmount <= 0) return;
        int maxAmountOfPlayers = (Main.AllPlayerControls.Count - 1) / 2;
        RoleTeamAmount = Math.Min(RoleTeamAmount, maxAmountOfPlayers);
        if (RoleTeamAmount <= 0) RoleTeamAmount = 1;
    }

    private static List<RoleAssignmentData> GatherRoles(bool getAddons)
    {
        List<RoleAssignmentData> roles = [];

        foreach (var role in RolePrefabs)
        {
            if (role != null && role.GetChance() > 0 && role.CanBeAssigned)
            {
                if (role.IsAddon == getAddons && !role.IsGhostRole)
                {
                    roles.Add(new RoleAssignmentData { _role = role, Amount = role.GetAmount() });
                }
            }
        }

        return roles.Shuffle().ToList();
    }

    private static RoleAssignmentData? SelectRole(ref List<RoleAssignmentData> availableRoles, ref int ImpostorAmount, ref int BenignNeutralAmount, ref int KillingNeutralAmount, ref int ApocalypseAmount)
    {
        List<RoleClassTypes> validRoleTypes = [];
        RoleAssignmentData? lowestRngRole = null;
        int lowestRngValue = int.MaxValue;

        // Filter valid roles and track roles that fail RNG but are close
        foreach (var role in availableRoles)
        {
            if (role.Amount <= 0)
                continue;

            bool isValid = false;

            if (ImpostorAmount > 0 && availableRoles.Any(r => r._role.IsImpostor))
                isValid = role._role.IsImpostor;
            else if (KillingNeutralAmount > 0 && availableRoles.Any(r => r._role.IsNeutral && r._role.IsKillingRole))
                isValid = role._role.IsNeutral && role._role.IsKillingRole;
            else if (BenignNeutralAmount > 0 && availableRoles.Any(r => r._role.IsNeutral && !r._role.IsKillingRole))
                isValid = role._role.IsNeutral && !role._role.IsKillingRole;
            else if (ApocalypseAmount > 0 && availableRoles.Any(r => r._role.IsApocalypse))
                isValid = role._role.IsApocalypse && !role._role.IsKillingRole;
            else
                isValid = role._role.IsCrewmate;

            if (isValid)
                validRoleTypes.Add(role.RoleType);
        }

        if (validRoleTypes.Count == 0) return GetFallbackRole(ref ImpostorAmount);

        foreach (var roleData in availableRoles.Where(role => validRoleTypes.Contains(role.RoleType)))
        {
            int rng = IRandom.Instance.Next(100);

            if (rng <= roleData._role.GetChance())
            {
                roleData.Amount--;
                UpdateRoleAmounts(roleData, ref ImpostorAmount, ref BenignNeutralAmount, ref KillingNeutralAmount, ref ApocalypseAmount);
                return roleData;
            }

            if (rng < lowestRngValue)
            {
                lowestRngRole = roleData;
                lowestRngValue = rng;
            }
        }

        // If no role met the RNG condition, assign the one with the lowest RNG
        if (lowestRngRole != null)
        {
            lowestRngRole.Amount--;
            UpdateRoleAmounts(lowestRngRole, ref ImpostorAmount, ref BenignNeutralAmount, ref KillingNeutralAmount, ref ApocalypseAmount);
            return lowestRngRole;
        }

        // If no role is selected, return fallback
        return GetFallbackRole(ref ImpostorAmount);
    }

    private static void UpdateRoleAmounts(RoleAssignmentData roleData, ref int ImpostorAmount, ref int BenignNeutralAmount, ref int KillingNeutralAmount, ref int ApocalypseAmount)
    {
        if (roleData._role.IsImpostor) ImpostorAmount--;
        else if (roleData._role.IsNeutral)
        {
            if (roleData._role.CanKill) KillingNeutralAmount--;
            else BenignNeutralAmount--;
        }
        else if (roleData._role.IsApocalypse) ApocalypseAmount--;
    }


    private static RoleAssignmentData? GetFallbackRole(ref int ImpostorAmount)
    {
        if (ImpostorAmount > 0)
        {
            ImpostorAmount--;
            return new RoleAssignmentData { _role = Utils.GetCustomRoleClass(RoleClassTypes.Impostor), Amount = 1, };
        }

        return new RoleAssignmentData { _role = Utils.GetCustomRoleClass(RoleClassTypes.Crewmate), Amount = 1 };
    }

    private static List<RoleAssignmentData> AssignAddons(ref List<RoleAssignmentData> availableAddons, RoleAssignmentData? assignedRole, PlayerControl player)
    {
        List<RoleClassTypes> selectedAddons = [];
        int addonAmount = GetRNGAmount(TBRGameSettings.MinimumAddonAmount.GetInt(), TBRGameSettings.MaximumAddonAmount.GetInt());
        int safeAttempts = 0;

        List<RoleClassTypes> validAddons = availableAddons
            .Where(addonData => addonData.Amount > 0
                && addonData._role is AddonClass addon
                && addon.AssignmentConditionWithRole(assignedRole._role)
                && addon.CanBeAssignedWithTeam(assignedRole._role.RoleTeam)
                && addon.IsEnabled)
            .Shuffle().Select(data => data.RoleType).ToList();


        if (validAddons.Count < addonAmount)
        {
            addonAmount = validAddons.Count;
        }

        while (selectedAddons.Count < addonAmount && safeAttempts < 50)
        {
            foreach (var addonData in availableAddons.Where(role => validAddons.Contains(role.RoleType)))
            {
                if (!selectedAddons.Contains(addonData.RoleType)
                    && addonData._role.TryCast<AddonClass>(out var addon)
                    && addon.AddonCompatibilityCheck(selectedAddons))
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

    private static IEnumerator CoSyncPlayerRole(PlayerControl player, RoleAssignmentData? selectedRole, List<RoleAssignmentData> selectedAddons)
    {
        if (selectedRole?._role != null)
        {
            player.SendRpcSetCustomRole(selectedRole._role.RoleType, isAssigned: true);
            yield return new WaitForSeconds(0.05f);
        }

        foreach (var addon in selectedAddons)
        {
            if (addon?._role != null)
            {
                player.SendRpcSetCustomRole(addon._role.RoleType, isAssigned: true);
                yield return new WaitForSeconds(0.05f);
            }
        }
    }

    private static void LogAllAssignments(PlayerControl player, RoleAssignmentData role, List<RoleAssignmentData> addons)
    {
        Logger.LogPrivate($"Set Role: {player.Data.PlayerName} -> {role._role.RoleName}");
        foreach (var addon in addons)
        {
            Logger.LogPrivate($"Add Add-on: {player.Data.PlayerName} -> {addon._role.RoleName}");
        }
    }

    internal static Dictionary<NetworkedPlayerInfo, RoleClassTypes> QueuedGhostRoles = [];
    internal static List<RoleAssignmentData> AvailableGhostRoles = [];

    internal static void GatherAvailableGhostRolesOnStart()
    {
        AvailableGhostRoles.Clear();
        foreach (var role in RolePrefabs)
        {
            if (role != null && role.IsEnabled && !role.IsAddon && role.IsGhostRole && role.CanBeAssigned)
            {
                AvailableGhostRoles.Add(new RoleAssignmentData { _role = role, Amount = role.GetAmount() });
            }
        }
    }
    internal static void AssignGhostRoleOnDeath(PlayerControl player)
    {
        if (!GameState.IsHost || GameState.IsFreePlay) return;

        int shuffleCount = 25;

        for (int s = 0; s < shuffleCount; s++)
        {
            for (int i = AvailableGhostRoles.Count - 1; i > 0; i--)
            {
                int j = IRandom.Instance.Next(i + 1);

                var temp = AvailableGhostRoles[i];
                AvailableGhostRoles[i] = AvailableGhostRoles[j];
                AvailableGhostRoles[j] = temp;
            }
        }

        RoleAssignmentData? selectedGhostRole = null;

        if (!QueuedGhostRoles.ContainsKey(player.Data))
        {
            selectedGhostRole = AssignGhostRole(player);
        }
        else
        {
            RoleAssignmentData roleData = AvailableGhostRoles.FirstOrDefault(data => data.RoleType == QueuedGhostRoles[player.Data] && data.Amount > 0) ?? new() { Amount = 1, _role = Utils.GetCustomRoleClass(QueuedGhostRoles[player.Data]) };
            selectedGhostRole = roleData;
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
        }, 2.5f, shouldLog: false);
    }

    internal static RoleAssignmentData? AssignGhostRole(PlayerControl player)
    {
        if (AvailableGhostRoles.Count > 0)
        {
            foreach (var roleData in AvailableGhostRoles)
            {
                if (roleData.Amount <= 0
                    || roleData.RoleTeam != RoleClassTeam.Neutral
                    && roleData.RoleTeam != player.Role()?.RoleTeam) continue;
                int rng = IRandom.Instance.Next(100);
                if (rng <= roleData._role.GetChance())
                {
                    return roleData;
                }
            }
        }

        return null;
    }

    internal static void SetNewTasks(this PlayerControl player, int longTasks = -1, int shortTasks = -1, int commonTasks = -1)
    {
        if (!GameState.IsHost) return;

        if (player != null)
        {
            if (player?.ExtendedData()?.RoleInfo != null)
            {
                player.ExtendedData().RoleInfo.OverrideLongTasks = longTasks;
                player.ExtendedData().RoleInfo.OverrideShortTasks = shortTasks;
                player.ExtendedData().RoleInfo.OverrideCommonTasks = commonTasks;
            }
            player.Data.RpcSetTasks(new Il2CppStructArray<byte>(0));
        }
    }

    internal static string GetRoleMarks(PlayerControl target)
    {
        if (target == null) return string.Empty;

        var uniqueHashes = new HashSet<int>();
        var sb = new System.Text.StringBuilder();

        foreach (var role in AllActiveRoles)
        {
            if (uniqueHashes.Contains(role.RoleHash)) continue;

            if (uniqueHashes.Add(role.RoleHash))
            {
                var mark = role.SetNameMark(target);
                if (!string.IsNullOrEmpty(mark))
                    sb.Append(mark);
            }
        }

        return sb.ToString();
    }

    internal static void ClearRoles(this PlayerControl player)
    {
        if (player == null) return;

        var oldRole = player.Role();
        if (oldRole != null)
        {
            oldRole.Deinitialize();
        }

        var Addons = player.ExtendedData().RoleInfo.Addons;
        if (Addons.Count > 0)
        {
            var addonsCopy = Addons.ToList();
            foreach (var addon in addonsCopy)
            {
                addon.Deinitialize();
            }
        }
    }

    internal static void ClearAddonsSync(this PlayerControl player)
    {
        if (player == null) return;

        var Addons = player.ExtendedData().RoleInfo.Addons;
        if (Addons.Count > 0)
        {
            var addonsCopy = Addons.ToList();
            foreach (var addon in addonsCopy)
            {
                player.SendRpcSetCustomRole(addon.RoleType, true);
            }
        }
    }

    internal static void ClearAddons(this PlayerControl player)
    {
        if (player == null) return;

        var Addons = player.ExtendedData().RoleInfo.Addons;
        if (Addons.Count > 0)
        {
            var addonsCopy = Addons.ToList();
            foreach (var addon in addonsCopy)
            {
                RemoveAddon(player, addon.RoleType);
            }
        }
    }

    internal static RoleClass? SetCustomRole(PlayerControl player, RoleClassTypes role, bool isAssigned = false, bool bypassAssigned = false)
    {
        if (player == null) return null;

        var oldRole = player.Role();
        if (oldRole != null)
        {
            if (oldRole.RoleType == role && !bypassAssigned) return oldRole;

            oldRole.Deinitialize();
        }

        player.RawSetRole(RoleTypes.Crewmate);

        RoleClass? newRole = CreateNewRoleInstance(r => r.RoleType == role);
        var roleMono = RoleMono.Create();
        newRole?.Initialize(player, roleMono, isAssigned);

        return roleMono.Role;
    }

    internal static RoleClass? AddAddon(PlayerControl player, RoleClassTypes role, bool isAssigned = false)
    {
        if (player == null) return null;

        RoleClass? roleClass = RolePrefabs.FirstOrDefault(r => r.RoleType == role);

        if (roleClass != null)
        {
            if (roleClass.IsAddon)
            {
                if (!player.ExtendedData().RoleInfo.Addons.Any(addon => addon.RoleType == role))
                {
                    RoleClass? newRole = CreateNewRoleInstance(r => r.RoleType == role);
                    var roleMono = RoleMono.Create();
                    newRole?.Initialize(player, roleMono, isAssigned);
                    return roleMono.Role;
                }
                else
                {
                    return player.ExtendedData().RoleInfo.Addons.FirstOrDefault(addon => addon.RoleType == role);
                }
            }
        }

        return null;
    }

    internal static void RemoveAddon(PlayerControl player, RoleClassTypes role)
    {
        if (player == null) return;

        if (player.ExtendedData().RoleInfo.Addons.Any(ad => ad.RoleType == role))
        {
            var Role = player.ExtendedData().RoleInfo.Addons.FirstOrDefault(ad => ad.RoleType == role);
            Role?.Deinitialize();
        }
    }

    internal static void PlayIntro()
    {
        if (!TutorialManager.InstanceExists)
        {
            if (!GameManager.Instance.GameHasStarted)
            {
                if (GameState.IsHost) Main.AllPlayerControls.ToList().ForEach(player => player.Data.RpcSetTasks(new Il2CppStructArray<byte>(0)));
                PlayerControl.LocalPlayer.StopAllCoroutines();
                HudManager.Instance.StartCoroutine(HudManager.Instance.CoShowIntro());
                HudManager.Instance.HideGameLoader();
            }
        }
    }

    internal static int RNG(int input) => IRandom.Instance.Next(input);
}