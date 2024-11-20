using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheBetterRoles.Commands
{
    public abstract class BaseArgument(BaseCommand? command)
    {
        public BaseCommand? Command { get; } = command;
        public abstract string ArgInfo { get; }
        public string Arg { get; set; } = string.Empty;
        public abstract T? TryGetTarget<T>() where T : class;
        protected virtual string[] ArgSuggestions => GetArgSuggestions.Invoke();
        public Func<string[]> GetArgSuggestions { get; set; } = () => { return []; };
        public string GetClosestSuggestion() => ArgSuggestions.FirstOrDefault(name => name.StartsWith(Arg, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
    }
}
