using TheBetterRoles.Items.Attributes;

namespace TheBetterRoles.Commands;

[RegisterCommand]
internal sealed class ClearCommand : BaseCommand
{
    internal override CommandType Type => CommandType.Normal;
    protected override string CommandName => "Clear";
    internal override uint ShortNamesAmount => 0;

    internal override void Run()
    {
        var list = HudManager.Instance.Chat.chatBubblePool.GetComponentsInChildren<ChatBubble>();
        foreach (var item in list)
        {
            item.gameObject.SetActive(false);
        };
    }
}
