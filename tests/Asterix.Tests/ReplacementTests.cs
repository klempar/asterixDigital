using System.Collections.Generic;
using Xunit;
using Asterix.Core.Engine;
using Asterix.Core.Models;

namespace Asterix.Tests
{
    public class ReplacementTests
    {
        [Fact]
        public void ReplacementChoice_DrawsToken_WhenAvailableAndUnderCap()
        {
            var engine = new GameEngine();
            var rng = new SimpleRandom(42);
            var gs = engine.NewGame(2, rng);

            // prepare a pending replacement for player 1
            var newCard = new BattlefieldCard("TestField", 15, null);
            var tokenOptions = new List<TokenType> { TokenType.Boar, TokenType.Fish, TokenType.Potion };
            var pending = new PendingReplacementChoice(1, 0, newCard, new List<SideColor> { SideColor.Red }, tokenOptions.AsReadOnly());

            // ensure token deck contains at least one Potion
            var tokenDeck = new List<Token> { new Token(TokenType.Potion, "Potion", 0) };

            // give player 1 only one Potion currently (under cap)
            var p1tokens = new List<Token> { new Token(TokenType.Helmet, "Helmet", 1), new Token(TokenType.Potion, "Potion", 0) };

            var players = new Dictionary<int, PlayerState>(gs.Players);
            players[1] = new PlayerState(1, players[1].Score, p1tokens.AsReadOnly(), players[1].WonBattlefields);

            var modified = gs with { Players = players, PendingReplacement = pending, TokenDeck = tokenDeck.AsReadOnly(), CurrentPlayerId = 1 };

            // perform replacement choice: player 1 chooses orientation Red and requests Potion
            var next = engine.Step(modified, new ReplacementChoiceAction(SideColor.Red, TokenType.Potion), rng) as GameState;

            Assert.NotNull(next);
            // pending should be cleared
            Assert.Null(next.PendingReplacement);
            // player 1 should have one additional token (Potion)
            Assert.Equal(3, next.Players[1].Tokens.Count);
            // token deck should be empty after draw
            Assert.Empty(next.TokenDeck);
            // battlefield should have been inserted at index 0
            Assert.True(next.Battlefields.Count >= 1);
        }

        [Fact]
        public void ReplacementChoice_DoesNotDraw_WhenCapReached()
        {
            var engine = new GameEngine();
            var rng = new SimpleRandom(99);
            var gs = engine.NewGame(2, rng);

            // prepare pending replacement for player 0
            var newCard = new BattlefieldCard("TestField2", 15, null);
            var tokenOptions = new List<TokenType> { TokenType.Boar, TokenType.Fish, TokenType.Potion };
            var pending = new PendingReplacementChoice(0, 0, newCard, new List<SideColor> { SideColor.Blue }, tokenOptions.AsReadOnly());

            // token deck contains Potion but player already has 2 Potions
            var tokenDeck = new List<Token> { new Token(TokenType.Potion, "Potion", 0) };

            var p0tokens = new List<Token> { new Token(TokenType.Potion, "Potion", 0), new Token(TokenType.Potion, "Potion", 0) };
            var players = new Dictionary<int, PlayerState>(gs.Players);
            players[0] = new PlayerState(0, players[0].Score, p0tokens.AsReadOnly(), players[0].WonBattlefields);

            var modified = gs with { Players = players, PendingReplacement = pending, TokenDeck = tokenDeck.AsReadOnly(), CurrentPlayerId = 0 };

            var next = engine.Step(modified, new ReplacementChoiceAction(SideColor.Blue, TokenType.Potion), rng) as GameState;

            Assert.NotNull(next);
            // pending cleared
            Assert.Null(next.PendingReplacement);
            // player 0 should still have 2 potions
            var countPotion = 0;
            foreach (var t in next.Players[0].Tokens) if (t.Type == TokenType.Potion) countPotion++;
            Assert.Equal(2, countPotion);
            // token deck should remain unchanged because draw was denied
            Assert.Single(next.TokenDeck);
        }
    }
}
