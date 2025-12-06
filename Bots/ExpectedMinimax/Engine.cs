using PokerBot.Bots.Cfr;

namespace PokerBot.Bots;

public class Engine
{
    public Engine()
    {
    }

    // TODO: try expectedminimax using expectation, prune high equity nodes
    
    public Action Solve(Game.State state, List<Action> realActions)
    {
        AbstractGame abstractGame = AbstractGame.FromState(state);

        int player = abstractGame.GetTurn();
        double equity = Equity.ComputeEquity(abstractGame.GetState().Hand, abstractGame.GetState().River, 50);

        List<Action> actions = [];
        var ev = PreChanceNegamax(abstractGame, actions, player, equity, -1e9, 1e9, 100);
        // Console.WriteLine($"{equity}/{ev}");
        
        var action = actions[0];
        return AbstractGame.AbstractToReal(action, realActions);
    }

    private double UtilityFunc(double x)
    {
        x /= Game.BbAmount;
        double lambda = 0.001;
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
        // memo
        if (game.IsOver() || depth == 0)
        {
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
                break;

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
}