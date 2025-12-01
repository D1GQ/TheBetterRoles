using System.Text;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using UnityEngine;

namespace TheBetterRoles.Roles.Core.RoleBase;

internal class RoleNameAndAbilityAmountText(RoleClass role)
{
    private string _cachedText;
    private int _cachedMax = -2;
    private int _cachedCurrent = -2;
    private Color _cachedRoleColor;

    private string _cachedRoleNameAndColor;
    private string _cachedRoleColorHex;
    private string _cachedDarkenedHex;

    internal string GetText()
    {
        int max = -1, current = -1;
        role.SetAbilityAmountText(ref max, ref current);
        Color currentColor = role.RoleColor;

        if (_cachedText == null ||
            max != _cachedMax ||
            current != _cachedCurrent ||
            !ColorsEqual(currentColor, _cachedRoleColor))
        {
            _cachedMax = max;
            _cachedCurrent = current;
            _cachedRoleColor = currentColor;

            if (_cachedRoleColorHex == null || !ColorsEqual(currentColor, _cachedRoleColor))
            {
                _cachedRoleColorHex = null;
                _cachedDarkenedHex = null;
                _cachedRoleNameAndColor = null;
            }

            _cachedText = BuildText(max, current);
        }

        return _cachedText;
    }

    private string BuildText(int max, int current)
    {
        if (max <= -1 && current <= -1)
            return GetRoleNameAndColor();

        var sb = new StringBuilder(64);
        sb.Append(GetRoleNameAndColor());
        sb.Append(' ');

        sb.Append("(".ToColor(GetDarkenedHex()));

        if (current > -1)
        {
            sb.Append(current.ToString().ToColor(GetRoleColorHex()));
        }

        if (max > -1 && current > -1)
        {
            sb.Append("/".ToColor(GetDarkenedHex()));
        }

        if (max > -1)
        {
            sb.Append(max.ToString().ToColor(GetRoleColorHex()));
        }

        sb.Append(")".ToColor(GetDarkenedHex()));

        return sb.ToString();
    }

    private string GetRoleNameAndColor()
    {
        if (_cachedRoleNameAndColor == null)
        {
            _cachedRoleNameAndColor = role.RoleNameAndColor;
        }
        return _cachedRoleNameAndColor;
    }

    private string GetRoleColorHex()
    {
        if (_cachedRoleColorHex == null)
        {
            _cachedRoleColorHex = role.RoleColorHex;
        }
        return _cachedRoleColorHex;
    }

    private string GetDarkenedHex()
    {
        if (_cachedDarkenedHex == null)
        {
            _cachedDarkenedHex = Colors.Color32ToHex(_cachedRoleColor - new Color(0.15f, 0.15f, 0.15f));
        }
        return _cachedDarkenedHex;
    }

    private static bool ColorsEqual(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.001f &&
               Mathf.Abs(a.g - b.g) < 0.001f &&
               Mathf.Abs(a.b - b.b) < 0.001f;
    }

    internal void InvalidateCache()
    {
        _cachedText = null;
        _cachedRoleNameAndColor = null;
        _cachedRoleColorHex = null;
        _cachedDarkenedHex = null;
        _cachedMax = -2;
        _cachedCurrent = -2;
    }
}