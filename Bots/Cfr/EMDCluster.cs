using System.Text.Json;

namespace PokerBot.Bots.Cfr;

public class EmdCluster
{
    public Card[] Hole { get; set; }
    public Card[] Visible { get; set; }
    public double[] EquityDistribution { get; set; }

    public double Distance(EmdCluster other)
    {
        // use to L2 for now
        double distance = 0.0;
        for (int i = 0; i < EquityDistribution.Length; ++i)
        {
            distance += Math.Abs(other.EquityDistribution[i] - EquityDistribution[i]);
        }

        return distance;
    }

    /// <summary>
    /// Compute the equity distribution for hole and visible cards
    /// </summary>
    /// <returns></returns>
    public static EmdCluster ComputeCluster(Card[] hole, Card[] visible, int samples = 100, int bins = 20)
    {
        List<Card> remain = Card.AllCards().ToList();
        foreach (var card in hole)
            remain.Remove(card);
        foreach (var card in visible)
            remain.Remove(card);

        double[] equityDistribution = new double[bins + 1];
        double binWidth = 1.0 / bins;

        Card[] river = new Card[5];
        visible.CopyTo(river, 0);

        int n = remain.Count;
        for (int i = 0; i < n; ++i)
        {
            for (int j = i + 1; j < n; ++j)
            {
                Card[] oppHole = [remain[i], remain[j]];
                remain.Remove(oppHole[0]);
                remain.Remove(oppHole[1]);

                // compute estimated equity
                int wins = 0;
                Card[] others = remain.ToArray();
                for (int k = 0; k < samples; ++k)
                {
                    Random.Shared.Shuffle(others);
                    for (int z = visible.Length; z < 5; ++z)
                    {
                        river[z] = others[z - visible.Length];
                    }

                    var result = FastResolver.CompareHands(
                        river, hole, oppHole);
                    if (result == FastResolver.Player0)
                        wins += 1;
                }

                double equity = (double)wins / samples;
                int bin = (int)Math.Floor(equity / binWidth);
                equityDistribution[bin] += 1.0 / ((double)n * (n + 1) / 2);

                remain.Add(oppHole[0]);
                remain.Add(oppHole[1]);
            }
        }

        return new EmdCluster
        {
            Hole = hole,
            Visible = visible,
            EquityDistribution = equityDistribution
        };
    }

    private static List<List<int>> Combinations(int n, int k, int offset = 0)
    {
        if (k == 0)
        {
            return [[]];
        }

        if (n == 0)
        {
            return [];
        }


        // choose
        var choose = Combinations(n - 1, k - 1, offset + 1);
        var notChoose = Combinations(n - 1, k, offset + 1);

        foreach (var l in choose)
        {
            l.Insert(0, offset);
            notChoose.Add(l);
        }

        return notChoose;
    }

    public static List<EmdCluster> SaveHoleClusterPoints(string path, int samples = 100, int bins = 20)
    {
        List<EmdCluster> privateCluster = new List<EmdCluster>();

        Card[] cards = Card.AllCards();

        Dictionary<int, EmdCluster> cached = new Dictionary<int, EmdCluster>();

        // iterate all hole cards
        int progress = 0;
        int total = cards.Length * (cards.Length - 1) / 2;
        for (int i = 0; i < cards.Length; ++i)
        {
            for (int j = i + 1; j < cards.Length; ++j)
            {
                progress += 1;
                Console.WriteLine($"Hole card {i}/{j}, {(double)progress / total}");

                Card[] hole = [cards[i], cards[j]];
                int hash = Card.HashDeck(hole);
                if (cached.TryGetValue(hash, out var cachedCluster))
                {
                    privateCluster.Add(
                        new EmdCluster()
                        {
                            EquityDistribution = cachedCluster.EquityDistribution,
                            Hole = hole,
                            Visible = []
                        });
                }
                else
                {
                    EmdCluster cluster = ComputeCluster(
                        hole,
                        [],
                        samples,
                        bins
                    );
                    privateCluster.Add(cluster);
                    cached[hash] = cluster;
                }
            }

            string output = JsonSerializer.Serialize(privateCluster);
            File.WriteAllText(path, output);
        }

        return privateCluster;
    }

    public static List<EmdCluster>? LoadHoleClusterPoints(string path)
    {
        string text = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<EmdCluster>>(text);
    }

    public static Dictionary<int, List<EmdCluster>> ClusterHoleCards(string filename, List<EmdCluster> points,
        int iter = 100,
        int clusters = 20)
    {
        // use k mean clustering

        int bins = points[0].EquityDistribution.Length;
        EmdCluster[] centroids = new EmdCluster[clusters];
        for (int i = 0; i < clusters; ++i)
        {
            int j = Random.Shared.Next(points.Count);
            double[] newDist = new double[bins];
            Array.Copy(points[j].EquityDistribution, newDist, bins);
            centroids[i] = new EmdCluster()
            {
                Hole = null,
                Visible = null,
                EquityDistribution = newDist
            };
        }

        int[] centroidMap = new int[points.Count];

        // iterate kmeans (update centroid map, update centroid point)
        for (int it = 0; it < iter; ++it)
        {
            Console.WriteLine($"iter {it}");
            // update centroid map
            for (int i = 0; i < points.Count; ++i)
            {
                // closest centroid
                double closest = Double.MaxValue;
                for (int j = 0; j < clusters; ++j)
                {
                    double dist = points[i].Distance(centroids[j]);
                    if (dist < closest)
                    {
                        closest = dist;
                        centroidMap[i] = j;
                    }
                }
            }

            // update centroid point
            for (int i = 0; i < clusters; ++i)
            {
                int count = 0;

                for (int k = 0; k < bins; ++k)
                {
                    centroids[i].EquityDistribution[k] = 0;
                }

                for (int j = 0; j < points.Count; ++j)
                {
                    if (centroidMap[j] == i)
                    {
                        for (int k = 0; k < bins; ++k)
                        {
                            centroids[i].EquityDistribution[k] += points[j].EquityDistribution[k];
                        }

                        count += 1;
                    }
                }

                for (int k = 0; k < bins; ++k)
                {
                    centroids[i].EquityDistribution[k] /= count;
                }
            }
        }

        Dictionary<int, List<EmdCluster>> finalClusters = new Dictionary<int, List<EmdCluster>>();
        for (int i = 0; i < clusters; ++i)
        {
            finalClusters[i] = new List<EmdCluster>();
        }

        for (int i = 0; i < centroidMap.Length; ++i)
        {
            // centroidMap[pointIndex] = centroid index
            finalClusters[centroidMap[i]].Add(points[i]);
        }

        string output = JsonSerializer.Serialize(finalClusters);
        File.WriteAllText(filename, output);
        
        return finalClusters;
    }

    public static void SavePublicClusterEquityDistributions(string path, int samples = 100, int bins = 20)
    {
        List<EmdCluster> privateCluster = new List<EmdCluster>();

        Card[] cards = Card.AllCards();
        Card[] remain = new Card[cards.Length - 2];

        // iterate all hole cards
        int progress = 0;
        int total = cards.Length * (cards.Length - 1) / 2;
        for (int i = 0; i < cards.Length; ++i)
        {
            for (int j = i + 1; j < cards.Length; ++j)
            {
                progress += 1;
                Console.WriteLine($"Hole card {i}/{j}, {(double)progress / total}");

                EmdCluster cluster = ComputeCluster(
                    [cards[i], cards[j]],
                    [],
                    samples,
                    bins
                );
                privateCluster.Add(cluster);

                // // remain cards
                // int p = 0;
                // for (int k = 0; k < cards.Length; ++k)
                // {
                //     if (k == i || k == j) continue;
                //     remain[p++] = cards[k];
                // }
                //
                //
                // // iterate over all streets
                // foreach (var k in (int[]) [0, 3, 4, 5])
                // {
                //     Console.WriteLine($"Street {k}");
                //     Card[] selected = new Card[k];
                //
                //     // all combinations of size k
                //     foreach (var choices in Combinations(remain.Length, k))
                //     {
                //         int x = 0;
                //         foreach (var z in choices)
                //         {
                //             selected[x++] = remain[z];
                //         }
                //
                //         EmdCluster cluster = ComputeCluster(
                //             [cards[i], cards[j]],
                //             selected,
                //             samples,
                //             bins
                //         );
                //         privateCluster.Add(cluster);
                //     }
                // }
            }
        }

        string output = JsonSerializer.Serialize(privateCluster);
        File.WriteAllText(path, output);

        // var result = JsonSerializer.Deserialize<List<EmdCluster>>(output);
        // Console.WriteLine(result);
    }
}