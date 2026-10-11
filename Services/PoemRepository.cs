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
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();
    }

    // Finds the project directory containing the .csproj file.
    // Falls back to %LOCALAPPDATA%\Poems (e.g. for published builds).
    private static string FindProjectDirectory()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (directory.GetFiles("*.csproj").Length > 0)
                return directory.FullName;

            directory = directory.Parent;
        }

        return Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "Poems");
    }

    private SqliteConnection CreateConnection() =>
        new(_connectionString);

    // Creates the table and migrates older database schemas.
    // Writes nothing to the console: the dashboard clears the screen
    // right afterwards, so the output would never be seen.
    public async Task InitializeAsync()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var transaction =
            (SqliteTransaction)await connection.BeginTransactionAsync();

        await using (var create = connection.CreateCommand())
        {
            create.Transaction = transaction;
            create.CommandText =
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
            await create.ExecuteNonQueryAsync();
        }

        HashSet<string> columns =
            await GetColumnNamesAsync(connection, transaction);

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

            await using var alter = connection.CreateCommand();
            alter.Transaction = transaction;

            // Column names come only from the fixed list above.
            alter.CommandText =
                $"ALTER TABLE Poems ADD COLUMN {column.Name} " +
                $"{column.Definition};";

            await alter.ExecuteNonQueryAsync();
        }

        // Repair empty values in records from older versions.
        await using (var repair = connection.CreateCommand())
        {
            repair.Transaction = transaction;
            repair.CommandText =
                """
                UPDATE Poems SET Title = 'Untitled'
                WHERE Title IS NULL OR TRIM(Title) = '';

                UPDATE Poems SET Mood = 'Peaceful'
                WHERE Mood IS NULL OR TRIM(Mood) = '';

                UPDATE Poems SET SeedLinesJson = '[]'
                WHERE SeedLinesJson IS NULL OR TRIM(SeedLinesJson) = '';

                UPDATE Poems SET Text = ''
                WHERE Text IS NULL;

                UPDATE Poems SET LineCount = 0
                WHERE LineCount IS NULL;

                UPDATE Poems SET Source = 'Local fallback'
                WHERE Source IS NULL OR TRIM(Source) = '';

                UPDATE Poems SET CreatedAt = $now
                WHERE CreatedAt IS NULL OR TRIM(CreatedAt) = '';
                """;

            repair.Parameters.AddWithValue(
                "$now",
                DateTime.Now.ToString("O"));

            await repair.ExecuteNonQueryAsync();
        }

        await using (var count = connection.CreateCommand())
        {
            count.Transaction = transaction;
            count.CommandText = "SELECT COUNT(*) FROM Poems;";

            SavedCount = Convert.ToInt32(
                await count.ExecuteScalarAsync());
        }

        await transaction.CommitAsync();
    }

    private static async Task<HashSet<string>> GetColumnNamesAsync(
        SqliteConnection connection,
        SqliteTransaction transaction)
    {
        var columns = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "PRAGMA table_info('Poems');";

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
            columns.Add(reader.GetString(1));

        return columns;
    }

    public async Task SaveAsync(Poem poem)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO Poems
                (Title, Mood, SeedLinesJson, Text,
                 LineCount, Source, CreatedAt)
            VALUES
                ($title, $mood, $seeds, $text,
                 $lineCount, $source, $createdAt);

            SELECT last_insert_rowid();
            """;

        command.Parameters.AddWithValue("$title", poem.Title);
        command.Parameters.AddWithValue("$mood", poem.Mood);
        command.Parameters.AddWithValue(
            "$seeds", JsonSerializer.Serialize(poem.SeedLines));
        command.Parameters.AddWithValue("$text", poem.Text);
        command.Parameters.AddWithValue("$lineCount", poem.LineCount);
        command.Parameters.AddWithValue("$source", poem.Source);
        command.Parameters.AddWithValue(
            "$createdAt", poem.CreatedAt.ToString("O"));

        poem.Id = Convert.ToInt32(await command.ExecuteScalarAsync());
        SavedCount++;
    }

    // Returns all poems, newest first.
    public async Task<List<Poem>> GetAllAsync()
    {
        var poems = new List<Poem>();

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Title, Mood, SeedLinesJson,
                   Text, LineCount, Source, CreatedAt
            FROM Poems
            ORDER BY CreatedAt DESC, Id DESC;
            """;

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
            poems.Add(ReadPoem(reader));

        return poems;
    }

    public async Task<Poem?> GetByIdAsync(int id)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Title, Mood, SeedLinesJson,
                   Text, LineCount, Source, CreatedAt
            FROM Poems
            WHERE Id = $id;
            """;

        command.Parameters.AddWithValue("$id", id);

        await using var reader = await command.ExecuteReaderAsync();

        return await reader.ReadAsync()
            ? ReadPoem(reader)
            : null;
    }

    public async Task DeleteAsync(int id)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Poems WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);

        int deleted = await command.ExecuteNonQueryAsync();
        SavedCount = Math.Max(0, SavedCount - deleted);
    }

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
            Title = reader.IsDBNull(1) ? "Untitled" : reader.GetString(1),
            Mood = reader.IsDBNull(2) ? "Peaceful" : reader.GetString(2),
            SeedLines = seedLines,
            Text = text,
            LineCount = reader.IsDBNull(5)
                ? text.Split(
                    '\n', StringSplitOptions.RemoveEmptyEntries).Length
                : reader.GetInt32(5),
            Source = reader.IsDBNull(6)
                ? "Local fallback"
                : reader.GetString(6),
            CreatedAt = createdAt
        };
    }
}