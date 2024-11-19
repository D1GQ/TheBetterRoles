namespace TheBetterRoles.Items;

public class BoolQueue
{
    private uint queues = 0;
    private readonly bool invertLogic;

    public BoolQueue(bool invert = false)
    {
        invertLogic = invert;
        lastState = ToBool();
    }

    private bool lastState;
    private bool valueChanged = false;

    public void SetQueueCount(uint count) => queues = count;

    public uint GetQueueCount() => queues;

    public bool ValueChanged()
    {
        var changed = valueChanged;
        valueChanged = false;
        return changed;
    }

    public void ForceSetValueChanged()
    {
        valueChanged = true;
    }

    public void ResetBools()
    {
        queues = 0;
        UpdateValueChanged();
    }

    public void Add(bool value)
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

    public static bool operator ==(BoolQueue bq, bool value)
    {
        return bq.ToBool() == value;
    }

    public static bool operator !=(BoolQueue bq, bool value)
    {
        return bq.ToBool() != value;
    }

    public static implicit operator bool(BoolQueue bq)
    {
        return bq.ToBool();
    }

    public static bool operator !(BoolQueue bq)
    {
        return !bq.ToBool();
    }

    private bool ToBool()
    {
        return invertLogic ? queues != 0 : queues == 0;
    }

    private void UpdateValueChanged()
    {
        bool currentState = ToBool();
        if (currentState != lastState)
        {
            valueChanged = true;
            lastState = currentState;
        }
    }

    public override bool Equals(object? obj)
    {
        if (obj is BoolQueue other)
        {
            return queues == other.queues && invertLogic == other.invertLogic;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(queues, invertLogic);
    }
}