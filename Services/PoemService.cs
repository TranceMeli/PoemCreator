
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

    public string ModelName { get; } = "gemini-2.5-flash";

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
            Path.Combine(
                AppContext.BaseDirectory,
                "appsettings.json"),

            Path.Combine(
                Directory.GetCurrentDirectory(),
                "appsettings.json")
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
            catch (JsonException)
            {
                // Try the next configuration file.
            }
            catch (IOException)
            {
                // Try the next configuration file.
            }
            catch (UnauthorizedAccessException)
            {
                // Try the next configuration file.
            }
            catch (InvalidOperationException)
            {
                // The API key has an unexpected JSON type.
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

        if (IsAiConfigured)
        {
            try
            {
                return await GenerateWithGeminiAsync(
                    mood,
                    seedLines,
                    lineCount);
            }
            catch (HttpRequestException)
            {
                // API unavailable or returned an unsuccessful status.
                // Continue with the local fallback.
            }
            catch (TaskCanceledException)
            {
                // Request timed out.
            }
            catch (JsonException)
            {
                // Invalid JSON returned by the API.
            }
            catch (InvalidOperationException)
            {
                // Unexpected API response structure or empty result.
            }
            catch (KeyNotFoundException)
            {
                // Expected properties are missing from the response.
            }
            catch (IndexOutOfRangeException)
            {
                // The API returned no candidate or text parts.
            }
        }

        return CreateFallbackPoem(
            mood,
            seedLines,
            lineCount);
    }

    private async Task<Poem> GenerateWithGeminiAsync(
        string mood,
        IReadOnlyList<string> seedLines,
        int lineCount)
    {
        string url =
            $"https://generativelanguage.googleapis.com/v1beta/" +
            $"models/{ModelName}:generateContent";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new
                        {
                            text =
                                $"Write an original poem in English.\n" +
                                $"Mood: {mood}\n" +
                                $"Exactly {lineCount} non-empty lines.\n" +
                                "Use these seed lines as inspiration and " +
                                "incorporate them naturally where possible:\n" +
                                string.Join("\n", seedLines) +
                                "\nReturn only the poem text, without a title."
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.9,
                maxOutputTokens = Math.Min(4096, lineCount * 45)
            }
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            url);

        request.Headers.Add(
            "x-goog-api-key",
            _apiKey);

        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response =
            await HttpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        string json =
            await response.Content.ReadAsStringAsync();

        using JsonDocument document =
            JsonDocument.Parse(json);

        string text = document.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException(
                "The API returned an empty poem.");
        }

        string[] lines = text
            .Split('\n', StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Take(lineCount)
            .ToArray();

        if (lines.Length == 0)
        {
            throw new InvalidOperationException(
                "No poem lines were returned.");
        }

        return new Poem
        {
            Title = $"{mood} Poem",
            Mood = mood,
            SeedLines = seedLines.ToList(),
            Text = string.Join(Environment.NewLine, lines),
            LineCount = lines.Length,
            Source = "Gemini AI",
            CreatedAt = DateTime.Now
        };
    }

    private static Poem CreateFallbackPoem(
        string mood,
        IReadOnlyList<string> seedLines,
        int lineCount)
    {
        IReadOnlyList<string> lines =
            FallbackPoems.GetRandomLines(
                mood,
                lineCount,
                seedLines);

        string text = string.Join(
            Environment.NewLine,
            lines);

        return new Poem
        {
            Title = $"{mood} Poem",
            Mood = mood,
            SeedLines = seedLines.ToList(),
            Text = text,
            LineCount = lines.Count,
            Source = "Local fallback",
            CreatedAt = DateTime.Now
        };
    }
}
