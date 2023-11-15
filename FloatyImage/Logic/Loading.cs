using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using static System.Reflection.Assembly;
using static FloatyImage.FloatyStrings;
using Image = System.Drawing.Image;

namespace FloatyImage
{
  public sealed partial class Form1
  {
    private void LoadNextFile(List<string> paths)
    {
      if (paths.Count == 0)
      {
        return;
      }

      var path = paths[0];

      var title = path.Substring(path.LastIndexOf('\\') + 1);

      var failedToLoad = false;

      try
      {
        var image = Image.FromFile(path);
        LoadImage(image, title);
        ResetPictureBoxPosition();
      }
      catch (Exception ex)
      {
        failedToLoad = true;
        LogException(ex);
      }

      paths.RemoveAt(0);
      if (paths.Count > 0)
      {
        LaunchNextInstance(paths);
      }

      if (failedToLoad)
      {
        HandleLoadFailure(path, title);
        return;
      }

      StoreCurrentZoomValue();
    }

    private void LoadImage(Image image, string title)
    {
      if (image == null)
      {
        return;
      }

      Text = title;
      pictureBox1.Image = image;

      SetIcon();

      void SetIcon()
      {
        var newWidth = MaxIconDim;
        var newHeight = MaxIconDim;

        if (image.Width > image.Height)
        {
          newHeight = (int)((float)image.Height / image.Width * MaxIconDim);
        }
        else
        {
          newWidth = (int)((float)image.Width / image.Height * MaxIconDim);
        }

        var bitmap = new Bitmap(image, newWidth, newHeight);
        Icon = Icon.FromHandle(bitmap.GetHicon());
      }
    }

    private static void LaunchNextInstance(string path)
    {
      LaunchNextInstance(new List<string>() { path });
    }

    private static void LaunchNextInstance(List<string> paths)
    {
      var args = "";
      foreach (var path in paths)
      {
        args += "\"" + path + "\"" + " ";
      }

      args = args.TrimEnd(' ');

      var location = GetEntryAssembly()?.Location;

      try
      {
        var p = new Process();
        p.StartInfo.FileName = location ?? throw new InvalidOperationException();
        p.StartInfo.Arguments = args;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.CreateNoWindow = true;
        p.Start();
      }
      catch (Exception ex)
      {
        LogException(ex);
      }
    }

    private void HandleLoadFailure(string path, string title)
    {
      var message =
        FailedToLoadImageMessageString +
        Environment.NewLine +
        title;

      var selection =
        MessageBox.Show(message,
          GenericErrorCaptionString,
          MessageBoxButtons.AbortRetryIgnore,
          MessageBoxIcon.Error,
          MessageBoxDefaultButton.Button1,
          MessageBoxOptions.ServiceNotification);

      if (selection == DialogResult.Retry)
      {
        LaunchNextInstance(path);
        Close();
      }

      if (selection == DialogResult.Abort)
      {
        Close();
      }
    }
    
  }
}
