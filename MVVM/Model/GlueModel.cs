namespace Filmauswertung_ModernUI.MVVM.Model
{
    public class ImageItem
    {
        public string FileNameWithoutExtension { get; set; }
        public string FullPath { get; set; }

        // Optional: override ToString() for debugging or display
        public override string ToString() => FileNameWithoutExtension;
    }
}
