using HarmonyLib;
using TheBetterRoles.Helpers;
using UnityEngine;

namespace TheBetterRoles.Items;

/// <summary>
/// The `ArrowLocator` class is used to create and manage arrows that point to specific targets in the game world.
/// It is responsible for displaying these arrows on the HUD to guide players toward designated objectives, such as other players or important locations.
/// The arrows dynamically update their position to track their targets, and they can be removed when no longer needed.
/// This class uses the `ArrowBehaviour` to handle the graphical aspects of the arrows, such as scaling, positioning, and sprite rendering.
/// </summary>
class ArrowLocator
{
    /// <summary>
    /// A static list that keeps track of all active arrows in the game.
    /// Each instance of `ArrowLocator` is added to this list when created and removed when destroyed.
    /// </summary>
    internal static List<ArrowLocator> allArrows { get; set; } = [];

    /// <summary>
    /// A flag that indicates whether the arrow is currently tracking a player target.
    /// When `true`, the arrow will follow the position of the specified target object.
    /// </summary>
    internal bool HasPlayerTarget { get; set; }

    /// <summary>
    /// The game object that the arrow is tracking. This object will be followed by the arrow until it is removed or the target is no longer valid.
    /// </summary>
    internal GameObject? TargetObj { get; set; }

    /// <summary>
    /// The offset applied to the arrow's position, allowing it to be adjusted relative to the target (e.g., to avoid overlap or display above the target).
    /// </summary>
    internal Vector2 Offset { get; set; } = Vector2.zero;

    /// <summary>
    /// A function that determines whether the arrow should be removed. It returns `true` if the arrow should be removed, and `false` if it should remain.
    /// </summary>
    internal Func<bool> RemoveListener { get; set; } = () => { return false; };

    /// <summary>
    /// The `ArrowBehaviour` component attached to the arrow GameObject. It controls the arrow's behavior, such as scaling, rotation, and visibility.
    /// </summary>
    internal ArrowBehaviour? Arrow { get; set; }

    /// <summary>
    /// The `SpriteRenderer` component attached to the arrow GameObject, which is responsible for rendering the arrow's sprite.
    /// </summary>
    internal SpriteRenderer? SpriteRenderer { get; set; }

    /// <summary>
    /// Creates a new arrow locator at the specified position. Optionally, a custom sprite, color, and scale can be provided.
    /// The arrow is displayed on the HUD and will track a target if one is assigned.
    /// </summary>
    /// <param name="pos">The position where the arrow should point (defaults to the origin if not provided).</param>
    /// <param name="sprite">The sprite to use for the arrow (defaults to the default arrow sprite if not provided).</param>
    /// <param name="color">The color of the arrow (defaults to white if not provided).</param>
    /// <param name="maxScale">The maximum scale of the arrow (defaults to 1).</param>
    /// <param name="minDistance">The minimum distance at which the arrow will disappear (defaults to 0.5).</param>
    /// <returns>The current instance of the `ArrowLocator` class.</returns>
    internal ArrowLocator Create(Vector3 pos = default, Sprite? sprite = null, Color color = default, float maxScale = 1f, float minDistance = 0.5f)
    {
        pos = pos == default ? new Vector3(0, 0, 0) : pos;
        color = color == default ? Color.white : color;

        var obj = new GameObject
        {
            name = "ArrowLocator"
        };
        obj.transform.SetParent(HudManager.Instance.transform);
        obj.AddComponent<SpriteRenderer>();
        ArrowBehaviour arrow = obj.AddComponent<ArrowBehaviour>();
        Arrow = arrow;
        Arrow.MaxScale = maxScale;
        Arrow.minDistanceToShowArrow = minDistance;
        SpriteRenderer = arrow.image;
        if (sprite == null)
        {
            Arrow.image.sprite = Utils.LoadSprite("TheBetterRoles.Resources.Images.Icons.Arrow.png", 100f);
        }
        else
        {
            Arrow.image.sprite = sprite;
        }
        Arrow.image.color = color;
        Arrow.target = pos;

        allArrows.Add(this);
        return this;
    }

    /// <summary>
    /// Sets the target for the arrow to track. When a target is set, the arrow will follow the position of the target object.
    /// </summary>
    /// <param name="target">The GameObject to track.</param>
    internal void SetTarget(GameObject target)
    {
        HasPlayerTarget = true;
        TargetObj = target;
    }

    /// <summary>
    /// Removes the arrow from the UI. This will delete the arrow object and remove it from the list of active arrows.
    /// </summary>
    internal void Remove()
    {
        allArrows.Remove(this);
        Arrow.DestroyObj();
    }

    /// <summary>
    /// A Harmony patch that hooks into the `ArrowBehaviour` class to update the arrow's position. This method is invoked after the position is updated.
    /// It checks if the arrow has a target and adjusts its position accordingly. If the target is no longer valid or the `RemoveListener` returns `true`,
    /// the arrow will be removed.
    /// </summary>
    [HarmonyPatch(typeof(ArrowBehaviour))]
    class ArrowBehaviourPatch
    {
        [HarmonyPatch(nameof(ArrowBehaviour.UpdatePosition))]
        [HarmonyPostfix]
        internal static void UpdatePosition_Postfix(ArrowBehaviour __instance)
        {
            if (allArrows.Select(a => a.Arrow).Contains(__instance))
            {
                foreach (var arrow in allArrows)
                {
                    if (arrow.TargetObj != null && arrow.HasPlayerTarget && !arrow.RemoveListener.Invoke())
                    {
                        arrow.Arrow.target = arrow.TargetObj.transform.position + (Vector3)arrow.Offset;
                    }
                    else if (arrow.HasPlayerTarget)
                    {
                        arrow.Remove();
                        return;
                    }
                }
                __instance.gameObject.transform.position = new Vector3(__instance.gameObject.transform.position.x, __instance.gameObject.transform.position.y, -100f);
            }
        }
    }
}