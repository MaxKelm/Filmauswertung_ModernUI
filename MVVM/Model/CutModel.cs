using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace Filmauswertung_ModernUI.MVVM.Model
{
    /// <summary>
    /// Represents a rectangular segment on an image.
    /// </summary>
    public class Segment
    {
        public Rect BoundingBox { get; set; }

        // Optionally add properties like ID, Label, Confidence, etc.
        public string Id { get; set; }
        public string Label { get; set; }

        public Segment() { }

        public Segment(Rect boundingBox, string id = null, string label = null)
        {
            BoundingBox = boundingBox;
            Id = id;
            Label = label;
        }
    }

    /// <summary>
    /// Represents a marker point on the image (e.g. user click).
    /// </summary>
    public class Marker
    {
        public Point Position { get; set; }

        // Optionally add properties like timestamp, color, etc.
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public Marker() { }

        public Marker(Point position)
        {
            Position = position;
        }
    }

    /// <summary>
    /// Main data model holding all segments and markers for a cut operation.
    /// </summary>
    internal class CutModel
    {
        public ObservableCollection<Segment> Segments { get; } = new ObservableCollection<Segment>();

        public ObservableCollection<Marker> Markers { get; } = new ObservableCollection<Marker>();

        // You can add other relevant properties, e.g., associated image path/name, metadata, etc.
        public string ImagePath { get; set; }

        public CutModel() { }

        public CutModel(string imagePath)
        {
            ImagePath = imagePath;
        }
    }
}
