using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
            DefaultExtension = defaultExtension.StartsWith(".") ? defaultExtension : "." + defaultExtension;
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

            var sfd = new System.Windows.Forms.SaveFileDialog
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
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select Export Folder";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    string targetFolder = Path.Combine(fbd.SelectedPath, $"{inputBaseName}_{SuggestedSuffix}");
                    Directory.CreateDirectory(targetFolder);
                    return targetFolder;
                }
            }

            return null;
        }
    }
}
