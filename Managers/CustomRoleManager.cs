
using AmongUs.GameOptions;
using System.Reflection;

namespace TheBetterRoles;

public class RoleAssignmentData
{
    public void SetRoleToPlayer() => _player.SetRoleSync(_role.RoleType);
    public PlayerControl? _player;
    public CustomRoleBehavior? _role;

    public CustomRoleTeam RoleTeam => _role.RoleTeam;
    public bool IsAddon => _role.IsAddon;
    public float Chance => _role.GetChance();
    public float Amount;
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

        if (player.Data.Role == null)
        {
            player.StartCoroutine(player.CoSetRole(RoleTypes.Crewmate, false));
        }
        else
        {
            player.RawSetRole(RoleTypes.Crewmate);
        }

        player?.BetterData()?.RoleInfo?.Role?.Deinitialize();

        CustomRoleBehavior? newRole = CreateNewRoleInstance(r => r.RoleType == role);
        newRole?.Initialize(player);
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