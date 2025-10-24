namespace TheBetterRoles.Helpers.Random;

internal class HashRandomWrapper : IRandom
{
    public int Next(int minValue, int maxValue) => HashRandom.Next(minValue, maxValue);
    public float Next(float minValue, float maxValue)
    {
        int randomInt = HashRandom.Next(int.MinValue, int.MaxValue);
        float normalized = (randomInt - (float)int.MinValue) / ((float)int.MaxValue - int.MinValue);
        return minValue + normalized * (maxValue - minValue);
    }
    public int Next(int maxValue) => HashRandom.Next(0, maxValue);
    public float Next(float maxValue)
    {
        return Next(0f, maxValue);
    }
    internal uint Next() => HashRandom.Next();
    internal int FastNext(int maxValue) => HashRandom.FastNext(maxValue);
}