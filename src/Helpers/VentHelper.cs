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
        con?.ConsoleId = newVent.Id;
        newVent.AddToShipStatus();
        return newVent;
    }

    /// <summary>
    /// Adds the specified <see cref="Vent"/> to the collection of vents in the current <see cref="ShipStatus"/>
    /// instance.
    /// </summary>
    /// <param name="vent">The <see cref="Vent"/> to add to the ship's vent collection.</param>
    /// <param name="name">An optional name to assign to the vent. If not specified or empty, the vent's name remains unchanged.</param>
    internal static void AddToShipStatus(this Vent vent, string name = "")
    {
        var allVents = ShipStatus.Instance.AllVents.ToList();
        allVents.Add(vent);
        ShipStatus.Instance.AllVents = allVents.ToArray();
        if (!string.IsNullOrEmpty(name))
            vent.name = name;
    }

    /// <summary>
    /// Removes the specified <see cref="Vent"/> from the ship's vent status collection.
    /// </summary>
    /// <remarks>After removal, the vent is destroyed and will no longer be tracked by <see
    /// cref="ShipStatus.Instance.AllVents"/>.</remarks>
    /// <param name="vent">The <see cref="Vent"/> instance to remove from the ship status. Must not be <c>null</c>.</param>
    internal static void RemoveFromShipStatus(this Vent vent)
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
    internal static void UnsetVent(this Vent vent, Vent target)
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

    /// <summary>
    /// Enables or disables arrow indicators for the specified <see cref="Vent"/> and its connected vents.
    /// </summary>
    /// <param name="vent">The <see cref="Vent"/> for which to set arrow indicators.</param>
    /// <param name="active"><see langword="true"/> to enable arrow indicators; <see langword="false"/> to disable them.</param>
    internal static void SetArrows(this Vent vent, bool active)
    {
        Vent[] nearbyVents = vent.NearbyVents;
        Vector2 vector;
        if (vent.Right && vent.Left)
        {
            vector = (vent.Right.transform.position + vent.Left.transform.position) / 2f - vent.transform.position;
        }
        else
        {
            vector = Vector2.zero;
        }
        for (int i = 0; i < vent.Buttons.Length; i++)
        {
            ButtonBehavior buttonBehavior = vent.Buttons[i];
            if (active)
            {
                Vent v = nearbyVents[i];
                if (v)
                {
                    buttonBehavior.gameObject.SetActive(true);
                    Vector3 vector2 = v.transform.position - vent.transform.position;
                    Vector3 vector3 = vector2.normalized * (0.7f + v.spreadShift);
                    vector3.x *= Mathf.Sign(ShipStatus.Instance.transform.localScale.x);
                    vector3.y -= 0.08f;
                    vector3.z = -10f;
                    buttonBehavior.transform.localPosition = vector3;
                    buttonBehavior.transform.LookAt2d(v.transform);
                    vector3 = vector3.RotateZ((vector.AngleSigned(vector2) > 0f) ? v.spreadAmount : (-v.spreadAmount));
                    buttonBehavior.transform.localPosition = vector3;
                    buttonBehavior.transform.Rotate(0f, 0f, (vector.AngleSigned(vector2) > 0f) ? v.spreadAmount : (-v.spreadAmount));
                }
                else
                {
                    buttonBehavior.gameObject.SetActive(false);
                }
            }
            else
            {
                buttonBehavior.gameObject.SetActive(false);
            }
        }
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