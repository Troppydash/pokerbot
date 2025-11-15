namespace PokerBot;

public class Program
{
    public static void Main()
    {
        
        Arena arena = new Arena([new RandomAgent(), new RandomAgent()]);
        arena.Simulate();
    }
}