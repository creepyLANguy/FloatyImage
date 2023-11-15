using System.Drawing;
using System;

namespace FloatyImage
{
  public sealed partial class Form1
  {
    private void DebounceTimer_Tick(object sender, EventArgs e)
    {
      if (_isImagePositionLocked == false)
      {
        Zoom();
      }

      _zoomDebounceTimer.Stop();
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

    internal Tuple<int, int> GetDeltasForZoom()
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

    private void ZoomOneToOne(object sender = null, EventArgs e = null)
    {
      if (pictureBox1.Image == null)
      {
        return;
      }

      pictureBox1.Width = pictureBox1.Image.Width;
      pictureBox1.Height = pictureBox1.Image.Height;

      pictureBox1.Location = new Point(0, 0);

      StoreCurrentZoomValue();

      Refresh();
    }

    private void StoreCurrentZoomValue()
    {
      var zoomRatio = (double)pictureBox1.ClientSize.Height / pictureBox1.Image.Height;
      _zoomPercentageCurrent = (int)(zoomRatio * 100);
    }

    
  }
}

