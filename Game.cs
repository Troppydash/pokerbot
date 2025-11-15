using System.Numerics;

namespace PokerBot;

public class Card
{
    public const int NumberSuits = 4;
    public const int NumberRanks = 13;

    // 0-3
    public int Suit { get; private set; }

    // 0-12
    public int Rank { get; private set; }

    public Card(int suit, int rank)
    {
        Suit = suit;
        Rank = rank;
    }

    public static Card[] AllCards()
    {
        Card[] cards = new Card[NumberSuits * NumberRanks];
        for (int suit = 0; suit < NumberSuits; ++suit)
        {
            for (int rank = 0; rank < NumberRanks; ++rank)
            {
                cards[suit * NumberRanks + rank] = new Card(suit, rank);
            }
        }

        return cards;
    }
}

public class Action
{
    /// If zero, a check.
    /// If equal to raise, a call.
    /// Otherwise, a raise.
    public int Amount { get; private set; }

    /// If true, Fold
    public bool IsFold { get; private set; }

    private Action(int amount, bool fold)
    {
        Amount = amount;
        IsFold = fold;
    }

    public static Action Raise(int amount)
    {
        return new Action(amount, false);
    }

    public static Action Fold()
    {
        return new Action(0, true);
    }
}

public static class HandResolver
{
    private const int HighCard = 0;
    private const int Pair = 200;
    private const int TwoPair = 400;
    private const int ThreeKind = 600;
    private const int Straight = 800;
    private const int Flush = 1000;
    private const int FullHouse = 1200;
    private const int FourKind = 1400;
    private const int StraightFlush = 1600;
    private const int RoyalFlush = 1800;

    private const int NumberCards = 7;
    private const int NumberMatch = 5;

    public const int Player0 = 0;
    public const int Player1 = 1;
    public const int Equal = 2;

    private static int GetValue(Card[] cards)
    {
        List<Card> sortedCards = cards.OrderBy(c => c.Rank).ToList();

        int bestValue = 0;

        for (uint mask = 0; mask < (1 << NumberCards); ++mask)
        {
            if (BitOperations.PopCount(mask) != NumberMatch)
                continue;

            // select set of five
            List<Card> selected = new List<Card>();
            for (int i = 0; i < NumberCards; ++i)
            {
                if ((mask & (1 << i)) > 0)
                {
                    selected.Add(sortedCards[i]);
                }
            }

            bool isStraight = true;
            for (int i = 1; i < NumberCards; ++i)
            {
                if (selected[i].Rank != selected[i - 1].Rank + 1)
                {
                    isStraight = false;
                    break;
                }
            }


            bool isFlush = true;
            for (int i = 1; i < NumberCards; ++i)
            {
                if (selected[i].Suit != selected[0].Suit)
                {
                    isFlush = false;
                    break;
                }
            }

            // rank to frequency
            int[] rankFrequency = new int[Card.NumberRanks];
            foreach (var card in sortedCards)
            {
                rankFrequency[card.Rank] += 1;
            }

            // frequency to ranks
            List<int>[] frequencyRank = new List<int>[NumberCards];
            for (int i = 0; i < rankFrequency.Length; ++i)
            {
                frequencyRank[rankFrequency[i]].Add(i);
            }

            // royal flush and straight flush
            if (isStraight && isFlush)
            {
                int topRank = selected[NumberCards - 1].Rank;
                if (topRank == Card.NumberRanks - 1)
                {
                    bestValue = int.Max(bestValue, RoyalFlush);
                }
                else
                {
                    bestValue = int.Max(bestValue, StraightFlush + topRank);
                }
            }

            // four kind
            if (frequencyRank[4].Count > 0)
            {
                bestValue = int.Max(bestValue, FourKind + frequencyRank[4].Last());
            }

            // full house
            if (frequencyRank[3].Count > 0 && frequencyRank[2].Count > 2)
            {
                bestValue = int.Max(bestValue,
                    FullHouse + frequencyRank[3].Last() * Card.NumberRanks + frequencyRank[2].Last());
            }


            // flush
            if (isFlush)
            {
                int topRank = selected[NumberCards - 1].Rank;
                bestValue = int.Max(bestValue, Flush + topRank);
            }

            // straight
            if (isStraight)
            {
                int topRank = selected[NumberCards - 1].Rank;
                bestValue = int.Max(bestValue, Straight + topRank);
            }

            // three kind
            if (frequencyRank[3].Count > 0)
            {
                bestValue = int.Max(bestValue,
                    ThreeKind + frequencyRank[3].Last());
            }

            // two pair
            if (frequencyRank[2].Count == 2)
            {
                bestValue = int.Max(bestValue,
                    TwoPair + frequencyRank[2].Last() * Card.NumberRanks +
                    frequencyRank[2][frequencyRank[2].Count - 2]);
            }

            // pair
            if (frequencyRank[2].Count == 1)
            {
                bestValue = int.Max(bestValue,
                    Pair + frequencyRank[2].Last());
            }

            // high card
            bestValue = int.Max(bestValue,
                HighCard + sortedCards.Last().Rank);
        }

        return bestValue;
    }

    /// <summary>
    /// Compare hand values for two players
    /// </summary>
    /// <param name="river">River cards (5 total)</param>
    /// <param name="p0">Player 0 cards (2 total)</param>
    /// <param name="p1">Player 0 cards (2 total)</param>
    /// <returns>The winner</returns>
    public static int CompareHands(Card[] river, Card[] p0, Card[] p1)
    {
        int highCard0 = int.Max(p0[0].Rank, p0[1].Rank);
        int lowCard0 = int.Min(p0[0].Rank, p0[1].Rank);
        int highCard1 = int.Max(p1[0].Rank, p1[1].Rank);
        int lowCard1 = int.Min(p1[0].Rank, p1[1].Rank);
        int high0 = highCard0 * Card.NumberRanks + lowCard0;
        int high1 = highCard1 * Card.NumberRanks + lowCard1;

        int value0 = GetValue(river.Concat(p0).ToArray()) * 200 + high0;
        int value1 = GetValue(river.Concat(p1).ToArray()) * 200 + high1;
        if (value0 == value1)
        {
            return Equal;
        }

        if (value0 > value1)
        {
            return Player0;
        }

        return Player1;
    }
}

public class Game
{
    //// rule constants
    public const int AllInAmount = 4000;
    public const int BbAmount = 20;

    //// player indices
    public const int PlayerSb = 0;
    public const int PlayerBb = 1;

    //// chance sampling
    public const int SbHandOffset = 0;
    public const int BbHandOffset = 2;
    public const int RiverHandOffset = 4;

    private Card[] _hands;

    private int _turn;
    private int[] _money;
    private int _pot;
    private int _raise;
    private int _riverCards;
    private List<Action> _history;

    /// <summary>
    /// Create a new game
    /// </summary>
    public Game()
    {
        _turn = PlayerSb;
        _money = [AllInAmount, AllInAmount];
        _pot = 0;
        _raise = 0;
        _riverCards = 0;
        _history = new List<Action>();

        // setup
        _money[PlayerSb] -= BbAmount / 2;
        _money[PlayerBb] -= BbAmount;
        _pot += BbAmount / 2 + BbAmount;
        _raise = BbAmount;

        // chance sample
        _hands = Card.AllCards();
        Random.Shared.Shuffle(_hands);
    }

    public int GetPlayer()
    {
        return _turn;
    }

    public List<Action> GetActions()
    {
        if (_riverCards == 5)
        {
            throw new Exception("invalid state to call GetActions(), all cards revealed");
        }

        List<Action> actions = new List<Action>();
        actions.Add(Action.Fold());
        for (int raise = _raise; raise <= _money[_turn]; raise += BbAmount)
        {
            actions.Add(Action.Raise(raise));
        }

        return actions;
    }

    public int[]? Utility()
    {
        // if river
        if (_riverCards == 5)
        {
            // resolve winner
            int winner = HandResolver.CompareHands(
                _hands.Skip(RiverHandOffset).Take(5).ToArray(),
                _hands.Skip(SbHandOffset).Take(2).ToArray(),
                _hands.Skip(BbHandOffset).Take(2).ToArray()
            );

            return winner switch
            {
                HandResolver.Equal => [0, 0],
                HandResolver.Player0 => [_pot, -_pot],
                HandResolver.Player1 => [-_pot, _pot],
                _ => throw new Exception("unexpected winner")
            };
        }

        // if fold
        if (_history.Count > 0 && _history.Last().IsFold)
        {
        }


        return null;
    }

    public void Play(Action action)
    {
    }

    public void Display()
    {
    }
}