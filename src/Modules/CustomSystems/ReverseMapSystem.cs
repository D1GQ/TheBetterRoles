using Hazel;
using Reactor.Utilities.Attributes;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Modules;

// Add system to reverse maps.
[RegisterInIl2Cpp(typeof(ISystemType), typeof(IActivatable))]
internal class ReverseMapSystem : BaseSystem, IISystemType, IIActivatable
{
    /// <summary>
    /// Check if there is a active instance of ReverseMapSystem.
    /// </summary>
    /// <returns>bool</returns>
    internal static bool IsReverseActive() => Instance?.IsActive ?? false;
    internal static CustomSystemTypes Type => CustomSystemTypes.ReverseMap;
    public bool IsDirty { get; private set; }
    public bool IsActive { get; private set; }

    internal static ReverseMapSystem? Instance { get; private set; }

    /// <summary>
    /// Integrate custom system into AU systems.
    /// </summary>
    internal static void AddSystem()
    {
        if (Instance != null) return;
        var reverseMap = ShipStatus.Instance.gameObject.AddComponent<ReverseMapSystem>();
        ShipStatus.Instance.Systems.Add(Type, reverseMap.Cast<ISystemType>());
        Instance = reverseMap;
    }

    internal float Timer { get; private set; }

    internal override void SetUp()
    {
        Reverse();
    }

    internal override void Destroy()
    {
        Instance = null;
    }

    /// <summary>
    /// Reverse current active map.
    /// </summary>
    private void Reverse()
    {
        if (!IsActive)
        {
            IsActive = true;
            Vector2 center = ShipStatus.Instance.transform.position;
            Vector3 current = ShipStatus.Instance.transform.localScale;
            ShipStatus.Instance.transform.localScale = new(-current.x, current.y, current.z);
            foreach (var player in Main.AllPlayerControls)
            {
                Vector2 playerPosition = player.GetCustomPosition();
                Vector2 mirroredPosition = new(2 * center.x - playerPosition.x, playerPosition.y);
                player.NetTransform.SnapTo(mirroredPosition);
                player.MyPhysics.body.velocity = -player.MyPhysics.body.velocity;
                player.MyPhysics.FlipX = !player.MyPhysics.FlipX;
                player.MyPhysics.ResetAnimState();
            }

            foreach (var vent in ShipStatus.Instance.AllVents)
            {
                vent.myRend.flipX = true;
            }
        }
    }

    public void Deteriorate(float deltaTime) { }

    public void UpdateSystem(PlayerControl player, MessageReader msgReader) { }

    public void Serialize(MessageWriter writer, bool initialState) { }

    public void Deserialize(MessageReader reader, bool initialState) { }
}
