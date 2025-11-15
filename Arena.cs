namespace PokerBot;

/// <summary>
/// Agent simulation arena
/// </summary>
public class Arena
{
    /// <summary>
    /// 2-list agents
    /// </summary>
    private List<IAgent> _agents;

    public Arena(List<IAgent> agents)
    {
        _agents = agents;
    }

    /// <summary>
    /// Simulate one game of poker and return utility result
    /// </summary>
    /// <param name="seed">Shuffle seed</param>
    /// <param name="verbose">Print game states and actions</param>
    /// <returns></returns>
    public int[] Simulate(int seed, bool verbose = true)
    {
        foreach (var agent in _agents)
        {
            agent.Reset();
        }

        Game game = new Game();
        game.Shuffle(seed);
        while (game.Utility() == null)
        {
            if (verbose)
            {
                game.Display();
            }
            
            Action action = _agents[game.GetTurn()].Play(game);
            game.Play(action);

            if (verbose)
            {
                Console.WriteLine($"\nPlayed: {action}\n");
            }
        }

        if (verbose)
        {
            game.Display();
        }
        return game.Utility()!;
    }
}