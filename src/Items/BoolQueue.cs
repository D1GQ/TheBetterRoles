namespace TheBetterRoles.Items;

/// <summary>
/// Represents a queue of boolean values, where the state is tracked using a counter.
/// The queue can be manipulated by adding or removing values, and the logic can be inverted.
/// </summary>
internal class BoolQueue
{
    private uint queues = 0;
    private readonly bool invertLogic;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoolQueue"/> class.
    /// </summary>
    /// <param name="invert">Indicates whether the logic should be inverted.</param>
    internal BoolQueue(bool invert = false)
    {
        invertLogic = invert;
        lastState = ToBool();
    }

    private bool lastState;
    private bool valueChanged = false;

    /// <summary>
    /// Sets the count of the boolean queue.
    /// </summary>
    /// <param name="count">The count to set for the queue.</param>
    internal void SetQueueCount(uint count) => queues = count;

    /// <summary>
    /// Gets the current count of the boolean queue.
    /// </summary>
    /// <returns>The current count of the queue.</returns>
    internal uint GetQueueCount() => queues;

    /// <summary>
    /// Checks if the value in the queue has changed since the last check.
    /// </summary>
    /// <returns>True if the value has changed, otherwise false.</returns>
    internal bool ValueChanged()
    {
        var changed = valueChanged;
        valueChanged = false;
        return changed;
    }

    /// <summary>
    /// Forces the queue to register a value change.
    /// </summary>
    internal void ForceSetValueChanged()
    {
        valueChanged = true;
    }

    /// <summary>
    /// Resets the boolean queue to its initial state.
    /// </summary>
    internal void ResetBools()
    {
        queues = 0;
        UpdateValueChanged();
    }

    /// <summary>
    /// Adds a value to the queue. Increases or decreases the queue count depending on the value.
    /// </summary>
    /// <param name="value">The boolean value to add to the queue.</param>
    internal void Add(bool value)
    {
        if (invertLogic)
        {
            value = !value;
        }

        if (value)
        {
            if (queues < uint.MaxValue)
                queues++;
        }
        else
        {
            if (queues > 0)
                queues--;
        }

        UpdateValueChanged();
    }

    /// <summary>
    /// Updates the valueChanged flag if the queue's state has changed.
    /// </summary>
    private void UpdateValueChanged()
    {
        bool currentState = ToBool();
        if (currentState != lastState)
        {
            valueChanged = true;
            lastState = currentState;
        }
    }

    /// <summary>
    /// Increments the queue count (equivalent to adding true)
    /// </summary>
    public static BoolQueue operator ++(BoolQueue bq)
    {
        bq.Add(true);
        return bq;
    }

    /// <summary>
    /// Decrements the queue count (equivalent to adding false)
    /// </summary>
    public static BoolQueue operator --(BoolQueue bq)
    {
        bq.Add(false);
        return bq;
    }

    /// <summary>
    /// Adds a boolean value to the queue
    /// </summary>
    public static BoolQueue operator +(BoolQueue bq, bool value)
    {
        bq.Add(value);
        return bq;
    }

    /// <summary>
    /// Subtracts a boolean value from the queue (equivalent to adding the inverse)
    /// </summary>
    public static BoolQueue operator -(BoolQueue bq, bool value)
    {
        bq.Add(!value);
        return bq;
    }

    /// <summary>
    /// Compares the queue to a boolean value for equality.
    /// </summary>
    /// <param name="bq">The <see cref="BoolQueue"/> instance to compare.</param>
    /// <param name="value">The boolean value to compare against.</param>
    /// <returns>True if the queue equals the boolean value, otherwise false.</returns>
    public static bool operator ==(BoolQueue bq, bool value)
    {
        return bq.ToBool() == value;
    }

    /// <summary>
    /// Compares the queue to a boolean value for inequality.
    /// </summary>
    /// <param name="bq">The <see cref="BoolQueue"/> instance to compare.</param>
    /// <param name="value">The boolean value to compare against.</param>
    /// <returns>True if the queue does not equal the boolean value, otherwise false.</returns>
    public static bool operator !=(BoolQueue bq, bool value)
    {
        return bq.ToBool() != value;
    }

    /// <summary>
    /// Implicitly converts a <see cref="BoolQueue"/> to a boolean value.
    /// </summary>
    /// <param name="bq">The <see cref="BoolQueue"/> to convert.</param>
    /// <returns>The boolean representation of the queue.</returns>
    public static implicit operator bool(BoolQueue bq)
    {
        return bq.ToBool();
    }

    /// <summary>
    /// Negates the boolean value of the queue.
    /// </summary>
    /// <param name="bq">The <see cref="BoolQueue"/> to negate.</param>
    /// <returns>The negated boolean value of the queue.</returns>
    public static bool operator !(BoolQueue bq)
    {
        return !bq.ToBool();
    }

    /// <summary>
    /// Converts the queue's internal state to a boolean value based on the queue count and inversion logic.
    /// </summary>
    /// <returns>The boolean representation of the queue's state.</returns>
    private bool ToBool()
    {
        return invertLogic ? queues != 0 : queues == 0;
    }

    /// <summary>
    /// Compares the current instance with another <see cref="object"/> for equality.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>True if the objects are equal, otherwise false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is BoolQueue other)
        {
            return queues == other.queues && invertLogic == other.invertLogic;
        }
        return false;
    }

    /// <summary>
    /// Gets the hash code for the current instance.
    /// </summary>
    /// <returns>The hash code for the current instance.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(queues, invertLogic);
    }
}