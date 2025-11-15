namespace PokerBot;

public class Arena
{
    private List<IAgent> _agents;

    public Arena(List<IAgent> agents)
    {
        _agents = agents;
    }

    public int[] Simulate(bool verbose = true)
    {
        foreach (var agent in _agents)
        {
            agent.Reset();
        }

        Game game = new Game();
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