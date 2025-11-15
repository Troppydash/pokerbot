namespace PokerBot;

/// <summary>
/// Agent bot interface
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Called on game start
    /// </summary>
    public void Reset();
    
    /// <summary>
    /// Think function, takes game state, return action
    /// </summary>
    /// <param name="game">Game state</param>
    /// <returns>Action</returns>
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