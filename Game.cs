using System.Numerics;

namespace PokerBot;

/// <summary>
/// A poker card
/// </summary>
public class Card
{
    public const int NumberSuits = 4;
    public const int NumberRanks = 13;

    // card suit 0-3
    public int Suit { get; }

    // card rank 0-12
    public int Rank { get; }

    public Card(int suit, int rank)
    {
        Suit = suit;
        Rank = rank;
    }

    /// <summary>
    /// Get all cards
    /// </summary>
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

    protected bool Equals(Card other)
    {
        return Suit == other.Suit && Rank == other.Rank;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Card)obj);
    }

    public override string ToString()
    {
        string[] suits = ["♥", "♦", "♠", "♣"];
        string[] ranks = ["2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A"];
        return $"{suits[Suit]}{ranks[Rank]}";
    }

    /// <summary>
    /// Compute the unique hash of the card
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Suit, Rank);
    }

    /// <summary>
    /// Compute the maximum hash for a deck of cards. Namely, max_{deck}(HashDeck(deck))
    /// </summary>
    /// <param name="count">Number of cards</param>
    /// <returns>Highest hash</returns>
    public static long MaxHashDeck(int count)
    {
        long hash = 1;
        for (int i = 0; i < count; ++i)
        {
            hash *= NumberRanks * NumberSuits;
        }

        return hash;
    }

    /// <summary>
    /// Compute the perfect unique hash (within the same card numbers) for a number of cards 
    /// </summary>
    /// <param name="deck">A list of cards</param>
    /// <returns>Unique hash</returns>
    /// <exception cref="Exception">If the hash is not unique (number too large)</exception>
    public static long HashDeck(Card[] deck)
    {
        // sort by rank
        Card[] sorted = deck.OrderBy(card => card.Rank * NumberSuits + card.Suit).ToArray();

        // suitMapping[suit] = newSuit
        int nextSuit = 0;
        int[] suitMapping = [-1, -1, -1, -1];

        long hash = 0;
        foreach (var card in sorted)
        {
            if (suitMapping[card.Suit] == -1)
            {
                suitMapping[card.Suit] = nextSuit;
                nextSuit += 1;
            }

            hash = (hash * (NumberRanks * NumberSuits)) + suitMapping[card.Suit] * NumberRanks + card.Rank;
            if (hash > 1L << 60)
            {
                throw new Exception("int overflow");
            }
        }

        return hash;
    }
    
    public static long HashDeck7(Card[] hole, Card[] river)
    {
        // sort by rank
        Card[] sorted = hole.OrderBy(card => card.Rank * NumberSuits + card.Suit).ToArray();
        sorted = sorted.Concat(river.OrderBy(card => card.Rank * NumberSuits + card.Suit).ToArray()).ToArray();
        
        // suitMapping[suit] = newSuit
        int nextSuit = 0;
        int[] suitMapping = [-1, -1, -1, -1];

        long hash = 0;
        foreach (var card in sorted)
        {
            if (suitMapping[card.Suit] == -1)
            {
                suitMapping[card.Suit] = nextSuit;
                nextSuit += 1;
            }

            hash = (hash * (NumberRanks * NumberSuits)) + suitMapping[card.Suit] * NumberRanks + card.Rank;
            if (hash > 1L << 61)
            {
                throw new Exception("int overflow");
            }
        }

        return hash;
    }
}

/// <summary>
/// A player action in poker
/// </summary>
public class Action
{
    public const int FoldFlag = 1;
    // CALL and CHECK
    public const int CheckFlag = 2;
    public const int AllinFlag = 4;

    /// If zero, a check.
    /// Otherwise, a call/raise
    public int Amount { get; }

    /// <summary>
    /// The pot amount at the time of action
    /// </summary>
    public int Pot { get; }

    /// 1th is fold, 2nd is allin, 3rd is check
    public int Flag { get; }

    private Action(int amount, int pot, int flag)
    {
        if (amount < 0 || pot < 0)
        {
            throw new Exception("invalid action");
        }

        Amount = amount;
        Pot = pot;
        Flag = flag;
    }

    public static Action Raise(int pot, int amount, int flag)
    {
        return new Action(amount, pot, flag);
    }

    public static Action Fold(int pot)
    {
        return new Action(0, pot, FoldFlag);
    }

    public bool IsFold()
    {
        return (Flag & FoldFlag) > 0;
    }

    public bool IsAllin()
    {
        return (Flag & AllinFlag) > 0;
    }

    public bool IsCheck()
    {
        return (Flag & CheckFlag) > 0;
    }

    public double Proportion()
    {
        return (double)Amount / Pot;
    }

    public override string ToString()
    {
        return IsFold() ? "Fold" : $"Raise {Amount}";
    }

    /// <summary>
    /// Simplified representation of the action
    /// </summary>
    /// <returns>String representation</returns>
    public string Repr()
    {
        if (IsFold())
        {
            return "F";
        }

        if (IsAllin())
        {
            return "A";
        }

        if (IsCheck())
        {
            return "C";
        }

        double percent = (double)Amount / Pot * 100;
        int simplified = (int)Math.Round(percent / 10);
        return $"R{simplified}";
    }
}

/// <summary>
/// Hand comparison handler
/// </summary>
public static class HandResolver
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

    /// <summary>
    /// Get the best matching value out of 7 cards
    /// </summary>
    /// <param name="cards">7 total cards</param>
    /// <returns>Best matching value</returns>
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
            int[] rankFrequency = new int[Card.NumberRanks];
            foreach (var card in selected)
            {
                rankFrequency[card.Rank] += 1;
            }

            // frequency to ranks
            List<int>[] frequencyRank = new List<int>[NumberMatch + 1];
            for (int i = 0; i < NumberMatch + 1; ++i)
            {
                frequencyRank[i] = new List<int>();
            }

            for (int i = 0; i < rankFrequency.Length; ++i)
            {
                frequencyRank[rankFrequency[i]].Add(i);
            }

            // royal flush and straight flush
            if (isStraight && isFlush)
            {
                int topRank = selected.Last().Rank;
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
            if (frequencyRank[3].Count > 0 && frequencyRank[2].Count > 0)
            {
                bestValue = int.Max(bestValue,
                    FullHouse + frequencyRank[3].Last() * Card.NumberRanks + frequencyRank[2].Last());
            }

            // flush
            if (isFlush)
            {
                int topRank = selected.Last().Rank;
                bestValue = int.Max(bestValue, Flush + topRank);
            }

            // straight
            if (isStraight)
            {
                int topRank = selected.Last().Rank;
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

        int value0 = GetValue(river.Concat(p0).ToArray()) * Base + high0;
        int value1 = GetValue(river.Concat(p1).ToArray()) * Base + high1;

        return [value0, value1];
    }
}

public class Game
{
    /// <summary>
    /// Agent accessible state
    /// </summary>
    public class State
    {
        /// <summary>
        /// Current player index
        /// </summary>
        public readonly int Index;

        /// <summary>
        /// Street
        /// </summary>
        public readonly int Street;

        /// <summary>
        /// Current highest raise
        /// </summary>
        public readonly int Raise;

        /// <summary>
        /// Player raised amounts
        /// </summary>
        public readonly int[] Raised;

        /// <summary>
        /// Player check status
        /// </summary>
        public readonly bool[] Checked;

        /// <summary>
        /// Player money
        /// </summary>
        public readonly int[] Money;

        /// <summary>
        /// Pot amount
        /// </summary>
        public readonly int Pot;

        public readonly int LastIncrement;

        /// <summary>
        /// Public cards
        /// </summary>
        public readonly Card[] River;

        /// <summary>
        /// Private cards
        /// </summary>
        public readonly Card[] Hand;

        /// <summary>
        /// History actions
        /// </summary>
        public readonly List<Action> History;

        public State(
            int index,
            int street,
            int raise,
            int[] raised,
            bool[] hasChecked,
            int[] money,
            int pot,
            int lastIncrement,
            Card[] river,
            Card[] hand,
            List<Action> history)
        {
            Index = index;
            Street = street;
            Raise = raise;
            Raised = raised;
            Checked = hasChecked;
            Money = money;
            Pot = pot;
            LastIncrement = lastIncrement;
            River = river;
            Hand = hand;
            History = history.GetRange(0, history.Count);
        }
    }

    // rule constants
    public const int AllInAmount = 4000;
    public const int BbAmount = 20;

    // player indices
    public const int PlayerSb = 0;
    public const int PlayerBb = 1;

    // chance sampling constants
    public const int SbHandOffset = 0;
    public const int BbHandOffset = 2;
    public const int RiverHandOffset = 4;

    /// <summary>
    /// chance sampling cards
    /// </summary>
    protected Card[] _hands;

    /// <summary>
    /// Player turn
    /// </summary>
    protected int _turn;

    /// <summary>
    /// [player 0 money left, player 1 money left]
    /// </summary>
    protected int[] _money;

    /// <summary>
    /// Pot value
    /// </summary>
    protected int _pot;

    /// <summary>
    /// Highest raise in cycle, _raise = Max(_raised)
    /// </summary>
    protected int _raise;

    /// <summary>
    /// [player 0 raise, player 1 raise] in cycle, this is total bet
    /// </summary>
    protected int[] _raised;

    /// <summary>
    /// Last increment raise, _lastIncrement = _raise[0] - _raise[1] if 0 raised
    /// </summary>
    protected int _lastIncrement;

    /// <summary>
    /// [player 0 checked, player 1 checked] in cycle
    /// </summary>
    protected bool[] _checked;

    /// <summary>
    /// Number of river cards revealed
    /// </summary>
    protected int _riverCards;

    /// <summary>
    /// Play history for entire game
    /// </summary>
    protected List<Action> _history;

    /// <summary>
    /// History for only the current street
    /// </summary>
    protected List<Action> _streetHistory;

    // /// <summary>
    // /// Custom abstracted street history
    // /// </summary>
    // private List<Action> _abstractStreetHistory;

    /// <summary>
    /// Create a new game
    /// </summary>
    public Game()
    {
        _turn = PlayerSb;
        _money = [AllInAmount, AllInAmount];
        _pot = 0;
        _raise = BbAmount;
        _raised = [BbAmount / 2, BbAmount];
        _lastIncrement = BbAmount;
        _checked = [false, false];
        _riverCards = 0;
        _history = new List<Action>();
        _streetHistory = new List<Action>();
        // _abstractStreetHistory = new List<Action>();

        // setup
        _money[PlayerSb] -= BbAmount / 2;
        _money[PlayerBb] -= BbAmount;
        _pot += BbAmount / 2 + BbAmount;

        // chance sample
        _hands = Card.AllCards();
    }


    public Game(Card[] hands, int turn, int[] money, int pot, int raise, int[] raised, int lastIncrement,
        bool[] @checked, int riverCards, List<Action> history, List<Action> streetHistory)
    {
        _hands = hands;
        _turn = turn;
        _money = money;
        _pot = pot;
        _raise = raise;
        _raised = raised;
        _lastIncrement = lastIncrement;
        _checked = @checked;
        _riverCards = riverCards;
        _history = history;
        _streetHistory = streetHistory;
        // _abstractStreetHistory = abstractStreetHistory;
    }

    /// <summary>
    /// Deep clone the game state
    /// </summary>
    /// <returns>A complete game state clone</returns>
    public virtual Game Clone()
    {
        return new Game(
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
            [.._streetHistory]
            // [.._abstractStreetHistory]
        );
    }

    /// <summary>
    /// Shuffle game
    /// </summary>
    /// <param name="seed"></param>
    public void Shuffle(int seed)
    {
        Random rng = new Random(seed);
        rng.Shuffle(_hands);
    }

    public void Seed(Card[] sbHole, Card[] bbHole, Card[] visible)
    {
        for (int i = 0; i < 2; ++i)
        {
            _hands[SbHandOffset + i] = sbHole[i];
        }

        for (int i = 0; i < 2; ++i)
        {
            _hands[BbHandOffset + i] = bbHole[i];
        }

        for (int i = 0; i < 5; ++i)
        {
            _hands[RiverHandOffset + i] = visible[i];
        }
    }

    /// <summary>
    /// Get next turn
    /// </summary>
    /// <returns>Player to play next</returns>
    public int GetTurn()
    {
        return _turn;
    }

    /// <summary>
    /// Get a list of actions for current player
    /// </summary>
    /// <returns>List of actions</returns>
    /// <exception cref="Exception"></exception>
    public List<Action> GetActions()
    {
        if (_riverCards == 6)
        {
            throw new Exception("invalid state to call GetActions(), all cards revealed");
        }

        List<Action> actions = new List<Action>();

        // add fold
        actions.Add(Action.Fold(_pot));

        // add check
        actions.Add(Action.Raise(_pot, _raise - _raised[_turn], Action.CheckFlag));

        // raise rules (no-limit texas hold'em):
        // - minimum raise must be size of previous increment
        // - increment defined as the additional amount
        // house rules
        // - limited to multiples of bb (and all in)

        // add raise
        int increment = _lastIncrement == 0 ? BbAmount : _lastIncrement;
        while (true)
        {
            int amount = _raise + increment - _raised[_turn];
            if (amount > _money[_turn])
                break;

            actions.Add(Action.Raise(_pot, amount, 0));
            increment += BbAmount;
        }

        // check all-in
        actions.Add(Action.Raise(_pot, _money[_turn], Action.AllinFlag));

        return actions;
    }

    public virtual State GetState()
    {
        return new State(_turn, _riverCards, _raise, _raised, _checked, _money, _pot,
            _lastIncrement, _hands.Skip(RiverHandOffset).Take(int.Min(5, _riverCards)).ToArray(),
            _hands.Skip(_turn == PlayerSb ? SbHandOffset : BbHandOffset).Take(2).ToArray(), _streetHistory);
    }

    public bool IsOver()
    {
        return _riverCards == 6 || _history.Count > 0 && _history.Last().IsFold();
    }
    
    /// <summary>
    /// Get utility for game, null if not finished
    /// </summary>
    /// <returns>[player 0 utility, player 1 utility] if finished, null otherwise</returns>
    /// <exception cref="Exception"></exception>
    public int[]? Utility()
    {
        int winner = HandResolver.Equal;

        if (_riverCards == 6)
        {
            // if river
            winner = HandResolver.CompareHands(
                _hands.Skip(RiverHandOffset).Take(5).ToArray(),
                _hands.Skip(SbHandOffset).Take(2).ToArray(),
                _hands.Skip(BbHandOffset).Take(2).ToArray()
            );
        }
        else if (_history.Count > 0 && _history.Last().IsFold())
        {
            // if fold
            // remember that fold don't switch players
            winner = 1 - _turn;
        }
        else
        {
            return null;
        }

        return winner switch
        {
            HandResolver.Equal => [0, 0],
            HandResolver.Player0 => [_pot + _money[0] - AllInAmount, _money[1] - AllInAmount],
            HandResolver.Player1 => [_money[0] - AllInAmount, _pot + _money[1] - AllInAmount],
            _ => throw new Exception("unexpected winner")
        };
    }

    /// <summary>
    /// Play an action
    /// </summary>
    /// <param name="action"></param>
    /// <exception cref="Exception"></exception>
    public virtual void Play(Action action)
    {
        _history.Add(action);
        _streetHistory.Add(action);

        // do nothing on fold
        if (action.IsFold())
        {
            return;
        }

        if (action.Amount > _raise - _raised[_turn])
        {
            // handle raise

            _checked[_turn] = true;
            _checked[1 - _turn] = false;

            int amount = action.Amount;
            _pot += amount;
            _money[_turn] -= amount;

            _lastIncrement = action.Amount + _raised[_turn] - _raise;
            _raised[_turn] += action.Amount;
            _raise = _raised[_turn];
            _turn = 1 - _turn;
        }
        else if (action.Amount == _raise - _raised[_turn])
        {
            int amount = action.Amount;
            _pot += amount;
            _money[_turn] -= amount;
            _raised[_turn] += amount;
            _checked[_turn] = true;

            if (_checked[0] && _checked[1])
            {
                if (_money[0] == 0 && _money[1] == 0)
                {
                    // check for all in
                    // skip to end
                    _riverCards = 6;
                }
                else
                {
                    // else advance
                    if (_riverCards == 0)
                    {
                        _riverCards = 3;
                    }
                    else
                    {
                        _riverCards += 1;
                    }
                }

                _turn = PlayerSb;
                _raised = [0, 0];
                _checked = [false, false];
                _raise = 0;
                _lastIncrement = 0;
                _streetHistory = [];
            }
            else
            {
                // else switch turn to raise/fold
                _turn = 1 - _turn;
            }
        }
        else
        {
            throw new Exception("invalid raise");
        }
    }

    /// <summary>
    /// Print the game state
    /// </summary>
    public void Display()
    {
        string sep = ", ";
        Console.WriteLine($"turn {_turn}, #public {_riverCards}");
        Console.WriteLine($"pot {_pot}, raise {_raise}, incre {_lastIncrement}");
        Console.WriteLine($"public {string.Join(sep, _hands.Skip(RiverHandOffset).Take(5))}");
        Console.WriteLine(
            $"sb: {string.Join(sep, _hands.Skip(SbHandOffset).Take(2))}, checked {_checked[PlayerSb]}, raised {_raised[PlayerSb]}, money {_money[PlayerSb]}");
        Console.WriteLine(
            $"bb: {string.Join(sep, _hands.Skip(BbHandOffset).Take(2))}, checked {_checked[PlayerBb]}, raised {_raised[PlayerBb]}, money {_money[PlayerBb]}");

        var util = Utility();
        if (util != null)
        {
            int[] values = HandResolver.HandValues(_hands.Skip(RiverHandOffset).Take(5).ToArray(),
                _hands.Skip(SbHandOffset).Take(2).ToArray(),
                _hands.Skip(BbHandOffset).Take(2).ToArray());

            Console.WriteLine($"Game Finished.");
            Console.WriteLine(
                $"sb: {HandResolver.ParseValue(values[PlayerSb])}/{values[PlayerSb]}, utility {util[PlayerSb]}");
            Console.WriteLine(
                $"bb: {HandResolver.ParseValue(values[PlayerBb])}/{values[PlayerBb]}, utility {util[PlayerBb]}");
        }
        else
        {
            Console.WriteLine($"Actions: {string.Join(sep, GetActions())}");
        }
    }
}