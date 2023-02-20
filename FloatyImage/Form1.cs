using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static System.Reflection.Assembly;

namespace FloatyImage
{
  public sealed partial class Form1 : Form
  {
    private readonly string _defaultTitle = "(Drag an image file onto this window to begin)";

    private Point _mouseLocation;
    private readonly Color _blankAreaColor = Color.DarkGray;
    private readonly Color _dragDropColor = Color.CornflowerBlue;

    private readonly int _zoomMin = 10;
    private readonly int _zoomMax = 500;
    private readonly int _zoomStep = 3;
    private int _zoomCurrent = 100;

    //private Image _lastLoadedImage;

    public Form1(string [] args)
    {
      InitializeComponent();

      Load += Form1_Load;

      pictureBox1.MouseWheel += PictureBox1_MouseWheel;
      pictureBox1.MouseDown += PictureBox1_MouseDown;
      pictureBox1.MouseEnter += PictureBox1_MouseEnter;
      pictureBox1.MouseLeave += PictureBox1_MouseLeave;
      pictureBox1.MouseMove += PictureBox1_MouseMove;
      
      pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
      pictureBox1.BackColor = _blankAreaColor;

      MouseWheel += PictureBox1_MouseWheel;
      DragEnter += Form1_DragEnter;
      DragDrop += Form1_DragDrop;
      DragLeave += Form1_DragLeave;

      Text = _defaultTitle;

      if (args.Length == 0 || string.IsNullOrEmpty(args[0]))
      {
        return;
      }
      
      var files = GetAllFiles(args);
      LoadNextFile(files);
    }

    private void Form1_Load(object sender, EventArgs e)
    {
      pictureBox1.BackColor = Color.Transparent;
      BackColor = _blankAreaColor;
      DoubleBuffered = true;
      TopLevel = true;
      TopMost = true;
    }

    private void Form1_DragEnter(object sender, DragEventArgs e)
    {
      if (!e.Data.GetDataPresent(DataFormats.FileDrop))
      {
        return;
      }

      e.Effect = DragDropEffects.Copy;
      ShowHoverEffect();
    }

    private void Form1_DragLeave(object sender, EventArgs e)
    {
      BackColor = _blankAreaColor;
      pictureBox1.Show();
      //pictureBox1.Image = _lastLoadedImage;
    }

    private void Form1_DragDrop(object sender, DragEventArgs e)
    {
      BackColor = _blankAreaColor;
      pictureBox1.Show();

      var dropped = (string[])e.Data.GetData(DataFormats.FileDrop);
      var files = GetAllFiles(dropped);
      LoadNextFile(files);
    }

    private void PictureBox1_MouseWheel(object sender, MouseEventArgs e)
    {
      var oldZoom = _zoomCurrent;

      _zoomCurrent += e.Delta > 0 ? _zoomStep : -_zoomStep;
      
      if (_zoomCurrent < _zoomMin)
      {
        _zoomCurrent = _zoomMin;
      }
      else if (_zoomCurrent > _zoomMax)
      {
        _zoomCurrent = _zoomMax;
      }

      if (_zoomCurrent != oldZoom)
      {
        UpdateImageSize();
      }
    }

    private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
    { 
      _mouseLocation = e.Location;
    }

    private void PictureBox1_MouseMove(object sender, MouseEventArgs e)
    {
      if (pictureBox1.Image == null)
      {
        return;
      }

      switch (e.Button)
      {
        case MouseButtons.Left:
        case MouseButtons.Middle:
        case MouseButtons.Right:
        {
          var xDiff = e.Location.X - _mouseLocation.X;
          var yDiff = e.Location.Y - _mouseLocation.Y;

          pictureBox1.Location = new Point(pictureBox1.Location.X + xDiff, pictureBox1.Location.Y + yDiff);
          break;
        }
        case MouseButtons.None:
        case MouseButtons.XButton1:
        case MouseButtons.XButton2:
        default:
        {
          break;
        }
      }
    }    
    
    private void PictureBox1_MouseEnter(object sender, EventArgs e)
    {
      if (pictureBox1.Image == null)
      {
        return;
      }

      pictureBox1.Cursor = Cursors.Hand;
    }

    private void PictureBox1_MouseLeave(object sender, EventArgs e)
    {
      if (pictureBox1.Image == null)
      {
        return;
      }

      pictureBox1.Cursor = Cursors.Default;
    }

    private void UpdateImageSize()
    {
      if (pictureBox1.Image == null)
      {
        return;
      }

      var newWidth = pictureBox1.Image.Width * _zoomCurrent / 100;
      var newHeight = pictureBox1.Image.Height * _zoomCurrent / 100;

      pictureBox1.Size = new Size(newWidth, newHeight);
    }

    private void ShowHoverEffect()
    {
      if (pictureBox1.Image == null)
      {
        return;
      }

      BackColor = _dragDropColor;
      //_lastLoadedImage = (Image)pictureBox1.Image.Clone();
      pictureBox1.Hide();
    }

    private void LoadNextFile(List<string> paths)
    {
      var path = paths[0];

      Text = path.Substring(path.LastIndexOf('\\') + 1);

      try
      {
        var image = Image.FromFile(path);
        pictureBox1.Image = image;

        //_lastLoadedImage = image;

        var bmp = (Bitmap)image;
        Icon = Icon.FromHandle(bmp.GetHicon());
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
      }

      paths.RemoveAt(0);  //paths = paths.Skip(1).ToArray();
      if (paths.Count > 0)
      {
        LaunchNextInstance(paths);
      }
    }
    
    private static void LaunchNextInstance(List<string> args)
    {
      var argsFormatted = "";
      foreach (var arg in args)
      {
        argsFormatted += "\"" + arg + "\"" + " ";
      }
      argsFormatted = argsFormatted.TrimEnd(' ');

      var location = GetEntryAssembly()?.Location;

      try
      {
        var p = new System.Diagnostics.Process();
        p.StartInfo.FileName = location ?? throw new InvalidOperationException();
        p.StartInfo.Arguments = argsFormatted;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.CreateNoWindow = true;
        p.Start();
      }
      catch (Exception e)
      {
        Console.WriteLine(e.Message);
      }
    }

    private static List<string> GetAllFiles(string[] paths)
    {
      var fileList = new List<string>();

      foreach (var path in paths)
      {
        if (File.Exists(path))
        {
          fileList.Add(path);
        }
        else if (Directory.Exists(path))
        {
          fileList.AddRange(GetFilesFromDirectoryRecursively(path));
        }
      }

      return fileList;
    }

    private static List<string> GetFilesFromDirectoryRecursively(string directory)
    {
      var files = Directory.GetFiles(directory).ToList();

      foreach (string subDirectory in Directory.GetDirectories(directory))
      {
        files.AddRange(GetFilesFromDirectoryRecursively(subDirectory));
      }

      return files;
    }
  }
}
