using System.Collections.Generic;

namespace Asterix.Core.Interfaces
{
    public record GameOutcome(string Result, int? WinnerId, IReadOnlyDictionary<int, double> Scores);
}
