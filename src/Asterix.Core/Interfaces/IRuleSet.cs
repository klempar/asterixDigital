using System.Collections.Generic;

namespace Asterix.Core.Interfaces
{
    public interface IRuleSet
    {
        IGameState Step(IGameState state, IAction action, IRandomSource rng);
        IReadOnlyList<IAction> LegalMoves(IGameState state);
        bool IsTerminal(IGameState state);
        GameOutcome Evaluate(IGameState state);
    }
}
