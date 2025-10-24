namespace TheBetterRoles.Items.Enums;

/// <summary>
/// This enumeration defines flags for multi-permission levels associated with users. 
/// Each flag represents a specific permission that can be assigned to users.
/// These flags can be combined using bitwise operations to represent a user’s access level.
/// </summary>
[Flags]
internal enum MultiPermissionFlags : ushort
{
    Contributor_1 = 1 << 0, // Represents Tier 1 Contributor permission
    Contributor_2 = 1 << 1, // Represents Tier 2 Contributor permission
    Contributor_3 = 1 << 2, // Represents Tier 3 Contributor permission
    Tester = 1 << 3, // Represents Tester permission
    Staff = 1 << 4, // Represents Staff permission
    Dev = 1 << 5, // Represents Developer permission
    All = 1 << 6 // Represents All permissions combined
}