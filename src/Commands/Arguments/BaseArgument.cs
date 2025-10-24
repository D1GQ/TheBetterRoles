using TheBetterRoles.Modules;

namespace TheBetterRoles.Commands;

internal abstract class BaseArgument(BaseCommand? command, string argInfoTranStr, (string prefix, string postfix) prefix_postfix)
{
    internal BaseCommand? Command { get; } = command;
    internal string GetArgInfo() => $"{Prefix_Postfix.prefix}{Translator.GetString(ArgInfo)}{Prefix_Postfix.postfix}";
    private string ArgInfo { get; } = argInfoTranStr;
    private (string prefix, string postfix) Prefix_Postfix { get; } = prefix_postfix;
    internal string Arg { get; set; } = string.Empty;
    protected virtual string[] ArgSuggestions => GetArgSuggestions.Invoke();
    internal Func<string[]> GetArgSuggestions { get; set; } = () => { return []; };
    internal string GetClosestSuggestion() => ArgSuggestions.FirstOrDefault(name => name.StartsWith(Arg, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
}
