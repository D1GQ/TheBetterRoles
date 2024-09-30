
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

    public static CustomRoleBehavior? GetRoleInstance(CustomRoles role) => allRoles.FirstOrDefault(r => r.RoleType == role);

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

            int ImposterAmount = 1;  // BetterGameSettings.ImposterAmount.GetInt();
            int BenignNeutralAmount = 1; // BetterGameSettings.BenignNeutralAmount.GetInt();
            int KillingNeutralAmount = 1; // BetterGameSettings.KillingNeutralAmount.GetInt();

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

            List<RoleAssignmentData> Roles = [];

            foreach (var role in allRoles)
            {
                if (role.GetChance() <= 0) continue;
                RoleAssignmentData roleData = new() { _role = role, Amount = role.GetAmount() };
                Roles.Add(roleData);
            }

            List<PlayerControl> players = [.. Main.AllPlayerControls];

            for (int i = players.Count - 1; i > 0; i--)
            {
                int j = IRandom.Instance.Next(i + 1);
                (players[i], players[j]) = (players[j], players[i]);
            }

            Dictionary<PlayerControl, RoleAssignmentData?> SetRole = [];

            foreach (var player in players)
            {
                if (player == null) continue;

                Dictionary<RoleAssignmentData, int> RoleAndRng = [];

                foreach (var role in Roles)
                {
                    RoleAndRng[role] = RNG(1000);
                }

                RoleAssignmentData? Role = null;
                int attempt = 100;
                int Value = int.MaxValue;

                while (Role == null && attempt > 0)
                {
                    foreach (var kvp in RoleAndRng.Where(r => ImposterAmount > 0 ? r.Key.RoleTeam == CustomRoleTeam.Impostor : r.Key.RoleTeam != CustomRoleTeam.Impostor
                    && r.Key.Amount > 0 && ImposterAmount > 0))
                    {
                        var chance = kvp.Key.Chance * 10;
                        var rng = kvp.Value;

                        if (rng <= chance && rng < Value && kvp.Key.Amount > 0)
                        {
                            Value = chance - rng > 0 ? chance - rng : 0;
                            Role = kvp.Key;
                        }
                    }

                    attempt--;
                }

                if (Role == null)
                {
                    if (ImposterAmount > 0) Role = new RoleAssignmentData() { _role = GetRoleInstance(CustomRoles.Impostor), Amount = 100 };
                    else Role = new RoleAssignmentData() { _role = GetRoleInstance(CustomRoles.Crewmate), Amount = 100 };
                    if (Role._role.IsImpostor) ImposterAmount--;

                    SetRole[player] = Role;
                }
                else
                {
                    Roles.FirstOrDefault(Role).Amount--;
                    if (Role._role.IsImpostor) ImposterAmount--;
                    if (Role._role.IsNeutral && Role._role.CanKill) KillingNeutralAmount--;
                    if (Role._role.IsNeutral && !Role._role.CanKill) BenignNeutralAmount--;

                    SetRole[player] = Role;
                }
            }

            foreach (var data in SetRole)
            {
                Logger.Log($"{data.Key.Data.PlayerName} -> {data.Value._role.RoleName}");
                data.Key.SetRoleSync(data.Value._role.RoleType);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
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