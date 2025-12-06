namespace PokerBot.Bots.ExpectedMinimax;

public class ManualAgent : IAgent
{
    public void Reset()
    {
        
    }

    public Action Play(Game.State state, List<Action> actions)
    {
        Engine engine = new Engine();
        return engine.Solve(state, actions);
    }
}