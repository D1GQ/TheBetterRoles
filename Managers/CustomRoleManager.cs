
using AmongUs.GameOptions;
using System.Linq;
using System.Reflection;
using TheBetterRoles.Patches;

namespace TheBetterRoles;

public class RoleAssignmentData
{
    public CustomRoleBehavior? _role;
    public CustomRoleTeam RoleTeam => _role.RoleTeam;
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

    public static void AssignRoles()
    {
        if (!GameStates.IsHost) return;

        try
        {
            IRandom.SetInstanceById(0);

            // Define role amounts
            int ImposterAmount = 1;  // BetterGameSettings.ImposterAmount.GetInt();
            int BenignNeutralAmount = 1; // BetterGameSettings.BenignNeutralAmount.GetInt();
            int KillingNeutralAmount = 1; // BetterGameSettings.KillingNeutralAmount.GetInt();

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
                if (role.GetChance() > 0 && role.GetAmount() > 0 && !role.IsAddon)
                {
                    availableRoles.Add(new RoleAssignmentData { _role = role, Amount = role.GetAmount() });
                }
            }


            // Shuffle the available roles to randomize selection multiple times
            int shuffleCount = 25;

            for (int s = 0; s < shuffleCount; s++)
            {
                for (int i = availableRoles.Count - 1; i > 0; i--)
                {
                    int j = IRandom.Instance.Next(i + 1); // Get a random index
                                                          // Swap the elements at indices i and j
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

                // Filter valid roles based on current game needs
                var validRoles = availableRoles
                    .Where(r => r.Amount > 0 && (ImposterAmount > 0 ? r._role.RoleTeam == CustomRoleTeam.Impostor : r._role.RoleTeam != CustomRoleTeam.Impostor))
                    .ToList();

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
                        ImposterAmount--;
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
                if (selectedRole._role.IsNeutral && selectedRole._role.CanKill) KillingNeutralAmount--;
                if (selectedRole._role.IsNeutral && !selectedRole._role.CanKill) BenignNeutralAmount--;
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

    public static void RoleUpdate(PlayerControl player)
    {
        foreach (var role in player.BetterData().RoleInfo.AllRoles)
        {
            if (role == null) continue;

            role.Update();

            if (player.IsLocalPlayer())
            {
                foreach (var button in role.Buttons)
                {
                    if (button?.ActionButton == null) continue;

                    button.Update();
                }
            }
        }
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

    public static bool RoleChecks(this PlayerControl player, Func<CustomRoleBehavior, bool> predicate, bool log = true, CustomRoleBehavior? targetRole = null)
    {
        foreach (var role in player.BetterData().RoleInfo.AllRoles)
        {
            if (role == null || targetRole != null && targetRole != role) continue;

            if (!predicate(role))
            {
                if (log) Logger.Log($"Role check failed in {role.GetType().Name}.cs for player: {player.Data.PlayerName}", "CustomRoleManager");
                return false;
            }
        }

        return true;
    }

    public static bool RoleChecksOther(Func<CustomRoleBehavior, bool> predicate, bool log = true)
    {
        foreach (var player in Main.AllPlayerControls)
        {
            if (player == null) continue;

            foreach (var role in player.BetterData().RoleInfo.AllRoles)
            {
                if (role == null) continue;

                if (!predicate(role))
                {
                    if (log) Logger.Log($"Role check failed in {role.GetType().Name}.cs for player: {player.Data.PlayerName}", "CustomRoleManager");
                    return false;
                }
            }
        }

        return true;
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
            foreach (var addon in Addons)
            {
                if (addon == null) continue;

                addon.Deinitialize();
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
            if (Main.AllPlayerControls.All(pc => pc.Data != null && pc.BetterData().RoleInfo.RoleAssigned || pc.Data.Disconnected))
            {
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
    // Roles
    Crewmate,
    Impostor,
    Sheriff,
    Morphling,
    Swooper,
    Janitor,

    // Addons
    ButtonBerry,
    Swift,
    Giant,
    NoiseMaker,
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

    Benign,
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