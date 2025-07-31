using System;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Forms;

namespace Filmauswertung_ModernUI.Services
{
    public enum ExportType
    {
        SingleFile,
        Folder
    }

    public abstract class Exporter
    {
        public string SuggestedSuffix { get; }
        public string DefaultExtension { get; }

        public abstract ExportType OutputType { get; }

        protected Exporter(string defaultExtension, string suggestedSuffix = "")
        {
            DefaultExtension = string.IsNullOrEmpty(defaultExtension)
                ? string.Empty
                : (defaultExtension.StartsWith(".") ? defaultExtension : "." + defaultExtension);
            SuggestedSuffix = suggestedSuffix;
        }

        /// <summary>
        /// Provides the full export path based on user dialog.
        /// </summary>
        public abstract string GetExportPath(string inputBaseName);
    }

    public class SingleFileExporter : Exporter
    {
        public SingleFileExporter(string defaultExtension, string suggestedSuffix = "")
            : base(defaultExtension, suggestedSuffix) { }

        public override ExportType OutputType => ExportType.SingleFile;

        public override string GetExportPath(string inputBaseName)
        {
            string suggestedFileName = $"{inputBaseName}_{SuggestedSuffix}{DefaultExtension}";

            var sfd = new SaveFileDialog
            {
                Filter = $"{DefaultExtension.ToUpper().Trim('.')} files (*{DefaultExtension})|*{DefaultExtension}|All files (*.*)|*.*",
                FileName = suggestedFileName,
                DefaultExt = DefaultExtension,
                AddExtension = true,
                Title = "Save File"
            };

            return sfd.ShowDialog() == DialogResult.OK ? sfd.FileName : null;
        }
    }

    public class FolderExporter : Exporter
    {
        public FolderExporter(string suggestedSuffix = "")
            : base("", suggestedSuffix) { }

        public override ExportType OutputType => ExportType.Folder;

        public override string GetExportPath(string inputBaseName)
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select Export Folder"
            };

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string targetFolder = Path.Combine(dlg.FileName, $"{inputBaseName}_{SuggestedSuffix}");
                Directory.CreateDirectory(targetFolder);
                return targetFolder;
            }

            return null;
        }
    }
}
