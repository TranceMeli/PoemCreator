
using Poems.Models;
using Poems.Services;

namespace Poems.Components;

public sealed class SavedPoemsView
{
    public async Task ShowAsync(PoemRepository repository)
    {
        List<Poem> poems = await repository.GetAllAsync();

        ConsoleUI.Clear();
        ConsoleUI.Title("SAVED POEMS");

        if (poems.Count == 0)
        {
            ConsoleUI.Warning("No saved poems found.");
            ConsoleUI.Pause();
            return;
        }

        string[] options = poems
            .Select(poem =>
                $"{poem.Title} | {poem.Mood} | " +
                $"{poem.CreatedAt:yyyy-MM-dd HH:mm}")
            .ToArray();

        int selectedIndex = ConsoleUI.Select(
            "SELECT A POEM",
            options);

        if (selectedIndex < 0 || selectedIndex >= poems.Count)
            return;

        Poem selectedPoem = poems[selectedIndex];

        if (string.IsNullOrWhiteSpace(selectedPoem.Text))
        {
            ConsoleUI.Clear();
            ConsoleUI.Title(selectedPoem.Title);

            ConsoleUI.Warning(
                "This database entry contains no poem text.");

            Console.WriteLine();
            ConsoleUI.Muted($"Poem ID: {selectedPoem.Id}");
            ConsoleUI.Muted($"Mood: {selectedPoem.Mood}");
            ConsoleUI.Muted($"Source: {selectedPoem.Source}");

            Console.WriteLine();
            ConsoleUI.Muted(
                "Older database records may have been migrated " +
                "without their original text.");

            ConsoleUI.Pause();
            return;
        }

        GeneratePoem.ShowPoem(selectedPoem);

        ConsoleUI.Pause();
    }
}
