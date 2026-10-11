
using System.Diagnostics;

namespace Poems.Components;

public static class ConsoleProgressBar
{
    private const int BarWidth = 24;
    private const int DefaultCountdownSeconds = 20;

    private static readonly TimeSpan MinimumDisplayDuration =
        TimeSpan.FromSeconds(5);

    public static async Task<T> RunAsync<T>(
        string message,
        Task<T> operation,
        int countdownSeconds = DefaultCountdownSeconds)
    {
        ArgumentNullException.ThrowIfNull(operation);

        countdownSeconds = Math.Max(1, countdownSeconds);

        var stopwatch = Stopwatch.StartNew();
        bool previousCursorVisible = Console.CursorVisible;

        try
        {
            Console.CursorVisible = false;

            // Keep displaying the progress bar while the task runs.
            // Successful fast tasks stay on screen for at least 5 seconds.
            while (!operation.IsCompleted ||
                   (operation.IsCompletedSuccessfully &&
                    stopwatch.Elapsed < MinimumDisplayDuration))
            {
                double remaining =
                    countdownSeconds - stopwatch.Elapsed.TotalSeconds;

                int remainingSeconds = (int)Math.Ceiling(
                    Math.Max(0, remaining));

                int filledBlocks = (int)Math.Ceiling(
                    remainingSeconds / (double)countdownSeconds
                    * BarWidth);

                filledBlocks = Math.Clamp(
                    filledBlocks, 0, BarWidth);

                string bar =
                    new string('█', filledBlocks) +
                    new string('░', BarWidth - filledBlocks);

                string status = remainingSeconds > 0
                    ? $"{remainingSeconds,2}s remaining"
                    : "Still waiting...";

                Console.Write('\r');

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(message);
                Console.ResetColor();

                Console.Write($" [{bar}] {status}     ");

                await Task.Delay(100);
            }

            // Retrieve the generated poem or propagate the exception.
            T result = await operation;

            Console.Write('\r');

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(message);
            Console.Write($" [{new string('█', BarWidth)}]");
            Console.ResetColor();

            Console.WriteLine(" Complete!                    ");

            return result;
        }
        catch
        {
            Console.Write('\r');

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(message);
            Console.Write($" [{new string('░', BarWidth)}]");
            Console.ResetColor();

            Console.WriteLine(" Failed.                      ");

            throw;
        }
        finally
        {
            Console.ResetColor();
            Console.CursorVisible = previousCursorVisible;
        }
    }
}
