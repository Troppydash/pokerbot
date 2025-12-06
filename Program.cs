using PokerBot.Bots;
using PokerBot.Bots.Cfr;

namespace PokerBot;

public class Program
{
    static int[] SimulateAll(List<IAgent> agents, int iters = 1000, bool verbose = true)
    {
        int[] scores = [0, 0];

        Random rng = new Random(42);
        List<double> winnings = [];
        for (int i = 0; i < iters; ++i)
        {
            Console.WriteLine($"Game {i}");
            int seed = rng.Next();

            Arena arena = new Arena([agents[0], agents[1]]);
            int[] result = arena.Simulate(seed, verbose);
            scores[0] += result[0];
            scores[1] += result[1];
            winnings.Add(result[0]);

            arena = new Arena([agents[1], agents[0]]);
            result = arena.Simulate(seed, verbose);
            scores[0] += result[1];
            scores[1] += result[0];
            winnings.Add(result[1]);

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
            Console.WriteLine($"Player 0 {avg:0.00}, CI 95% = ({avg - 1.96 * se:0.00}, {avg + 1.96 * se:0.00})");
        }


        return scores;
    }

    static void Solve()
    {
        Infoset set = Infoset.FromClusters();
        Solver solver = new Solver(set);
        solver.Simulate(1000000, 56453);
    }

    public static void Main()
    {
        // foreach (var comb in Helper.LazyCombinations(5, 3))
        // {
        //     Console.WriteLine($"{comb[0]} {comb[1]} {comb[2]}");
        // }
        // FastResolver.Cached.GetInstance();
        // EmdCluster.MakePrivateClusterPoints(0);
        // TODO: simulate bot
        // Solve();

        // Infoset set = Infoset.FromClusters();
        // CfrAgent agent = new CfrAgent(set, Solver.Result.Load("result4.json", set));
        // int n = 100000;

        // var result = SimulateAll([new EvAgent(false), new ManualAgent()], 1000, false);
        var result = SimulateAll([new EvAgent(false), new ManualAgent()], 1000, true);
        
        //
        //

        // Console.WriteLine($"player 0 average utility {(float)result[0] / (2*n)}, player 1 average utility {(float)result[1] / (2*n)}");
        // Card[] cards = Card.AllCards();
        // var dist = EMDCluster.ComputeEquityDistribution(
        //     [cards[0], cards[1]], []);

        // foreach (var i in dist)
        // {
        //     Console.Write($"{i}, ");
        // }


        // var publicPoints = EmdCluster.LoadPublicClusterPoints("publicCluster.json");
        // var group0 = EmdCluster.ClusterPublicCards("publicClusterGroup0.json", publicPoints, 0, 10, 1);
        // var group3 = EmdCluster.ClusterPublicCards("publicClusterGroup3.json", publicPoints, 3, 10, 20);
        // var group4 = EmdCluster.ClusterPublicCards("publicClusterGroup4.json", publicPoints, 4, 10, 40);
        // var group5 = EmdCluster.ClusterPublicCards("publicClusterGroup5.json", publicPoints, 5, 10, 50);

        // var group0 = EmdCluster.LoadClusterPublicCards("publicClusterGroup3.json", 3);
        // foreach (var group in group0)
        // {
        //     Console.WriteLine($"cluster {group.Key} with {group.Value.Count}");
        //     foreach (var point in group.Value)
        //     {
        //         if (point.Visible[0].Rank == 0 && point.Visible[1].Rank == 1 && point.Visible[2].Rank == 2)
        //         {
        //             Console.Write("here");
        //         }
        //         Console.Write($"{point.Visible[0]} {point.Visible[1]} {point.Visible[2]}, ");
        //     }
        //     Console.WriteLine();
        // }

        // foreach (var group in group3)
        // {
        //     Console.WriteLine($"cluster {group.Key} with {group.Value.Count}");
        // }
        //
        // foreach (var group in group4)
        // {
        //     Console.WriteLine($"cluster {group.Key} with {group.Value.Count}");
        // }
        //
        // foreach (var group in group5)
        // {
        //     Console.WriteLine($"cluster {group.Key} with {group.Value.Count}");
        // }


        // cluster


        // var clusters = EmdCluster.LoadHoleClusterPoints("holeCluster.json")!;
        // var groups = EmdCluster.LoadClusterHoleCards("holeClusterGroups.json")!;
        //
        // EmdCluster.SavePublicClusterPoints("publicCluster.json", groups, 5, 5, 5);

        // foreach (var cluster in clusters)
        // {
        // Console.WriteLine($"{cluster.Hole[0]}{cluster.Hole[1]}");
        // Console.WriteLine(cluster.EquityDistribution[0]);
        // }

        // var result = EmdCluster.ClusterHoleCards("holeClusterGroups.json", clusters, 1000, 20);
        // foreach (var item in result)
        // {
        //     Console.WriteLine($"cluster {item.Key} with {item.Value.Count}");
        //     foreach (var c in item.Value)
        //     {
        //         Console.Write($"{c.Hole[0]} {c.Hole[1]}, ");
        //     }
        //
        //     Console.WriteLine();
        // }
    }
}