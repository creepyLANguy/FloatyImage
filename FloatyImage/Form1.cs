using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using static System.Reflection.Assembly;

namespace FloatyImage
{
  public sealed partial class Form1 : Form
  {
    private const string DefaultTitle = "(Right click on canvas or drag on images/folders to begin)";
    private const string PastedImageTitle = "[Pasted Image]";

    private readonly Icon DefaultIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

    private static readonly Color BackgroundColor1 = Color.White;
    private static readonly Color BackgroundColor2 = Color.LightGray;
    private const HatchStyle BackgroundStyle = HatchStyle.LargeGrid;
    private static readonly Color OverlayColor = Color.FromArgb(128, Color.MediumTurquoise);
    
    private static readonly HatchBrush BackgroundBrush = new HatchBrush(BackgroundStyle, BackgroundColor1, BackgroundColor2);
    private readonly SolidBrush _overlayBrush = new SolidBrush(OverlayColor);
    
    private const int ZoomMin = 1;
    private const int ZoomMax = 500;
    private const int ZoomStep = 3;
    private int _zoomCurrent;

    private const int FadeIntervalMilliseconds = 10;
    private const double FadeOpacityStep = 0.1;
    private const double FadeFloor = 0.2;
    private const double FadeCeiling = 0.3;

    private Point _mouseLocation;

    private bool _isHovering;
    private bool _isDragging;

    private int _borderWidth;
    private int _titlebarHeight;
    private bool _isTitlebarHidden;

    private int cachedPictureBoxPos_x = 0;
    private int cachedPictureBoxPos_y = 0;

    private readonly OpenFileDialog _openFileDialog = new OpenFileDialog();

    private readonly ContextMenu _contextMenu = new ContextMenu();
    private readonly MenuItem _menuItemOpen = new MenuItem("Open");
    private readonly MenuItem _menuItemDivider1 = new MenuItem("-");
    private readonly MenuItem _menuItemCut = new MenuItem("Cut");
    private readonly MenuItem _menuItemCopy = new MenuItem("Copy");
    private readonly MenuItem _menuItemPaste = new MenuItem("Paste");
    private readonly MenuItem _menuItemDivider2 = new MenuItem("-");
    private readonly MenuItem _menuItemRecenter = new MenuItem("Recenter");
    private readonly MenuItem _menuItemLock = new MenuItem("Lock");
    private readonly MenuItem _menuItemUnlock = new MenuItem("Unlock");
    
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
      var screenRectangle = RectangleToScreen(ClientRectangle);
      _titlebarHeight = screenRectangle.Top - Top;
      _borderWidth = (screenRectangle.Left - Left);

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

      DoubleClick += ToggleTitlebar;
      MouseWheel += PictureBox1_MouseWheel;
      DragEnter += Form1_DragEnter;
      DragDrop += Form1_DragDrop;
      DragLeave += Form1_DragLeave;
      KeyDown += Form1_KeyDown;
      Resize += Form1_Resize;
      ResizeBegin += Form1_ResizeBegin;

      pictureBox1.DoubleClick += ToggleTitlebar;
      pictureBox1.MouseWheel += PictureBox1_MouseWheel;
      pictureBox1.MouseDown += PictureBox1_MouseDown;
      pictureBox1.MouseUp += PictureBox1_MouseUp;
      pictureBox1.MouseEnter += PictureBox1_MouseEnter;
      pictureBox1.MouseLeave += PictureBox1_MouseLeave;
      pictureBox1.MouseMove += PictureBox1_MouseMove;
    }

    private void SetupContextMenu()
    {
      _contextMenu.Popup += ContextMenu_Opening;

      _menuItemOpen.Click += ShowOpenDialog;
      _menuItemCut.Click += Cut;
      _menuItemCopy.Click += Copy;
      _menuItemPaste.Click += Paste;
      _menuItemRecenter.Click += ResetPictureBoxPosition;
      _menuItemLock.Click += ToggleTitlebar;
      _menuItemUnlock.Click += ToggleTitlebar;

      _contextMenu.MenuItems.Add(_menuItemOpen);
      _contextMenu.MenuItems.Add(_menuItemDivider1);
      _contextMenu.MenuItems.Add(_menuItemCut);
      _contextMenu.MenuItems.Add(_menuItemCopy);
      _contextMenu.MenuItems.Add(_menuItemPaste);
      _contextMenu.MenuItems.Add(_menuItemDivider2);
      _contextMenu.MenuItems.Add(_menuItemRecenter);
      _contextMenu.MenuItems.Add(_menuItemLock);

      ContextMenu = _contextMenu;     
    }

    private void ContextMenu_Opening(object sender, EventArgs e)
    {
      var hasImage = pictureBox1.Image != null;
      _menuItemCut.Enabled = hasImage;
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

    private void ToggleTitlebar(object sender, EventArgs e)
    {
      FadeOut();

      if (_isTitlebarHidden)
      {
        _contextMenu.MenuItems.Remove(_menuItemUnlock);
        _contextMenu.MenuItems.Add(_menuItemLock);
        FormBorderStyle = FormBorderStyle.Sizable;
        Location = new Point(Location.X - _borderWidth, Location.Y - _titlebarHeight);
      }
      else
      {
        _contextMenu.MenuItems.Remove(_menuItemLock);
        _contextMenu.MenuItems.Add(_menuItemUnlock);
        FormBorderStyle = FormBorderStyle.None;
        Location = new Point(Location.X + _borderWidth, Location.Y + _titlebarHeight);
      }

      _isTitlebarHidden = !_isTitlebarHidden;

      FadeIn();
    }

    private void FadeOut()
    {
      while (Opacity > FadeFloor)
      {
        Thread.Sleep(FadeIntervalMilliseconds);
        Opacity -= FadeOpacityStep;
      }
      Opacity = 0;     
    }

    private void FadeIn()
    {
      while (Opacity < FadeCeiling)
      {
        Thread.Sleep(FadeIntervalMilliseconds);
        Opacity += FadeOpacityStep;
      }
      Opacity = 1;
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

    private void Form1_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Control == false)
      {
        return;
      }

      switch (e.KeyCode)
      {
        case Keys.V:
          Paste(sender, e); Copy(sender, e);
          break;
        case Keys.C: 
          Copy(sender, e);
          break;
        case Keys.X:
          Cut(sender, e);
          break;
        case Keys.O:
          ShowOpenDialog(sender, e);
          break;
        case Keys.L:
          ToggleTitlebar(sender, e);
          break;
        case Keys.R:
          ResetPictureBoxPosition(sender, e);
          break;
        default:
          break;
      }
    }

    private void Form1_Resize(object sender, EventArgs e)
    {
      pictureBox1.Left = cachedPictureBoxPos_x;
      pictureBox1.Top = cachedPictureBoxPos_y;
    }
    
    private void Form1_ResizeBegin(object sender, EventArgs e)
    {
      cachedPictureBoxPos_x = pictureBox1.Left;
      cachedPictureBoxPos_y = pictureBox1.Top;     
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
        ZoomImage();
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

    private void Cut(object sender, EventArgs e)
    {
      if (pictureBox1.Image == null)
      {
        return;
      }

      try
      {
        Clipboard.SetImage(pictureBox1.Image);
        Text = DefaultTitle;
        pictureBox1.Image = null;
        Icon = DefaultIcon;
      }
      catch (Exception ex)
      {
        LogException(ex);
      }
    }
    
    private void Copy(object sender, EventArgs e)
    {
      if (pictureBox1.Image == null)
      {
        return;
      }

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
        if (Clipboard.ContainsImage() == false)
        {
          return;
        }

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
      _zoomCurrent = 100;
      pictureBox1.Left = 0;
      pictureBox1.Top = 0;
      pictureBox1.Width = pictureBox1.Image.Width;
      pictureBox1.Height = pictureBox1.Image.Height;
      Refresh();
    }

    private void ZoomImage()
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
        return;
      }

      StoreCurrentZoomValue();
    }

    private void SetImage(Image image, string title)
    {
      Text = title;

      pictureBox1.Image = image;

      var bmp = (Bitmap)image;
      Icon = Icon.FromHandle(bmp.GetHicon());
    }

    private void StoreCurrentZoomValue()
    {
      var zoomRatio = (double) pictureBox1.ClientSize.Height / pictureBox1.Image.Height;
      _zoomCurrent = (int) (zoomRatio * 100);
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
