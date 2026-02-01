using System.Collections.Generic;
using Xunit;
using Asterix.Core.Engine;
using Asterix.Core.Models;

namespace Asterix.Tests
{
    public class TerminalTests
    {
        [Fact]
        public void IsTerminal_ReturnsTrue_WhenPlayerHas50()
        {
            var engine = new GameEngine();
            var rng = new SimpleRandom(1);
            var gs = engine.NewGame(2, rng);

            var players = new Dictionary<int, PlayerState>(gs.Players);
            // set player 0 score to 50
            players[0] = new PlayerState(0, 50, players[0].Tokens, players[0].WonBattlefields);

            var mod = gs with { Players = players };

            Assert.True(engine.IsTerminal(mod));

            var outcome = engine.Evaluate(mod);
            Assert.Equal("win", outcome.Result);
            Assert.Equal(0, outcome.WinnerId);
            Assert.True(outcome.Scores.ContainsKey(0) && outcome.Scores[0] == 50);
        }
    }
}
