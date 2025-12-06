namespace PokerBot.Attributes;

/// <summary>
/// Means that the code should work, but is not tested. Use at own risk.
/// </summary>
[AttributeUsage(AttributeTargets.All)]
public class Untested : Attribute
{
    
}