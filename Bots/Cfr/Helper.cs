namespace PokerBot.Bots.Cfr;

public class Helper
{
    public static List<List<int>> Combinations(int n, int k, int offset = 0)
    {
        if (k == 0)
        {
            return [[]];
        }

        if (n == 0)
        {
            return [];
        }


        // choose
        var choose = Combinations(n - 1, k - 1, offset + 1);
        var notChoose = Combinations(n - 1, k, offset + 1);

        foreach (var l in choose)
        {
            l.Insert(0, offset);
            notChoose.Add(l);
        }

        return notChoose;
    }

    public static IEnumerable<(Card[], Card[])> AllSeeds()
    {
        Card[] cards = Card.AllCards();
        for (int i = 0; i < cards.Length; ++i)
        {
            for (int j = i + 1; j < cards.Length; ++j)
            {
                List<Card> remain = Card.AllCards().ToList();
                remain.Remove(cards[i]);
                remain.Remove(cards[j]);

                foreach (var counts in Combinations(remain.Count, 5))
                {
                    List<Card> visible = [];
                    foreach (var count in counts)
                    {
                        visible.Add(remain[count]);
                    }

                    yield return ([cards[i], cards[j]], visible.ToArray());
                }
            }
        }
    }
    
    
    public static Card[] SelectRemain(Card[] exist, int count)
    {
        Card[] cards = Card.AllCards();
        List<Card> remain = [];

        foreach (var card in cards)
        {
            if (!exist.Contains(card))
            {
                remain.Add(card);
                if (remain.Count == count)
                    break;
            }
        }

        return remain.ToArray();
    }

    public static bool Distinct(Card[] set1, Card[] set2)
    {
        foreach (Card card in set1)
        {
            if (set2.Contains(card))
            {
                return false;
            }
        }

        return true;
    }
}