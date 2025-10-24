using System.Reflection;

namespace TheBetterRoles.Helpers.Random;

internal interface IRandom
{
    /// <summary>Generates a random number between 0 and maxValue</summary>
    internal int Next(int maxValue);
    /// <summary>Generates a random number between minValue and maxValue</summary>
    internal int Next(int minValue, int maxValue);

    /// <summary>Generates a random number between 0 and maxValue</summary>
    internal float Next(float maxValue);
    /// <summary>Generates a random number between minValue and maxValue</summary>
    internal float Next(float minValue, float maxValue);

    // == static ==
    // List of classes implementing IRandom
    internal static Dictionary<int, Type> randomTypes = new()
    {
        { 0, typeof(NetRandomWrapper) },
        { 1, typeof(NetRandomWrapper) },
        { 2, typeof(HashRandomWrapper) },
        { 3, typeof(Xorshift) },
        { 4, typeof(MersenneTwister) },
    };

    internal static IRandom? Instance { get; private set; }
    internal static void SetInstance(IRandom instance)
    {
        if (instance != null)
            Instance = instance;
    }

    internal static void SetInstanceById(int id)
    {
        if (randomTypes.TryGetValue(id, out var type))
        {
            // Current instance is null or current instance type does not match specified type
            if (Instance == null || Instance.GetType() != type)
            {
                try
                {
                    // Create instance even if the constructor is internal
                    Instance = (IRandom?)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, null, null) ?? Instance;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to create instance of type {type.Name}: {ex.Message}", "IRandom.SetInstanceById");
                }
            }
        }
        else
        {
            Logger.Error($"Invalid ID: {id}", "IRandom.SetInstanceById");
        }
    }
}