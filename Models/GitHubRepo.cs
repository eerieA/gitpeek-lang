namespace gitpeek_lang.Models;

public class GitHubRepo
{
    public required string Name { get; set; }
    public string? Language { get; set; } // Language is nullable
}