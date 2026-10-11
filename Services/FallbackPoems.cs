namespace Poems.Services;

public static class FallbackPoems
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>>
        Collections =
        new Dictionary<string, IReadOnlyList<string>>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["Melancholic"] =
            [
                "The rain remembers what I tried to forget",
                "An empty room still echoes with your name",
                "The evening folds its shadows into blue",
                "Some goodbyes never learn to end",
                "Old photographs grow quiet in the dark",
                "The clock moves on, but I remain",
                "Your laughter lingers in the hallway",
                "Autumn leaves collect the words we lost",
                "I keep the silence where your voice once lived",
                "Even fading stars leave traces in the sky"
            ],

            ["Hopeful"] =
            [
                "A little light is finding its way through",
                "Tomorrow waits beyond the open door",
                "Even broken seeds remember how to grow",
                "The morning gives the world another chance",
                "Small steps can lead us somewhere new",
                "The darkest night still ends in dawn",
                "A quiet spark can wake a sleeping flame",
                "Every falling leaf prepares the spring",
                "There is still a road beyond the rain",
                "I choose to believe in what comes next"
            ],

            ["Romantic"] =
            [
                "Your laughter turns the ordinary gold",
                "The stars lean closer when you speak",
                "I find my home inside your eyes",
                "Two quiet hearts can change the weather",
                "Your hand fits softly into mine",
                "The world grows still whenever you are near",
                "I would choose your smile a thousand times",
                "Our shadows dance beneath the evening sky",
                "You make the smallest moments feel like home",
                "My favorite place is anywhere with you"
            ],

            ["Dreamlike"] =
            [
                "The moon left silver footprints on the sea",
                "A paper bird flew through my sleep",
                "The clocks dissolved like sugar in the rain",
                "We sailed across the gardens of the sky",
                "Blue butterflies carried yesterday away",
                "The clouds grew roots beneath the stars",
                "A thousand doors opened inside a dream",
                "The ocean whispered secrets to the moon",
                "We danced on rings of Saturn's light",
                "The sunrise bloomed beneath the earth"
            ],

            ["Dark"] =
            [
                "The shadows know the shape of every fear",
                "A cold wind moves behind the walls",
                "The last candle trembles in the dark",
                "Beneath the silence something stirs",
                "Black branches scratch the face of night",
                "The hallway stretches farther than before",
                "A distant whisper calls without a name",
                "The moon hides slowly behind the clouds",
                "Old secrets sleep beneath the floor",
                "Still, one small ember refuses to die"
            ],

            ["Peaceful"] =
            [
                "The lake lies still beneath the sky",
                "Soft light rests upon the windowsill",
                "The trees breathe slowly in the wind",
                "For once, the world asks nothing more",
                "A cup of tea warms both my hands",
                "The morning drifts across the quiet room",
                "Birdsong settles gently on the air",
                "The clouds move slowly over fields of green",
                "I let the passing hours come and go",
                "And find a little peace in being here"
            ]
        };

    public static IReadOnlyList<string> GetRandomLines(
        string mood,
        int lineCount,
        IEnumerable<string>? seedLines = null)
    {
        if (!Collections.TryGetValue(mood, out var collection))
            collection = Collections["Peaceful"];

        lineCount = Math.Clamp(lineCount, 1, 80);

        var result = collection
            .OrderBy(_ => Random.Shared.Next())
            .Take(Math.Min(lineCount, collection.Count))
            .ToList();

        if (seedLines is not null)
        {
            foreach (string seed in seedLines
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (result.Count >= lineCount)
                    break;

                result.Add(seed.Trim());
            }
        }

        // Für Gedichte mit mehr als zehn Zeilen weitere
        // zufällige Zeilen aus der Sammlung wiederverwenden.
        while (result.Count < lineCount)
        {
            result.Add(collection[
                Random.Shared.Next(collection.Count)]);
        }

        return result
            .OrderBy(_ => Random.Shared.Next())
            .ToList();
    }
}