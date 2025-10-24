namespace TheBetterRoles.Helpers.Random;

internal class NetRandomWrapper(System.Random instance) : IRandom
{
    internal System.Random wrapping = instance;

    internal NetRandomWrapper() : this(new System.Random())
    { }
    internal NetRandomWrapper(int seed) : this(new System.Random(seed))
    { }

    public int Next(int minValue, int maxValue) => wrapping.Next(minValue, maxValue);
    public int Next(int maxValue) => wrapping.Next(maxValue);
    public int Next() => wrapping.Next();

    public float Next(float minValue, float maxValue)
    {
        if (minValue > maxValue) throw new ArgumentException("maxValue must be greater than minValue.");
        else if (Math.Abs(minValue - maxValue) < float.Epsilon) return minValue;
        int randomInt = wrapping.Next(int.MinValue, int.MaxValue);
        float normalized = (randomInt - (float)int.MinValue) / ((float)int.MaxValue - int.MinValue);
        return minValue + normalized * (maxValue - minValue);
    }

    public float Next(float maxValue)
    {
        if (maxValue < 0) throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be greater than 0.");
        return Next(0f, maxValue);
    }

}