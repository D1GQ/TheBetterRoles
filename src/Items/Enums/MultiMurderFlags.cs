namespace TheBetterRoles.Items.Enums;

[Flags]
enum MultiMurderFlags : short
{
    snapToTarget = 1 << 1,
    spawnBody = 1 << 2,
    showAnimation = 1 << 3,
    playSound = 1 << 4,
}