using System.Collections.Generic;
using System.Text.Json;
using Asterix.Core.Interfaces;

namespace Asterix.Core.Models
{
    public record PlayCardAction(int CardIndexInHand, int BattlefieldIndex) : IAction
    {
        public string ToJson() => JsonSerializer.Serialize(this);
        public string Describe() => $"PlayCard handIdx={CardIndexInHand} bfIdx={BattlefieldIndex}";
    }

    public record PlayTokenAction(int TokenIndex) : IAction
    {
        public string ToJson() => JsonSerializer.Serialize(this);
        public string Describe() => $"PlayToken idx={TokenIndex}";
    }

    public record DiscardAndDrawAction(IReadOnlyList<int> HandIndicesToDiscard) : IAction
    {
        public string ToJson() => JsonSerializer.Serialize(this);
        public string Describe() => $"DiscardAndDraw indices=[{string.Join(',', HandIndicesToDiscard)}]";
    }
}
