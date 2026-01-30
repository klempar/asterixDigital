using System.Collections.Generic;

namespace Asterix.Core.Models
{
    public record PlayerState(int PlayerId, int Score, IReadOnlyList<Token> Tokens, IReadOnlyList<BattlefieldCard> WonBattlefields);
}
