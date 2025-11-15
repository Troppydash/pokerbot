using PokerBot.Bots;

namespace PokerBot;

public class Program
{
    static int[] SimulateAll(List<IAgent> agents, int iters = 1000, bool verbose = true)
    {
        int[] scores = [0, 0];

        for (int i = 0; i < iters; ++i)
        {
            Console.WriteLine($"Game {i}");
            int seed = Random.Shared.Next();

            Arena arena = new Arena([agents[0], agents[1]]);
            int[] result = arena.Simulate(seed, verbose);
            scores[0] += result[0];
            scores[1] += result[1];
            

            arena = new Arena([agents[1], agents[0]]);
            result = arena.Simulate(seed, verbose);
            scores[0] += result[1];
            scores[1] += result[0];
            
            Console.WriteLine($"{scores[0]}, {scores[1]}");
        }


        return scores;
    }

    public static void Main()
    {
        // Arena arena = new Arena([new RandomAgent(), new RandomAgent()]);
        // arena.Simulate(55);

        int n = 1000;
        var result = SimulateAll([new EvAgent(false), new RandomAgent2()], n, false);
        Console.WriteLine($"player 0 average utility {(float)result[0] / (2*n)}, player 1 average utility {(float)result[1] / (2*n)}");
    }
}