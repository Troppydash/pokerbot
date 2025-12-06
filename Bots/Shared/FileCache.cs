using System.Text.Json;
using PokerBot.Attributes;

namespace PokerBot.Bots.Shared;

[Stable]
public class FileCache
{
    public static T OptionalCompute<T>(string path, Func<T> compute)
    {
        if (File.Exists(path))
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path))!;
        }

        T result = compute();
        File.WriteAllText(path, JsonSerializer.Serialize(result));
        return result;
    }
}