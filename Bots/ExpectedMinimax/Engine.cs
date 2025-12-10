using PokerBot.Bots.Shared;

namespace PokerBot.Bots.ExpectedMinimax;
//
// class Transposition
// {
//     public class Entry
//     {
//         public int Hash { get; }
//         public int Depth { get; }
//         public int Score { get; }
//         public Action Best { get; }
//         public int Flag { get; }
//
//         public (int, bool, Action) Get()
//         {
//             // TODO:
//         }
//
//         public void Set()
//         {
//             
//         }
//     }
//
//     private Entry[] _table;
//
//     public Transposition(int size)
//     {
//         _table = new Entry[size];
//     }
//
//     public Entry Probe(int hash)
//     {
//         return _table[hash % _table.Length];
//     }
// }

public class Engine
{
    private int _statsNodes;
    private int _statsLeafNodes;
    private int _statsCutoffNodes;
    private int _statsPrunedNodes;

    private double _lambda;


    public Engine(double lambda = 0.0)
    {
        _lambda = lambda;
        Reset();
    }

    public void Reset()
    {
        _statsNodes = 0;
        _statsLeafNodes = 0;
        _statsCutoffNodes = 0;
        _statsPrunedNodes = 0;
    }

    public void DisplayStats()
    {
        Console.WriteLine($"[Engine Stats]");
        Console.WriteLine($"explored {_statsNodes}, leaf {(double)_statsLeafNodes / _statsNodes * 100:0.00}%");
        Console.WriteLine($"depth cutoff {(double)_statsCutoffNodes / _statsNodes * 100:0.00}%");
        Console.WriteLine($"pruned {(double)_statsPrunedNodes / _statsNodes * 100:0.00}%");
    }

    // TODO: try expectedminimax using expectation, prune high equity nodes

    public Action Solve(Game.State state, List<Action> realActions)
    {
        Reset();
        AbstractGame abstractGame = AbstractGame.FromState(state);

        int player = abstractGame.GetTurn();
        double equity = Equity.ComputeEquity(abstractGame.GetState().Hand, abstractGame.GetState().River, 50);

        List<Action> actions = [];
        PreChanceNegamax(abstractGame, actions, player, equity, -1e9, 1e9, 100);
        var action = actions[0];
        return AbstractGame.AbstractToReal(action, realActions);
    }

    public Action SolveTotal(Game.State state, List<Action> realActions)
    {
        Reset();
        Game game = Game.FromState(state);

        int player = game.GetTurn();
        double equity = Equity.ComputeEquity(state.Hand, state.River, 50);

        List<Action> actions = [];
        TotalNegamax(game, actions, player, equity, -1e9, 1e9, 6);
        return actions[0];
    }

    private double UtilityFunc(double x)
    {
        x /= Game.BbAmount;
        double lambda = _lambda;
        return x - lambda / 2 * x * x;
    }

    /// <summary>
    /// ExpectedMinimax using prechance algorithm
    /// </summary>
    /// <param name="game"></param>
    /// <param name="actions"></param>
    /// <param name="depth"></param>
    /// <returns></returns>
    private double PreChanceNegamax(AbstractGame game, List<Action> actions, int player, double equity, double alpha,
        double beta, int depth)
    {
        _statsNodes += 1;

        if (game.IsOver() || depth == 0)
        {
            _statsLeafNodes += 1;

            // Monte Carlo ev weighted return
            // Hole cards from *game*, public cards from *visible*
            // Precomputed in *equity*

            var state = game.GetState();
            double currentEquity = game.GetTurn() == player ? equity : 1.0 - equity;

            // if no more play
            if (game.IsOver())
            {
                // if folded
                if (state.History.Count > 0 && state.History.Last().IsFold())
                {
                    return UtilityFunc(state.Money[state.Index] - Game.AllInAmount);
                }

                double winnings = UtilityFunc(state.Pot + state.Money[state.Index] - Game.AllInAmount);
                double ev = currentEquity * winnings + (1 - currentEquity) * -winnings;
                return ev;
            }

            _statsCutoffNodes += 1;


            // first try check
            double checkWinnings = UtilityFunc(state.Pot + state.Money[state.Index] - Game.AllInAmount);
            double checkEv = currentEquity * checkWinnings + (1 - currentEquity) * -checkWinnings;

            // then try fold
            double foldEv = UtilityFunc(state.Money[state.Index] - Game.AllInAmount);
            double evHorizon = double.Max(checkEv, foldEv);

            return evHorizon;
        }

        // action node
        var moves = game.AbstractActions();
        double best = double.MinValue;
        foreach (var action in moves)
        {
            AbstractGame newGame = game.Clone();
            newGame.Play(action);

            List<Action> child = [];
            double value;
            if (newGame.GetTurn() == game.GetTurn())
            {
                value = PreChanceNegamax(newGame, child, player, equity, alpha, beta, depth - 1);
            }
            else
            {
                value = -PreChanceNegamax(newGame, child, player, equity, -beta, -alpha, depth - 1);
            }

            if (value > best)
            {
                best = value;
            }

            if (value >= beta)
            {
                _statsPrunedNodes += 1;
                break;
            }

            if (value > alpha)
            {
                alpha = value;
                actions.Clear();
                actions.Add(action);
                actions.AddRange(child);
            }
        }


        return best;
    }

    private double Heuristics(Game game, double equity)
    {
        var state = game.GetState();
        double checkWinnings = UtilityFunc(state.Pot + state.Money[state.Index] - Game.AllInAmount);
        double checkEV = equity * checkWinnings + (1 - equity) * -checkWinnings;

        // double raise = game.GetActions()[2].Amount;
        // double raiseEV = equity * (checkWinnings + raise) + (1 - equity) * -(checkWinnings + raise);
        // double trueEv = checkEV * 0.8 + raiseEV * 0.2;
        
        double foldEv = UtilityFunc(state.Money[state.Index] - Game.AllInAmount);
        double evHorizon = double.Max(checkEV, foldEv);
        return evHorizon;
    }

    private double TotalNegamax(Game game, List<Action> actions, int player, double equity, double alpha,
        double beta, int depth)
    {
        _statsNodes += 1;

        if (game.IsOver() || depth == 0)
        {
            _statsLeafNodes += 1;

            // Monte Carlo ev weighted return
            // Hole cards from *game*, public cards from *visible*
            // Precomputed in *equity*

            var state = game.GetState();
            double currentEquity = game.GetTurn() == player ? equity : 1.0 - equity;

            // if no more play
            if (game.IsOver())
            {
                // if folded
                if (state.History.Count > 0 && state.History.Last().IsFold())
                {
                    return UtilityFunc(state.Money[state.Index] - Game.AllInAmount);
                }

                double winnings = UtilityFunc(state.Pot + state.Money[state.Index] - Game.AllInAmount);
                double ev = equity * winnings + (1 - currentEquity) * -winnings;
                return ev;
            }

            _statsCutoffNodes += 1;

            return Heuristics(game, currentEquity);

            // first try check
            double checkWinnings = UtilityFunc(state.Pot + state.Money[state.Index] - Game.AllInAmount);
            double checkEv = currentEquity * checkWinnings + (1 - currentEquity) * -checkWinnings;

            // then try fold
            double foldEv = UtilityFunc(state.Money[state.Index] - Game.AllInAmount);
            double evHorizon = double.Max(checkEv, foldEv);

            return evHorizon;
        }

        // int hash = game.GetState().GetHashCode();
        // if (_cache.ContainsKey(hash))
        // {
        //     return _cache.
        // }

        // action node
        var moves = game.GetActions();

        // List<Action> exploredMoves = [];
        // exploredMoves.AddRange(moves.Take(10));
        //
        // if (moves.Count > 10)
        //     exploredMoves.Add(moves.Last());
        // limit to only 10

        if (moves[1].Amount == 0)
        {
            moves.RemoveAt(0);
        }

        double best = double.MinValue;
        foreach (var action in moves)
        {
            Game newGame = game.Clone();
            newGame.Play(action);

            List<Action> child = [];
            double value;
            if (newGame.GetTurn() == game.GetTurn())
            {
                value = TotalNegamax(newGame, child, player, equity, alpha, beta, depth - 1);
            }
            else
            {
                value = -TotalNegamax(newGame, child, player, equity, -beta, -alpha, depth - 1);
            }

            if (value > best)
            {
                best = value;

                actions.Clear();
                actions.Add(action);
                actions.AddRange(child);
            }

            if (value >= beta)
            {
                _statsPrunedNodes += 1;
                break;
            }

            if (value > alpha)
            {
                alpha = value;
            }
        }

        return best;
    }
}