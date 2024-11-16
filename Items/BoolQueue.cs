
namespace TheBetterRoles.Item;

public class BoolQueue
{
    private uint queues = 0;
    private readonly bool invertLogic;

    public BoolQueue(bool invert = false)
    {
        invertLogic = invert;
    }

    public uint GetQueueCount() => queues;

    public void ResetBools()
    {
        queues = 0;
    }

    public void Add(bool value)
    {
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