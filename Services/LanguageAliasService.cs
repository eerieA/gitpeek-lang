using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace gitpeek_lang.Services;

public class LanguageAliasService
{
    private static readonly string AliasFilePath = Path.Combine(AppContext.BaseDirectory, "Assets/aliases.json");
    private Dictionary<string, string> _aliases = new();

    public LanguageAliasService()
    {
        Init();
    }

    // Load aliases from aliases.json if present
    public void Init()
    {
        if (File.Exists(AliasFilePath))
        {
            try
            {
                var json = File.ReadAllText(AliasFilePath);
                _aliases = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                Console.WriteLine($"Loaded {_aliases.Count} language aliases from {AliasFilePath}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load aliases from {AliasFilePath}: {ex.Message}");
                _aliases = new Dictionary<string, string>();
            }
        }
        else
        {
            Console.WriteLine($"No aliases file found at {AliasFilePath}. Using no aliases.");
        }
    }

    // Return alias if exists, otherwise return the original language name
    public string GetDisplayName(string language)
    {
        if (_aliases != null && _aliases.TryGetValue(language, out var alias) && !string.IsNullOrWhiteSpace(alias))
            return alias;

        return language;
    }
}
