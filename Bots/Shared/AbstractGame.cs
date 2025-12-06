namespace PokerBot.Bots.Cfr;

public class AbstractGame : Game
{
    public const int Depth = 100;

    private List<Action> _abstractHistory;

    public AbstractGame() : base()
    {
        _abstractHistory = [];
    }

    public AbstractGame(Card[] hands, int turn, int[] money, int pot, int raise, int[] raised, int lastIncrement,
        bool[] @checked, int riverCards, List<Action> history, List<Action> streetHistory,
        List<Action> abstractHistory) : base(hands, turn, money, pot, raise, raised, lastIncrement, @checked,
        riverCards, history, streetHistory)
    {
        _abstractHistory = abstractHistory;
    }

    public static AbstractGame FromState(State state)
    {
        List<Action> abstractHistory = [];
        foreach (var action in state.History.TakeLast(Depth))
        {
            abstractHistory.Add(RealToAbstract(action, ValidActions));
        }

        Card[] hand = new Card[Card.NumberRanks * Card.NumberSuits];
        state.Hand.CopyTo(hand, state.Index == 0 ? Game.SbHandOffset : Game.BbHandOffset);

        // fake other hand
        Card[] other = Helper.SelectRemain(
            Helper.RemoveCards(Card.AllCards(), state.Hand.Concat(state.River).ToArray()),
            2
        );
        other.CopyTo(hand, state.Index == 0 ? Game.BbHandOffset : Game.SbHandOffset);

        state.River.CopyTo(hand, Game.RiverHandOffset);
        return new AbstractGame(
            hand, state.Index, state.Money, state.Pot, state.Raise, state.Raised, 0, state.Checked,
            state.River.Length, state.History, state.History, abstractHistory
        );
    }

    public override AbstractGame Clone()
    {
        return new AbstractGame(
            (Card[])_hands.Clone(),
            _turn,
            (int[])_money.Clone(),
            _pot,
            _raise,
            (int[])_raised.Clone(),
            _lastIncrement,
            (bool[])_checked.Clone(),
            _riverCards,
            [.._history],
            [.._streetHistory],
            [.._abstractHistory]
        );
    }

    public static Action AbstractToReal(Action @abstract, List<Action> actions)
    {
        // find the closest
        if (@abstract.IsFold())
        {
            return actions[0];
        }

        if (@abstract.IsCheck())
        {
            return actions[1];
        }

        if (@abstract.IsAllin())
        {
            return actions.Last();
        }

        Action best = actions[1];
        double diff = Double.MaxValue;
        for (int i = 2; i < actions.Count; ++i)
        {
            var action = actions[i];
            double newDiff = double.Abs(action.Proportion() - @abstract.Proportion());
            if (newDiff < diff)
            {
                best = action;
                diff = newDiff;
            }
        }

        return best;
    }

    public static Action RealToAbstract(Action real, List<Action> actions)
    {
        // find the closest
        if (real.IsFold())
        {
            return actions[0];
        }

        if (real.IsCheck())
        {
            return actions[1];
        }

        if (real.IsAllin())
        {
            return actions.Last();
        }

        Action best = actions[1];
        double diff = Double.MaxValue;
        for (int i = 2; i < actions.Count; ++i)
        {
            var action = actions[i];
            double newDiff = double.Abs(action.Proportion() - real.Proportion());
            if (newDiff < diff)
            {
                best = action;
                diff = newDiff;
            }
        }

        return best;
    }

    public static List<Action> ValidActions =
    [
        Action.Fold(10),
        Action.Raise(10, 10, Action.CheckFlag),
        Action.Raise(10, 1, 0),
        Action.Raise(10, 3, 0),
        Action.Raise(10, 6, 0),
        Action.Raise(10, 10, 0),
        Action.Raise(10, 20, 0),
        Action.Raise(10, 50, 0),
        Action.Raise(10, 100, Action.AllinFlag),
    ];

    public static List<Action> LimitActions =
    [
        Action.Fold(10),
        Action.Raise(10, 10, Action.CheckFlag)
    ];

    public List<Action> AbstractActions()
    {
        if (_streetHistory.Count >= Depth)
        {
            return LimitActions;
        }

        return ValidActions;
    }

    public override State GetState()
    {
        State state = base.GetState();
        return new State(
            state.Index,
            state.Street,
            state.Raise,
            state.Raised,
            state.Checked,
            state.Money,
            state.Pot,
            state.LastIncrement,
            state.River,
            state.Hand,
            _abstractHistory);
    }

    public override void Play(Action abstractAction)
    {
        // convert to real action
        var action = AbstractToReal(abstractAction, base.GetActions());
        base.Play(action);

        // update abstract history
        if (_streetHistory.Count > 0)
        {
            _abstractHistory.Add(abstractAction);
        }
        else
        {
            _abstractHistory.Clear();
        }
    }
}