using System;
using System.Collections.Generic;
using System.Linq;


namespace Filmauswertung_ModernUI.MVVM.Model
{
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

        public static double ComputeZScale(OpgFileData data)
        {
            double xRange = data.X.Last() - data.X.First();
            double yRange = data.Y.Last() - data.Y.First();
            double zRange = data.PixelValues.SelectMany(r => r).Max() - data.PixelValues.SelectMany(r => r).Min();
            return zRange > 0 ? Math.Min(xRange, yRange) / zRange * 0.5 : 1.0;
        }
    }

    internal class DoseViewerModel
    {
        public string ReferenceFilePath { get; set; }
        public string CompareFilePath { get; set; }

        public OpgFileData ReferenceData { get; set; }
        public OpgFileData CompareData { get; set; }

        public double ReferenceDose { get; set; } = 1.0;
        public string SelectedBackgroundColor { get; set; } = "Transparent";
        public string SelectedGradientMode { get; set; } = "Default";

        // UI Options (static)
        public List<string> BackgroundColorOptions { get; } = new List<string>
        {
            "LightGray", "White","Transparent", "Black", "LightBlue"
        };

        public List<string> GradientModes { get; } = new List<string>
        {
            "Default", "Bipolar", "Stepped", "Uniform", 
        };
    }
}
