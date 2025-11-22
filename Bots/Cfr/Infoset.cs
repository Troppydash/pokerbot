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
        public int PublicClusterId { get; set; }
        public int HoleClusterId { get; set; }
        public List<Action> Bets { get; set; }

        private string? _string;

        public Entry(int street, int position, int publicClusterId, int holeClusterId, List<Action> bets)
        {
            Street = street;
            Position = position;
            PublicClusterId = publicClusterId;
            HoleClusterId = holeClusterId;
            Bets = bets;

            _string = null;
        }

        public override string ToString()
        {
            if (_string == null)
            {
                _string = $"{Street}/{Position}/{PublicClusterId}/{HoleClusterId}/";
                // list of raises
                foreach (var action in Bets)
                {
                    _string += action.Repr();
                }
            }

            return _string;
        }
    }

    private int[] _holeCluster;
    private int[] _preflopCluster;
    private int[] _flopCluster;
    private int[] _turnCluster;
    private int[] _riverCluster;

    private Dictionary<int, List<EmdCluster>> _holeMap;
    private Dictionary<int, List<EmdCluster>> _preflopMap;
    private Dictionary<int, List<EmdCluster>> _flopMap;
    private Dictionary<int, List<EmdCluster>> _turnMap;
    private Dictionary<int, List<EmdCluster>> _riverMap;


    public Infoset(
        Dictionary<int, List<EmdCluster>> holeCluster,
        Dictionary<int, List<EmdCluster>> preflopCluster,
        Dictionary<int, List<EmdCluster>> flopCluster,
        Dictionary<int, List<EmdCluster>> turnCluster,
        Dictionary<int, List<EmdCluster>> riverCluster
    )
    {
        _holeMap = holeCluster;
        _preflopMap = preflopCluster;
        _flopMap = flopCluster;
        _turnMap = turnCluster;
        _riverMap = riverCluster;

        _holeCluster = new int[Card.MaxHashDeck(2)];
        _preflopCluster = new int[Card.MaxHashDeck(0)];
        _flopCluster = new int[Card.MaxHashDeck(3)];
        _turnCluster = new int[Card.MaxHashDeck(4)];
        _riverCluster = new int[Card.MaxHashDeck(5)];

        // create mappings
        foreach (var entry in holeCluster)
        {
            foreach (var point in entry.Value)
            {
                _holeCluster[Card.HashDeck(point.Hole!)] = entry.Key;
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
        List<Action> bets
    )
    {
        int holeCluster = _holeCluster[Card.HashDeck(hole)];
        int publicCluster = visible.Length switch
        {
            0 => _preflopCluster[Card.HashDeck(visible)],
            3 => _flopCluster[Card.HashDeck(visible)],
            4 => _turnCluster[Card.HashDeck(visible)],
            5 => _riverCluster[Card.HashDeck(visible)],
            6 => _riverCluster[Card.HashDeck(visible)],
            _ => throw new Exception("invalid visible card length")
        };

        return new Entry(street, position, publicCluster, holeCluster, bets);
    }

    public Entry FromGame(Game game)
    {
        var state = game.GetState();
        return Lookup(state.Street, state.Index, state.Hand, state.River, state.History);
    }

    public IEnumerable<(Card[], Card[], Card[])> AllStarters()
    {
        // need to generate all hole/public cluster pairs
        foreach (var sbHole in _holeMap)
        {
            Card[] sbh = sbHole.Value[0].Hole!;

            foreach (var bbHole in _holeMap)
            {
                foreach (var bbHoleEmd in bbHole.Value)
                {
                    // ensure sbh and bbh are different
                    Card[] bbh = bbHoleEmd.Hole!;
                    if (!Helper.Distinct(sbh, bbh)) continue;

                    Card[] combined = sbh.Concat(bbh).ToArray();

                    foreach (var map in (List<Dictionary<int, List<EmdCluster>>>) [_flopMap, _turnMap, _riverMap])
                    {
                        foreach (var emdCluster in map)
                        {
                            // ensure no conflicts
                            foreach (var cluster in emdCluster.Value)
                            {
                                if (!Helper.Distinct(combined, cluster.Visible!)) continue;

                                Card[] remain = Helper.SelectRemain(combined,
                                    5 - cluster.Visible!.Length);

                                yield return (sbh, bbh, cluster.Visible.Concat(remain).ToArray());
                                break;
                            }
                        }
                    }

                    break;
                }
            }
        }
    }

    public IEnumerable<Entry> Forward(int depth, List<Action> validActions)
    {
        // 0/0/0/4
        // 0/1/0/3/C
        foreach (int street in (int[]) [0, 3, 4, 5, 6])
        {
            // bets
            List<List<Action>> actions = [[]];
            for (int count = 0; count <= depth + 1; ++count)
            {
                foreach (var list in actions)
                {
                    foreach (var hole in _holeMap)
                    {
                        // public
                        var choice = street switch
                        {
                            0 => _preflopMap,
                            3 => _flopMap,
                            4 => _turnMap,
                            5 => _riverMap,
                            6 => _riverMap,
                            _ => throw new ArgumentOutOfRangeException()
                        };
                        foreach (var visible in choice)
                        {
                            foreach (int position in (int[]) [0, 1])
                            {
                                yield return new Entry(street, position, visible.Key, hole.Key, list);
                            }
                        }
                    }
                }

                List<List<Action>> newActions = [];
                foreach (var action in actions)
                {
                    foreach (var valid in validActions)
                    {
                        newActions.Add([..action, valid]);
                    }
                }

                actions = newActions;
            }
        }
    }

    public static Infoset FromClusters(bool compute = false)
    {
        Console.WriteLine("Loading Hole Clusters");

        if (compute)
            EmdCluster.SaveHoleClusterPoints("holeCluster.json", 1000, 10);

        var clusters = EmdCluster.LoadHoleClusterPoints("holeCluster.json")!;

        if (compute)
            EmdCluster.ClusterHoleCards("holeClusterGroups.json", clusters, 1000, 20);

        var groups = EmdCluster.LoadClusterHoleCards("holeClusterGroups.json")!;

        Console.WriteLine("Loading Public Clusters");

        if (compute)
            EmdCluster.SavePublicClusterPoints("publicCluster.json", groups, 5, 5, 5);

        var publicPoints = EmdCluster.LoadPublicClusterPoints("publicCluster.json");

        if (compute)
        {
            EmdCluster.ClusterPublicCards("publicClusterGroup0.json", publicPoints, 0, 10, 1);
            EmdCluster.ClusterPublicCards("publicClusterGroup3.json", publicPoints, 3, 10, 20);
            EmdCluster.ClusterPublicCards("publicClusterGroup4.json", publicPoints, 4, 10, 40);
            EmdCluster.ClusterPublicCards("publicClusterGroup5.json", publicPoints, 5, 10, 50);
        }

        var group0 = EmdCluster.LoadClusterPublicCards("publicClusterGroup0.json", 0)!;
        var group3 = EmdCluster.LoadClusterPublicCards("publicClusterGroup3.json", 3)!;
        var group4 = EmdCluster.LoadClusterPublicCards("publicClusterGroup4.json", 4)!;
        var group5 = EmdCluster.LoadClusterPublicCards("publicClusterGroup5.json", 5)!;

        return new Infoset(
            groups,
            group0,
            group3,
            group4,
            group5
        );
    }
}