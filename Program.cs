namespace PokerBot;

public class Program
{
    static int[] SimulateAll(List<IAgent> agents, int iters = 1000)
    {
        int[] scores = [0, 0];

        for (int i = 0; i < iters; ++i)
        {
            int seed = Random.Shared.Next();

            Arena arena = new Arena([agents[0], agents[1]]);
            int[] result = arena.Simulate(seed, false);
            scores[0] += result[0];
            scores[1] += result[1];


            arena = new Arena([agents[1], agents[0]]);
            result = arena.Simulate(seed, false);
            scores[0] += result[1];
            scores[1] += result[0];
        }


        return scores;
    }

    public static void Main()
    {
        // Arena arena = new Arena([new RandomAgent(), new RandomAgent()]);
        // arena.Simulate(55);

        int n = 100000;
        var result = SimulateAll([new RandomAgent(), new RandomAgent()], n);
        Console.WriteLine($"player 0 average utility {(float)result[0] / n}, player 1 average utility {(float)result[1] / n}");
    }
}