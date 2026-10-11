namespace Poems.Components;

public static class ConsoleUI
{
    public static void Clear()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        Console.WriteLine("╔══════════════════════════════════════════════╗");
        Console.WriteLine("║                 POEM CREATOR                 ║");
        Console.WriteLine("╚══════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
    }

    public static void Title(string title)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"━━ {title} ━━");
        Console.ResetColor();
        Console.WriteLine();
    }

    public static void Success(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[OK] {message}");
        Console.ResetColor();
    }

    public static void Warning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[!] {message}");
        Console.ResetColor();
    }

    public static void Error(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERROR] {message}");
        Console.ResetColor();
    }

    public static void Muted(string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public static void Pause()
    {
        Console.WriteLine();
        Muted("Press any key to continue...");
        Console.ReadKey(true);
    }

    public static int Select(
        string title,
        IReadOnlyList<string> options)
    {
        if (options.Count == 0)
            return -1;

        int selected = 0;

        while (true)
        {
            Clear();
            Title(title);

            Console.WriteLine("Use ↑/↓ to navigate, Enter to select.");
            Muted("Press Esc to go back.");
            Console.WriteLine();

            for (int i = 0; i < options.Count; i++)
            {
                if (i == selected)
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.Cyan;
                    Console.Write($"  > {options[i]}  ");
                    Console.ResetColor();
                }
                else
                {
                    Console.Write($"    {options[i]}");
                }

                Console.WriteLine();
            }

            ConsoleKey key = Console.ReadKey(true).Key;

            switch (key)
            {
                case ConsoleKey.UpArrow:
                    selected = (selected - 1 + options.Count)
                        % options.Count;
                    break;

                case ConsoleKey.DownArrow:
                    selected = (selected + 1) % options.Count;
                    break;

                case ConsoleKey.Enter:
                    return selected;

                case ConsoleKey.Escape:
                    return -1;

                default:
                    if (key >= ConsoleKey.D1 &&
                        key <= ConsoleKey.D9)
                    {
                        int index =
                            (int)key - (int)ConsoleKey.D1;

                        if (index < options.Count)
                            return index;
                    }
                    break;
            }
        }
    }

    public static List<string> SelectMultiple(
        string title,
        IReadOnlyList<string> options)
    {
        var selected = new bool[options.Count];
        int cursor = 0;

        while (true)
        {
            Clear();
            Title(title);

            Console.WriteLine(
                "↑/↓ Navigate | Space Toggle | Enter Confirm");

            Muted("Esc cancels the selection.");
            Console.WriteLine();

            for (int i = 0; i < options.Count; i++)
            {
                if (i == cursor)
                    Console.ForegroundColor = ConsoleColor.Cyan;

                Console.WriteLine(
                    $"{(i == cursor ? ">" : " ")} " +
                    $"[{(selected[i] ? "x" : " ")}] {options[i]}");

                Console.ResetColor();
            }

            ConsoleKey key = Console.ReadKey(true).Key;

            switch (key)
            {
                case ConsoleKey.UpArrow:
                    cursor = (cursor - 1 + options.Count)
                        % options.Count;
                    break;

                case ConsoleKey.DownArrow:
                    cursor = (cursor + 1) % options.Count;
                    break;

                case ConsoleKey.Spacebar:
                    selected[cursor] = !selected[cursor];
                    break;

                case ConsoleKey.Enter:
                    return options
                        .Where((_, i) => selected[i])
                        .ToList();

                case ConsoleKey.Escape:
                    return [];
            }
        }
    }
}