using Poems.Services;

namespace Poems.Components;

public sealed class Collections
{
    public void Show(PoemService service)
    {
        string[] moods = FallbackPoems.Collections.Keys.ToArray();

        int choice = ConsoleUI.Select("MOOD COLLECTIONS", moods);

        if (choice < 0)
            return;

        string mood = moods[choice];

        ConsoleUI.Clear();
        ConsoleUI.Title($"{mood.ToUpperInvariant()} COLLECTION");

        int index = 1;

        foreach (string line in FallbackPoems.Collections[mood])
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{index++,2}. ");
            Console.ResetColor();

            Console.WriteLine(line);
        }

        Console.WriteLine();
        ConsoleUI.Muted(
            "These lines are available for local fallback generation.");

        ConsoleUI.Pause();
    }
}