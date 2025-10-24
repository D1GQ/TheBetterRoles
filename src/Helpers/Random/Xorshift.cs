namespace TheBetterRoles.Helpers.Random;

internal class Xorshift(uint seed) : IRandom
{
    // Reference element
    internal const string REFERENCE = "https://ja.wikipedia.org/wiki/Xorshift";

    private uint num = seed;

    internal Xorshift() : this((uint)DateTime.UtcNow.Ticks)
    { }

    internal uint Next()
    {
        num ^= num << 13;
        num ^= num >> 17;
        num ^= num << 5;

        return num;
    }

    public int Next(int minValue, int maxValue)
    {
        if (minValue < 0) throw new ArgumentOutOfRangeException(nameof(minValue), "minValue must be bigger than 0.");
        else if (maxValue < 0) throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be bigger than 0.");
        else if (minValue > maxValue) throw new ArgumentException("maxValue must be bigger than minValue.");
        else if (minValue == maxValue) return minValue;

        return (int)(minValue + (Next() % (maxValue - minValue)));
    }

    public int Next(int maxValue) => Next(0, maxValue);

    public float Next(float minValue, float maxValue)
    {
        if (minValue > maxValue) throw new ArgumentException("maxValue must be greater than minValue.");
        else if (Math.Abs(minValue - maxValue) < float.Epsilon) return minValue;

        float normalized = Next() / (float)uint.MaxValue;

        return minValue + normalized * (maxValue - minValue);
    }

    public float Next(float maxValue)
    {
        if (maxValue < 0) throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be greater than 0.");
        return Next(0f, maxValue);
    }
}