using System.Collections.Generic;
using System.Linq;
using Xunit;
using Asterix.Core.Engine;
using Asterix.Core.Models;

namespace Asterix.Tests
{
    public class PurpleCardTests
    {
        [Fact]
        public void PurpleCard_IsPlayable_OnBothSidesFacingPlayer()
        {
            var engine = new GameEngine();
            var rng = new SimpleRandom(123);
            var gs = engine.NewGame(2, rng);

            // Create a purple card in player 0 hand
            var purple = new Card("PurpleTest", "Purple", 5, "Simple", "noop", CardBackColor.Red);
            var hands = new Dictionary<int, IReadOnlyList<Card>>(gs.Hands);
            hands[0] = new List<Card> { purple }.AsReadOnly();

            var modified = gs with { Hands = hands };

            var moves = engine.LegalMoves(modified);

            var playMoves = moves.OfType<PlayCardAction>().Where(m => m.CardIndexInHand == 0).Select(m => m.BattlefieldIndex).ToList();

            // Expect purple card to be playable to both battlefield indices (0 and 1)
            Assert.Contains(0, playMoves);
            Assert.Contains(1, playMoves);
        }
    }
}
