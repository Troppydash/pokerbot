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
    /// <param name="state">Game state</param>
    /// <param name="actions">Possible actions</param>
    /// <returns>Action</returns>
    public Action Play(Game.State state, List<Action> actions);
}
