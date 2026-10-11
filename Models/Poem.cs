
namespace Poems.Models;

public sealed class Poem
{
    public int Id { get; set; }

    public string Title { get; set; } = "Untitled";

    public string Mood { get; set; } = "Peaceful";

    public List<string> SeedLines { get; set; } = [];

    public string Text { get; set; } = string.Empty;

    public int LineCount { get; set; }

    public string Source { get; set; } = "Local fallback";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
