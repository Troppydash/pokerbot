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

        public Result(Dictionary<string, double[]> inference, Infoset infoset)
        {
            Inference = inference;
            _infoset = infoset;
        }

        public void Save(string filename)
        {
            string output = JsonSerializer.Serialize(this);
            File.WriteAllText(filename, output);
        }

        public int Missed()
        {
            int missed = 0;
            foreach (var entry in _infoset.Forward(AbstractGame.Depth, AbstractGame.ValidActions))
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
            foreach (var entry in _infoset.Forward(AbstractGame.Depth, AbstractGame.ValidActions))
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
    private Dictionary<string, AbstractGame> _suspense;

    public Solver(Infoset infoset)
    {
        _infoset = infoset;
        _states = new Dictionary<string, Node>();
        _suspense = new Dictionary<string, AbstractGame>();
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


    public Result ToResult()
    {
        Dictionary<string, double[]> inference = new Dictionary<string, double[]>();
        foreach (var entry in _states)
        {
            double[] strategy = entry.Value.AverageStrategy();
            if (double.Abs(strategy[0] - 1.0 / strategy.Length) > 0.001)
                inference.Add(entry.Key, entry.Value.AverageStrategy());
        }

        return new Result(inference, _infoset);
    }

    public void Simulate(int iters, int seed)
    {
        Console.WriteLine("Generating States");
        _states = new Dictionary<string, Node>();

        List<Infoset.Entry> entries = _infoset.Forward(AbstractGame.Depth, AbstractGame.ValidActions).ToList();
        List<Infoset.Entry> reversedEntries = [..entries];
        reversedEntries.Reverse();

        foreach (var entry in entries)
        {
            int actions = AbstractGame.ValidActions.Count;
            if (entry.Bets.Count == AbstractGame.Depth)
            {
                actions = AbstractGame.LimitActions.Count;
            }

            _states[entry.ToString()] = new Node(entry, actions);
        }

        Random rng = new Random(seed);
        for (int it = 0; it < iters; ++it)
        {
            if (it % 50 == 0)
                Console.WriteLine($"Iteration {it}");
            AbstractGame start = new AbstractGame();
            start.Shuffle(rng.Next());

            // forward pass
            {
                _suspense = new Dictionary<string, AbstractGame>();
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

                    AbstractGame game = _suspense[key];
                    Node node = _states[key];

                    // ignore finished game
                    if (game.Utility() != null)
                        continue;

                    // visit children
                    double[] strategy = node.GetStrategy();
                    // List<Action> realActions = game.GetLimitedActions(_depth);
                    List<Action> abstractActions = game.AbstractActions();
                    for (int i = 0; i < abstractActions.Count; ++i)
                    {
                        // Action realAction = AbstractToReal(abstractActions[i], realActions);

                        AbstractGame newGame = game.Clone();
                        newGame.Play(abstractActions[i]);

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

                    AbstractGame game = _suspense[key];
                    Node node = _states[key];

                    // if finished game
                    if (game.Utility() != null)
                    {
                        node.Util = game.Utility()![game.GetTurn()];
                        continue;
                    }

                    // visit children
                    double[] strategy = node.Strategy;
                    double[] regrets = new double[strategy.Length];
                    node.Util = 0.0;

                    // compute util
                    List<Action> abstractActions = game.AbstractActions();
                    for (int i = 0; i < abstractActions.Count; ++i)
                    {
                        Game newGame = game.Clone();
                        newGame.Play(abstractActions[i]);

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
                ToResult().Save("result2.json");
            }
        }
    }
}