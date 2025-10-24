namespace TheBetterRoles.Managers;

internal class CustomLoadingBarManager
{
    internal static AmongUsLoadingBar? LoadingBar => LoadingBarManager.Instance?.loadingBar;

    internal static void ToggleLoadingBar(bool on)
    {
        LoadingBar?.gameObject?.SetActive(on);
    }

    internal static void SetLoadingPercent(float percent, string loadText)
    {
        LoadingBar?.SetLoadingPercent(percent, StringNames.None);
        LoadingBar?.loadingText?.SetText(loadText);
    }
}
