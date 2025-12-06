using PokerBot.Bots;
using PokerBot.Bots.Cfr;

namespace PokerBot;

public class Program
{

    public static void Main()
    {
        var result = Arena.SimulateAll([new EvAgent(false), new ManualAgent()], 42, 1000, true);
    }
}