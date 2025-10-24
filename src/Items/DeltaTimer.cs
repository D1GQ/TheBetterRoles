using UnityEngine;

namespace TheBetterRoles.Items;

/// <summary>
/// Tracks time with automatic delta time calculation
/// </summary>
/// <remarks>
/// Create a new DeltaTimer
/// </remarks>
/// <param name="countDown">If true, timer counts down instead of up</param>
/// <param name="initialTime">Starting time value</param>
public class DeltaTimer(bool countDown = false, float initialTime = 0f)
{
    private float _accumulatedTime = initialTime;
    private readonly bool _countDown = countDown;

    /// <summary>
    /// Current time value (automatically includes deltaTime)
    /// </summary>
    public float CurrentTime => _countDown
        ? _accumulatedTime - Time.deltaTime
        : _accumulatedTime + Time.deltaTime;

    /// <summary>
    /// Reset the timer to a specific value
    /// </summary>
    /// <param name="newTime">Value to reset to (default 0)</param>
    public void Reset(float newTime = 0f)
    {
        _accumulatedTime = newTime;
    }

    /// <summary>
    /// Directly set the accumulated time without affecting delta calculation
    /// </summary>
    public float AccumulatedTime
    {
        get => _accumulatedTime;
        set => _accumulatedTime = value;
    }
}