namespace PokerBot.Bots.Cfr;

/// <summary>
/// Handles the infoset lookup step of the CFR algorithm
/// </summary>
public class Infoset
{
    /// <summary>
    /// Represent an abstract game state, containing multiple game states
    /// </summary>
    public class Entry
    {
        public int Street { get; set; }
        public int Position { get; set; }
        public int PublicClusterId { get; set; }
        public int HoleClusterId { get; set; }
        public required List<Action> Bets { get; set; }

        public override string ToString()
        {
            // TODO:
            return base.ToString();
        }
    }

    public Infoset()
    {
        // TODO: handle mappings
    }

    public Entry Lookup(
        int street,
        int position,
        Card[] hole,
        Card[] visible,
        List<Action> bets
    )
    {
        // TODO:
        return null;
    }
}