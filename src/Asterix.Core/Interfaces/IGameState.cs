using System.Collections.Generic;

namespace Asterix.Core.Interfaces
{
    public interface IGameState
    {
        int CurrentPlayerId { get; }
        int TurnNumber { get; }
        string SerializeToJson();
    }
}
