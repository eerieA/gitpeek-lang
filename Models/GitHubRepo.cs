namespace gitpeek_lang.Models;

public class GitHubOwner
{
    public required string Login { get; set; } // "login" field from JSON
}

public class GitHubRepo
{
    public required string Name { get; set; }
    public string? Language { get; set; } // Language is nullable
    public required GitHubOwner Owner { get; set; } // "owner" field from JSON
}
