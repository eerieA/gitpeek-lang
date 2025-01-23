using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gitpeek_lang.Services;

public class GraphMaker
{
    private const int LegendItemHeight = 25;
    private const int LegendPadding = 10;

    public string GenerateSvgWithLegend(Dictionary<string, long> languageStats, int width, int barHeight, int lgItemWidth)
    {
        if (languageStats == null || languageStats.Count == 0)
            return string.Empty;

        long totalLines = languageStats.Values.Sum();
        
        // Calculate how many legend items can fit per row
        int itemsPerRow = Math.Max(1, (width - LegendPadding * 2) / lgItemWidth);
        int legendRows = (int)Math.Ceiling(languageStats.Count / (double)itemsPerRow);
        int legendHeight = legendRows * LegendItemHeight + LegendPadding * 2;
        
        // Create SVG builder
        var svgBuilder = new StringBuilder();
        svgBuilder.AppendLine($"<svg width='{width}' height='{barHeight + legendHeight}' xmlns='http://www.w3.org/2000/svg'>");
        
        // Draw the percentage bars
        double x = 0.0;
        foreach (var (language, lines) in languageStats)
        {
            double percentage = (double)lines / totalLines;
            double barWidth = percentage * width;
            string color = GetColorForLanguage(language);
            
            svgBuilder.AppendFormat(
                "<rect x='{0}' y='0' width='{1}' height='{2}' fill='{3}' />",
                x, barWidth, barHeight, color);
            
            x += barWidth;
        }

        // Draw the legend
        int currentRow = 0;
        int currentCol = 0;
        
        foreach (var (language, lines) in languageStats)
        {
            double percentage = (double)lines / totalLines;
            string color = GetColorForLanguage(language);
            
            // Calculate position for this legend item
            int itemX = LegendPadding + (currentCol * lgItemWidth);
            int itemY = barHeight + LegendPadding + (currentRow * LegendItemHeight);
            
            // Color box
            svgBuilder.AppendFormat(
                "<rect x='{0}' y='{1}' width='15' height='15' fill='{2}' />",
                itemX, itemY + 2, color);
            
            // Language name and percentage
            svgBuilder.AppendFormat(
                "<text x='{0}' y='{1}' font-family='Arial, sans-serif' font-size='14'>{2} ({3:P1})</text>",
                itemX + 25, itemY + 14, language, percentage);
            
            currentCol++;
            if (currentCol >= itemsPerRow)
            {
                currentCol = 0;
                currentRow++;
            }
        }
        
        svgBuilder.AppendLine("</svg>");
        return svgBuilder.ToString();
    }

    private string GetColorForLanguage(string language)
    {
        // Return predefined colors for languages (extend this as needed)
        return language switch
        {
            "Ruby" => "#701516",
            "Shell" => "#89e051",
            "CSS" => "#563d7c",
            "HTML" => "#e34c26",
            "JavaScript" => "#f1e05a",
            "TypeScript" => "#3178c6",
            "Python" => "#3572A5",
            "Java" => "#b07219",
            "C#" => "#178600",
            "C++" => "#f76e6e",
            "C" => "#fbd7a7",
            _ => "#cccccc", // Default color for unknown languages
        };
    }
}