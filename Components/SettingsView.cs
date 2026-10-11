using Poems.Services;

namespace Poems.Components;

public sealed class SettingsView
{
    public void Show(
        PoemService service,
        PoemRepository repository)
    {
        ConsoleUI.Clear();
        ConsoleUI.Title("SETTINGS");

        Console.WriteLine("Application: Poem Creator");
        Console.WriteLine("Interface: System.Console");
        Console.WriteLine($"AI model: {service.ModelName}");

        Console.Write("Gemini API: ");

        if (service.IsAiConfigured)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Configured");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Not configured; using local fallback");
        }

        Console.ResetColor();

        Console.WriteLine($"Saved poems: {repository.SavedCount}");
        Console.WriteLine($"Database: {repository.DatabasePath}");

        Console.WriteLine();
        ConsoleUI.Muted(
            "Configure GEMINI_API_KEY to enable AI generation.");

        ConsoleUI.Pause();
    }
}