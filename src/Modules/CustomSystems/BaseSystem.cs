using TheBetterRoles.Items;
using UnityEngine;

namespace TheBetterRoles.Modules;

/// <summary>
/// Base class for creating and managing custom systems in the game, which integrates into the Among Us (AU) systems interface.
/// It provides hooks for system initialization, destruction, and interaction with sabotages, allowing custom systems to be part of the game mechanics during meetings.
/// </summary>
internal abstract class BaseSystem : MonoBehaviour
{
    public static void AddSystems()
    {
        BlackoutSabotageSystem.AddSystem();
    }

    /// <summary>
    /// Invoked when a meeting starts, it iterates through all systems in the current game session.
    /// It checks if the system is of type `CustomSystemTypes.Blackout` or beyond, and if so, it calls the `OnMeetingStart` method of each custom system.
    /// This allows each custom system to react to a meeting start and apply any changes or logic related to the meeting phase of the game.
    /// </summary>
    internal static void InvokeOnMeetingStart()
    {
        foreach (var kvp in ShipStatus.Instance.Systems)
        {
            if ((byte)kvp.Key >= (byte)CustomSystemTypes.Blackout)
            {
                kvp.Value.Cast<BaseSystem>().OnMeetingStart();
            }
        }
    }

    /// <summary>
    /// This Unity method is automatically called when the object is initialized.
    /// It calls the `SetUp` method to allow each custom system to initialize any necessary data or states.
    /// </summary>
    private void Start()
    {
        SetUp();
    }

    /// <summary>
    /// This Unity method is called when the object is destroyed.
    /// It triggers the `Destroy` method, allowing the system to clean up any resources or state when it's no longer needed.
    /// </summary>
    private void OnDestroy()
    {
        Destroy();
    }

    /// <summary>
    /// This method is called during the `Start` method to initialize the system.
    /// Derived classes should override this method to set up their specific logic or variables. By default, it does nothing.
    /// </summary>
    internal virtual void SetUp() { }

    /// <summary>
    /// This method is called during the `OnDestroy` method to clean up the system.
    /// Derived classes should override this method to handle specific clean-up logic. By default, it does nothing.
    /// </summary>
    internal virtual void Destroy() { }

    /// <summary>
    /// Registers the custom system as an impostor sabotage.
    /// It looks for the sabotage system in the game state and attempts to cast the custom system as an `IActivatable` object. 
    /// If successful, it adds the activatable system to the sabotage system's special actions. This is used to integrate custom systems with sabotage actions in the game.
    /// </summary>
    internal void SetAsSabotage()
    {
        if (ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Sabotage, out var saboSys))
        {
            var activatable = TryCast<IActivatable>();
            if (activatable != null)
            {
                var sabotageSystem = saboSys.Cast<SabotageSystemType>();
                sabotageSystem.specials.Add(activatable);
            }
        }
    }

    /// <summary>
    /// This method is invoked when a meeting starts, allowing custom systems to perform any specific actions or logic when the meeting phase begins. 
    /// Derived classes can override this method to implement custom behaviors that should occur when the meeting starts (e.g., modifying the game state, displaying UI elements, etc.).
    /// By default, this method does nothing.
    /// </summary>
    internal virtual void OnMeetingStart() { }
}