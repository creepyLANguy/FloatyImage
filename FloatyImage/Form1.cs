using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
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
    private readonly Color _backgroundColor1 = Color.White;
    private readonly Color _backgroundColor2 = Color.LightGray;
    private readonly Color _overlayColor = Color.FromArgb(128, Color.MediumTurquoise);
    private readonly HatchStyle _backgroundStyle = HatchStyle.LargeGrid;


    private readonly int _zoomMin = 1;
    private readonly int _zoomMax = 500;
    private readonly int _zoomStep = 3;
    private static int _zoomDefault = 100;
    private static int _zoomCurrent = _zoomDefault; //AL. //TODO - make sure that when you set the zoom to the default that it's actually the picturebox width as a percentage of the actual image width. 

    private bool _isHovering;
    private bool _isDragging;
    private readonly HatchBrush _backgroundBrush;
    private readonly SolidBrush _overlayBrush;

    public Form1(string [] args)
    {
      InitializeComponent();

      Load += Form1_Load;
      Paint += PaintOverlay;
      Paint += PaintBackground;
      DoubleClick += ResetPictureboxPosition;

      pictureBox1.Paint += PaintOverlay;
      pictureBox1.DoubleClick+= ResetPictureboxPosition;
      
      pictureBox1.MouseWheel += PictureBox1_MouseWheel;
      pictureBox1.MouseDown += PictureBox1_MouseDown;
      pictureBox1.MouseUp += PictureBox1_MouseUp;
      pictureBox1.MouseEnter += PictureBox1_MouseEnter;
      pictureBox1.MouseLeave += PictureBox1_MouseLeave;
      pictureBox1.MouseMove += PictureBox1_MouseMove;
      
      pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;

      MouseWheel += PictureBox1_MouseWheel;
      DragEnter += Form1_DragEnter;
      DragDrop += Form1_DragDrop;
      DragLeave += Form1_DragLeave;

      Text = _defaultTitle;

      _backgroundBrush = new HatchBrush(_backgroundStyle, _backgroundColor1, _backgroundColor2);
      _overlayBrush = new SolidBrush(_overlayColor);

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
      DoubleBuffered = true;
      TopLevel = true;
      TopMost = true;
      AllowDrop = true;
    }

    private void PaintBackground(object sender, PaintEventArgs e)
    {
      if (_isHovering)
      {
        return;
      }

      e.Graphics.FillRectangle(_backgroundBrush, ClientRectangle);
    }

    private void PaintOverlay(object sender, PaintEventArgs e)
    {
      if (_isHovering  == false)
      {
        return;
      }

      if (sender is Control control)
      {
        e.Graphics.FillRectangle(_overlayBrush, new Rectangle(Point.Empty, control.Size));
      }
    }

    private void Form1_DragEnter(object sender, DragEventArgs e)
    {
      if (!e.Data.GetDataPresent(DataFormats.FileDrop))
      {
        return;
      }

      _isHovering = true;
      Invalidate();

      e.Effect = DragDropEffects.Copy;
    }

    private void Form1_DragLeave(object sender, EventArgs e)
    {
      _isHovering = false;
      Invalidate();
    }

    private void Form1_DragDrop(object sender, DragEventArgs e)
    {
      _isHovering = false;
      Invalidate();

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
      _isDragging = true;
      _mouseLocation = e.Location;
    }

    private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
    {
      _isDragging = false;
    }

    private void PictureBox1_MouseMove(object sender, MouseEventArgs e)
    {
      if (pictureBox1.Image == null || _isDragging == false)
      {
        return;
      }

      pictureBox1.Left += e.X - _mouseLocation.X;
      pictureBox1.Top += e.Y - _mouseLocation.Y;
      Refresh();
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

    private void ResetPictureboxPosition(object sender, EventArgs e)
    {
      _zoomCurrent = _zoomDefault;
      pictureBox1.Left = 0;
      pictureBox1.Top = 0;
      UpdateImageSize();
      Refresh();
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

      Refresh();
    }

    private void LoadNextFile(List<string> paths)
    {
      var path = paths[0];

      Text = path.Substring(path.LastIndexOf('\\') + 1);

      try
      {
        var image = Image.FromFile(path);
        pictureBox1.Image = image;

        var bmp = (Bitmap)image;
        Icon = Icon.FromHandle(bmp.GetHicon());
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
      }

      paths.RemoveAt(0);
      if (paths.Count > 0)
      {
        LaunchNextInstance(paths);
      }
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

      foreach (var subDirectory in Directory.GetDirectories(directory))
      {
        files.AddRange(GetFilesFromDirectoryRecursively(subDirectory));
      }

      return files;
    }
  }
}
