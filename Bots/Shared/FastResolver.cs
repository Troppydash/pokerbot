using System.Numerics;
using PokerBot.Bots.Cfr;

namespace PokerBot.Bots;

public class FastResolver
{
    // card value constants
    // formula is: player hand value + matching value * Base

    private const int Base = 200;
    private const int HighCard = 200;
    private const int Pair = 400;
    private const int TwoPair = 600;
    private const int ThreeKind = 800;
    private const int Straight = 1000;
    private const int Flush = 1200;
    private const int FullHouse = 1400;
    private const int FourKind = 1600;
    private const int StraightFlush = 1800;
    private const int RoyalFlush = 2000;

    /// <summary>
    /// Total cards
    /// </summary>
    private const int NumberCards = 7;

    /// <summary>
    /// Cards used for matching
    /// </summary>
    private const int NumberMatch = 5;

    // comparison return constants 

    public const int Player0 = 0;
    public const int Player1 = 1;
    public const int Equal = 2;

    /// <summary>
    /// Get string repr for a value
    /// </summary>
    /// <param name="value">Card value</param>
    /// <returns>String repr</returns>
    /// <exception cref="Exception"></exception>
    public static string ParseValue(int value)
    {
        value /= Base;
        switch (value)
        {
            case >= RoyalFlush:
                return "Royal Flush";
            case >= StraightFlush:
                return "Straight Flush";
            case >= FourKind:
                return "Four Kind";
            case >= FullHouse:
                return "Full House";
            case >= Flush:
                return "Flush";
            case >= Straight:
                return "Straight";
            case >= ThreeKind:
                return "Three Kind";
            case >= TwoPair:
                return "Two Pair";
            case >= Pair:
                return "Pair";
            case >= HighCard:
                return "High Card";
        }

        throw new Exception("unparsable value");
    }

    public class Cached
    {
        private static Cached? _instance = null;
        private Dictionary<long, int> _5handLookup;
        private Dictionary<long, int> _7handLookup;

        public const string FIVE_FILE = "fastResolverCache5Hand.json";
        public const string SEVEN_FILE = "fastResolverCache7Hand.json";

        public static Cached GetInstance()
        {
            if (_instance == null)
            {
                _instance = new Cached();
            }

            return _instance;
        }

        private Cached()
        {
            // build cache table

            // build 5 hand lookup
            _5handLookup = FileCache.OptionalCompute(
                FIVE_FILE,
                () =>
                {
                    Console.WriteLine("Building 5 hand");
                    var lookup = new Dictionary<long, int>();
                    foreach (var hands in Helper.SelectCombinations(Card.AllCards(), 5))
                    {
                        long hash = Card.HashDeck(hands);
                        if (lookup.ContainsKey(hash))
                            continue;

                        lookup.Add(hash, GetValue5Sorted(hands.OrderBy(c => c.Rank).ToArray()));
                    }

                    return lookup;
                });

            // build 7 hand lookup
            _7handLookup = FileCache.OptionalCompute(
                SEVEN_FILE,
                () =>
                {
                    Console.WriteLine("Building 7 hand");
                    var lookup = new Dictionary<long, int>();
                    int count = 0;
                    foreach (var hands in Helper.SelectCombinations(Card.AllCards(), 7))
                    {
                        count += 1;
                        long hash = Card.HashDeck(hands);
                        if (lookup.ContainsKey(hash))
                            continue;

                        Console.WriteLine($"Progress {(double)count / 133784560 * 100}%");

                        int bestValue = 0;
                        foreach (var selected in Helper.SelectCombinations(hands, 5))
                        {
                            long h = Card.HashDeck(selected);
                            bestValue = int.Max(bestValue, _5handLookup[h]);
                        }

                        lookup.Add(hash, bestValue);
                    }

                    return lookup;
                });
        }

        private static int GetValue5Sorted(Card[] selected)
        {
            int[] rankFrequency = new int[Card.NumberRanks];
            int[,] frequencyRank = new int[NumberMatch + 1, 2];

            bool isStraight = true;
            for (int i = 1; i < NumberMatch; ++i)
            {
                if (selected[i].Rank != selected[i - 1].Rank + 1)
                {
                    isStraight = false;
                    break;
                }
            }

            bool isFlush = true;
            for (int i = 1; i < NumberMatch; ++i)
            {
                if (selected[i].Suit != selected[0].Suit)
                {
                    isFlush = false;
                    break;
                }
            }

            // rank to frequency
            foreach (var card in selected)
            {
                rankFrequency[card.Rank] += 1;
            }

            // frequency to ranks
            for (int i = 0; i < rankFrequency.Length; ++i)
            {
                frequencyRank[rankFrequency[i], 1] = frequencyRank[rankFrequency[i], 0];
                frequencyRank[rankFrequency[i], 0] = i;
            }

            int bestValue = 0;

            // royal flush and straight flush
            if (isStraight && isFlush)
            {
                int topRank = selected.Last().Rank;
                if (topRank == Card.NumberRanks - 1)
                {
                    return RoyalFlush;
                }

                return StraightFlush + topRank;
            }


            // four kind
            if (frequencyRank[4, 0] >= 0)
            {
                return FourKind + frequencyRank[4, 0];
            }

            // full house
            if (frequencyRank[3, 0] >= 0 && frequencyRank[2, 0] >= 0)
            {
                return FullHouse + frequencyRank[3, 0] * Card.NumberRanks + frequencyRank[2, 0];
            }

            // flush
            if (isFlush)
            {
                int topRank = selected.Last().Rank;
                return Flush + topRank;
            }

            // straight
            if (isStraight)
            {
                int topRank = selected.Last().Rank;
                return Straight + topRank;
            }


            // three kind
            if (frequencyRank[3, 0] >= 0)
            {
                return ThreeKind + frequencyRank[3, 0];
            }

            // two pair
            if (frequencyRank[2, 0] >= 0 && frequencyRank[2, 1] >= 0)
            {
                return TwoPair + frequencyRank[2, 0] * Card.NumberRanks +
                       frequencyRank[2, 1];
            }

            // pair
            if (frequencyRank[2, 0] >= 0)
            {
                return Pair + frequencyRank[2, 0];
            }

            // high card
            return HighCard + selected.Last().Rank;
        }

        public int CompareHands(Card[] river, Card[] p0, Card[] p1)
        {
            int highCard0 = int.Max(p0[0].Rank, p0[1].Rank);
            int lowCard0 = int.Min(p0[0].Rank, p0[1].Rank);
            int highCard1 = int.Max(p1[0].Rank, p1[1].Rank);
            int lowCard1 = int.Min(p1[0].Rank, p1[1].Rank);
            int high0 = highCard0 * Card.NumberRanks + lowCard0;
            int high1 = highCard1 * Card.NumberRanks + lowCard1;

            Card[] cards = new Card[NumberCards];
            river.CopyTo(cards, 0);
            p0.CopyTo(cards, 5);
            int value0 = _7handLookup[Card.HashDeck(cards)] * Base + high0;
            p1.CopyTo(cards, 5);
            int value1 = _7handLookup[Card.HashDeck(cards)] * Base + high1;

            if (value0 == value1)
            {
                return Equal;
            }

            if (value0 < value1)
            {
                return Player1;
            }

            return Player0;
        }
    }


    /// <summary>
    /// Get the best matching value out of 7 cards
    /// </summary>
    /// <param name="cards">7 total cards</param>
    /// <returns>Best matching value</returns>
    private static int GetValue(Card[] cards)
    {
        Card[] sortedCards = cards.OrderBy(c => c.Rank).ToArray();

        int bestValue = 0;

        // pre-alloc
        Card[] selected = new Card[NumberMatch];
        int k = 0;

        int[] rankFrequency = new int[Card.NumberRanks];
        int[,] frequencyRank = new int[NumberMatch + 1, 2];

        for (uint mask = 0; mask < (1 << NumberCards); ++mask)
        {
            if (BitOperations.PopCount(mask) != NumberMatch)
                continue;

            // clear cache
            k = 0;
            for (int i = 0; i < Card.NumberRanks; ++i)
            {
                rankFrequency[i] = 0;
            }

            for (int i = 0; i < NumberMatch + 1; ++i)
            {
                frequencyRank[i, 0] = frequencyRank[i, 1] = -1;
            }

            // select set of five
            for (int i = 0; i < NumberCards; ++i)
            {
                if ((mask & (1 << i)) > 0)
                {
                    selected[k++] = sortedCards[i];
                }
            }

            bool isStraight = true;
            for (int i = 1; i < NumberMatch; ++i)
            {
                if (selected[i].Rank != selected[i - 1].Rank + 1)
                {
                    isStraight = false;
                    break;
                }
            }

            bool isFlush = true;
            for (int i = 1; i < NumberMatch; ++i)
            {
                if (selected[i].Suit != selected[0].Suit)
                {
                    isFlush = false;
                    break;
                }
            }

            // rank to frequency
            foreach (var card in selected)
            {
                rankFrequency[card.Rank] += 1;
            }

            // frequency to ranks
            for (int i = 0; i < rankFrequency.Length; ++i)
            {
                frequencyRank[rankFrequency[i], 1] = frequencyRank[rankFrequency[i], 0];
                frequencyRank[rankFrequency[i], 0] = i;
            }

            // royal flush and straight flush
            if (isStraight && isFlush)
            {
                int topRank = selected.Last().Rank;
                if (topRank == Card.NumberRanks - 1)
                {
                    bestValue = int.Max(bestValue, RoyalFlush);
                    break;
                }

                bestValue = int.Max(bestValue, StraightFlush + topRank);
            }

            if (bestValue >= StraightFlush)
                continue;

            // four kind
            if (frequencyRank[4, 0] >= 0)
            {
                bestValue = int.Max(bestValue, FourKind + frequencyRank[4, 0]);
            }

            if (bestValue >= FourKind)
                continue;

            // full house
            if (frequencyRank[3, 0] >= 0 && frequencyRank[2, 0] >= 0)
            {
                bestValue = int.Max(bestValue,
                    FullHouse + frequencyRank[3, 0] * Card.NumberRanks + frequencyRank[2, 0]);
            }

            if (bestValue >= FullHouse)
                continue;

            // flush
            if (isFlush)
            {
                int topRank = selected.Last().Rank;
                bestValue = int.Max(bestValue, Flush + topRank);
            }

            if (bestValue >= Flush)
                continue;

            // straight
            if (isStraight)
            {
                int topRank = selected.Last().Rank;
                bestValue = int.Max(bestValue, Straight + topRank);
            }

            if (bestValue >= Straight)
                continue;

            // three kind
            if (frequencyRank[3, 0] >= 0)
            {
                bestValue = int.Max(bestValue,
                    ThreeKind + frequencyRank[3, 0]);
            }

            if (bestValue >= ThreeKind)
                continue;

            // two pair
            if (frequencyRank[2, 0] >= 0 && frequencyRank[2, 1] >= 0)
            {
                bestValue = int.Max(bestValue,
                    TwoPair + frequencyRank[2, 0] * Card.NumberRanks +
                    frequencyRank[2, 1]);
            }

            if (bestValue >= TwoPair)
                continue;

            // pair
            if (frequencyRank[2, 0] >= 0)
            {
                bestValue = int.Max(bestValue,
                    Pair + frequencyRank[2, 0]);
            }

            if (bestValue >= Pair)
                continue;

            // high card
            bestValue = int.Max(bestValue,
                HighCard + selected.Last().Rank);
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
        int[] values = HandValues(river, p0, p1);
        if (values[Player0] == values[Player1])
        {
            return Equal;
        }

        if (values[Player0] > values[Player1])
        {
            return Player0;
        }

        return Player1;
    }

    /// <summary>
    /// Get the hand values
    /// </summary>
    /// <param name="river">River cards (5 total)</param>
    /// <param name="p0">Player 0 cards (2 total)</param>
    /// <param name="p1">Player 1 cards (2 total)</param>
    /// <returns>A 2-array of [player 0 value, player 1 value]</returns>
    public static int[] HandValues(Card[] river, Card[] p0, Card[] p1)
    {
        int highCard0 = int.Max(p0[0].Rank, p0[1].Rank);
        int lowCard0 = int.Min(p0[0].Rank, p0[1].Rank);
        int highCard1 = int.Max(p1[0].Rank, p1[1].Rank);
        int lowCard1 = int.Min(p1[0].Rank, p1[1].Rank);
        int high0 = highCard0 * Card.NumberRanks + lowCard0;
        int high1 = highCard1 * Card.NumberRanks + lowCard1;

        Card[] cards = new Card[NumberCards];
        river.CopyTo(cards, 0);
        p0.CopyTo(cards, 5);
        int value0 = GetValue(cards) * Base + high0;
        p1.CopyTo(cards, 5);
        int value1 = GetValue(cards) * Base + high1;

        return [value0, value1];
    }
}