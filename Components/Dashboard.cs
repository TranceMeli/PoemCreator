using Poems.Services;

namespace Poems.Components;

public sealed class Dashboard(
    PoemService poemService,
    PoemRepository repository)
{
    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            ConsoleUI.Clear();
            ConsoleUI.Title("Your poetry workspace");

            Console.WriteLine(
                "Create poems inspired by moods and seed lines.");
            Console.WriteLine();

            Console.Write("AI status: ");

            Console.ForegroundColor = poemService.IsAiConfigured
                ? ConsoleColor.Green
                : ConsoleColor.Yellow;

            Console.WriteLine(poemService.IsAiConfigured
                ? $"Gemini ({poemService.ModelName})"
                : "Local fallback active");

            Console.ResetColor();

            Console.WriteLine($"Saved poems: {repository.SavedCount}");
            Console.WriteLine();

            string[] options =
            [
                "Generate poem",
                "Browse collections",
                "Saved poems",
                "Settings",
                "Exit"
            ];

            int choice = ConsoleUI.Select("MAIN MENU", options);

            switch (choice)
            {
                case 0:
                    await new GeneratePoem()
                        .ShowAsync(poemService, repository);
                    break;

                case 1:
                    new Collections().Show(poemService);
                    break;

                case 2:
                    await new SavedPoemsView()
                        .ShowAsync(repository);
                    break;

                case 3:
                    new SettingsView()
                        .Show(poemService, repository);
                    break;

                default:
                    running = false;
                    break;
            }
        }

        ConsoleUI.Clear();
        ConsoleUI.Success("Thank you for creating with Poem Creator.");
    }
}