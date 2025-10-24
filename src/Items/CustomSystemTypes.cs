namespace TheBetterRoles.Items;

internal readonly struct CustomSystemTypes
{
    internal static void Initialize()
    {
        SystemTypeHelpers.AllTypes = SystemTypeHelpers.AllTypes.Concat(All.Select(t => t.systemType)).ToArray();
    }

    internal static List<CustomSystemTypes> All { get; } = [];
    private static readonly Dictionary<SystemTypes, CustomSystemTypes> _mapping = [];

    internal readonly SystemTypes systemType;
    internal readonly StringNames stringName;

    private CustomSystemTypes(int systemType, StringNames stringName = StringNames.None)
    {
        this.systemType = (SystemTypes)systemType;
        this.stringName = stringName;

        All.Add(this);
        _mapping.Add(this.systemType, this);
    }

    internal static bool TryGetFromSystemType(SystemTypes systemTypes, out CustomSystemTypes result) => _mapping.TryGetValue(systemTypes, out result);

    public static implicit operator SystemTypes(CustomSystemTypes self) => self.systemType;

    public static explicit operator byte(CustomSystemTypes self) => (byte)self.systemType;

    internal static readonly CustomSystemTypes Blackout = new(180);

    internal static readonly CustomSystemTypes ReverseMap = new(181);
}