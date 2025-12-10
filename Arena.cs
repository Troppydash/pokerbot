using PokerBot.Attributes;

namespace PokerBot;

/// <summary>
/// Agent simulation arena
/// </summary>
[Stable]
public class Arena
{
    /// <summary>
    /// 2-list agents
    /// </summary>
    private List<IAgent> _agents;

    private Arena(List<IAgent> agents)
    {
        _agents = agents;
    }

    /// <summary>
    /// Simulate one game of poker and return utility result
    /// </summary>
    /// <param name="seed">Shuffle seed</param>
    /// <param name="verbose">Print game states and actions</param>
    /// <returns></returns>
    private int[] Simulate(int seed, bool verbose = true)
    {
        foreach (var agent in _agents)
        {
            agent.Reset();
        }

        Game game = new Game();
        game.Shuffle(seed);
        while (game.Utility() == null)
        {
            if (verbose)
            {
                Console.WriteLine($"Turn: {_agents[game.GetTurn()].Name()}");
                Console.WriteLine("[Game State]");
                game.Display();
                Console.WriteLine("[Game State Over]");
            }

            string player = game.GetTurn() == 0 ? "SB" : "BB";
            Action action = _agents[game.GetTurn()].Play(game.GetState(), game.GetActions());
            game.Play(action);

            if (verbose)
            {
                Console.WriteLine($"(Action) Player {player} played: {action}\n");
            }
        }

        if (verbose)
        {
            Console.WriteLine("[Game State]");
            game.Display();
            Console.WriteLine("[Game State Over]\n");
        }

        return game.Utility()!;
    }

    /// <summary>
    /// Simulate multiple games of 1v1s, displays the average pnl and 95CI in stdout
    /// </summary>
    /// <param name="agents">Agents</param>
    /// <param name="iters">Number of games</param>
    /// <param name="verbose">Whether to display the poker game states</param>
    /// <returns>Average pnl for both agents</returns>
    public static int[] SimulateAll(List<IAgent> agents, int seed = 42, int iters = 1000, bool verbose = false)
    {
        int[] scores = [0, 0];

        Random rng = new Random(seed);
        List<double> winnings = [];
        for (int i = 0; i < iters; ++i)
        {
            Console.WriteLine($"\n~~~~ Simulating Round {i} ~~~~");
            int s = rng.Next();

            Arena arena = new Arena([agents[0], agents[1]]);
            int[] result = arena.Simulate(s, verbose);
            scores[0] += result[0];
            scores[1] += result[1];
            winnings.Add((double)result[0] / Game.BbAmount);

            arena = new Arena([agents[1], agents[0]]);
            result = arena.Simulate(s, verbose);
            scores[0] += result[1];
            scores[1] += result[0];
            winnings.Add((double)result[1] / Game.BbAmount);

            double avg = 0.0;
            foreach (var win in winnings)
            {
                avg += win;
            }

            avg /= winnings.Count;

            double std = 0.0;
            foreach (var win in winnings)
            {
                std += (win - avg) * (win - avg);
            }

            std = Math.Sqrt(std / winnings.Count);
            double se = std / Math.Sqrt(winnings.Count);
            Console.WriteLine(
                $"(Round {i}) Player 0 E[Profit] = {avg:0.00}BB, 95%-CI = [ {avg - 1.96 * se:0.00}BB, {avg + 1.96 * se:0.00}BB ]");
        }

        return scores;
    }
}