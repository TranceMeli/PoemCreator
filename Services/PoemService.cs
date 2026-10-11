using System.Text;
using System.Text.Json;
using Poems.Models;

namespace Poems.Services;

public sealed class PoemService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(45)
    };

    private readonly string _apiKey;

    public string ModelName { get; } = "gemini-3.8-flash";

    // Why the last GenerateAsync call used the local fallback.
    // Null when Gemini answered or when no API key is configured.
    public string? LastError { get; private set; }

    public bool IsAiConfigured =>
        !string.IsNullOrWhiteSpace(_apiKey) &&
        !_apiKey.Equals(
            "DEIN_GEMINI_API_KEY",
            StringComparison.OrdinalIgnoreCase) &&
        !_apiKey.Equals(
            "HIER_DEINEN_GEMINI_API_KEY_EINTRAGEN",
            StringComparison.OrdinalIgnoreCase);

    public PoemService()
    {
        string? environmentKey =
            Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        _apiKey = !string.IsNullOrWhiteSpace(environmentKey)
            ? environmentKey.Trim()
            : ReadApiKey();
    }

    private static string ReadApiKey()
    {
        string[] paths =
        [
            Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")
        ];

        foreach (string path in paths.Distinct())
        {
            if (!File.Exists(path))
                continue;

            try
            {
                using JsonDocument document =
                    JsonDocument.Parse(File.ReadAllText(path));

                if (document.RootElement.TryGetProperty(
                        "Gemini", out JsonElement gemini) &&
                    gemini.TryGetProperty(
                        "ApiKey", out JsonElement apiKey))
                {
                    string? value = apiKey.GetString();

                    if (!string.IsNullOrWhiteSpace(value))
                        return value.Trim();
                }
            }
            catch (Exception ex) when (
                ex is JsonException or IOException
                    or UnauthorizedAccessException
                    or InvalidOperationException)
            {
                // Unreadable file or unexpected JSON: try the next path.
            }
        }

        return string.Empty;
    }

    public async Task<Poem> GenerateAsync(
        string mood,
        IReadOnlyList<string> seedLines,
        int lineCount)
    {
        lineCount = Math.Clamp(lineCount, 1, 80);
        LastError = null;

        if (IsAiConfigured)
        {
            try
            {
                return await GenerateWithGeminiAsync(
                    mood,
                    seedLines,
                    lineCount);
            }
            catch (Exception ex) when (
                ex is HttpRequestException or TaskCanceledException
                    or JsonException or InvalidOperationException)
            {
                // Remember the reason; the UI shows it after the poem,
                // because nothing written to the console during the
                // progress bar survives the next screen clear.
                LastError = ex is TaskCanceledException
                    ? "The Gemini request timed out."
                    : ex.Message;
            }
        }

        return CreateFallbackPoem(mood, seedLines, lineCount);
    }

    private async Task<Poem> GenerateWithGeminiAsync(
        string mood,
        IReadOnlyList<string> seedLines,
        int lineCount)
    {
        string url =
            "https://generativelanguage.googleapis.com/v1beta/" +
            $"models/{ModelName}:generateContent";

        var prompt = new StringBuilder();
        prompt.AppendLine("Write an original poem in English.");
        prompt.AppendLine($"Mood: {mood}");
        prompt.AppendLine($"Exactly {lineCount} non-empty lines.");

        if (seedLines.Count > 0)
        {
            prompt.AppendLine(
                "Use these seed lines as inspiration and " +
                "incorporate them naturally where possible:");

            foreach (string seed in seedLines)
                prompt.AppendLine(seed);
        }

        prompt.Append(
            "Return only the poem text, without a title, " +
            "markdown or commentary.");

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt.ToString() } }
                }
            },
            generationConfig = new
            {
                temperature = 0.9,
                // Generous limit: thinking models count their internal
                // reasoning tokens against this budget, and a tight limit
                // leaves no room for the poem itself (empty answer).
                maxOutputTokens = 8192
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("x-goog-api-key", _apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response =
            await HttpClient.SendAsync(request);

        string json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"HTTP {(int)response.StatusCode} " +
                $"({response.StatusCode}): {ReadErrorMessage(json)}");
        }

        using JsonDocument document = JsonDocument.Parse(json);

        string text = ExtractText(document.RootElement);

        string[] lines = text
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.TrimEntries)
            .Where(line => line.Length > 0 && !line.StartsWith("```"))
            .ToArray();

        if (lines.Length < lineCount)
        {
            throw new InvalidOperationException(
                $"Gemini returned only {lines.Length} of " +
                $"{lineCount} requested lines.");
        }

        lines = lines.Take(lineCount).ToArray();

        return new Poem
        {
            Title = $"{mood} Poem",
            Mood = mood,
            SeedLines = seedLines.ToList(),
            Text = string.Join(Environment.NewLine, lines),
            LineCount = lines.Length,
            Source = "Gemini API",
            CreatedAt = DateTime.Now
        };
    }

    // Joins all text parts of the first candidate and explains
    // why there is no text (blocked prompt, token limit, ...).
    private static string ExtractText(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out JsonElement candidates) ||
            candidates.GetArrayLength() == 0)
        {
            string reason =
                root.TryGetProperty("promptFeedback", out JsonElement feedback) &&
                feedback.TryGetProperty("blockReason", out JsonElement block)
                    ? block.GetString() ?? "unknown"
                    : "no candidates";

            throw new InvalidOperationException(
                $"Gemini returned no poem ({reason}).");
        }

        JsonElement candidate = candidates[0];
        var text = new StringBuilder();

        if (candidate.TryGetProperty("content", out JsonElement content) &&
            content.TryGetProperty("parts", out JsonElement parts))
        {
            foreach (JsonElement part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("thought", out JsonElement thought) &&
                    thought.ValueKind == JsonValueKind.True)
                    continue;

                if (part.TryGetProperty("text", out JsonElement value))
                    text.Append(value.GetString());
            }
        }

        if (string.IsNullOrWhiteSpace(text.ToString()))
        {
            string finish =
                candidate.TryGetProperty("finishReason", out JsonElement f)
                    ? f.GetString() ?? "unknown"
                    : "unknown";

            throw new InvalidOperationException(
                $"Gemini returned an empty poem (finishReason: {finish}).");
        }

        return text.ToString();
    }

    private static string ReadErrorMessage(string json)
    {
        try
        {
            using JsonDocument errorDocument = JsonDocument.Parse(json);

            if (errorDocument.RootElement.TryGetProperty(
                    "error", out JsonElement error) &&
                error.TryGetProperty(
                    "message", out JsonElement message))
            {
                return message.GetString() ?? json;
            }
        }
        catch (Exception ex) when (
            ex is JsonException or InvalidOperationException)
        {
            // Not the usual error format: show the raw body below.
        }

        return json.Length > 300 ? json[..300] : json;
    }

    private static Poem CreateFallbackPoem(
        string mood,
        IReadOnlyList<string> seedLines,
        int lineCount)
    {
        IReadOnlyList<string> lines =
            FallbackPoems.GetRandomLines(mood, lineCount, seedLines);

        return new Poem
        {
            Title = $"{mood} Poem",
            Mood = mood,
            SeedLines = seedLines.ToList(),
            Text = string.Join(Environment.NewLine, lines),
            LineCount = lines.Count,
            Source = "Local fallback",
            CreatedAt = DateTime.Now
        };
    }
}