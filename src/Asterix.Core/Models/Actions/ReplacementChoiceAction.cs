using System.Text.Json;
using Asterix.Core.Interfaces;

namespace Asterix.Core.Models
{
    public record ReplacementChoiceAction(SideColor FacingPlayer0, TokenType? TokenChoice) : IAction
    {
        public string ToJson() => JsonSerializer.Serialize(this);

        public string Describe()
        {
            var tokenDesc = TokenChoice.HasValue ? TokenChoice.Value.ToString() : "(none)";
            return $"ReplacementChoice facing={FacingPlayer0} token={tokenDesc}";
        }
    }
}
