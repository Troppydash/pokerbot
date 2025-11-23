namespace PokerBot.Bots.Cfr;

public class CfrAgent : IAgent
{
    private Infoset _infoset;
    private Solver.Result _result;

    private int _hits;
    private int _plays;

    public CfrAgent(Infoset infoset, Solver.Result result)
    {
        _infoset = infoset;
        _result = result;
        _hits = 0;
        _plays = 0;
    }

    public void Reset()
    {
        Console.WriteLine($"Hits {(double)_hits / _plays * 100}%");
    }

    private T WeightedRandom<T>(Random random, List<T> items, List<double> weights)
    {
        double rng = random.NextDouble();
        double total = 0.0;
        for (int i = 0; i < items.Count; ++i)
        {
            total += weights[i];
            if (rng <= total)
            {
                return items[i];
            }
        }

        return items.Last();
    }


    public Action Play(Game.State state, List<Action> realActions)
    {
        _plays += 1;
        AbstractGame game = AbstractGame.FromState(state);

        // lookup state
        string key = _infoset.FromGame(game).ToString();

        List<Action> actions = game.AbstractActions();

        // select strategy action
        Action action;
        if (_result.Inference.TryGetValue(key, out var value))
        {
            _hits += 1;
            List<double> strategy = value.ToList();
            action = WeightedRandom(Random.Shared, actions, strategy);
        }
        else
        {
            action = actions[Random.Shared.Next(actions.Count)];
        }
        

        // convert action to real action
        return AbstractGame.AbstractToReal(action, realActions);
    }
}