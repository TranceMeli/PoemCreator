using Poems.Models;
using Poems.Services;

namespace Poems.Components;

public sealed class GeneratePoem
{
    public async Task ShowAsync(
        PoemService service,
        PoemRepository repository)
    {
        string[] moods = FallbackPoems.Collections.Keys.ToArray();

        int moodIndex = ConsoleUI.Select("CHOOSE A MOOD", moods);

        if (moodIndex < 0)
            return;

        string mood = moods[moodIndex];

        var seedLines = ConsoleUI.SelectMultiple(
            "SELECT SEED LINES",
            FallbackPoems.Collections[mood]);

        ConsoleUI.Clear();
        ConsoleUI.Title("POEM SETTINGS");

        // The warning is shown after the screen clear, otherwise
        // it would be erased immediately.
        if (seedLines.Count == 0)
        {
            ConsoleUI.Warning(
                "No seed lines selected. The mood will guide the poem.");
            Console.WriteLine();
        }

        Console.Write("Number of lines (1-80, default 8): ");
        string? input = Console.ReadLine();

        int lineCount = 8;

        if (!string.IsNullOrWhiteSpace(input) &&
            (!int.TryParse(input, out lineCount) ||
             lineCount < 1 ||
             lineCount > 80))
        {
            ConsoleUI.Warning("Invalid number. Using 8 lines.");
            lineCount = 8;
        }

        Console.WriteLine();

        Poem poem = await ConsoleProgressBar.RunAsync(
            "Creating your poem",
            service.GenerateAsync(mood, seedLines, lineCount));

        ShowPoem(poem);

        // Tell the user why the local fallback was used.
        if (service.LastError is not null)
        {
            Console.WriteLine();
            ConsoleUI.Warning(
                $"Gemini unavailable, local fallback used: " +
                service.LastError);
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Save this poem? (Y/N): ");
        Console.ResetColor();

        string? saveChoice = Console.ReadLine();

        if (string.Equals(
                saveChoice?.Trim(), "Y",
                StringComparison.OrdinalIgnoreCase))
        {
            Console.Write("Title (leave empty for default): ");
            string? title = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(title))
                poem.Title = title.Trim();

            await repository.SaveAsync(poem);

            ConsoleUI.Success("Poem saved.");
        }
        else
        {
            ConsoleUI.Muted("Poem was not saved.");
        }

        ConsoleUI.Pause();
    }

    internal static void ShowPoem(Poem poem)
    {
        ConsoleUI.Clear();
        ConsoleUI.Title(poem.Title);

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"Mood: {poem.Mood}");
        Console.ResetColor();

        ConsoleUI.Muted(
            $"Source: {poem.Source} | Lines: {poem.LineCount}");

        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(poem.Text);
        Console.ResetColor();
    }
}