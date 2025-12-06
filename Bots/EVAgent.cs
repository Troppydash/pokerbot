using PokerBot.Attributes;

namespace PokerBot.Bots;

/// <summary>
/// EV strategy agent
/// </summary>
[Stable]
public class EvAgent : IAgent
{
    private bool _verbose;
    private int _equityIterations;
    private Random _rng;

    public EvAgent(int equityIterations = 1000, int seed = 42, bool verbose = false)
    {
        this._verbose = verbose;
        _equityIterations = equityIterations;
        _rng = new Random(seed);
    }

    public void Reset()
    {
    }

    public Action Play(Game.State state, List<Action> actions)
    {
        List<Card> cards = Card.AllCards().ToList();

        // remove own cards
        foreach (var card in state.Hand)
        {
            cards.Remove(card);
        }

        // remove river cards
        foreach (var card in state.River)
        {
            cards.Remove(card);
        }

        // simulate rolls
        int n = _equityIterations;
        double wins = 0;
        Card[] rolls = cards.GetRange(0, cards.Count).ToArray();

        for (int i = 0; i < n; ++i)
        {
            _rng.Shuffle(rolls);

            Card[] pOther = rolls.Take(2).ToArray();
            Card[] fullRiver = rolls.Skip(2).Take(5 - state.River.Length).ToArray();

            int winner = FastResolver.CompareHands(
                state.River.Concat(fullRiver).ToArray(),
                state.Hand,
                pOther
            );

            if (winner == HandResolver.Player0)
            {
                wins += 1.0;
            }
            else if (winner == HandResolver.Equal)
            {
                wins += 0.5;
            }
        }

        double winProb = wins / n;

        if (_verbose)
        {
            Console.WriteLine("==== Agent ====");
            Console.WriteLine($"    Win prob {winProb}");
        }

        double ev = winProb * state.Pot + (1 - winProb) * -(state.Raise - state.Raised[state.Index]);
        if (_verbose)
        {
            Console.WriteLine($"    EV {ev}");
        }

        if (ev >= 20)
        {
            // try raise if ev is high
            if (winProb * state.Pot + (1 - winProb) * -actions[2].Amount >= 10)
                return actions[2];
        }
        
        if (ev >= 10)
        {
            // match raise if positive ev
            return actions[1];
        }

        // if can check, check
        if (state.Raise == 0)
        {
            foreach (var action in actions)
            {
                if (!action.IsFold() && action.Amount == 0)
                {
                    return action;
                }
            }

            throw new Exception("no check?");
        }

        // bluff with chance
        double chance = 0.2;
        if (ev >= -20 && _rng.NextDouble() < chance)
        {
            // 2nd is always check
            return actions[1];
        }

        // otherwise fold
        return actions[0];
    }
}