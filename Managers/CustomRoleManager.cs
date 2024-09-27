
using AmongUs.GameOptions;
using Cpp2IL.Core.Extensions;
using System.Reflection;

namespace TheBetterRoles;

public static class CustomRoleManager
{
    public static CustomRoleBehavior[] allRoles = GetAllCustomRoleInstances();

    public static CustomRoleBehavior[] GetAllCustomRoleInstances() => Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(t => t.IsSubclassOf(typeof(CustomRoleBehavior)) && !t.IsAbstract)
        .Select(t => (CustomRoleBehavior)Activator.CreateInstance(t))
        .OrderBy(role => role.RoleId)
        .ToArray();

    public static void ClearRoles(this PlayerControl player)
    {
        var Role = player.BetterData().RoleInfo.Role;
        Role?.RemoveRole();

        var Addons = player.BetterData().RoleInfo.Addons;
        foreach ( var addon in Addons )
        {
            if (addon == null) continue;
            {
                addon.RemoveRole();
            }
        }
    }

    public static void SetCustomRole(this PlayerControl player, CustomRoles role)
    {
        if (player == null || player.isDummy) return;

        // Reset vanilla role
        player.roleAssigned = false;
        player.StartCoroutine(player.CoSetRole(RoleTypes.Crewmate, true));

        if (player.BetterData()?.RoleInfo?.Role != null)
            player.BetterData().RoleInfo.Role.RemoveRole();

        CustomRoleBehavior? roleClass = allRoles.FirstOrDefault(r => r.RoleType == role);
        if (roleClass != null)
        {
            player.BetterData().RoleInfo.Role = (CustomRoleBehavior)Activator.CreateInstance(roleClass.GetType());
            player.BetterData().RoleInfo.RoleType = player.BetterData().RoleInfo.Role.RoleType;
            player.BetterData().RoleInfo.Role._player = player;
            player.BetterData().RoleInfo.Role._data = player.Data;
            player.BetterData().RoleInfo.Role.SetUpRole();
        }
    }
    public static void AddAddon(this PlayerControl player, CustomRoles role)
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
    public static void RemoveAddon(this PlayerControl player, CustomRoles role)
    {
        if (player == null || player.isDummy) return;

        CustomRoleBehavior? roleClass = allRoles.FirstOrDefault(r => r.RoleType == role);
        if (roleClass.RoleTeam == CustomRoleTeam.None)
        {
            var addon = player.BetterData().RoleInfo.Addons.FirstOrDefault(addon => addon.GetType() == roleClass.GetType());
            if (addon != null)
            {
                addon.RemoveRole();
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
