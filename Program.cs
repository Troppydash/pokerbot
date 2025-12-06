using PokerBot.Bots;
using PokerBot.Bots.Cfr;
using PokerBot.Bots.ExpectedMinimax;

namespace PokerBot;

public class Program
{

    public static void Main()
    {
        var result = Arena.SimulateAll([new EvAgent(), new ManualAgent()], 42, 1000, false);
    }
}