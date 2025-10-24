using TheBetterRoles.Helpers;
using UnityEngine;

namespace TheBetterRoles.Modules;

/// <summary>
/// The ShipStatusExtension class provides an extended behavior for different types of ships in the game.
/// It includes functionality for managing spawn positions for players, handling reverse map systems, 
/// and managing the setup for different ship types (e.g., Skeld, Mira, Polus, etc.). 
/// It allows for differentiated behavior based on whether the extension is considered "better."
/// The extension is added to the ship's GameObject and facilitates map-specific spawning logic and reverse map handling.
/// </summary>
internal class ShipStatusExtension : MonoBehaviour
{
    /// <summary>
    /// Property indicating whether this extension is considered "better." 
    /// This is used for distinguishing between the default and upgraded behavior.
    /// </summary>
    internal virtual bool IsBetter { get; } = false;

    /// <summary>
    /// Singleton instance of the ShipStatusExtension.
    /// </summary>
    internal static ShipStatusExtension? Instance { get; private set; }

    /// <summary>
    /// Cached reference to the ShipStatus instance.
    /// </summary>
    private ShipStatus? shipStatus => ShipStatus.Instance;

    /// <summary>
    /// The initial spawn center position on the map.
    /// </summary>
    private Vector2 _initialSpawnCenter = Vector2.zero;

    /// <summary>
    /// The spawn center for the meeting room.
    /// </summary>
    private Vector2 _meetingSpawnCenter = Vector2.zero;

    /// <summary>
    /// The second spawn center for the meeting room (used for alternate positioning).
    /// </summary>
    private Vector2 _meetingSpawnCenter2 = Vector2.zero;

    /// <summary>
    /// The radius for player spawn areas (affects spawn position calculations).
    /// </summary>
    private float _spawnRadius = 0f;

    /// <summary>
    /// Property for the initial spawn center, adjusted if the reverse map system is active.
    /// </summary>
    internal Vector2 InitialSpawnCenter
    {
        get
        {
            return ReverseMapSystem.IsReverseActive()
                ? new Vector2(shipStatus.transform.position.x - _initialSpawnCenter.x, _initialSpawnCenter.y)
                : _initialSpawnCenter;
        }
        set
        {
            _initialSpawnCenter = value;
        }
    }

    /// <summary>
    /// Property for the meeting spawn center, adjusted if the reverse map system is active.
    /// </summary>
    internal Vector2 MeetingSpawnCenter
    {
        get
        {
            return ReverseMapSystem.IsReverseActive()
                ? new Vector2(shipStatus.transform.position.x - _meetingSpawnCenter.x, _meetingSpawnCenter.y)
                : _meetingSpawnCenter;
        }
        set
        {
            _meetingSpawnCenter = value;
        }
    }

    /// <summary>
    /// Property for the second meeting spawn center, adjusted if the reverse map system is active.
    /// </summary>
    internal Vector2 MeetingSpawnCenter2
    {
        get
        {
            return ReverseMapSystem.IsReverseActive()
                ? new Vector2(shipStatus.transform.position.x - _meetingSpawnCenter2.x, _meetingSpawnCenter2.y)
                : _meetingSpawnCenter2;
        }
        set
        {
            _meetingSpawnCenter2 = value;
        }
    }

    /// <summary>
    /// Property for the spawn radius.
    /// </summary>
    internal float SpawnRadius
    {
        get
        {
            return _spawnRadius;
        }
        set
        {
            _spawnRadius = value;
        }
    }

    /// <summary>
    /// Attempts to set up a specific ship extension based on the ship's type.
    /// This method is used to add the appropriate extension for the current ship.
    /// </summary>
    internal static void TrySetShipExtension(ShipStatus _shipStatus)
    {
        if (Instance != null) return;

        if (GameState.ModdedMapIsActive) return;
        if (_shipStatus.TryCast<SkeldShipStatus>()) _shipStatus.gameObject.AddComponent<SkeldStatusExtension>();
        if (_shipStatus.TryCast<MiraShipStatus>()) _shipStatus.gameObject.AddComponent<MiraStatusExtension>();
        if (_shipStatus.TryCast<PolusShipStatus>()) _shipStatus.gameObject.AddComponent<PolusStatusExtension>();
        if (_shipStatus.TryCast<AirshipStatus>()) _shipStatus.gameObject.AddComponent<AirshipStatusExtension>();
        if (_shipStatus.TryCast<FungleShipStatus>()) _shipStatus.gameObject.AddComponent<FungleStatusExtension>();
    }

    /// <summary>
    /// Unity's Start method. Sets up the extension based on whether it is considered "better" or not.
    /// Also handles the reverse map setup.
    /// </summary>
    internal void Start()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        if (!IsBetter)
        {
            SetUp();
        }
        else if (IsBetter)
        {
            BetterSetUp();
        }

        SetReverse();
    }

    /// <summary>
    /// Method for setting up the extension, called when IsBetter is false.
    /// </summary>
    internal virtual void SetUp() { }

    /// <summary>
    /// Method for setting up the extension, called when IsBetter is true.
    /// This setup may include additional or upgraded functionality.
    /// </summary>
    internal virtual void BetterSetUp() { }

    /// <summary>
    /// Configures reverse map system settings if active.
    /// </summary>
    internal virtual void SetReverse() { }

    /// <summary>
    /// Spawns a player at a calculated position based on the total number of players and their player ID.
    /// Adjusts for initial spawn or meeting spawn depending on the parameter.
    /// </summary>
    internal virtual bool SpawnPlayer(PlayerControl player, int numPlayers, bool initialSpawn)
    {
        Vector2 offset = Vector2.up.Rotate((player.PlayerId - 1) * (360f / numPlayers)) * SpawnRadius;
        Vector2 spawnCenter = initialSpawn ? InitialSpawnCenter : MeetingSpawnCenter;
        Vector2 position = spawnCenter + offset + new Vector2(0f, 0.3636f);

        if (player.IsLocalPlayer())
        {
            player.NetTransform.RpcSnapTo(position);
        }
        else
        {
            player.NetTransform.SnapTo(position);
        }

        return false;
    }
}