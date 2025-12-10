namespace PokerBot.Bots.ExpectedMinimax;

public class Expectiminimax : IAgent
{
    private bool _verbose;
    private bool _total;
    private double _lambda;

    public Expectiminimax(bool total, double lambda = 0.0, bool verbose = false)
    {
        _verbose = verbose;
        _total = total;
        _lambda = lambda;
    }

    public string Name()
    {
        if (_total)
        {
            return "Expectiminimax_Total";
        }

        return "Expectiminimax_Abstract";
    }

    public void Reset()
    {
    }

    public Action Play(Game.State state, List<Action> actions)
    {
        Engine engine = new Engine(_lambda);
        Action result = _total ? engine.SolveTotal(state, actions) : engine.Solve(state, actions);

        if (_verbose)
        {
            engine.DisplayStats();
            Console.WriteLine();
        }

        return result;
    }
}