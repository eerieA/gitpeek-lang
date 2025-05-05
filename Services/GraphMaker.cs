using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gitpeek_lang.Services;

public class GraphMaker
{
    private readonly LanguageColorService _colorService;
    private const int LegendItemHeight = 25;
    private const int LegendPadding = 10;
    public GraphMaker(LanguageColorService colorService)
    {
        _colorService = colorService;
    }

    public string GenerateSvgWithLegend(Dictionary<string, long> languageStats, int width, int barHeight, int lgItemWidth, int lgItemMaxCnt, int fontSize)
    {
        if (languageStats == null || languageStats.Count == 0)
            return string.Empty;

        long totalLines = languageStats.Values.Sum();
        
        // Calculate how many legend items can fit per row
        int itemsPerRow = Math.Max(1, (width - LegendPadding * 2) / lgItemWidth);
        int legendRows = (int)Math.Ceiling(Math.Min(languageStats.Count, lgItemMaxCnt) / (double)itemsPerRow);
        int legendHeight = legendRows * LegendItemHeight; // No adding legend padding here because gaps are added in the loop
        
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
        int idx = 0;
        
        foreach (var (language, lines) in languageStats)
        {
            if (idx >= lgItemMaxCnt) break;    // Stop generating once reached the max count

            double percentage = (double)lines / totalLines;
            string color = GetColorForLanguage(language);
            
            // Calculate position for this legend item
            int itemX = LegendPadding + (currentCol * lgItemWidth);
            int itemY = barHeight + LegendPadding + (currentRow * LegendItemHeight);
            
            // Color circle
            svgBuilder.AppendFormat(
                "<circle cx='{0}' cy='{1}' r='{2}' fill='{3}' />",
                itemX+(fontSize/2), itemY+(fontSize/2), fontSize/2, color);
            
            // Language name and percentage
            svgBuilder.AppendFormat(
                "<text x='{0}' y='{1}' font-family='Arial, sans-serif' font-size='{4}'>{2} ({3:P1})</text>",
                itemX + fontSize * 1.2, itemY + fontSize - fontSize*0.15, language, percentage, fontSize);
            
            currentCol++;
            if (currentCol >= itemsPerRow)
            {
                currentCol = 0;
                currentRow++;
            }

            idx ++;     // Inc counter in languageStats
        }
        
        svgBuilder.AppendLine("</svg>");
        return svgBuilder.ToString();
    }

    private string GetColorForLanguage(string language)
    {
        return _colorService.GetColorForLanguage(language);
    }
}