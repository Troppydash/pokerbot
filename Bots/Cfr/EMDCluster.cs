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
    public static EmdCluster ComputeCluster(Card[] hole, Card[] visible, int samples, int bins)
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

    public static double ComputeEquity(Card[] hole, Card[] visible, int oppSamples, int samples)
    {
        List<Card> remain = Card.AllCards().ToList();
        foreach (var card in hole)
            remain.Remove(card);
        foreach (var card in visible)
            remain.Remove(card);

        Card[] river = new Card[5];
        visible.CopyTo(river, 0);

        int n = remain.Count;
        double equity = 0.0;

        for (int i = 0; i < oppSamples; ++i)
        {
            int a = 0;
            int b = 0;
            while (a == b)
            {
                a = Random.Shared.Next(n);
                b = Random.Shared.Next(n);
            }

            Card[] oppHole = [remain[a], remain[b]];
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

            equity += wins;

            remain.Add(oppHole[0]);
            remain.Add(oppHole[1]);
        }

        equity /= (double)samples * oppSamples;
        return equity;
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

    #region Private Hole Clusters

    public static List<EmdCluster> SaveHoleClusterPoints(string path, int samples = 1000, int bins = 10)
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
        int iter = 1000,
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
        for (int i = 0; i < points.Count; ++i)
            centroidMap[i] = -1;

        // iterate kmeans (update centroid map, update centroid point)
        for (int it = 0; it < iter; ++it)
        {
            Console.WriteLine($"iter {it}");

            // update centroid map
            bool changed = false;
            for (int i = 0; i < points.Count; ++i)
            {
                int original = centroidMap[i];

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

                if (centroidMap[i] != original)
                    changed = true;
            }

            // early exit
            if (!changed)
                break;

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

                if (count > 0)
                {
                    for (int k = 0; k < bins; ++k)
                    {
                        centroids[i].EquityDistribution[k] /= count;
                    }
                }
                else
                {
                    // choose a random point
                    int p = Random.Shared.Next(points.Count);
                    for (int z = 0; z < bins; ++z)
                    {
                        centroids[i].EquityDistribution[z] = points[p].EquityDistribution[z];
                    }
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

    public static Dictionary<int, List<EmdCluster>>? LoadClusterHoleCards(string filename)
    {
        string text = File.ReadAllText(filename);
        return JsonSerializer.Deserialize<Dictionary<int, List<EmdCluster>>>(text);
    }

    #endregion
    
    #region Public Clusters

    /// <summary>
    /// Computes the public cluster points. Notes that the return clusters ignores
    /// duplicated decks, unlike *LoadPublicClusterPoints* (which adds them back)
    /// </summary>
    /// <param name="path"></param>
    /// <param name="privateClusters"></param>
    /// <param name="clusterSample"></param>
    /// <param name="oppSamples"></param>
    /// <param name="samples"></param>
    /// <returns></returns>
    public static List<EmdCluster> SavePublicClusterPoints(
        string path,
        Dictionary<int, List<EmdCluster>> privateClusters,
        int clusterSample = 5,
        int oppSamples = 5,
        int samples = 5
    )
    {
        List<EmdCluster> publicCluster = [];

        Card[] cards = Card.AllCards();

        // iterate over all streets
        foreach (var k in (int[]) [0, 3, 4, 5])
        {
            Dictionary<int, EmdCluster> cached = new Dictionary<int, EmdCluster>();

            // all combinations of size k
            int f = 0;
            List<List<int>> combs = Combinations(cards.Length, k);
            foreach (var choices in combs)
            {
                f += 1;
                Console.Write($"\r{k}th Street, Progress {(double)f / combs.Count * 100}%");

                Card[] selected = new Card[choices.Count];
                for (int i = 0; i < choices.Count; ++i)
                {
                    selected[i] = cards[choices[i]];
                }

                int hash = Card.HashDeck(selected);
                if (cached.ContainsKey(hash))
                {
                    // ignore
                }
                else
                {
                    // iterate over all private clusters
                    double[] histogram = new double[privateClusters.Count];
                    double total = 0.0;

                    // sample hole cards
                    foreach (var privateCluster in privateClusters)
                    {
                        double equity = 0.0;

                        // sample from private cluster
                        for (int t = 0; t < clusterSample; ++t)
                        {
                            var cluster = privateCluster.Value[Random.Shared.Next(privateCluster.Value.Count)];
                            equity += ComputeEquity(
                                cluster.Hole,
                                selected,
                                oppSamples,
                                samples
                            );
                        }

                        histogram[privateCluster.Key] = equity / clusterSample;
                        total += histogram[privateCluster.Key];
                    }

                    for (int i = 0; i < histogram.Length; ++i)
                    {
                        histogram[i] /= total;
                    }

                    EmdCluster newCluster = new EmdCluster()
                    {
                        EquityDistribution = histogram,
                        Hole = null,
                        Visible = selected
                    };
                    publicCluster.Add(newCluster);
                    cached.Add(hash, newCluster);
                }
            }

            Console.WriteLine();

            string output = JsonSerializer.Serialize(publicCluster);
            File.WriteAllText(path, output);
        }

        return publicCluster;
    }

    public static List<EmdCluster> LoadPublicClusterPoints(
        string path)
    {
        var points = JsonSerializer.Deserialize<List<EmdCluster>>(File.ReadAllText(path))!;

        // adds the duplicated back
        List<EmdCluster> completePoints = [];
        Card[] cards = Card.AllCards();

        // iterate over all streets
        foreach (var k in (int[]) [0, 3, 4, 5])
        {
            Dictionary<int, EmdCluster> cache = new Dictionary<int, EmdCluster>();
            foreach (var cluster in points)
            {
                if (cluster.Visible.Length == k)
                {
                    cache.Add(Card.HashDeck(cluster.Visible), cluster);
                }
            }

            // all combinations of size k
            List<List<int>> combs = Combinations(cards.Length, k);
            foreach (var choices in combs)
            {
                Card[] selected = new Card[choices.Count];
                for (int i = 0; i < choices.Count; ++i)
                {
                    selected[i] = cards[choices[i]];
                }

                int hash = Card.HashDeck(selected);
                completePoints.Add(new EmdCluster()
                {
                    Visible = selected,
                    Hole = null,
                    EquityDistribution = cache[hash].EquityDistribution
                });
            }
        }

        return completePoints;
    }

    public static Dictionary<int, List<EmdCluster>> ClusterPublicCards(
        string filename,
        List<EmdCluster> points,
        int street,
        int iter = 100,
        int clusters = 20
    )
    {
        List<EmdCluster> filteredPoints = [];
        foreach (var point in points)
        {
            if (point.Visible.Length == street)
                filteredPoints.Add(point);
        }

        // use k mean clustering
        int bins = filteredPoints[0].EquityDistribution.Length;
        EmdCluster[] centroids = new EmdCluster[clusters];
        for (int i = 0; i < clusters; ++i)
        {
            int j = Random.Shared.Next(filteredPoints.Count);
            double[] newDist = new double[bins];
            Array.Copy(filteredPoints[j].EquityDistribution, newDist, bins);
            centroids[i] = new EmdCluster()
            {
                Hole = null,
                Visible = null,
                EquityDistribution = newDist
            };
        }

        int[] centroidMap = new int[filteredPoints.Count];
        for (int i = 0; i < filteredPoints.Count; ++i)
            centroidMap[i] = -1;

        // iterate kmeans (update centroid map, update centroid point)
        for (int it = 0; it < iter; ++it)
        {
            Console.WriteLine($"iter {it}");

            // update centroid map
            bool changed = false;
            for (int i = 0; i < filteredPoints.Count; ++i)
            {
                int original = centroidMap[i];

                // closest centroid
                double closest = Double.MaxValue;
                for (int j = 0; j < clusters; ++j)
                {
                    double dist = filteredPoints[i].Distance(centroids[j]);
                    if (dist < closest)
                    {
                        closest = dist;
                        centroidMap[i] = j;
                    }
                }

                if (centroidMap[i] != original)
                    changed = true;
            }

            // early exit
            if (!changed)
                break;

            // update centroid point
            for (int i = 0; i < clusters; ++i)
            {
                int count = 0;

                for (int k = 0; k < bins; ++k)
                {
                    centroids[i].EquityDistribution[k] = 0;
                }

                for (int j = 0; j < filteredPoints.Count; ++j)
                {
                    if (centroidMap[j] == i)
                    {
                        for (int k = 0; k < bins; ++k)
                        {
                            centroids[i].EquityDistribution[k] += filteredPoints[j].EquityDistribution[k];
                        }

                        count += 1;
                    }
                }

                if (count > 0)
                {
                    for (int k = 0; k < bins; ++k)
                    {
                        centroids[i].EquityDistribution[k] /= count;
                    }
                }
                else
                {
                    // choose a random point
                    int p = Random.Shared.Next(filteredPoints.Count);
                    for (int z = 0; z < bins; ++z)
                    {
                        centroids[i].EquityDistribution[z] = filteredPoints[p].EquityDistribution[z];
                    }
                }
            }
        }

        // output

        Dictionary<int, EmdCluster> cache = new Dictionary<int, EmdCluster>();

        Dictionary<int, List<EmdCluster>> finalClusters = new Dictionary<int, List<EmdCluster>>();
        for (int i = 0; i < clusters; ++i)
        {
            finalClusters[i] = new List<EmdCluster>();
        }

        for (int i = 0; i < centroidMap.Length; ++i)
        {
            // centroidMap[pointIndex] = centroid index
            int hash = Card.HashDeck(filteredPoints[i].Visible);
            if (!cache.ContainsKey(hash))
            {
                finalClusters[centroidMap[i]].Add(filteredPoints[i]);
                cache.Add(hash, filteredPoints[i]);
            }
        }

        string output = JsonSerializer.Serialize(finalClusters);
        File.WriteAllText(filename, output);

        return finalClusters;
    }

    public static Dictionary<int, List<EmdCluster>>? LoadClusterPublicCards(string filename, int street)
    {
        Dictionary<int, List<EmdCluster>> groups = LoadClusterHoleCards(filename)!;

        // complete load

        Dictionary<int, List<EmdCluster>> completeGroups = new Dictionary<int, List<EmdCluster>>();
        
        // create mapping from hash to groupId
        Dictionary<int, (int, EmdCluster)> hashToGroup = new Dictionary<int, (int, EmdCluster)>();
        foreach (var entry in groups)
        {
            completeGroups[entry.Key] = [];
            
            foreach (var point in entry.Value)
            {
                hashToGroup[Card.HashDeck(point.Visible)] = (entry.Key, point);
            }
        }

        Card[] cards = Card.AllCards();
        List<List<int>> combs = Combinations(cards.Length, street);
        foreach (var choices in combs)
        {
            Card[] selected = new Card[choices.Count];
            for (int i = 0; i < choices.Count; ++i)
            {
                selected[i] = cards[choices[i]];
            }

            var (groupId, point) = hashToGroup[Card.HashDeck(selected)];
            completeGroups[groupId].Add(new EmdCluster()
            {
                EquityDistribution =point.EquityDistribution,
                Visible = selected,
                Hole = null
            });
        }

        return completeGroups;
    }

    #endregion
    
    public static void GenerateAndLoadCluster()
    {
        EmdCluster.SaveHoleClusterPoints("holeCluster.json", 1000, 10);
        var clusters = EmdCluster.LoadHoleClusterPoints("holeCluster.json")!;
        EmdCluster.ClusterHoleCards("holeClusterGroups.json", clusters, 1000, 20);
        var groups = EmdCluster.LoadClusterHoleCards("holeClusterGroups.json")!;
        
        EmdCluster.SavePublicClusterPoints("publicCluster.json", groups, 5, 5, 5);
        var publicPoints = EmdCluster.LoadPublicClusterPoints("publicCluster.json");
        
        EmdCluster.ClusterPublicCards("publicClusterGroup0.json", publicPoints, 0, 10, 1);
        EmdCluster.ClusterPublicCards("publicClusterGroup3.json", publicPoints, 3, 10, 20);
        EmdCluster.ClusterPublicCards("publicClusterGroup4.json", publicPoints, 4, 10, 40);
        EmdCluster.ClusterPublicCards("publicClusterGroup5.json", publicPoints, 5, 10, 50);

        var group0 = EmdCluster.LoadClusterPublicCards("publicClusterGroup0.json", 0);
        var group3 = EmdCluster.LoadClusterPublicCards("publicClusterGroup3.json", 3);
        var group4 = EmdCluster.LoadClusterPublicCards("publicClusterGroup4.json", 4);
        var group5 = EmdCluster.LoadClusterPublicCards("publicClusterGroup5.json", 5);
    }
}