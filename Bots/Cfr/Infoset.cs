using PokerBot.Bots.Shared;

namespace PokerBot.Bots.Cfr;

/// <summary>
/// Handles the infoset lookup step of the CFR algorithm
/// </summary>
public class Infoset
{
    /// <summary>
    /// Represent an abstract game state, containing multiple game states
    /// </summary>
    public class Entry
    {
        public int Street { get; set; }
        public int Position { get; set; }

        public int[] PublicClusterIds { get; set; }

        public int[] HoleClusterIds { get; set; }

        public int EffectiveStack { get; set; }
        public List<Action> Bets { get; set; }

        private string? _string;

        public Entry(
            int street, int position,
            int[] publicClusterIds,
            int[] holeClusterIds,
            int stack, List<Action> bets)
        {
            Street = street;
            Position = position;
            PublicClusterIds = publicClusterIds;
            HoleClusterIds = holeClusterIds;
            EffectiveStack = stack;
            Bets = bets;

            _string = null;
        }

        public override string ToString()
        {
            if (_string == null)
            {
                _string =
                    $"{Street}/{Position}/{PublicClusterIds[0]},{PublicClusterIds[1]},{PublicClusterIds[2]},{PublicClusterIds[3]}/{HoleClusterIds[0]},{HoleClusterIds[1]},{HoleClusterIds[2]},{HoleClusterIds[3]}/{EffectiveStack}/";
                // list of raises
                foreach (var action in Bets)
                {
                    _string += action.Repr();
                }
            }

            return _string;
        }
    }

    private int[] _preflopPrivateCluster;
    private int[] _flopPrivateCluster;
    private Dictionary<long, int> _turnPrivateCluster;
    private Dictionary<long, int> _riverPrivateCluster;

    private int[] _preflopCluster;
    private int[] _flopCluster;
    private int[] _turnCluster;
    private int[] _riverCluster;

    private Dictionary<int, List<EmdCluster>> _preflopPrivateMap;
    private Dictionary<int, List<EmdCluster>> _flopPrivateMap;
    private Dictionary<int, List<EmdCluster>> _turnPrivateMap;
    private Dictionary<int, List<EmdCluster>> _riverPrivateMap;

    private Dictionary<int, List<EmdCluster>> _preflopMap;
    private Dictionary<int, List<EmdCluster>> _flopMap;
    private Dictionary<int, List<EmdCluster>> _turnMap;
    private Dictionary<int, List<EmdCluster>> _riverMap;


    public Infoset(
        Dictionary<int, List<EmdCluster>> preflopPrivateCluster,
        Dictionary<int, List<EmdCluster>> flopPrivateCluster,
        Dictionary<int, List<EmdCluster>> turnPrivateCluster,
        Dictionary<int, List<EmdCluster>> riverPrivateCluster,
        Dictionary<int, List<EmdCluster>> preflopCluster,
        Dictionary<int, List<EmdCluster>> flopCluster,
        Dictionary<int, List<EmdCluster>> turnCluster,
        Dictionary<int, List<EmdCluster>> riverCluster
    )
    {
        _preflopPrivateMap = preflopPrivateCluster;
        _flopPrivateMap = flopPrivateCluster;
        _turnPrivateMap = turnPrivateCluster;
        _riverPrivateMap = riverPrivateCluster;

        _preflopMap = preflopCluster;
        _flopMap = flopCluster;
        _turnMap = turnCluster;
        _riverMap = riverCluster;


        _preflopPrivateCluster = new int[Card.MaxHashDeck(2)];
        _flopPrivateCluster = new int[Card.MaxHashDeck(5)];
        _turnPrivateCluster = new Dictionary<long, int>();
        _riverPrivateCluster = new Dictionary<long, int>();

        _preflopCluster = new int[Card.MaxHashDeck(0)];
        _flopCluster = new int[Card.MaxHashDeck(3)];
        _turnCluster = new int[Card.MaxHashDeck(4)];
        _riverCluster = new int[Card.MaxHashDeck(5)];

        // create mappings
        foreach (var entry in preflopPrivateCluster)
        {
            foreach (var point in entry.Value)
            {
                _preflopPrivateCluster[Card.HashDeck7(point.Hole!, point.Visible!)] = entry.Key;
            }
        }

        foreach (var entry in flopPrivateCluster)
        {
            foreach (var point in entry.Value)
            {
                _flopPrivateCluster[Card.HashDeck7(point.Hole!, point.Visible!)] = entry.Key;
            }
        }

        foreach (var entry in turnPrivateCluster)
        {
            foreach (var point in entry.Value)
            {
                _turnPrivateCluster.TryAdd(Card.HashDeck7(point.Hole!, point.Visible!), entry.Key);
            }
        }

        foreach (var entry in riverPrivateCluster)
        {
            foreach (var point in entry.Value)
            {
                _riverPrivateCluster.TryAdd(Card.HashDeck7(point.Hole!, point.Visible!), entry.Key);
            }
        }

        foreach (var entry in preflopCluster)
        {
            foreach (var point in entry.Value)
            {
                _preflopCluster[Card.HashDeck(point.Visible!)] = entry.Key;
            }
        }

        foreach (var entry in flopCluster)
        {
            foreach (var point in entry.Value)
            {
                _flopCluster[Card.HashDeck(point.Visible!)] = entry.Key;
            }
        }

        foreach (var entry in turnCluster)
        {
            foreach (var point in entry.Value)
            {
                _turnCluster[Card.HashDeck(point.Visible!)] = entry.Key;
            }
        }

        foreach (var entry in riverCluster)
        {
            foreach (var point in entry.Value)
            {
                _riverCluster[Card.HashDeck(point.Visible!)] = entry.Key;
            }
        }
    }

    public Entry Lookup(
        int street,
        int position,
        Card[] hole,
        Card[] visible,
        int stack,
        List<Action> bets
    )
    {
        // TODO: make the public and hole cluster ids hierarchical
        int[] privateClusters =
        [
            _preflopPrivateCluster[Card.HashDeck7(hole, visible.Take(0).ToArray())],
            visible.Length < 3 ? -1 : _flopPrivateCluster[Card.HashDeck7(hole, visible.Take(3).ToArray())],
            visible.Length < 4 ? -1 : _turnPrivateCluster[Card.HashDeck7(hole, visible.Take(4).ToArray())],
            visible.Length < 5 ? -1 : _riverPrivateCluster[Card.HashDeck7(hole, visible.Take(5).ToArray())]
        ];

        int[] publicClusters =
        [
            _preflopCluster[Card.HashDeck(visible.Take(0).ToArray())],
            visible.Length < 3 ? -1 : _flopCluster[Card.HashDeck(visible.Take(3).ToArray())],
            visible.Length < 4 ? -1 : _turnCluster[Card.HashDeck(visible.Take(4).ToArray())],
            visible.Length < 5 ? -1 : _riverCluster[Card.HashDeck(visible.Take(5).ToArray())]
        ];

        // partition stacks
        int abstractStack = stack / 200;
        return new Entry(street, position, publicClusters, privateClusters, abstractStack, bets);
    }

    public Entry FromGame(Game game)
    {
        var state = game.GetState();
        return Lookup(state.Street, state.Index, state.Hand, state.River, int.Min(state.Money[0], state.Money[1]),
            state.History);
    }

    // public IEnumerable<(Card[], Card[], Card[])> AllStarters()
    // {
    //     // need to generate all hole/public cluster pairs
    //     foreach (var sbHole in _holeMap)
    //     {
    //         Card[] sbh = sbHole.Value[0].Hole!;
    //
    //         foreach (var bbHole in _holeMap)
    //         {
    //             foreach (var bbHoleEmd in bbHole.Value)
    //             {
    //                 // ensure sbh and bbh are different
    //                 Card[] bbh = bbHoleEmd.Hole!;
    //                 if (!Helper.Distinct(sbh, bbh)) continue;
    //
    //                 Card[] combined = sbh.Concat(bbh).ToArray();
    //
    //                 foreach (var map in (List<Dictionary<int, List<EmdCluster>>>)[_flopMap, _turnMap, _riverMap])
    //                 {
    //                     foreach (var emdCluster in map)
    //                     {
    //                         // ensure no conflicts
    //                         foreach (var cluster in emdCluster.Value)
    //                         {
    //                             if (!Helper.Distinct(combined, cluster.Visible!)) continue;
    //
    //                             Card[] remain = Helper.SelectRemain(combined,
    //                                 5 - cluster.Visible!.Length);
    //
    //                             yield return (sbh, bbh, cluster.Visible.Concat(remain).ToArray());
    //                             break;
    //                         }
    //                     }
    //                 }
    //
    //                 break;
    //             }
    //         }
    //     }
    // }

    public IEnumerable<Entry> Forward(int depth, List<Action> validActions)
    {
        // 0/0/0/4
        // 0/1/0/3/C
        foreach (int street in (int[])[0, 3, 4, 5, 6])
        {
            // bets
            List<List<Action>> actions = [[]];
            for (int count = 0; count <= depth + 1; ++count)
            {
                foreach (var list in actions)
                {
                    foreach (var private0 in _preflopPrivateMap.Keys.Concat([-1]))
                    {
                        foreach (var private3 in _flopPrivateMap.Keys.Concat([-1]))
                        {
                            foreach (var private4 in _turnPrivateMap.Keys.Concat([-1]))
                            {
                                foreach (var private5 in _riverPrivateMap.Keys.Concat([-1]))
                                {
                                    foreach (var public0 in _preflopMap.Keys.Concat([-1]))
                                    {
                                        foreach (var public3 in _flopMap.Keys.Concat([-1]))
                                        {
                                            foreach (var public4 in _turnMap.Keys.Concat([-1]))
                                            {
                                                foreach (var public5 in _riverMap.Keys.Concat([-1]))
                                                {
                                                    foreach (int stack in (int[])[0, 1, 2, 3, 4])
                                                    {
                                                        int[] publicIds =
                                                            [public0, public3, public4, public5];
                                                        int[] privateIds =
                                                            [private0, private3, private4, private5];

                                                        int position = count % 2;
                                                        yield return new Entry(street, position, publicIds, privateIds,
                                                            stack, list);

                                                        if (list.Count > 0 && list.Last().IsFold())
                                                        {
                                                            yield return new Entry(street, 1 - position, publicIds,
                                                                privateIds, stack, list);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (street == 6)
                    break;

                List<List<Action>> newActions = [];
                foreach (var action in actions)
                {
                    foreach (var valid in validActions)
                    {
                        if (action.Count > 0 && action.Last().IsFold())
                            continue;

                        newActions.Add([..action, valid]);
                    }
                }

                actions = newActions;
            }
        }
    }

    public static Infoset FromClusters()
    {
        // private clusters
        var preflopPrivatePoints =
            FileCache.OptionalCompute(
                "PrivatePoint0.json",
                () => EmdCluster.MakePrivateClusterPoints(0, 50, 10, 20)
            );
        var flopPrivatePoints =
            FileCache.OptionalCompute(
                "PrivatePoint3.json",
                () => EmdCluster.MakePrivateClusterPoints(3, 10, 10, 10)
            );
        var turnPrivatePoints =
            FileCache.OptionalCompute(
                "PrivatePoint4.json",
                () => EmdCluster.MakePrivateClusterPoints(4, 3, 5, 10)
            );
        var riverPrivatePoints =
            FileCache.OptionalCompute(
                "PrivatePoint5.json",
                () => EmdCluster.MakePrivateClusterPoints(5, 3, 5, 10)
            );
        
        var preflopPrivateClusters =
            FileCache.OptionalCompute(
                "PrivateCluster0.json",
                () =>
                    EmdCluster.ClusterPrivatePoints(preflopPrivatePoints, 10, 50));
        var flopPrivateClusters =
            FileCache.OptionalCompute(
                "PrivateCluster3.json",
                () =>
                    EmdCluster.ClusterPrivatePoints(flopPrivatePoints, 10, 50));
        var turnPrivateClusters =
            FileCache.OptionalCompute(
                "PrivateCluster4.json",
                () =>
                    EmdCluster.ClusterPrivatePoints(turnPrivatePoints, 10, 50));
        var riverPrivateClusters =
            FileCache.OptionalCompute(
                "PrivateCluster5.json",
                () =>
                    EmdCluster.ClusterPrivatePoints(riverPrivatePoints, 10, 50));

        // public clusters
        var preflopPublicPoints =
            FileCache.OptionalCompute(
                "PublicPoint0.json",
                () => EmdCluster.MakePublicClusterPoints(preflopPrivateClusters, 0, 10, 10)
            );
        var flopPublicPoints =
            FileCache.OptionalCompute(
                "PublicPoint3.json",
                () => EmdCluster.MakePublicClusterPoints(preflopPrivateClusters, 3, 10, 10)
            );
        var turnPublicPoints =
            FileCache.OptionalCompute(
                "PublicPoint4.json",
                () => EmdCluster.MakePublicClusterPoints(preflopPrivateClusters, 4, 10, 10)
            );
        var riverPublicPoints =
            FileCache.OptionalCompute(
                "PublicPoint5.json",
                () => EmdCluster.MakePublicClusterPoints(preflopPrivateClusters, 5, 10, 10)
            );

        var preflopPublicClusters =
            FileCache.OptionalCompute(
                "PublicCluster0.json",
                () =>
                    EmdCluster.ClusterPublicPoints(preflopPublicPoints, 1, 50));
        var flopPublicClusters =
            FileCache.OptionalCompute(
                "PublicCluster3.json",
                () =>
                    EmdCluster.ClusterPublicPoints(flopPublicPoints, 20, 50));
        var turnPublicClusters =
            FileCache.OptionalCompute(
                "PublicCluster4.json",
                () =>
                    EmdCluster.ClusterPublicPoints(turnPublicPoints, 10, 50));
        var riverPublicClusters =
            FileCache.OptionalCompute(
                "PublicCluster5.json",
                () =>
                    EmdCluster.ClusterPublicPoints(riverPublicPoints, 10, 50));


        return new Infoset(
            preflopPrivateClusters,
            flopPrivateClusters,
            turnPrivateClusters,
            riverPrivateClusters,
            preflopPublicClusters,
            flopPublicClusters,
            turnPublicClusters,
            riverPublicClusters
        );
    }
}