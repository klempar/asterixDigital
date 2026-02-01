using System.Collections.Generic;
using System.Linq;
using Asterix.Core.Interfaces;
using Asterix.Core.Models;

namespace Asterix.Core.Engine
{
    public class GameEngine : IRuleSet
    {
        public IGameState Step(IGameState state, IAction action, IRandomSource rng)
        {
            if (state is not GameState gs) return state;

            // Prepare mutable copies
            var draw = new List<Card>(gs.DrawDeck);
            var discard = new List<Card>(gs.DiscardPile);
            var hands = new Dictionary<int, List<Card>>();
            foreach (var kv in gs.Hands) hands[kv.Key] = new List<Card>(kv.Value);
            var players = new Dictionary<int, PlayerState>(gs.Players);
            var battlefields = new List<BattlefieldInstance>(gs.Battlefields);
            var tokenDeck = new List<Token>(gs.TokenDeck);
            var bfDeck = new List<BattlefieldCard>(gs.BattlefieldDeck);
            var pending = gs.PendingReplacement;

            int current = gs.CurrentPlayerId;

            Card DrawOne()
            {
                if (draw.Count == 0)
                {
                    // recycle discard into draw
                    if (discard.Count > 0)
                    {
                        draw.AddRange(discard);
                        discard.Clear();
                        // simple shuffle using rng
                        for (int i = draw.Count - 1; i > 0; i--)
                        {
                            int j = rng.NextInt(i + 1);
                            var tmp = draw[i]; draw[i] = draw[j]; draw[j] = tmp;
                        }
                        // discard recycled silently (no reshuffle log)
                    }
                }

                if (draw.Count == 0) return null;
                var c = draw[0];
                draw.RemoveAt(0);
                return c;
            }

            // helper: format a single player's hand
            string FormatHand(List<Card> h)
            {
                if (h == null || h.Count == 0) return "(empty)";
                return string.Join(", ", h.Select(c => $"{c.Color} {c.Power}"));
            }

            // separate turns visually
            var colWidth = 48;

            // No action -> pass
            if (action == null)
            {
                // advance turn
                Console.WriteLine($"[ACTION] Player {gs.CurrentPlayerId} passes.");
                var afterColsLocal = new List<string>();
                for (int p = 0; p < gs.PlayerCount; p++)
                {
                    var handForP = hands.ContainsKey(p) ? hands[p] : new List<Card>();
                    afterColsLocal.Add(FormatHand(handForP).PadRight(colWidth));
                }
                Console.WriteLine($"[HANDS AFTER ] {string.Join(" | ", afterColsLocal)}");
                var scoreColsLocal = new List<string>();
                for (int p = 0; p < gs.PlayerCount; p++)
                {
                    var ps = players.ContainsKey(p) ? players[p] : new PlayerState(p, 0, new List<Token>().AsReadOnly(), new List<BattlefieldCard>().AsReadOnly());
                    scoreColsLocal.Add($"P{p}={ps.Score}");
                }
                Console.WriteLine($"[SCORE] {string.Join(" | ", scoreColsLocal)}");
                Console.WriteLine();
                return gs with { TurnNumber = gs.TurnNumber + 1, CurrentPlayerId = (gs.CurrentPlayerId + 1) % gs.PlayerCount };
            }

            switch (action)
            {
                case ReplacementChoiceAction rca:
                {
                    // handle a player's replacement choice if pending
                    if (pending == null) break;
                    if (current != pending.PlayerId) break;

                    // validate orientation
                    var chosenFacing = rca.FacingPlayer0;

                    // insert the new battlefield at the requested index
                    var newInstance = new BattlefieldInstance(pending.NewCard, chosenFacing, new List<Card>().AsReadOnly(), new List<Card>().AsReadOnly());
                    if (pending.BattlefieldIndex <= battlefields.Count)
                        battlefields.Insert(pending.BattlefieldIndex, newInstance);

                    // handle token draw choice
                    if (rca.TokenChoice.HasValue)
                    {
                        var desired = rca.TokenChoice.Value;
                        // check player's current count of that token type
                        var pstate = players[current];
                        var currentCount = pstate.Tokens?.Count(t => t.Type == desired) ?? 0;
                        if (currentCount < 2)
                        {
                            var idx = tokenDeck.FindIndex(t => t.Type == desired);
                            if (idx >= 0)
                            {
                                var drawn = tokenDeck[idx];
                                tokenDeck.RemoveAt(idx);
                                var newTokens = new List<Token>(pstate.Tokens) { drawn };
                                players[current] = new PlayerState(pstate.PlayerId, pstate.Score, newTokens.AsReadOnly(), pstate.WonBattlefields ?? new List<BattlefieldCard>().AsReadOnly());
                                Console.WriteLine($"[TOKEN DRAW] Player {current} drew token {drawn.Name}");
                            }
                            else
                            {
                                Console.WriteLine($"[TOKEN DRAW] Player {current} requested {desired} but none left");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[TOKEN DRAW] Player {current} cannot hold 3 of {desired}, draw denied");
                        }
                    }

                    // clear pending
                    pending = null;
                    break;
                }
                case PlayCardAction pca:
                {
                    var hand = hands[current];
                    if (pca.CardIndexInHand < 0 || pca.CardIndexInHand >= hand.Count) break;
                    if (pca.BattlefieldIndex < 0 || pca.BattlefieldIndex >= battlefields.Count) break;
                    var card = hand[pca.CardIndexInHand];

                    // determine which side this player occupies on the battlefield
                    var bf = battlefields[pca.BattlefieldIndex];
                    bool playerIsRedSide = (current == 0 && bf.FacingPlayer0 == SideColor.Red) || (current == 1 && bf.FacingPlayer0 == SideColor.Blue);

                    // enforce rule: player may play a card only if its color matches the side facing them, or it's Purple
                    var requiredColor = playerIsRedSide ? "Red" : "Blue";
                    if (!(string.Equals(card.Color, requiredColor, System.StringComparison.OrdinalIgnoreCase) || string.Equals(card.Color, "Purple", System.StringComparison.OrdinalIgnoreCase)))
                    {
                        System.Console.WriteLine($"[ILLEGAL MOVE] Player {current} attempted to play {card.Color} card to {requiredColor} side -> denied");
                        break;
                    }

                    // legal: remove from hand and add to appropriate side
                    hand.RemoveAt(pca.CardIndexInHand);
                    List<Card> newRed = new List<Card>(bf.SideRedCards);
                    List<Card> newBlue = new List<Card>(bf.SideBlueCards);
                    if (playerIsRedSide) newRed.Add(card); else newBlue.Add(card);

                    battlefields[pca.BattlefieldIndex] = new BattlefieldInstance(bf.Card, bf.FacingPlayer0, newRed.AsReadOnly(), newBlue.AsReadOnly());

                    System.Console.WriteLine($"[ACTION] Player {current} played card {card.Color} {card.Power} to battlefield {pca.BattlefieldIndex} ({(playerIsRedSide?"Red":"Blue")} side)");

                    break;
                }

                case PlayTokenAction pta:
                {
                    var pstate = players[current];
                    var tlist = new List<Token>(pstate.Tokens);
                    if (pta.TokenIndex < 0 || pta.TokenIndex >= tlist.Count) break;
                    var tok = tlist[pta.TokenIndex];
                    // helmets are not playable
                    if (tok.Type == TokenType.Helmet)
                    {
                        System.Console.WriteLine($"[ACTION] Player {current} attempted to play Helmet -> ignored");
                        break;
                    }
                    tlist.RemoveAt(pta.TokenIndex);
                    // recompute score: sum of won battlefields + sum of helmet values
                    var wonList = pstate.WonBattlefields != null ? new List<BattlefieldCard>(pstate.WonBattlefields) : new List<BattlefieldCard>();
                    int newScore = (wonList.Sum(b => b.Points)) + tlist.Where(x => x.Type == TokenType.Helmet).Sum(x => x.Value);
                    players[current] = new PlayerState(pstate.PlayerId, newScore, tlist.AsReadOnly(), wonList.AsReadOnly());

                    System.Console.WriteLine($"[ACTION] Player {current} played token {tok.Type}");

                    // draw one if hand < 5
                    var hand = hands[current];
                    if (hand.Count < 5)
                    {
                        var drawn = DrawOne();
                        if (drawn != null) hand.Add(drawn);
                    }

                    break;
                }

                case DiscardAndDrawAction dda:
                {
                    var hand = hands[current];
                    var indices = new List<int>(dda.HandIndicesToDiscard);
                    indices.Sort((a,b)=>b.CompareTo(a)); // remove from highest to lowest
                    var discardedCards = new List<Card>();
                    foreach (var idx in indices)
                    {
                        if (idx < 0 || idx >= hand.Count) continue;
                        var card = hand[idx];
                        hand.RemoveAt(idx);
                        discardedCards.Add(card);
                    }
                    // add to discard pile in given order
                    discard.AddRange(discardedCards);
                    var desc = string.Join(", ", discardedCards.Select(c => $"{c.Color} {c.Power}"));
                    System.Console.WriteLine($"[ACTION] Player {current} discarded {discardedCards.Count} card(s): {desc}");

                    // draw up to 5
                    while (hand.Count < 5)
                    {
                        var drawn = DrawOne();
                        if (drawn == null) break;
                        hand.Add(drawn);
                    }

                    break;
                }

                default:
                    break;
            }

            // After action: check for battlefield resolution
            // If one side's total power >= battlefield.Points and that side has strictly larger total, award the battlefield
                for (int bi = battlefields.Count - 1; bi >= 0; bi--)
            {
                var bf = battlefields[bi];
                int redSum = bf.SideRedCards?.Sum(c => c.Power) ?? 0;
                int blueSum = bf.SideBlueCards?.Sum(c => c.Power) ?? 0;
                int target = bf.Card.Points;
                bool redEligible = redSum >= target;
                bool blueEligible = blueSum >= target;
                if (redEligible || blueEligible)
                {
                    int winnerPlayerId;
                    // determine winner: if sums differ, larger sum wins; if equal, break tie in favor of last actor
                    if (redSum > blueSum)
                    {
                        winnerPlayerId = bf.FacingPlayer0 == SideColor.Red ? 0 : 1;
                    }
                    else if (blueSum > redSum)
                    {
                        winnerPlayerId = bf.FacingPlayer0 == SideColor.Blue ? 0 : 1;
                    }
                    else
                    {
                        // tie by sum: award to the player who acted (last actor) to avoid ties
                        var lastActor = current; // `current` is the player who performed the action
                        winnerPlayerId = lastActor;
                    }

                    // award points and move battlefield card to player's won list
                    var pw = players[winnerPlayerId];
                    var won = pw.WonBattlefields != null ? new List<BattlefieldCard>(pw.WonBattlefields) : new List<BattlefieldCard>();
                    won.Add(bf.Card);
                    int updatedScore = won.Sum(b => b.Points) + (pw.Tokens?.Where(t => t.Type == TokenType.Helmet).Sum(t => t.Value) ?? 0);
                    players[winnerPlayerId] = new PlayerState(pw.PlayerId, updatedScore, pw.Tokens ?? new List<Token>().AsReadOnly(), won.AsReadOnly());

                    // move cards from both sides of the battlefield to the discard pile
                    if (bf.SideRedCards != null && bf.SideRedCards.Count > 0)
                    {
                        discard.AddRange(bf.SideRedCards);
                        Console.WriteLine($"[RESOLVE] Moved {bf.SideRedCards.Count} cards from Red side of battlefield {bi} to discard");
                    }
                    if (bf.SideBlueCards != null && bf.SideBlueCards.Count > 0)
                    {
                        discard.AddRange(bf.SideBlueCards);
                        Console.WriteLine($"[RESOLVE] Moved {bf.SideBlueCards.Count} cards from Blue side of battlefield {bi} to discard");
                    }

                    Console.WriteLine($"[RESOLVE] Battlefield {bi} ({bf.Card.Name}) won by Player {winnerPlayerId}; +{bf.Card.Points} points");
                    // also print updated score for all players
                    var newPw = players[winnerPlayerId];
                    Console.WriteLine($"[SCORE] P{winnerPlayerId}={newPw.Score}");

                    // remove battlefield from active list
                    battlefields.RemoveAt(bi);

                    // draw replacement battlefield from battlefield deck (if any) and create pending replacement choice for loser
                    if (bfDeck.Count > 0)
                    {
                        var newBfCard = bfDeck[0];
                        bfDeck.RemoveAt(0);
                        int loser = 1 - winnerPlayerId;
                        // compute token options: types present in tokenDeck excluding helmets and excluding types the loser already has >=2
                        var tokenTypesAvailable = tokenDeck.Select(t => t.Type).Distinct().Where(tt => tt != TokenType.Helmet).ToList();
                        var loserPState = players[loser];
                        var tokenOptions = new List<TokenType>();
                        foreach (var tt in tokenTypesAvailable)
                        {
                            var cnt = loserPState.Tokens?.Count(t => t.Type == tt) ?? 0;
                            if (cnt < 2) tokenOptions.Add(tt);
                        }

                        var orientationOptions = new List<SideColor> { SideColor.Red, SideColor.Blue };
                        pending = new PendingReplacementChoice(loser, bi, newBfCard, orientationOptions.AsReadOnly(), tokenOptions.AsReadOnly());
                        Console.WriteLine($"[PENDING REPLACEMENT] Player {loser} must choose orientation and token options: {string.Join(",", tokenOptions)} for battlefield {newBfCard.Name}");
                    }
                }
            }

            // After action: print hands after in aligned columns
            var afterCols = new List<string>();
            for (int p = 0; p < gs.PlayerCount; p++)
            {
                var handForP = hands.ContainsKey(p) ? hands[p] : new List<Card>();
                afterCols.Add(FormatHand(handForP).PadRight(colWidth));
            }
            Console.WriteLine($"[HANDS AFTER ] {string.Join(" | ", afterCols)}");
            // print current scores
            var scoreCols = new List<string>();
            for (int p = 0; p < gs.PlayerCount; p++)
            {
                var ps = players.ContainsKey(p) ? players[p] : new PlayerState(p, 0, new List<Token>().AsReadOnly(), new List<BattlefieldCard>().AsReadOnly());
                scoreCols.Add($"P{p}={ps.Score}");
            }
            Console.WriteLine($"[SCORE] {string.Join(" | ", scoreCols)}");
            // separate entire turn blocks with a blank line for readability
            Console.WriteLine();

            // rebuild readonly structures
            var newHands = new Dictionary<int, IReadOnlyList<Card>>();
            foreach (var kv in hands) newHands[kv.Key] = kv.Value.AsReadOnly();

            var newPlayers = new Dictionary<int, PlayerState>(players);

            var newDraw = draw.AsReadOnly();
            var newDiscard = discard.AsReadOnly();

            var newBattlefields = battlefields.AsReadOnly();

            var nextPlayer = (gs.CurrentPlayerId + 1) % gs.PlayerCount;
            var newTokenDeck = tokenDeck.AsReadOnly();
            var newBattlefieldDeck = bfDeck.AsReadOnly();
            return new GameState(gs.TurnNumber + 1, nextPlayer, gs.PlayerCount, newDraw, newDiscard, gs.SupportDeck, newHands, newPlayers, newBattlefields, newBattlefieldDeck, pending, newTokenDeck);
        }

        public IReadOnlyList<IAction> LegalMoves(IGameState state)
        {
            if (state is not GameState gs) return new List<IAction>();

            var moves = new List<IAction>();
            var current = gs.CurrentPlayerId;

            // If a pending replacement choice exists, only the designated player may make replacement actions
            if (gs.PendingReplacement != null && gs.PendingReplacement.PlayerId == current)
            {
                foreach (var orient in gs.PendingReplacement.OrientationOptions)
                {
                    // allow choosing no token (null) as well as any provided token options
                    moves.Add(new ReplacementChoiceAction(orient, null));
                    foreach (var tt in gs.PendingReplacement.TokenOptions)
                    {
                        moves.Add(new ReplacementChoiceAction(orient, tt));
                    }
                }
                return moves;
            }

            // play card to any battlefield: for each card index and battlefield index
            if (gs.Hands.TryGetValue(current, out var hand))
            {
                for (int ci = 0; ci < hand.Count; ci++)
                {
                    for (int bi = 0; bi < gs.Battlefields.Count; bi++)
                    {
                        // determine if this play would be legal: card color must match side facing the player (or be Purple)
                        var bf = gs.Battlefields[bi];
                        bool playerIsRedSide = (current == 0 && bf.FacingPlayer0 == SideColor.Red) || (current == 1 && bf.FacingPlayer0 == SideColor.Blue);
                        var requiredColor = playerIsRedSide ? "Red" : "Blue";
                        var cardColor = hand[ci].Color ?? "";
                        if (string.Equals(cardColor, requiredColor, System.StringComparison.OrdinalIgnoreCase) || string.Equals(cardColor, "Purple", System.StringComparison.OrdinalIgnoreCase))
                        {
                            moves.Add(new PlayCardAction(ci, bi));
                        }
                    }
                }
            }

            // play any token
            if (gs.Players.TryGetValue(current, out var pstate) && pstate.Tokens != null)
            {
                for (int ti = 0; ti < pstate.Tokens.Count; ti++) moves.Add(new PlayTokenAction(ti));
            }

            // discard/draw actions: allow discarding any subset of hand indices (including empty set)
            var handCount = hand?.Count ?? 0;
            if (handCount > 0)
            {
                var maxMask = 1 << handCount; // up to 2^handCount combinations (handCount <= 5)
                for (int mask = 0; mask < maxMask; mask++)
                {
                    var indices = new List<int>();
                    for (int i = 0; i < handCount; i++) if ((mask & (1 << i)) != 0) indices.Add(i);
                    moves.Add(new DiscardAndDrawAction(indices.AsReadOnly()));
                }
            }
            else
            {
                moves.Add(new DiscardAndDrawAction(new List<int>().AsReadOnly()));
            }

            return moves;
        }

        public bool IsTerminal(IGameState state)
        {
            if (state is not GameState gs) return false;
            // Terminal when any player's score reaches or exceeds 50
            foreach (var kv in gs.Players)
            {
                if (kv.Value != null && kv.Value.Score >= 50) return true;
            }
            return false;
        }

        public GameOutcome Evaluate(IGameState state)
        {
            if (state is not GameState gs) return new GameOutcome("ongoing", null, new Dictionary<int, double>());

            // build score dictionary
            var scores = new Dictionary<int, double>();
            foreach (var kv in gs.Players) scores[kv.Key] = kv.Value?.Score ?? 0;

            // Check for winner (first player with score >= 50)
            foreach (var kv in gs.Players)
            {
                if (kv.Value != null && kv.Value.Score >= 50)
                {
                    return new GameOutcome("win", kv.Key, scores);
                }
            }

            return new GameOutcome("ongoing", null, scores);
        }

        // Initialize a new game: create decks, shuffle, and deal hands
        public GameState NewGame(int playerCount, IRandomSource rng)
        {
            // Build a basic draw deck: 10 red cards (power 1..10) and 10 blue cards (power 1..10)
            var cards = new List<Card>();
            for (int i = 1; i <= 10; i++)
            {
                cards.Add(new Card($"Red {i}", "Red", i, "Simple", "noop", CardBackColor.Red));
            }
            for (int i = 1; i <= 10; i++)
            {
                cards.Add(new Card($"Blue {i}", "Blue", i, "Simple", "noop", CardBackColor.Blue));
            }

            // Add Purple cards: 10 cards, power 1..10. Purple can be played to either side facing the player.
            for (int i = 1; i <= 10; i++)
            {
                // assign random back color for purple cards so some look like Red-backed and some like Blue-backed
                var back = (rng.NextInt(2) == 0) ? CardBackColor.Red : CardBackColor.Blue;
                cards.Add(new Card($"Purple {i}", "Purple", i, "Simple", "noop", back));
            }

            // Shuffle: Fisher-Yates
            var deck = new List<Card>(cards);
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = rng.NextInt(i + 1);
                var tmp = deck[i];
                deck[i] = deck[j];
                deck[j] = tmp;
            }

            var hands = new Dictionary<int, IReadOnlyList<Card>>();
            for (int p = 0; p < playerCount; p++)
            {
                var hand = new List<Card>();
                for (int d = 0; d < 5 && deck.Count > 0; d++)
                {
                    hand.Add(deck[0]);
                    deck.RemoveAt(0);
                }
                hands[p] = hand.AsReadOnly();
            }

            var draw = deck.AsReadOnly();
            var discard = new List<Card>().AsReadOnly();
            var support = new List<Card>().AsReadOnly();

            // Create battlefield deck (set of 10)
            var bfNames = new[] { "Hill", "River", "Forest", "Ruins", "Plains", "Mountain", "Swamp", "Lake", "Valley", "Castle" };
            var bfCards = new List<BattlefieldCard>();
            foreach (var n in bfNames)
            {
                bfCards.Add(new BattlefieldCard(n, 15, null));
            }

            var bfDeck = new List<BattlefieldCard>(bfCards);
            for (int i = bfDeck.Count - 1; i > 0; i--)
            {
                int j = rng.NextInt(i + 1);
                var tmp = bfDeck[i];
                bfDeck[i] = bfDeck[j];
                bfDeck[j] = tmp;
            }

            // Draw two battlefields (first two after shuffle) and orient them relative to player 0
            var battlefields = new List<BattlefieldInstance>();
            if (bfDeck.Count >= 2)
            {
                // draw two and remove them from the battlefield deck so they're no longer available
                var b0 = bfDeck[0];
                bfDeck.RemoveAt(0);
                var b1 = bfDeck[0];
                bfDeck.RemoveAt(0);
                battlefields.Add(new BattlefieldInstance(b0, SideColor.Red, new List<Card>().AsReadOnly(), new List<Card>().AsReadOnly()));   // facing player0 = Red
                battlefields.Add(new BattlefieldInstance(b1, SideColor.Blue, new List<Card>().AsReadOnly(), new List<Card>().AsReadOnly()));  // facing player0 = Blue
            }

            // Build shared token deck: 10 copies of Boar, Fish, Potion; 10 normal Helmets (value 1) and 10 Centurion Helmets (value 3)
            var tokenDeck = new List<Token>();
            for (int i = 0; i < 10; i++) tokenDeck.Add(new Token(TokenType.Boar, "Boar", 0));
            for (int i = 0; i < 10; i++) tokenDeck.Add(new Token(TokenType.Fish, "Fish", 0));
            for (int i = 0; i < 10; i++) tokenDeck.Add(new Token(TokenType.Potion, "Potion", 0));
            for (int i = 0; i < 10; i++) tokenDeck.Add(new Token(TokenType.Helmet, "Helmet", 1));
            for (int i = 0; i < 10; i++) tokenDeck.Add(new Token(TokenType.Helmet, "Centurion Helmet", 3));

            // token deck left in deterministic order (no shuffle). Players will draw specific token types.

            // Create tokens for each player: give each player one Boar, one Fish, one Potion and one NORMAL Helmet (value 1)
            var players = new Dictionary<int, PlayerState>();
            for (int p = 0; p < playerCount; p++)
            {
                var tokens = new List<Token>();
                // take one normal helmet if available, otherwise any helmet
                var idxHelmet = tokenDeck.FindIndex(t => t.Type == TokenType.Helmet && t.Value == 1);
                if (idxHelmet < 0) idxHelmet = tokenDeck.FindIndex(t => t.Type == TokenType.Helmet);
                if (idxHelmet >= 0) { tokens.Add(tokenDeck[idxHelmet]); tokenDeck.RemoveAt(idxHelmet); }

                // take one Boar
                var idxBoar = tokenDeck.FindIndex(t => t.Type == TokenType.Boar);
                if (idxBoar >= 0) { tokens.Add(tokenDeck[idxBoar]); tokenDeck.RemoveAt(idxBoar); }

                // take one Fish
                var idxFish = tokenDeck.FindIndex(t => t.Type == TokenType.Fish);
                if (idxFish >= 0) { tokens.Add(tokenDeck[idxFish]); tokenDeck.RemoveAt(idxFish); }

                // take one Potion
                var idxPotion = tokenDeck.FindIndex(t => t.Type == TokenType.Potion);
                if (idxPotion >= 0) { tokens.Add(tokenDeck[idxPotion]); tokenDeck.RemoveAt(idxPotion); }

                int initialScore = tokens.Where(t => t.Type == TokenType.Helmet).Sum(t => t.Value);
                players[p] = new PlayerState(p, initialScore, tokens.AsReadOnly(), new List<BattlefieldCard>().AsReadOnly());
            }

            // Expose full battlefield deck (before drawing) in game state
            var battlefieldDeck = bfDeck.AsReadOnly();
            var tokenDeckRead = tokenDeck.AsReadOnly();

            return new GameState(0, 0, playerCount, draw, discard, support, hands, players, battlefields, battlefieldDeck, null, tokenDeckRead);
        }
    }
}
