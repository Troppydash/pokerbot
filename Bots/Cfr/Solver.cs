using System.Text.Json;

namespace PokerBot.Bots.Cfr;

/// <summary>
/// CFR solver
/// </summary>
public class Solver
{
    public const int Players = 2;

    class Node
    {
        public const int Self = 0;
        public const int Opponent = 1;

        public Infoset.Entry Entry { get; }
        public int Actions { get; }
        public double Util { get; set; }
        public double[] ReachProb { get; set; }
        public double[] RegretSum { get; set; }
        public double[] Strategy { get; set; }
        public double[] StrategySum { get; set; }

        // TODO: eps modelling

        public Node(Infoset.Entry entry, int actions)
        {
            Entry = entry;
            Actions = actions;
            Util = 0;
            ReachProb = new double[Players];
            RegretSum = new double[actions];
            Strategy = new double[actions];
            StrategySum = new double[actions];
        }

        public double[] GetStrategy()
        {
            double total = 0.0;
            for (int i = 0; i < Actions; ++i)
            {
                Strategy[i] = double.Max(0, RegretSum[i]);
                total += Strategy[i];
            }

            if (total == 0.0)
            {
                // uniform strategy
                for (int i = 0; i < Actions; ++i)
                {
                    Strategy[i] = 1.0 / Actions;
                }
            }
            else
            {
                for (int i = 0; i < Actions; ++i)
                {
                    Strategy[i] /= total;
                }
            }

            // update sum
            for (int i = 0; i < Actions; ++i)
            {
                StrategySum[i] += ReachProb[Self] * Strategy[i];
            }

            return Strategy;
        }

        public double[] AverageStrategy()
        {
            double[] average = new double[Actions];

            double total = 0.0;
            for (int i = 0; i < Actions; ++i)
            {
                total += StrategySum[i];
            }

            if (total == 0.0)
            {
                // uniform strategy
                for (int i = 0; i < Actions; ++i)
                {
                    average[i] = 1.0 / Actions;
                }
            }
            else
            {
                for (int i = 0; i < Actions; ++i)
                {
                    average[i] = StrategySum[i] / total;
                }
            }

            return average;
        }
    }

    public class Result
    {
        public Dictionary<string, double[]> Inference { get; }

        private Infoset _infoset;
        private int _depth;

        public Result(Dictionary<string, double[]> inference, Infoset infoset, int depth)
        {
            Inference = inference;
            _infoset = infoset;
            _depth = depth;
        }

        public void Save(string filename)
        {
            string output = JsonSerializer.Serialize(this);
            File.WriteAllText(filename, output);
        }

        public int Missed()
        {
            int missed = 0;
            foreach (var entry in _infoset.Forward(_depth, ValidActions))
            {
                if (!Inference.ContainsKey(entry.ToString()))
                {
                    missed += 1;
                }
            }

            return missed;
        }

        public void Display()
        {
            foreach (var entry in _infoset.Forward(_depth, ValidActions))
            {
                string key = entry.ToString();
                if (Inference.ContainsKey(key))
                {
                    Console.Write($"{key}: ");
                    foreach (var d in Inference[key])
                    {
                        Console.Write($"{d}, ");
                    }

                    Console.WriteLine();
                }
            }
        }
    }


    private Infoset _infoset;
    private Dictionary<string, Node> _states;
    private Dictionary<string, Game> _suspense;
    private int _depth;

    public Solver(Infoset infoset, int depth)
    {
        _depth = depth;
        _infoset = infoset;
        _states = new Dictionary<string, Node>();
        _suspense = new Dictionary<string, Game>();
    }

    #region Topo sort

    // private void ToposortDfs(Game game, List<Node> order, HashSet<string> visited)
    // {
    //     Infoset.Entry entry = _infoset.FromGame(game);
    //
    //     // ignore if visited
    //     if (visited.Contains(entry.ToString()))
    //         return;
    //
    //     int actionsCount = 0;
    //     // if not terminal
    //     if (game.Utility() == null)
    //     {
    //         // visit children
    //         List<Action> actions = game.GetLimitedActions(_depth);
    //         actionsCount = actions.Count;
    //
    //         foreach (var action in actions)
    //         {
    //             Game oldGame = game.Clone();
    //
    //             game.Play(action);
    //             ToposortDfs(game, order, visited);
    //
    //             game = oldGame;
    //         }
    //     }
    //
    //     visited.Add(entry.ToString());
    //
    //     // push to stack
    //     if (!_states.ContainsKey(entry.ToString()))
    //     {
    //         Node node = new Node(entry, actionsCount);
    //         order.Add(node);
    //         _states.Add(entry.ToString(), node);
    //     }
    // }
    //
    // private void CreateTopologicalSort()
    // {
    //     _states = new Dictionary<string, Node>();
    //     _nodeOrder = new Dictionary<string, int>();
    //     List<Node> order = [];
    //
    //     // need to try every starting game combination
    //     int k = 0;
    //     int total = _infoset.AllStarters().ToList().Count;
    //     Console.WriteLine(total);
    //     foreach (var (sb, bb, visible) in _infoset.AllStarters())
    //     {
    //         k += 1;
    //         Console.WriteLine($"{(double) k / total * 100}%");
    //
    //         Game game = new Game();
    //         game.Seed(sb, bb, visible);
    //
    //         HashSet<string> visited = new HashSet<string>();
    //         ToposortDfs(game, order, visited);
    //         Console.WriteLine();
    //     }
    //
    //     order.Reverse();
    //
    //     // generate state mappings
    //     for (int i = 0; i < order.Count; ++i)
    //     {
    //         _nodeOrder[order[i].Entry.ToString()] = i;
    //     }
    // }

    #endregion

    #region Action Mapping

    private Action AbstractToReal(Action @abstract, List<Action> actions)
    {
        // find the closest
        if (@abstract.IsFold())
        {
            return actions[0];
        }

        if (@abstract.IsCheck())
        {
            return actions[1];
        }

        if (@abstract.IsAllin())
        {
            return actions.Last();
        }

        Action best = actions[2];
        double diff = Double.MaxValue;
        for (int i = 2; i < actions.Count - 1; ++i)
        {
            var action = actions[i];
            double newDiff = double.Abs(action.Proportion() - @abstract.Proportion());
            if (newDiff < diff)
            {
                best = action;
                diff = newDiff;
            }
        }

        return best;
    }

    private Action RealToAbstract(Action @real, List<Action> actions)
    {
        // find the closest
        if (real.IsFold())
        {
            return actions[0];
        }

        if (real.IsCheck())
        {
            return actions[1];
        }

        if (real.IsAllin())
        {
            return actions.Last();
        }

        Action best = actions[2];
        double diff = Double.MaxValue;
        for (int i = 2; i < actions.Count - 1; ++i)
        {
            var action = actions[i];
            double newDiff = double.Abs(action.Proportion() - real.Proportion());
            if (newDiff < diff)
            {
                best = action;
                diff = newDiff;
            }
        }

        return best;
    }

    private static List<Action> ValidActions =
    [
        Action.Fold(10),
        Action.Raise(10, 0, Action.CheckFlag),
        Action.Raise(10, 3, 0),
        Action.Raise(10, 10, 0),
        // Action.Raise(10, 20, 0),
        Action.Raise(10, 100, Action.AllinFlag),
    ];

    private static List<Action> LimitActions =
    [
        Action.Fold(10),
        Action.Raise(10, 0, Action.CheckFlag)
    ];

    private List<Action> AbstractActions(Game game)
    {
        if (game.GetState().History.Count >= _depth)
        {
            return LimitActions;
        }

        return ValidActions;
    }

    #endregion

    public Result ToResult()
    {
        Dictionary<string, double[]> inference = new Dictionary<string, double[]>();
        foreach (var entry in _states)
        {
            double[] strategy = entry.Value.AverageStrategy();
            if (double.Abs(strategy[0] - 1.0 / strategy.Length) > 0.001)
                inference.Add(entry.Key, entry.Value.AverageStrategy());
        }

        return new Result(inference, _infoset, _depth);
    }

    public void Simulate(int iters, int seed)
    {
        Console.WriteLine("Generating States");
        _states = new Dictionary<string, Node>();

        List<Infoset.Entry> entries = _infoset.Forward(_depth, ValidActions).ToList();
        List<Infoset.Entry> reversedEntries = [..entries];
        reversedEntries.Reverse();

        foreach (var entry in entries)
        {
            int actions = ValidActions.Count;
            if (entry.Bets.Count == _depth)
            {
                actions = LimitActions.Count;
            }

            _states[entry.ToString()] = new Node(entry, actions);
        }

        Random rng = new Random(seed);
        for (int it = 0; it < iters; ++it)
        {
            if (it % 50 == 0)
                Console.WriteLine($"Iteration {it}");
            Game start = new Game();
            start.Shuffle(rng.Next());

            // forward pass
            {
                _suspense = new Dictionary<string, Game>();
                string startKey = _infoset.FromGame(start).ToString();
                _suspense[startKey] = start;
                _states[startKey].ReachProb = [1.0, 1.0];

                // direct enumeration
                foreach (var entry in entries)
                {
                    string key = entry.ToString();

                    // ignore if not reached
                    if (!_suspense.ContainsKey(key))
                        continue;

                    Game game = _suspense[key];
                    Node node = _states[key];

                    // ignore finished game
                    if (game.Utility() != null)
                        continue;

                    // visit children
                    double[] strategy = node.GetStrategy();
                    List<Action> realActions = game.GetLimitedActions(_depth);
                    List<Action> abstractActions = AbstractActions(game);
                    for (int i = 0; i < abstractActions.Count; ++i)
                    {
                        Action realAction = AbstractToReal(abstractActions[i], realActions);

                        Game newGame = game.Clone();
                        newGame.Play(realAction, abstractActions[i]);

                        // update reach prob
                        string newKey = _infoset.FromGame(newGame).ToString();
                        if (game.GetTurn() == newGame.GetTurn())
                        {
                            _states[newKey].ReachProb[Node.Self] += strategy[i] * node.ReachProb[Node.Self];
                            _states[newKey].ReachProb[Node.Opponent] += node.ReachProb[Node.Opponent];
                        }
                        else
                        {
                            _states[newKey].ReachProb[Node.Self] += node.ReachProb[Node.Opponent];
                            _states[newKey].ReachProb[Node.Opponent] += strategy[i] * node.ReachProb[Node.Self];
                        }

                        _suspense[newKey] = newGame;
                    }
                }
            }

            // reverse pass
            {
                foreach (var entry in reversedEntries)
                {
                    string key = entry.ToString();

                    // ignore if not reached
                    if (!_suspense.ContainsKey(key))
                        continue;

                    Game game = _suspense[key];
                    Node node = _states[key];

                    // if finished game
                    if (game.Utility() != null)
                    {
                        node.Util = game.Utility()[game.GetTurn()];
                        continue;
                    }

                    // visit children
                    double[] strategy = node.Strategy;
                    double[] regrets = new double[strategy.Length];
                    node.Util = 0.0;

                    // compute util
                    List<Action> realActions = game.GetLimitedActions(_depth);
                    List<Action> abstractActions = AbstractActions(game);
                    for (int i = 0; i < abstractActions.Count; ++i)
                    {
                        Action realAction = AbstractToReal(abstractActions[i], realActions);

                        Game newGame = game.Clone();
                        newGame.Play(realAction, abstractActions[i]);

                        string newKey = _infoset.FromGame(newGame).ToString();
                        double childUtil;
                        if (game.GetTurn() == newGame.GetTurn())
                        {
                            childUtil = strategy[i] * _states[newKey].Util;
                        }
                        else
                        {
                            childUtil = -strategy[i] * _states[newKey].Util;
                        }

                        node.Util += childUtil;
                        regrets[i] = childUtil;
                    }

                    // regret matching
                    for (int i = 0; i < abstractActions.Count; ++i)
                    {
                        regrets[i] -= node.Util;
                        node.RegretSum[i] += node.ReachProb[Node.Opponent] * regrets[i];
                    }

                    node.ReachProb = [0, 0];
                }
            }

            if (it % 1000 == 0)
                ToResult().Display();

            if (it % 100 == 0)
            {
                Console.WriteLine($"Missed {ToResult().Missed()}");
                ToResult().Save("result.json");
            }
        }
    }
}