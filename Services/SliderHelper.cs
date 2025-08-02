using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filmauswertung_ModernUI.Services
{
    public static class SliderMarginHelper
    {
        private static readonly Dictionary<int, string> SliderLabels = new Dictionary<int, string>()
        {
            { 0, "No Margin" },
            { 1, "Tight Margin" },
            { 2, "Moderate Margin" },
            { 3, "Wide Margin" },
        };

        private static readonly Dictionary<int, double> MarginFactors = new Dictionary<int, double>()
        {
            { 0, 0.0 },  // No Margin
            { 1, 0.10 }, // Tight Margin (10%)
            { 2, 0.30 }, // Moderate Margin (30%)
            { 3, 0.70 }   // Wide Margin (70%)
        };

        public static string GetLabel(int sliderValue)
        {
            return SliderLabels.TryGetValue(sliderValue, out var label) ? label : "Unknown";
        }

        public static double GetMarginFactor(int sliderValue)
        {
            return MarginFactors.TryGetValue(sliderValue, out var factor) ? factor : 0.0;
        }
    }
    public static class SliderContrastHelper
    {
        private static readonly Dictionary<int, string> SliderLabels = new Dictionary<int, string>()
        {
            { 0, "No Margin" },
            { 1, "Tight Margin" },
            { 2, "Moderate Margin" },
            { 3, "Wide Margin" },
        };

        private static readonly Dictionary<int, double> ContrastFactor = new Dictionary<int, double>()
        {
            { 0, 0.0 },  // No Margin
            { 1, 0.10 }, // Tight Margin (10%)
            { 2, 0.30 }, // Moderate Margin (30%)
            { 3, 0.70 }   // Wide Margin (70%)
        };

        public static string GetLabel(int sliderValue)
        {
            return SliderLabels.TryGetValue(sliderValue, out var label) ? label : "Unknown";
        }

        public static double GetContrastFactor(int sliderValue)
        {
            return ContrastFactor.TryGetValue(sliderValue, out var factor) ? factor : 0.0;
        }
    }
}
