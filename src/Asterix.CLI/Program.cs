using System;
using System.IO;
using Asterix.Core.Engine;
using Asterix.Core.Interfaces;
using Asterix.Bots;
using Asterix.Core.Models;
using System.Linq;

namespace Asterix.CLI
{
    internal class Program
    {
        static int Main(string[] args)
        {
            // Very small arg parsing
            ulong seed = (ulong)DateTime.UtcNow.Ticks;
            string bot1 = "random";
            string bot2 = "random";
            int games = 1;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--seed": seed = ulong.Parse(args[++i]); break;
                    case "--bot1": bot1 = args[++i]; break;
                    case "--bot2": bot2 = args[++i]; break;
                    case "--games": games = int.Parse(args[++i]); break;
                }
            }

            Console.WriteLine($"Starting {games} game(s) seed={seed} bot1={bot1} bot2={bot2}");

            var rng = new SimpleRandom(seed);
            var engine = new GameEngine();
            IBot b1 = new RandomBot();
            IBot b2 = new RandomBot();

            for (int g = 0; g < games; g++)
            {
                IGameState gameState = engine.NewGame(2, rng);

                // Print initial setup details
                if (gameState is GameState gs)
                {
                    Console.WriteLine($"[SETUP] Players={gs.PlayerCount} DrawDeck={gs.DrawDeck.Count} Discard={gs.DiscardPile.Count} Support={gs.SupportDeck.Count}");
                    foreach (var kv in gs.Hands)
                    {
                        Console.WriteLine($"[SETUP] Player {kv.Key} hand size = {kv.Value.Count}");
                    }
                    // Print battlefield orientation
                    for (int i = 0; i < gs.Battlefields.Count; i++)
                    {
                        var bf = gs.Battlefields[i];
                        var facing0 = bf.FacingPlayer0 == SideColor.Red ? "Red" : "Blue";
                        var facing1 = bf.FacingPlayer0 == SideColor.Red ? "Blue" : "Red";
                        Console.WriteLine($"[SETUP] Battlefield {i}: {bf.Card.Name} points={bf.Card.Points} facing player0={facing0} player1={facing1}");
                    }
                    // Print battlefield deck info (before draw)
                    if (gs.BattlefieldDeck != null)
                    {
                        Console.WriteLine($"[SETUP] Battlefield deck count (initial) = {gs.BattlefieldDeck.Count}");
                        Console.WriteLine($"[SETUP] Battlefield deck contains: {string.Join(", ", gs.BattlefieldDeck.Select(b => b.Name))}");
                    }

                    // Print draw deck contents (color and power)
                    if (gs.DrawDeck != null)
                    {
                        Console.WriteLine($"[SETUP] Draw deck ({gs.DrawDeck.Count}) = {string.Join(", ", gs.DrawDeck.Select(c => $"{c.Color} {c.Power}"))}");
                    }

                    // Print hands: color and power per card
                    foreach (var kv in gs.Hands)
                    {
                        Console.WriteLine($"[SETUP] Player {kv.Key} hand cards = {string.Join(", ", kv.Value.Select(c => $"{c.Color} {c.Power}"))}");
                    }
                    // Print tokens per player
                    foreach (var kv in gs.Players)
                    {
                        var p = kv.Value;
                        var tokenList = string.Join(", ", p.Tokens.Select(t => t.Type.ToString()));
                        Console.WriteLine($"[SETUP] Player {p.PlayerId} tokens = {tokenList}");
                    }
                }

                int turn = 0;
                while (!engine.IsTerminal(gameState))
                {
                    var currentPlayer = gameState.CurrentPlayerId == 0 ? 0 : 1;
                    var legal = engine.LegalMoves(gameState);
                    var bot = currentPlayer == 0 ? b1 : b2;
                    var action = bot.SelectAction(gameState, legal, rng);
                    string actionDesc = action?.Describe() ?? "(none)";
                    // For discard actions, show the actual cards being discarded (color + power)
                    if (action is Asterix.Core.Models.DiscardAndDrawAction dda && gameState is Asterix.Core.Models.GameState gstate)
                    {
                        if (gstate.Hands.TryGetValue(currentPlayer, out var hand))
                        {
                            var parts = new System.Collections.Generic.List<string>();
                            foreach (var idx in dda.HandIndicesToDiscard)
                            {
                                if (idx >= 0 && idx < hand.Count) parts.Add($"{hand[idx].Color} {hand[idx].Power}");
                            }
                            actionDesc = $"Discard [{string.Join(", ", parts)}]";
                        }
                    }
                    Console.WriteLine($"[DEBUG] Game={g} Turn={turn} Player={currentPlayer} Action={actionDesc}");
                    gameState = engine.Step(gameState, action, rng);
                    turn++;
                }

                var outcome = engine.Evaluate(gameState);
                Console.WriteLine($"Game {g} ended. Outcome={outcome.Result} Winner={outcome.WinnerId}");
            }

            return 0;
        }
    }
}
