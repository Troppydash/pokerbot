using System.Text.Json;
using PokerBot.Attributes;

namespace PokerBot.Bots.Shared;

[Stable]
public class FileCache
{
    /// <summary>
    /// Caches a computation in a disk file
    /// </summary>
    /// <param name="path">Disk file name</param>
    /// <param name="compute">Lambda computation</param>
    /// <typeparam name="T">Type of data to store</typeparam>
    /// <returns>Data either loaded from disk, or computed and saved using lambda</returns>
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