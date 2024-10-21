namespace TheBetterRoles;

public static class CastHelper
{
    public static bool CanCast<T>(this object obj) where T : class
    {
        return obj is T;
    }
}
