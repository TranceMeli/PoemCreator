using Poems.Models;
using Poems.Services;

namespace Poems.Components;

public sealed class SavedPoemsView
{
    public async Task ShowAsync(PoemRepository repository)
    {
        while (true)
        {
            List<Poem> poems = await repository.GetAllAsync();

            if (poems.Count == 0)
            {
                ConsoleUI.Clear();
                ConsoleUI.Title("SAVED POEMS");
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
                "SAVED POEMS",
                options);

            if (selectedIndex < 0 || selectedIndex >= poems.Count)
                return;

            Poem poem = poems[selectedIndex];

            if (string.IsNullOrWhiteSpace(poem.Text))
            {
                ConsoleUI.Clear();
                ConsoleUI.Title(poem.Title);
                ConsoleUI.Warning(
                    "This database entry contains no poem text.");
                Console.WriteLine();
                ConsoleUI.Muted($"Poem ID: {poem.Id}");
                ConsoleUI.Muted($"Mood: {poem.Mood}");
                ConsoleUI.Muted($"Source: {poem.Source}");
            }
            else
            {
                GeneratePoem.ShowPoem(poem);
            }

            // ConsoleUI.Select clears the screen, so the actions are
            // offered as a key prompt to keep the poem visible.
            Console.WriteLine();
            ConsoleUI.Muted("Press D to delete this poem, any other key to go back.");

            ConsoleKey key = Console.ReadKey(true).Key;

            if (key == ConsoleKey.D &&
                ConsoleUI.Select(
                    "DELETE THIS POEM?",
                    ["Cancel", "Delete"]) == 1)
            {
                await repository.DeleteAsync(poem.Id);
                ConsoleUI.Success("Poem deleted.");
                ConsoleUI.Pause();
            }
        }
    }
}