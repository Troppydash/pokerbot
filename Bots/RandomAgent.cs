namespace PokerBot.Bots;

/// <summary>
/// Random agent that never all-ins or folds
/// </summary>
public class RandomAgent : IAgent
{
    public void Reset()
    {
    }

    public Action Play(Game.State state, List<Action> actions)
    {
        return actions[Random.Shared.Next(1, actions.Count - 1)];
    }
}

/// <summary>
/// Random agent that raises by a max amount
/// </summary>
public class RandomAgent2 : IAgent
{
    public void Reset()
    {
    }

    public Action Play(Game.State state, List<Action> actions)
    {
        return actions[Random.Shared.Next(1, int.Min(actions.Count, 4))];
    }
}


/// <summary>
/// Yolo
/// </summary>
public class RandomAgentYolo : IAgent
{
    public void Reset()
    {
    }

    public Action Play(Game.State state, List<Action> actions)
    {
        return actions.Last();
    }
}

