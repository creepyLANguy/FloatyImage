using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using static FloatyImage.FloatyStrings;

namespace FloatyImage
{
  public sealed partial class Form1 : Form
  {
    private readonly List<HotKey> _hotKeys;

    private Cursor _specialCursor = SpecialCursorDefault;

    private readonly HatchBrush _backgroundBrush 
      = new HatchBrush(BgStyle, BgColor1, BgColor2);

    private readonly SolidBrush _overlayBrush 
      = new SolidBrush(OverlayColor);

    private readonly System.Windows.Forms.Timer _zoomDebounceTimer
      = new System.Windows.Forms.Timer();

    private readonly OpenFileDialog _openFileDialog
      = new OpenFileDialog();

    private float _zoomPercentageCurrent;

    private Point _mouseLocation;

    private bool _isHovering;
    private bool _isDragging;

    private int _borderWidth;
    private int _titlebarHeight;
    private bool _isTitlebarHidden;
    private bool _isImagePositionLocked;

    private int _cachedPictureBoxPosX;
    private int _cachedPictureBoxPosY;

    private MouseEventArgs _cachedMouseEventArgs;

    public Form1(string[] args)
    {
      InitializeComponent();

      _hotKeys = ReadHotKeyConfig();

      _zoomDebounceTimer.Interval = DebounceTimerInterval;
      _zoomDebounceTimer.Tick += DebounceTimer_Tick;

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

    private void PaintBackground(object sender, PaintEventArgs e)
    {
      if (_isHovering)
      {
        return;
      }

      e.Graphics.FillRectangle(_backgroundBrush, ClientRectangle);
    }

    private void ToggleTitlebar(object sender, EventArgs e)
    {
      FadeOut();

      if (_isTitlebarHidden)
      {
        _menuItemTogglePin.Text = PinString;
        FormBorderStyle = FormBorderStyle.Sizable;
        Location = new Point(Location.X - _borderWidth, Location.Y - _titlebarHeight);
      }
      else
      {
        _menuItemTogglePin.Text = UnpinString;
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

      void FadeOut()
      {
        while (Opacity > FadeFloor)
        {
          Thread.Sleep(FadeIntervalMilliseconds);
          Opacity -= FadeOpacityStep;
        }

        Opacity = 0;
      }

      void FadeIn()
      {
        while (Opacity < FadeCeiling)
        {
          Thread.Sleep(FadeIntervalMilliseconds);
          Opacity += FadeOpacityStep;
        }

        Opacity = 1;
      }
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

      Color colour;
      try
      {
        colour = bitmap.GetPixel(clientCursorPos.X, clientCursorPos.Y);
      }
      catch (Exception)
      {
        HideColourIndicators();
        return;
      }

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

      void HideColourIndicators()
      {
        btn_colour.Visible = false;
        _menuItemColourDivider.Visible = false;
        _menuItemColourHex.Visible = false;
        _menuItemColourRgb.Visible = false;
      }
    }

    private void CopyTextToClipboard(object sender, EventArgs e)
    {
      Clipboard.SetText(((MenuItem) sender).Text);
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
        LoadImage(image, PastedImageTitle);

        ResetPictureBoxPosition();
      }
      catch (Exception ex)
      {
        LogException(ex);
      }
    }

    private void ResetPictureBoxPosition(object sender = null, EventArgs e = null)
    {
      if (pictureBox1.Image == null)
      {
        return;
      }

      pictureBox1.Width = ClientSize.Width;
      pictureBox1.Height = ClientSize.Height;

      pictureBox1.Location = new Point(0, 0);

      StoreCurrentZoomValue();

      Refresh();
    }

    private void ToggleAlwaysOnTop(object sender = null, EventArgs e = null)
    {
      var isAlwaysOnTop = TopLevel && TopMost;
      TopMost = !isAlwaysOnTop;
      _menuItemToggleFloat.Text = !isAlwaysOnTop ? UnfloatString : FloatString;
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

      IEnumerable<string> GetFilesFromDirectoryRecursively(string directory)
      {
        var files = Directory.GetFiles(directory).ToList();

        foreach (var subDirectory in Directory.GetDirectories(directory))
        {
          files.AddRange(GetFilesFromDirectoryRecursively(subDirectory));
        }

        return files;
      }
    }

    private void ToggleImagePositionLock(object sender = null, EventArgs e = null)
    {
      _isImagePositionLocked = !_isImagePositionLocked;

      _menuItemToggleLock.Text = _isImagePositionLocked ? UnlockString : LockString;

      _specialCursor = _isImagePositionLocked ? LockedCursorDefault : SpecialCursorDefault;
      pictureBox1.Cursor = _isImagePositionLocked ? _specialCursor : Cursors.Default;
    }

    private static void LogException(Exception ex)
    {
      Console.WriteLine(ex.ToString());
    }
  }
}