using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Attributes;
using TheBetterRoles.Modules;

namespace TheBetterRoles.Commands;

internal enum CommandType
{
    Normal,
    Sponsor,
    Debug,
}

internal abstract class BaseCommand
{
    internal static readonly BaseCommand?[] allCommands = [.. RegisterCommandAttribute.Instances];
    internal abstract CommandType Type { get; }
    protected abstract string CommandName { get; }
    internal abstract uint ShortNamesAmount { get; }

    private string[]? _cachedNames;
    internal string[] Names
    {
        get
        {
            if (_cachedNames != null)
                return _cachedNames;

            if (string.IsNullOrEmpty(CommandName))
                return _cachedNames = [];

            SetupShortNameTranslations();

            var namesToTranslate = new List<string>(ShortNames.Length + 1)
            {
                $"Command.{CommandName}"
            };

            if (ShortNames != null)
            {
                namesToTranslate.AddRange(ShortNames.Where(x => !string.IsNullOrEmpty(x)));
            }

            return _cachedNames = Translator.GetStrings(namesToTranslate);
        }
    }

    internal string Name => Translator.GetString($"Command.{CommandName}");
    private string[] ShortNames { get; set; } = [];
    internal string Description => Translator.GetString($"Command.{CommandName}.Description");
    internal BaseArgument[] Arguments { get; set; } = [];
    internal virtual bool SetChatTimer { get; set; } = false;
    internal virtual bool ShowCommand() => true;
    internal virtual bool ShowSuggestion() => ShowCommand();
    internal abstract void Run();

    protected string GetCommandTranslationKey(string tranKey) => Translator.GetString($"Command.{CommandName}.Result.{tranKey}");

    internal static string CommandResultText(string text, bool onlyGetStr = false)
    {
        if (!onlyGetStr) Utils.AddChatPrivate(text);
        return text;
    }

    internal static string CommandErrorText(string error, bool onlyGetStr = false)
    {
        string er = "<color=#f50000><size=150%><b>Error:</b></size></color>";
        if (!onlyGetStr) Utils.AddChatPrivate($"<color=#730000>{er}\n{error}");
        return $"<color=#730000>{er}\n{error}";
    }

    private void SetupShortNameTranslations()
    {
        if (ShortNamesAmount <= 0) return;

        List<string> list = [];
        for (int i = 1; i <= ShortNamesAmount; i++)
        {
            list.Add($"Command.{CommandName}.Short.{i}");
        }
        ShortNames = list.ToArray();
    }
}
