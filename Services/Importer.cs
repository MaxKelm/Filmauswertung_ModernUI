using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Filmauswertung_ModernUI.Services
{
    public enum ImportType
    {
        SingleFile,
        MultipleFiles,
        Folder
    }

    public abstract class Importer
    {
        public IReadOnlyList<string> AllowedExtensions { get; }

        /// <summary> Describes whether this importer accepts single files, multiple files, or folders </summary>
        public abstract ImportType InputType { get; }

        protected Importer(IEnumerable<string> allowedExtensions)
        {
            AllowedExtensions = allowedExtensions.ToList().AsReadOnly();
        }

        public abstract bool CanImport(string inputPath);

        public abstract Task<IEnumerable<string>> ImportAsync(string inputPath);
    }

    public class SingleFileImporter : Importer
    {
        public override ImportType InputType => ImportType.SingleFile;

        public SingleFileImporter(IEnumerable<string> allowedExtensions)
            : base(allowedExtensions) { }

        public override bool CanImport(string inputPath)
        {
            return File.Exists(inputPath) &&
                   AllowedExtensions.Contains(Path.GetExtension(inputPath), StringComparer.OrdinalIgnoreCase);
        }

        public override Task<IEnumerable<string>> ImportAsync(string inputPath)
        {
            var result = new List<string> { inputPath };
            return Task.FromResult<IEnumerable<string>>(result);
        }
    }

    public class MultipleFilesImporter : Importer
    {
        public override ImportType InputType => ImportType.MultipleFiles;

        public MultipleFilesImporter(IEnumerable<string> allowedExtensions)
            : base(allowedExtensions) { }

        public override bool CanImport(string inputPath)
        {
            var files = inputPath.Split(';');
            return files.All(f => File.Exists(f) &&
                                  AllowedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));
        }

        public override Task<IEnumerable<string>> ImportAsync(string inputPath)
        {
            var files = inputPath.Split(';');
            return Task.FromResult<IEnumerable<string>>(files);
        }
    }

    public class FolderImporter : Importer
    {
        public override ImportType InputType => ImportType.Folder;

        public FolderImporter(IEnumerable<string> allowedExtensions)
            : base(allowedExtensions) { }

        public override bool CanImport(string inputPath)
        {
            return Directory.Exists(inputPath);
        }

        public override Task<IEnumerable<string>> ImportAsync(string inputPath)
        {
            if (!Directory.Exists(inputPath))
            {
                return Task.FromResult<IEnumerable<string>>(Enumerable.Empty<string>());
            }

            var files = Directory.GetFiles(inputPath)
                .Where(f => AllowedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));

            return Task.FromResult<IEnumerable<string>>(files);
        }
    }

    public static class ImportDialogService
    {
        public static string ShowDialog(Importer importer)
        {
            if (importer == null)
                throw new ArgumentNullException(nameof(importer));

            switch (importer.InputType)
            {
                case ImportType.SingleFile:
                    var ofdSingle = new System.Windows.Forms.OpenFileDialog
                    {
                        Filter = BuildFilter(importer.AllowedExtensions),
                        Multiselect = false
                    };
                    return (ofdSingle.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        ? ofdSingle.FileName
                        : null;

                case ImportType.MultipleFiles:
                    var ofdMulti = new System.Windows.Forms.OpenFileDialog
                    {
                        Filter = BuildFilter(importer.AllowedExtensions),
                        Multiselect = true
                    };
                    return (ofdMulti.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        ? string.Join(";", ofdMulti.FileNames)
                        : null;

                case ImportType.Folder:
                    using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
                    {
                        return (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            ? fbd.SelectedPath
                            : null;
                    }

                default:
                    throw new NotSupportedException("Unsupported import type");
            }
        }

        private static string BuildFilter(IReadOnlyList<string> extensions)
        {
            if (extensions == null || extensions.Count == 0)
                return "All files (*.*)|*.*";

            var extList = extensions.Select(ext => "*" + ext.ToLowerInvariant());
            var filter = string.Join(";", extList);
            return $"Allowed Files ({filter})|{filter}|All files (*.*)|*.*";
        }
    }
}
