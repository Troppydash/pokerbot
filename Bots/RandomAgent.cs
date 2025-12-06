using PokerBot.Attributes;

namespace PokerBot.Bots;

/// <summary>
/// Random agent that never all-ins or folds
/// </summary>
[Stable]
public class AllRandomAgent : IAgent
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
[Stable]
public class StableRandomAgent : IAgent
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
[Stable]
public class YoloAgent : IAgent
{
    public void Reset()
    {
    }

    public Action Play(Game.State state, List<Action> actions)
    {
        return actions.Last();
    }
}