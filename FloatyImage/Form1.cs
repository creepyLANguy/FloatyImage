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
using static FloatyImage.FloatyStrings;

namespace FloatyImage
{
  public sealed partial class Form1 : Form
  {
    private readonly HatchBrush _backgroundBrush =
      new HatchBrush(BackgroundStyle, BackgroundColor1, BackgroundColor2);

    private readonly SolidBrush _overlayBrush = new SolidBrush(OverlayColor);

    private readonly System.Windows.Forms.Timer _debounceTimer = new System.Windows.Forms.Timer();

    private float _zoomPercentageCurrent;

    private Point _mouseLocation;

    private bool _isHovering;
    private bool _isDragging;

    private int _borderWidth;
    private int _titlebarHeight;
    private bool _isTitlebarHidden;

    private int _cachedPictureBoxPosX;
    private int _cachedPictureBoxPosY;

    private MouseEventArgs _cachedMouseEventArgs;

    private readonly OpenFileDialog _openFileDialog = new OpenFileDialog();

    private readonly ContextMenu _contextMenu = new ContextMenu();
    private readonly MenuItem _menuItemOpen = new MenuItem("Open");
    private readonly MenuItem _menuItemCut = new MenuItem("Cut");
    private readonly MenuItem _menuItemCopy = new MenuItem("Copy");
    private readonly MenuItem _menuItemPaste = new MenuItem("Paste");
    private readonly MenuItem _menuItemRecenter = new MenuItem("Recenter");
    private readonly MenuItem _menuItemOneToOne = new MenuItem("Actual Size");
    private readonly MenuItem _menuItemAlwaysOnTop = new MenuItem(StopPersistingString);
    private readonly MenuItem _menuItemToggleLock = new MenuItem(LockString);
    private readonly MenuItem _menuItemColourDivider = new MenuItem("-");
    private readonly MenuItem _menuItemColourHex = new MenuItem();
    private readonly MenuItem _menuItemColourRgb = new MenuItem();

    public Form1(string[] args)
    {
      InitializeComponent();

      _debounceTimer.Interval = DebounceTimerInterval; // Adjust the debounce interval as needed
      _debounceTimer.Tick += DebounceTimer_Tick;

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
      AllowDrop = true;

      SetAlwaysOnTop(true);

      pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
      pictureBox1.BackColor = Color.Transparent;

      btn_colour.Enabled = false;
      btn_colour.Visible = false;
    }

    private void SetAlwaysOnTop(bool mustBeOnTop)
    {
      TopLevel = mustBeOnTop;
      TopMost = mustBeOnTop;
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
      _contextMenu.Collapse += ContextMenu_Closing;

      _menuItemOpen.Click += ShowOpenDialog;
      _menuItemCut.Click += Cut;
      _menuItemCopy.Click += Copy;
      _menuItemPaste.Click += Paste;
      _menuItemRecenter.Click += ResetPictureBoxPosition;
      _menuItemOneToOne.Click += ZoomOneToOne;
      _menuItemAlwaysOnTop.Click += ToggleAlwaysOnTop;
      _menuItemToggleLock.Click += ToggleTitlebar;
      _menuItemColourHex.Click += CopyTextToClipboard;
      _menuItemColourRgb.Click += CopyTextToClipboard;

      _contextMenu.MenuItems.Add(_menuItemOpen);
      _contextMenu.MenuItems.Add(new MenuItem("-"));
      _contextMenu.MenuItems.Add(_menuItemCut);
      _contextMenu.MenuItems.Add(_menuItemCopy);
      _contextMenu.MenuItems.Add(_menuItemPaste);
      _contextMenu.MenuItems.Add(new MenuItem("-"));
      _contextMenu.MenuItems.Add(_menuItemRecenter);
      _contextMenu.MenuItems.Add(_menuItemOneToOne);
      _contextMenu.MenuItems.Add(new MenuItem("-"));
      _contextMenu.MenuItems.Add(_menuItemToggleLock);
      _contextMenu.MenuItems.Add(_menuItemAlwaysOnTop);
      _contextMenu.MenuItems.Add(_menuItemColourDivider);
      _contextMenu.MenuItems.Add(_menuItemColourHex);
      _contextMenu.MenuItems.Add(_menuItemColourRgb);

      ContextMenu = _contextMenu;
    }

    private void ContextMenu_Opening(object sender, EventArgs e)
    {
      var hasImage = pictureBox1.Image != null;
      _menuItemCut.Enabled = hasImage;
      _menuItemCopy.Enabled = hasImage;
      _menuItemPaste.Enabled = Clipboard.ContainsImage();
      _menuItemRecenter.Enabled = hasImage;
      _menuItemOneToOne.Enabled = hasImage;

      DisplayCurrentPixelColour();
    }

    private void ContextMenu_Closing(object sender, EventArgs e)
    {
      btn_colour.Visible = false;
    }

    private void DisplayCurrentPixelColour()
    {
      if (pictureBox1.Image == null)
      {
        HideColourIndicators();
        return;
      }

      var bitmap = new Bitmap(ClientSize.Width, ClientSize.Height);
      DrawToBitmap(bitmap, ClientRectangle);

      var screenCursorPos = PointToScreen(Cursor.Position);

      var windowRect = new Rectangle(Location, Size);
      var screenWindowPos = PointToScreen(windowRect.Location);

      var clientCursorPos = new Point(
        screenCursorPos.X - screenWindowPos.X,
        screenCursorPos.Y - screenWindowPos.Y
      );

      try
      {
        var colour = bitmap.GetPixel(clientCursorPos.X, clientCursorPos.Y);

        btn_colour.BackColor = colour;
        var cursorPos = PointToClient(Cursor.Position);
        btn_colour.Location = new Point(cursorPos.X - btn_colour.Width, cursorPos.Y - btn_colour.Height);
        btn_colour.Visible = true;

        var hex = $"#{colour.R:X2}{colour.G:X2}{colour.B:X2}";
        var rgb = $"{colour.R},{colour.G},{colour.B}";

        _menuItemColourHex.Text = hex;
        _menuItemColourRgb.Text = rgb;

        _menuItemColourDivider.Visible = true;
        _menuItemColourHex.Visible = true;
        _menuItemColourRgb.Visible = true;
      }
      catch (Exception)
      {
        HideColourIndicators();
      }

      void HideColourIndicators()
      {
        btn_colour.Visible = false;
        _menuItemColourDivider.Visible = false;
        _menuItemColourHex.Visible = false;
        _menuItemColourRgb.Visible = false;
      }
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
      if (_isHovering == false)
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
        _menuItemToggleLock.Text = LockString;
        FormBorderStyle = FormBorderStyle.Sizable;
        Location = new Point(Location.X - _borderWidth, Location.Y - _titlebarHeight);
      }
      else
      {
        _menuItemToggleLock.Text = UnlockString;
        FormBorderStyle = FormBorderStyle.None;
        Location = new Point(Location.X + _borderWidth, Location.Y + _titlebarHeight);
      }

      _isTitlebarHidden = !_isTitlebarHidden;

      if (pictureBox1.Left != _cachedPictureBoxPosX)
      {
        pictureBox1.Left = _cachedPictureBoxPosX;
      }

      if (pictureBox1.Top != _cachedPictureBoxPosY)
      {
        pictureBox1.Top = _cachedPictureBoxPosY;
      }

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

    private void CopyTextToClipboard(object sender, EventArgs e)
    {
      Clipboard.SetText(((MenuItem) sender).Text);
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

      var dropped = (string[]) e.Data.GetData(DataFormats.FileDrop);
      var files = GetAllFiles(dropped);
      LoadNextFile(files);
    }

    private void Form1_KeyDown(object sender, KeyEventArgs e)
    {
      switch (e.KeyCode)
      {
        case Keys.Delete:
        case Keys.Back:
          ClearImage();
          break;
      }

      if (e.Control == false)
      {
        return;
      }

      switch (e.KeyCode)
      {
        case Keys.V:
          Paste(sender, e);
          Copy(sender, e);
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
          return;
      }
    }

    private void Form1_Resize(object sender, EventArgs e)
    {
      //We are prolly toggling lock state
      if ((int)Opacity != 1)
      {
        return;
      }

      if (pictureBox1.Left != _cachedPictureBoxPosX)
      {
        pictureBox1.Left = _cachedPictureBoxPosX;
      }

      if (pictureBox1.Top != _cachedPictureBoxPosY)
      {
        pictureBox1.Top = _cachedPictureBoxPosY;
      }
    }

    private void Form1_ResizeBegin(object sender, EventArgs e)
    {
      //We are prolly toggling lock state
      if ((int)Opacity != 1)
      {
        return;
      }

      _cachedPictureBoxPosX = pictureBox1.Left;
      _cachedPictureBoxPosY = pictureBox1.Top;
    }

    private void PictureBox1_MouseWheel(object sender, MouseEventArgs e)
    {
      _cachedMouseEventArgs = e;
      _debounceTimer.Stop();
      _debounceTimer.Start();
    }

    private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
    {
      _isDragging = true;
      _mouseLocation = e.Location;
    }

    private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
    {
      _isDragging = false;
      _cachedPictureBoxPosX = pictureBox1.Left;
      _cachedPictureBoxPosY = pictureBox1.Top;
    }

    private void PictureBox1_MouseMove(object sender, MouseEventArgs e)
    {
      if (pictureBox1.Image == null)
      {
        return;
      }

      if (e.Button != MouseButtons.Right && _isDragging)
      {
        pictureBox1.Left += e.X - _mouseLocation.X;
        pictureBox1.Top += e.Y - _mouseLocation.Y;
        Refresh();
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
        ClearImage();
      }
      catch (Exception ex)
      {
        LogException(ex);
      }
    }

    private void ClearImage()
    {
      Text = DefaultTitle;
      pictureBox1.Image = null;
      Icon = DefaultIcon;
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
        SetIcon(image);

        ResetPictureBoxPosition();
      }
      catch (Exception ex)
      {
        LogException(ex);
      }
    }

    private void ResetPictureBoxPosition(object sender = null, EventArgs e = null)
    {
      pictureBox1.Width = ClientSize.Width;
      pictureBox1.Height = ClientSize.Height;

      pictureBox1.Location = new Point(0, 0);

      StoreCurrentZoomValue();

      Refresh();
    }

    private void ZoomOneToOne(object sender = null, EventArgs e = null)
    {
      pictureBox1.Width = pictureBox1.Image.Width;
      pictureBox1.Height = pictureBox1.Image.Height;

      pictureBox1.Location = new Point(-pictureBox1.Image.Width / 2, -pictureBox1.Image.Height / 2);

      StoreCurrentZoomValue();

      Refresh();
    }

    private void ToggleAlwaysOnTop(object sender = null, EventArgs e = null)
    {
      var isAlwaysOnTop = TopLevel && TopMost;
      TopMost = !isAlwaysOnTop;
      _menuItemAlwaysOnTop.Text = !isAlwaysOnTop ? StopPersistingString : PersistString;
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
        SetIcon(image);

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
        var message =
          FailedToLoadImageMessageString +
          Environment.NewLine +
          title;

        var selection =
          MessageBox.Show(message,
            FailedToLoadImageCaptionString,
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

        return;
      }

      StoreCurrentZoomValue();
    }

    private void SetImage(Image image, string title)
    {
      Text = title;
      pictureBox1.Image = image;
    }

    private void SetIcon(Image image)
    {
      var newWidth = MaxIconDim;
      var newHeight = MaxIconDim;

      if (image.Width > image.Height)
      {
        newHeight = (int) ((float) image.Height / image.Width * MaxIconDim);
      }
      else
      {
        newWidth = (int) ((float) image.Width / image.Height * MaxIconDim);
      }

      var bitmap = new Bitmap(image, newWidth, newHeight);
      Icon = Icon.FromHandle(bitmap.GetHicon());
    }

    private void StoreCurrentZoomValue()
    {
      var zoomRatio = (double) pictureBox1.ClientSize.Height / pictureBox1.Image.Height;
      _zoomPercentageCurrent = (int) (zoomRatio * 100);
    }

    private static void LaunchNextInstance(string path)
    {
      LaunchNextInstance(new List<string>() {path});
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

    private static IEnumerable<string> GetFilesFromDirectoryRecursively(string directory)
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

    private void DebounceTimer_Tick(object sender, EventArgs e)
    {
      Zoom();

      _debounceTimer.Stop();
    }

    private void Zoom()
    {
      if (pictureBox1.Image == null)
      {
        return;
      }

      var oldZoom = _zoomPercentageCurrent;

      _zoomPercentageCurrent += _cachedMouseEventArgs.Delta > 0 ? ZoomStep : -ZoomStep;

      if (_zoomPercentageCurrent < ZoomPercentageMin)
      {
        _zoomPercentageCurrent = ZoomPercentageMin;
      }
      else if (_zoomPercentageCurrent > ZoomPercentageMax)
      {
        _zoomPercentageCurrent = ZoomPercentageMax;
      }

      if (Math.Abs(_zoomPercentageCurrent - oldZoom) < ZoomComparisonMargin)
      {
        return;
      }

      var (deltaX, deltaY) = GetDeltasForZoom();
      if (deltaX != 0 || deltaY != 0)
      {
        pictureBox1.Location = new Point(pictureBox1.Location.X - deltaX, pictureBox1.Location.Y - deltaY);
      }

      Refresh();
    }

    private Tuple<int, int> GetDeltasForZoom()
    {
      var imageCenterX = pictureBox1.Location.X + pictureBox1.Width / 2;
      var imageCenterY = pictureBox1.Location.Y + pictureBox1.Height / 2;
      var distanceToCursorX = imageCenterX - _cachedMouseEventArgs.X;
      var distanceToCursorY = imageCenterY - _cachedMouseEventArgs.Y;

      var newWidth = pictureBox1.Image.Width * _zoomPercentageCurrent / 100;
      var newHeight = pictureBox1.Image.Height * _zoomPercentageCurrent / 100;
      pictureBox1.Size = new Size((int)newWidth, (int)newHeight);

      var newImageCenterX = pictureBox1.Location.X + pictureBox1.Width / 2;
      var newImageCenterY = pictureBox1.Location.Y + pictureBox1.Height / 2;
      var newDistanceToCursorX = newImageCenterX - _cachedMouseEventArgs.X;
      var newDistanceToCursorY = newImageCenterY - _cachedMouseEventArgs.Y;

      var deltaX = newDistanceToCursorX - distanceToCursorX;
      var deltaY = newDistanceToCursorY - distanceToCursorY;

      return new Tuple<int, int>(deltaX, deltaY);
    }
  }
}