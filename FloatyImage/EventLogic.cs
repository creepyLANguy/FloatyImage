using System.Windows.Forms;
using System;
using System.Drawing;

namespace FloatyImage
{
  public partial class Form1
  {
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

    private void Form1_Load(object sender, EventArgs e)
    {
      var screenRectangle = RectangleToScreen(ClientRectangle);
      _titlebarHeight = screenRectangle.Top - Top;
      _borderWidth = (screenRectangle.Left - Left);

      DoubleBuffered = true;
      AllowDrop = true;

      TopLevel = true;
      TopMost = true;

      pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
      pictureBox1.BackColor = Color.Transparent;

      btn_colour.Enabled = false;
      btn_colour.Visible = false;
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
        case Keys.F:
          ToggleAlwaysOnTop(sender, e);
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

    private void PictureBox1_MouseWheel(object sender, MouseEventArgs e)
    {
      _cachedMouseEventArgs = e;
      _zoomDebounceTimer.Start();
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
  }
}
