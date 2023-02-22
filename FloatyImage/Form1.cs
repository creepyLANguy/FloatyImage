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
    private const string DefaultTitle = "(Right click on canvas or drag on images/folders to begin)";
    private const string PastedImageTitle = "[Pasted Image]";

    private Point _mouseLocation;
    private static readonly Color BackgroundColor1 = Color.White;
    private static readonly Color BackgroundColor2 = Color.LightGray;
    private static readonly Color OverlayColor = Color.FromArgb(128, Color.MediumTurquoise);
    private const HatchStyle BackgroundStyle = HatchStyle.LargeGrid;

    private const int ZoomMin = 1;
    private const int ZoomMax = 500;
    private const int ZoomStep = 3;
    private const int ZoomDefault = 100;
    private int _zoomCurrent = ZoomDefault; //AL. //TODO - make sure that when you set the zoom to the default that it's actually the pictureBox width as a percentage of the actual image width. 

    private bool _isHovering;
    private bool _isDragging;

    private static readonly HatchBrush BackgroundBrush = new HatchBrush(BackgroundStyle, BackgroundColor1, BackgroundColor2);
    private readonly SolidBrush _overlayBrush = new SolidBrush(OverlayColor);

    private readonly OpenFileDialog _openFileDialog = new OpenFileDialog();

    private readonly ContextMenu _contextMenu = new ContextMenu();
    private readonly MenuItem _menuItemOpen = new MenuItem("Open");
    private readonly MenuItem _menuItemCopy = new MenuItem("Copy");
    private readonly MenuItem _menuItemPaste = new MenuItem("Paste");
    private readonly MenuItem _menuItemRecenter = new MenuItem("Recenter");

    public Form1(string [] args)
    {
      InitializeComponent();

      SetupEventHandlers();

      SetupContextMenu();

      _openFileDialog.Multiselect = true;

      Text = DefaultTitle;

      if (args.Length == 0 || string.IsNullOrEmpty(args[0]))
      {
        return;
      }
      
      var files = GetAllFiles(args);
      LoadNextFile(files);
    }

    private void Form1_Load(object sender, EventArgs e)
    {
      DoubleBuffered = true;
      TopLevel = true;
      TopMost = true;
      AllowDrop = true;

      pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
      pictureBox1.BackColor = Color.Transparent;
    }

    private void SetupEventHandlers()
    {
      Load += Form1_Load;
      Paint += PaintOverlay;
      Paint += PaintBackground;

      pictureBox1.Paint += PaintOverlay;

      pictureBox1.MouseWheel += PictureBox1_MouseWheel;
      pictureBox1.MouseDown += PictureBox1_MouseDown;
      pictureBox1.MouseUp += PictureBox1_MouseUp;
      pictureBox1.MouseEnter += PictureBox1_MouseEnter;
      pictureBox1.MouseLeave += PictureBox1_MouseLeave;
      pictureBox1.MouseMove += PictureBox1_MouseMove;

      MouseWheel += PictureBox1_MouseWheel;
      DragEnter += Form1_DragEnter;
      DragDrop += Form1_DragDrop;
      DragLeave += Form1_DragLeave;
    }

    private void SetupContextMenu()
    {
      _contextMenu.Popup += ContextMenu_Opening;

      _menuItemOpen.Click += ShowOpenDialog;
      _menuItemCopy.Click += Copy;
      _menuItemPaste.Click += Paste;
      _menuItemRecenter.Click += ResetPictureBoxPosition;

      _contextMenu.MenuItems.Add(_menuItemOpen);
      _contextMenu.MenuItems.Add(_menuItemCopy);
      _contextMenu.MenuItems.Add(_menuItemPaste);
      _contextMenu.MenuItems.Add(_menuItemRecenter);
      ContextMenu = _contextMenu;     
    }

    private void ContextMenu_Opening(object sender, EventArgs e)
    {
      var hasImage = pictureBox1.Image != null;
      _menuItemCopy.Enabled = hasImage;
      _menuItemPaste.Enabled = Clipboard.ContainsImage();
      _menuItemRecenter.Enabled = hasImage;
    }

    private void PaintBackground(object sender, PaintEventArgs e)
    {
      if (_isHovering)
      {
        return;
      }

      e.Graphics.FillRectangle(BackgroundBrush, ClientRectangle);
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

      _zoomCurrent += e.Delta > 0 ? ZoomStep : -ZoomStep;
      
      if (_zoomCurrent < ZoomMin)
      {
        _zoomCurrent = ZoomMin;
      }
      else if (_zoomCurrent > ZoomMax)
      {
        _zoomCurrent = ZoomMax;
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

    private void ShowOpenDialog(object sender, EventArgs e)
    {
      if (_openFileDialog.ShowDialog() == DialogResult.OK)
      {
        LoadNextFile(_openFileDialog.FileNames.ToList());
      }
    }

    private void Copy(object sender, EventArgs e)
    {
      try
      {
        Clipboard.SetImage(pictureBox1.Image);
      }
      catch (Exception ex)
      {
        LogException(ex);
      }
    }

    private void Paste(object sender, EventArgs e)
    {
      try
      {
        var image = Clipboard.GetImage();
        SetImage(image, PastedImageTitle);
        ResetPictureBoxPosition();
      }
      catch (Exception ex)
      {
        LogException(ex);
      }
    }

    private void ResetPictureBoxPosition(object sender = null, EventArgs e = null)
    {
      _zoomCurrent = ZoomDefault;
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

      var title = path.Substring(path.LastIndexOf('\\') + 1);

      var failedToLoad = false;

      try
      {
        var image = Image.FromFile(path);
        SetImage(image, title);
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
        Close();
      }
    }

    private void SetImage(Image image, string title)
    {
      Text = title;

      pictureBox1.Image = image;

      var bmp = (Bitmap)image;
      Icon = Icon.FromHandle(bmp.GetHicon());
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

    private static void LogException(Exception ex)
    {
      Console.WriteLine(ex.ToString());
    }
  }
}
