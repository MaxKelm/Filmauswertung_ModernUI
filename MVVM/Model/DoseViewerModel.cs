using System.Collections.Generic;

namespace Filmauswertung_ModernUI.MVVM.Model
{
    /// <summary>
    /// Represents a loaded OPG dose file with grid and pixel data.
    /// </summary>
    public class OpgFileData
    {
        public string ImageName { get; set; }
        public string Energy { get; set; }
        public string DataUnit { get; set; }
        public double DataFactor { get; set; } = 1.0;

        public int NoOfColumns { get; set; }
        public int NoOfRows { get; set; }

        public bool FFF { get; set; }

        public List<double> X { get; set; } = new List<double>();
        public List<double> Y { get; set; } = new List<double>();
        public List<List<double>> PixelValues { get; set; } = new List<List<double>>();
    }

    /// <summary>
    /// Model storing all dose viewer related data and UI state.
    /// </summary>
    internal class DoseViewerModel
    {
        // -----------------------------
        // File paths
        // -----------------------------
        public string ReferenceFilePath { get; set; }
        public string CompareFilePath { get; set; }

        // -----------------------------
        // Loaded data
        // -----------------------------
        public OpgFileData ReferenceData { get; set; }
        public OpgFileData CompareData { get; set; }

        // -----------------------------
        // Display settings
        // -----------------------------
        public double ReferenceDose { get; set; } = 1.0;

        // Selected options from UI
        public string SelectedBackgroundColor { get; set; } = "White";
        public string SelectedGradientMode { get; set; } = "Default";

        // Display mode flags
        public bool IsSingleDisplay { get; set; } = true;
        public bool IsDualDisplay { get; set; }
        public bool IsDoseDifference { get; set; }
        public bool IsGammaEvaluation { get; set; }

        // -----------------------------
        // UI binding options (static)
        // -----------------------------
        public List<string> BackgroundColorOptions { get; } = new List<string>
        {
            "LightGray", "White", "Black", "LightBlue", "Transparent"
        };

        public List<string> GradientModes { get; } = new List<string>
        {
            "Default", "Bipolar", "Stepped"
        };
    }
}
