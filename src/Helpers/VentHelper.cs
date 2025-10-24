using UnityEngine;

namespace TheBetterRoles.Helpers;

/// <summary>
/// A helper class for managing and manipulating vent objects within the game.
/// </summary>
internal static class VentHelper
{
    // Set outline properties for a vent
    /// <summary>
    /// Sets the outline visibility and color of the vent, as well as the color for the main area.
    /// </summary>
    internal static void SetOutline(this Vent vent, Color color, bool showOutline, bool showMain)
    {
        if (vent == null) return;
        vent.myRend.material.SetFloat("_Outline", showOutline ? 1 : 0);
        vent.myRend.material.SetColor("_OutlineColor", color);
        vent.myRend.material.SetColor("_AddColor", showMain ? color : Color.clear);
    }

    // Set a unique, unused vent ID
    /// <summary>
    /// Assigns a unique, unused ID to the vent by checking all other vents and selecting an available ID.
    /// </summary>
    internal static void SetUnusedVentId(this Vent vent)
    {
        var usedIds = Main.AllVents
            .Where(v => v != vent)
            .Select(v => v.Id)
            .OrderBy(id => id)
            .ToHashSet();

        int newId = 0;
        while (usedIds.Contains(newId))
        {
            newId++;
        }

        vent.Id = newId;
    }

    // Create a copy of the vent
    /// <summary>
    /// Creates a copy of the vent, optionally with a new name and/or an overridden vent ID.
    /// </summary>
    internal static Vent Copy(this Vent vent, string name = "", int? overrideVentId = null)
    {
        Vent newVent = UnityEngine.Object.Instantiate(vent, vent.transform.parent);
        newVent.UnsetVents();
        if (overrideVentId == null)
        {
            newVent.SetUnusedVentId();
        }
        else
        {
            newVent.Id = (int)overrideVentId;
        }
        var con = newVent.GetComponent<VentCleaningConsole>();
        if (con != null)
        {
            con.ConsoleId = newVent.Id;
        }
        var allVents = ShipStatus.Instance.AllVents.ToList();
        allVents.Add(newVent);
        ShipStatus.Instance.AllVents = allVents.ToArray();
        if (!string.IsNullOrEmpty(name))
            newVent.name = name;
        return newVent;
    }

    // Remove a vent from the game
    /// <summary>
    /// Removes the vent from the list of all vents and destroys the vent object.
    /// </summary>
    internal static void Remove(this Vent vent)
    {
        var allVents = ShipStatus.Instance.AllVents.ToList();
        allVents.Remove(allVents.FirstOrDefault(v => v.Id == vent.Id));
        ShipStatus.Instance.AllVents = allVents.ToArray();
        vent.DestroyObj();
    }

    // Unset connections to another vent (specific target)
    /// <summary>
    /// Unsets the vent connections to a specified target vent.
    /// </summary>
    internal static void UnsetVents(this Vent vent, Vent target)
    {
        if (target == null) return;
        if (vent.Left == target) vent.Left = null;
        if (vent.Right == target) vent.Right = null;
        if (vent.Center == target) vent.Center = null;
    }

    // Unset all connections to other vents
    /// <summary>
    /// Unsets all vent connections (Left, Right, Center) for the given vent.
    /// </summary>
    internal static void UnsetVents(this Vent vent)
    {
        vent.Left = null;
        vent.Right = null;
        vent.Center = null;
    }

    // Check if the vent is enabled
    /// <summary>
    /// Checks if the vent is enabled (active in the scene).
    /// </summary>
    internal static bool IsEnabled(this Vent vent) => vent?.enabled == true;

    // Enable or disable the vent
    /// <summary>
    /// Sets the vent's enabled status and updates the ventilation system arrows if necessary.
    /// </summary>
    internal static void SetEnabled(this Vent vent, bool @bool)
    {
        if (vent != null)
        {
            vent.enabled = @bool;

            if (ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Ventilation, out var system))
            {
                VentilationSystem ventilationSystem = system.Cast<VentilationSystem>();
                ventilationSystem?.UpdateVentArrows();
            }
        }
    }
}