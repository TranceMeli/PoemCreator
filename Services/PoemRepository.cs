
using Microsoft.Data.Sqlite;
using System.Globalization;
using System.Text.Json;
using Poems.Models;

namespace Poems.Services;

public sealed class PoemRepository
{
    private readonly string _databasePath;
    private readonly string _connectionString;

    public int SavedCount { get; private set; }

    public string DatabasePath => _databasePath;

    public PoemRepository()
    {
        string directory = FindProjectDirectory();

        Directory.CreateDirectory(directory);

        _databasePath = Path.Combine(directory, "poems.db");

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath
        }.ToString();
    }

    // Finds the project directory containing the .csproj file.
    private static string FindProjectDirectory()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (directory.GetFiles("*.csproj").Length > 0)
                return directory.FullName;

            directory = directory.Parent;
        }

        // Fallback if the project directory cannot be found.
        return Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "Poems");
    }

    private SqliteConnection CreateConnection() =>
        new(_connectionString);

    // Creates the table and migrates older database schemas.
    public async Task InitializeAsync()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        Console.WriteLine($"Database path: {_databasePath}");

        var createCommand = connection.CreateCommand();
        createCommand.CommandText =
            """
            CREATE TABLE IF NOT EXISTS Poems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Mood TEXT NOT NULL,
                SeedLinesJson TEXT NOT NULL,
                Text TEXT NOT NULL,
                LineCount INTEGER NOT NULL,
                Source TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );
            """;

        await createCommand.ExecuteNonQueryAsync();

        // Inspect the existing database table.
        HashSet<string> columns =
            await GetColumnNamesAsync(connection);

        if (!columns.Contains("Id"))
        {
            throw new InvalidOperationException(
                "The existing Poems table has no Id column. " +
                "Back up the database before migrating this schema.");
        }

        // Add any columns missing from an older schema.
        (string Name, string Definition)[] requiredColumns =
        [
            ("Title", "TEXT NOT NULL DEFAULT 'Untitled'"),
            ("Mood", "TEXT NOT NULL DEFAULT 'Peaceful'"),
            ("SeedLinesJson", "TEXT NOT NULL DEFAULT '[]'"),
            ("Text", "TEXT NOT NULL DEFAULT ''"),
            ("LineCount", "INTEGER NOT NULL DEFAULT 0"),
            ("Source", "TEXT NOT NULL DEFAULT 'Local fallback'"),
            ("CreatedAt", "TEXT NOT NULL DEFAULT ''")
        ];

        foreach (var column in requiredColumns)
        {
            if (columns.Contains(column.Name))
                continue;

            var alterCommand = connection.CreateCommand();
            alterCommand.CommandText =
                $"ALTER TABLE Poems ADD COLUMN {column.Name} " +
                $"{column.Definition};";

            await alterCommand.ExecuteNonQueryAsync();

            columns.Add(column.Name);
        }

        // Repair missing or empty values in existing records.
        var repairCommand = connection.CreateCommand();
        repairCommand.CommandText =
            """
            UPDATE Poems
            SET Title = 'Untitled'
            WHERE Title IS NULL OR TRIM(Title) = '';

            UPDATE Poems
            SET Mood = 'Peaceful'
            WHERE Mood IS NULL OR TRIM(Mood) = '';

            UPDATE Poems
            SET SeedLinesJson = '[]'
            WHERE SeedLinesJson IS NULL
               OR TRIM(SeedLinesJson) = '';

            UPDATE Poems
            SET Text = ''
            WHERE Text IS NULL;

            UPDATE Poems
            SET LineCount = 0
            WHERE LineCount IS NULL;

            UPDATE Poems
            SET Source = 'Local fallback'
            WHERE Source IS NULL OR TRIM(Source) = '';

            UPDATE Poems
            SET CreatedAt = $now
            WHERE CreatedAt IS NULL OR TRIM(CreatedAt) = '';
            """;

        repairCommand.Parameters.AddWithValue(
            "$now",
            DateTime.Now.ToString("O"));

        await repairCommand.ExecuteNonQueryAsync();

        // Verify that the table exists.
        var checkCommand = connection.CreateCommand();
        checkCommand.CommandText =
            """
            SELECT COUNT(*)
            FROM sqlite_master
            WHERE type = 'table'
              AND name = 'Poems';
            """;

        long tableExists = Convert.ToInt64(
            await checkCommand.ExecuteScalarAsync());

        if (tableExists == 0)
        {
            throw new InvalidOperationException(
                "The Poems table could not be created.");
        }

        // Refresh the number of saved poems.
        var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM Poems;";

        SavedCount = Convert.ToInt32(
            await countCommand.ExecuteScalarAsync());

        Console.WriteLine(
            "Database table 'Poems' initialized successfully.");
    }

    // Returns the existing column names.
    private static async Task<HashSet<string>> GetColumnNamesAsync(
        SqliteConnection connection)
    {
        var columns = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('Poems');";

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(1));
        }

        return columns;
    }

    // Saves a poem.
    public async Task SaveAsync(Poem poem)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO Poems
                (Title, Mood, SeedLinesJson, Text,
                 LineCount, Source, CreatedAt)
            VALUES
                ($title, $mood, $seeds, $text,
                 $lineCount, $source, $createdAt);
            """;

        command.Parameters.AddWithValue(
            "$title", poem.Title);

        command.Parameters.AddWithValue(
            "$mood", poem.Mood);

        command.Parameters.AddWithValue(
            "$seeds", JsonSerializer.Serialize(poem.SeedLines));

        command.Parameters.AddWithValue(
            "$text", poem.Text);

        command.Parameters.AddWithValue(
            "$lineCount", poem.LineCount);

        command.Parameters.AddWithValue(
            "$source", poem.Source);

        command.Parameters.AddWithValue(
            "$createdAt", poem.CreatedAt.ToString("O"));

        await command.ExecuteNonQueryAsync();

        var idCommand = connection.CreateCommand();
        idCommand.CommandText = "SELECT last_insert_rowid();";

        poem.Id = Convert.ToInt32(
            await idCommand.ExecuteScalarAsync());

        await RefreshCountAsync();
    }

    // Returns all poems, newest first.
    public async Task<List<Poem>> GetAllAsync()
    {
        var poems = new List<Poem>();

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Title, Mood, SeedLinesJson,
                   Text, LineCount, Source, CreatedAt
            FROM Poems
            ORDER BY CreatedAt DESC;
            """;

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            poems.Add(ReadPoem(reader));
        }

        return poems;
    }

    // Returns one poem by ID.
    public async Task<Poem?> GetByIdAsync(int id)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Title, Mood, SeedLinesJson,
                   Text, LineCount, Source, CreatedAt
            FROM Poems
            WHERE Id = $id;
            """;

        command.Parameters.AddWithValue("$id", id);

        await using var reader =
            await command.ExecuteReaderAsync();

        return await reader.ReadAsync()
            ? ReadPoem(reader)
            : null;
    }

    // Deletes a poem by ID.
    public async Task DeleteAsync(int id)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Poems WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);

        await command.ExecuteNonQueryAsync();

        await RefreshCountAsync();
    }

    // Refreshes the saved poem count.
    private async Task RefreshCountAsync()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Poems;";

        SavedCount = Convert.ToInt32(
            await command.ExecuteScalarAsync());
    }

    // Converts a database record into a Poem object.
    private static Poem ReadPoem(SqliteDataReader reader)
    {
        string text = reader.IsDBNull(4)
            ? string.Empty
            : reader.GetString(4);

        List<string> seedLines = [];

        if (!reader.IsDBNull(3))
        {
            try
            {
                seedLines =
                    JsonSerializer.Deserialize<List<string>>(
                        reader.GetString(3)) ?? [];
            }
            catch (JsonException)
            {
                seedLines = [];
            }
        }

        DateTime createdAt = DateTime.Now;

        if (!reader.IsDBNull(7) &&
            DateTime.TryParse(
                reader.GetString(7),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTime parsedDate))
        {
            createdAt = parsedDate;
        }

        return new Poem
        {
            Id = reader.GetInt32(0),

            Title = reader.IsDBNull(1)
                ? "Untitled"
                : reader.GetString(1),

            Mood = reader.IsDBNull(2)
                ? "Peaceful"
                : reader.GetString(2),

            SeedLines = seedLines,
            Text = text,

            LineCount = reader.IsDBNull(5)
                ? text.Split(
                    '\n',
                    StringSplitOptions.RemoveEmptyEntries).Length
                : reader.GetInt32(5),

            Source = reader.IsDBNull(6)
                ? "Local fallback"
                : reader.GetString(6),

            CreatedAt = createdAt
        };
    }
}
