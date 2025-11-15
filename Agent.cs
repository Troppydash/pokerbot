namespace PokerBot;

public interface IAgent
{
    public void Reset();
    public Action Play(Game game);
}

public class RandomAgent : IAgent
{
    public void Reset()
    {
    }

    public Action Play(Game game)
    {
        var actions = game.GetActions();
        return actions[Random.Shared.Next(actions.Count)];
    }
}