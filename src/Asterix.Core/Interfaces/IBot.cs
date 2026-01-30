using System.Collections.Generic;

namespace Asterix.Core.Interfaces
{
    public interface IBot
    {
        string Name { get; }
        IAction SelectAction(IGameState state, IReadOnlyList<IAction> legalMoves, IRandomSource rng);
    }
}
