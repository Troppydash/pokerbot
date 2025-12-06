using PokerBot.Bots.Shared;

namespace PokerBot.Bots.ExpectedMinimax;

public class Equity
{
    public static double ComputeEquity(Card[] holeCards, Card[] publicCards, int samples)
    {
        double wins = 0.0;
        double total = 0.0;
        Card[] remain = Helper.RemoveCards(Card.AllCards(), holeCards.Concat(publicCards).ToArray());
        foreach (var oppHoleCards in Helper.SelectCombinations(remain, 2))
        {
            Card[] rest = Helper.RemoveCards(remain, oppHoleCards);
            for (int i = 0; i < samples; ++i)
            {
                var (river, _) = Helper.SampleCards(rest, 5 - publicCards.Length);

                // var result = FastResolver.Cached.GetInstance().CompareHands(
                //     publicCards.Concat(river).ToArray(), holeCards, oppHoleCards);
                //
                var alt = FastResolver.CompareHands(
                    publicCards.Concat(river).ToArray(), holeCards, oppHoleCards);

                // if (alt != result)
                // {
                //     Console.WriteLine();
                // }
                if (alt == FastResolver.Player0)
                    wins += 1;
                else if (alt == FastResolver.Equal)
                    wins += 0.5;

                total += 1.0;
            }
        }
       

        return wins / total;
    }
}