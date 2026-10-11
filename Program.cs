using System.Text;
using Poems.Components;
using Poems.Services;

namespace Poems;

internal static class Program
{
    private static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        Console.Title = "Poem Creator";

        Console.CursorVisible = true;

        try
        {
            // Initialize the SQLite repository.
            var repository = new PoemRepository();
            await repository.InitializeAsync();

            // Initialize the poem generation service.
            var poemService = new PoemService();

            // Start the main application.
            var dashboard = new Dashboard(
                poemService,
                repository);

            await dashboard.RunAsync();
        }
        catch (Exception ex)
        {
            Console.ResetColor();
            Console.Clear();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("The application could not start.");
            Console.ResetColor();

            Console.WriteLine();
            Console.WriteLine(ex.Message);

#if DEBUG
            Console.WriteLine();
            Console.WriteLine(ex.StackTrace);
#endif

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
        }
        finally
        {
            Console.ResetColor();
            Console.CursorVisible = true;
        }
    }
}