using Frosty.Core;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using System.Reflection;

namespace Frosty.Core.Controls
{
    /// <summary>
    /// A variation of <see cref="FrostySaveFileDialog"/> for opening folders
    /// </summary>
    public class FrostyOpenFolderDialog
    {
        public string FileName => ofd.FileName;
        public string InitialDirectory { get => ofd.InitialDirectory; set => ofd.InitialDirectory = value; }

        private string key;
        private CommonOpenFileDialog ofd;

        public FrostyOpenFolderDialog(string title, string inKey)
        {
            key = inKey + "ExportPath";
            DirectoryInfo di = new DirectoryInfo(Config.Get(key, new FileInfo(Assembly.GetExecutingAssembly().FullName).DirectoryName));

            ofd = new CommonOpenFileDialog
            {
                Title = title,
                InitialDirectory = di.Exists ? di.FullName : "",
                IsFolderPicker = true
            };
        }

        public bool ShowDialog()
        {
            if (ofd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Config.Add(key, Path.GetDirectoryName(ofd.FileName));
                Config.Save();
                return true;
            }

            return false;
        }
    }
}
