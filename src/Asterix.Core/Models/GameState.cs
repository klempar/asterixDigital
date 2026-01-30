using System.Collections.Generic;
using System.Text.Json;
using Asterix.Core.Interfaces;

namespace Asterix.Core.Models
{
    public record GameState(
        int TurnNumber,
        int CurrentPlayerId,
        int PlayerCount,
        IReadOnlyList<Card> DrawDeck,
        IReadOnlyList<Card> DiscardPile,
        IReadOnlyList<Card> SupportDeck,
        IReadOnlyDictionary<int, IReadOnlyList<Card>> Hands,
        IReadOnlyDictionary<int, PlayerState> Players,
        IReadOnlyList<BattlefieldInstance> Battlefields,
        IReadOnlyList<BattlefieldCard> BattlefieldDeck
    ) : IGameState
    {
        public string SerializeToJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
