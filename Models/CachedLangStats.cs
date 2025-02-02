namespace gitpeek_lang.Models;

public class CachedLangStats
{
    public Dictionary<string, long>? Stats { get; set; }
    public Dictionary<string, string>? RateLimitInfo { get; set; }
}