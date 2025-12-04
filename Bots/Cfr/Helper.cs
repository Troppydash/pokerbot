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

    public static long Snoob(long x)
    {
        long smallest = x & -x;
        long ripple = x + smallest;
        long ones = x ^ ripple;
        ones = (ones >> 2) / smallest;
        return ripple | ones;
    }

    public static IEnumerable<int[]> LazyCombinations(int n, int k)
    {
        if (k == 0)
        {
            yield return [];
            yield break;
        }
        
        int[] selected = new int[k];

        long x = (1L << k) - 1;
        long limit = 1L << n;
        while (x < limit)
        {
            long selection = x;
            for (int i = 0; i < k; ++i)
            {
                selected[i] = (int)long.TrailingZeroCount(selection);
                selection ^= 1L << selected[i];
            }

            yield return selected;

            x = Snoob(x);
        }
    }

    public static IEnumerable<Card[]> SelectCombinations(Card[] population, int k)
    {
        Card[] selected = new Card[k];
        foreach (var comb in LazyCombinations(population.Length, k))
        {
            for (int i = 0; i < k; ++i)
            {
                selected[i] = population[comb[i]];
            }

            yield return selected;
        }
    }

    public static Card[] RemoveCards(Card[] population, Card[] toRemove)
    {
        Card[] newCards = new Card[population.Length - toRemove.Length];
        int k = 0;
        foreach (var card in population)
        {
            if (!toRemove.Contains(card))
            {
                newCards[k++] = card;
            }
        }

        return newCards;
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