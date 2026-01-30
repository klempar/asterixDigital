using System.Collections.Generic;
using Asterix.Core.Interfaces;

namespace Asterix.Bots
{
    public class RandomBot : IBot
    {
        public string Name => "RandomBot";

        public IAction SelectAction(IGameState state, IReadOnlyList<IAction> legalMoves, IRandomSource rng)
        {
            if (legalMoves == null || legalMoves.Count == 0) return null;
            var idx = rng.NextInt(legalMoves.Count);
            return legalMoves[idx];
        }
    }
}
