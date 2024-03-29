﻿using System.Windows.Forms;
using System;
using System.Drawing;

namespace FloatyImage
{
  public sealed partial class Form1
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
      foreach (var hotKey in _hotKeys)
      {
        var modifierMask = ModiferKeyUtils.GetModifierKeyMask(e);

        if (hotKey.ModifierMask != modifierMask)
        {
          continue;
        }
        
        if (hotKey.Key == e.KeyCode)
        {
          ExecuteAction(hotKey.Action);
          return;
        }
      }

      void ExecuteAction(HotKeyAction action)
      {
        switch (action)
        {
          case HotKeyAction.Clear:
            ClearImage();
            break;
          case HotKeyAction.Cut:
            Cut(sender, e);
            break;
          case HotKeyAction.Copy:
            Copy(sender, e);
            break;
          case HotKeyAction.Paste:
            Paste(sender, e);
            break;
          case HotKeyAction.New:
            LaunchNewInstance(sender, e);
            break;
          case HotKeyAction.Open:
            ShowOpenDialog(sender, e);
            break;
          case HotKeyAction.TogglePin:
            ToggleTitlebar(sender, e);
            break;
          case HotKeyAction.ToggleLock:
            ToggleImagePositionLock(sender, e);
            break;
          case HotKeyAction.ToggleFloat:
            ToggleAlwaysOnTop(sender, e);
            break;
          case HotKeyAction.Recenter:
            ResetPictureBoxPosition(sender, e);
            break;
          case HotKeyAction.ActualSize:
            ZoomOneToOne(sender, e);
            break;
          case HotKeyAction.RotateRight:
            RotateRight(sender, e);
            break;
          case HotKeyAction.RotateLeft:
            RotateLeft(sender, e);
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
      }
    }

    private void Form1_Resize(object sender, EventArgs e)
    {
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

      if (_isImagePositionLocked)
      {
        return;
      }

      if (e.Button == MouseButtons.Left && _isDragging)
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

      pictureBox1.Cursor = _specialCursor;
    }

    private void PictureBox1_MouseLeave(object sender, EventArgs e)
    {
      if (pictureBox1.Image == null)
      {
        return;
      }

      pictureBox1.Cursor = Cursors.Default;
    }

    private void ContextMenu_Opening(object sender, EventArgs e)
    {
      var hasImage = pictureBox1.Image != null;
      _menuItemCut.Enabled = hasImage;
      _menuItemCopy.Enabled = hasImage;
      _menuItemPaste.Enabled = Clipboard.ContainsImage();
      _menuItemRotateRight.Enabled = hasImage;
      _menuItemRotateLeft.Enabled = hasImage;
      _menuItemRecenter.Enabled = hasImage;
      _menuItemOneToOne.Enabled = hasImage;

      DisplayCurrentPixelColour();
    }

    private void ContextMenu_Closing(object sender, EventArgs e)
    {
      btn_colour.Visible = false;
    }
  }
}
