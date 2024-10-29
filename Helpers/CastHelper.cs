namespace TheBetterRoles.Helpers;

public static class CastHelper
{
    public static bool TryCast<T>(this object obj) => obj is T;

    public static bool TryCast<T>(this object obj, out T? item) where T : class
    {
        if (obj is T casted)
        {
            item = casted;
            return true;
        }
        else
        {
            item = null;
            return false;
        }
    }
}
