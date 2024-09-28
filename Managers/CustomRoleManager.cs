
using AmongUs.GameOptions;
using Cpp2IL.Core.Extensions;
using System.Reflection;

namespace TheBetterRoles;

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
            return (CustomRoleBehavior)Activator.CreateInstance(selectedType);
        }

        return null;
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

        player?.BetterData()?.RoleInfo?.Role?.Deinitialize();

        CustomRoleBehavior? newRole = CreateNewRoleInstance(r => r.RoleType == role);
        newRole?.Initialize(player);
    }

    public static void AddAddon(PlayerControl player, CustomRoles role)
    {
        if (player == null || player.isDummy) return;

        CustomRoleBehavior? roleClass = allRoles.FirstOrDefault(r => r.RoleType == role);

        if (roleClass != null)
        {
            if (roleClass.RoleTeam == CustomRoleTeam.None && !player.BetterData().RoleInfo.Addons.Contains(roleClass))
            {
                CustomRoleBehavior newRoleInstance = (CustomRoleBehavior)Activator.CreateInstance(roleClass.GetType());
                newRoleInstance._player = player;
                newRoleInstance._data = player.Data;
                if (player.IsLocalPlayer())
                {
                    newRoleInstance.SetUpRole();
                }
                player.BetterData().RoleInfo.Addons.Add(newRoleInstance);
            }
        }
    }
    public static void RemoveAddon(PlayerControl player, CustomRoles role)
    {
        if (player == null || player.isDummy) return;

        CustomRoleBehavior? roleClass = allRoles.FirstOrDefault(r => r.RoleType == role);
        if (roleClass.RoleTeam == CustomRoleTeam.None)
        {
            var addon = player.BetterData().RoleInfo.Addons.FirstOrDefault(addon => addon.GetType() == roleClass.GetType());
            if (addon != null)
            {
                addon.Deinitialize();
            }
        }
    }
}

public enum CustomRoles
{
    Crewmate,
    Impostor,
    Sheriff,
    Morphling,
    Swooper,
    Janitor
}

public enum CustomRoleTeam
{
    Cremate,
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
    KillingAddon,
    AbilityAddon,
    HelpfulAddon,
    HarmfulAddon,
}
