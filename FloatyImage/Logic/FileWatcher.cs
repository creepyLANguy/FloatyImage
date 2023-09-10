using System.IO;
using System.Windows.Forms;

namespace FloatyImage
{
  public sealed partial class Form1
  {
    public void RunFileWatcher()
    {
      _fileWatcher = new(Application.StartupPath);
      _fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
      _fileWatcher.Changed += OnChanged;
      _fileWatcher.Filter = ConfigFile;
      _fileWatcher.EnableRaisingEvents = true;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
      if (e.ChangeType != WatcherChangeTypes.Changed)
      {
        return;
      }

      SetupHotKeys(_hotKeys);
    }
  }
}
