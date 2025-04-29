using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace gitpeek_lang.Services;

public class LanguageColorService
{
    private static readonly string YamlUrl = "https://raw.githubusercontent.com/github-linguist/linguist/266912b913855446ec51c002985010dbe51c524a/lib/linguist/languages.yml";
    private static readonly string LocalCachePath = "Cache/language_colors.json";

    private Dictionary<string, string> _languageColors = new();

    public async Task InitAsync()
    {
        if (File.Exists(LocalCachePath))
        {
            // Load from local cache
            Console.WriteLine($"Loading colors from cached json.");
            var json = await File.ReadAllTextAsync(LocalCachePath);
            _languageColors = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                            ?? new Dictionary<string, string>();
        }
        else
        {
            // Fetch from GitHub
            Console.WriteLine($"Fetching colors from the linguist repo.");
            using var httpClient = new HttpClient();
            var yamlContent = await httpClient.GetStringAsync(YamlUrl);

            var yamlStream = new YamlStream();
            yamlStream.Load(new StringReader(yamlContent));

            var root = (YamlMappingNode)yamlStream.Documents[0].RootNode;

            foreach (var entry in root.Children)
            {
                var language = entry.Key.ToString();
                var props = (YamlMappingNode)entry.Value;

                if (props.Children.TryGetValue(new YamlScalarNode("color"), out var colorNode))
                {
                    _languageColors[language] = colorNode.ToString();
                }
            }

            // Save to local cache
            var json = JsonSerializer.Serialize(_languageColors, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(LocalCachePath, json);
        }
    }

    public string GetColorForLanguage(string language)
    {
        if (_languageColors != null && _languageColors.TryGetValue(language, out var color))
            return color;

        return "#cccccc"; // Default grey
    }

    // Optional: Force refresh from GitHub
    public async Task ForceRefreshAsync()
    {
        if (File.Exists(LocalCachePath))
            File.Delete(LocalCachePath);

        await InitAsync();
    }
}
