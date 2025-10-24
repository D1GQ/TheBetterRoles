using TheBetterRoles.Items.Attributes;
using UnityEngine;

namespace TheBetterRoles.Commands;

[RegisterCommand]
internal sealed class PlayIntroCommand : BaseCommand
{
    internal override CommandType Type => CommandType.Debug;
    protected override string CommandName => "PlayIntro";
    internal override uint ShortNamesAmount => 0;

    internal override void Run()
    {
        HudManager.Instance.FullScreen.gameObject.SetActive(true);
        HudManager.Instance.FullScreen.color = new Color(0, 0, 0, 1);
        PlayerControl.LocalPlayer.StopAllCoroutines();
        HudManager.Instance.StartCoroutine(HudManager.Instance.CoShowIntro());
        HudManager.Instance.HideGameLoader();
    }
}