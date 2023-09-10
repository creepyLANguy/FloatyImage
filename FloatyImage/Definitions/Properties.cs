using System.Drawing.Drawing2D;
using System.Drawing;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace FloatyImage
{
  public sealed partial class Form1
  {
    private const string ConfigFile = "config.json";

    private static readonly Cursor SpecialCursorDefault = Cursors.Hand;
    private static readonly Cursor LockedCursorDefault = Cursors.Default;

    private const int ZoomPercentageMin = 1;
    private const int ZoomPercentageMax = 500;
    private const int ZoomStep = 3;
    private const float ZoomComparisonMargin = 0.0f;

    private const int FadeIntervalMilliseconds = 10;
    private const double FadeOpacityStep = 0.1;
    private const double FadeFloor = 0.2;
    private const double FadeCeiling = 0.3;

    private const int DebounceTimerInterval = 1;

    private static readonly Icon DefaultIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
    private static readonly int MaxIconDim = Math.Max(DefaultIcon.Width, DefaultIcon.Height);

    private static readonly Color BgColor1 = Color.White;
    private static readonly Color BgColor2 = Color.LightGray;
    private const HatchStyle BgStyle = HatchStyle.LargeGrid;
    private static readonly Color OverlayColor = Color.FromArgb(128, Color.MediumTurquoise);

    private static readonly List<HotKey> StandardHotKeys = new()
    {
      new HotKey(false, false, false, Keys.Back, HotKeyAction.Clear),
      new HotKey(false, false, false, Keys.Delete, HotKeyAction.Clear),

      new HotKey(true, false, false, Keys.X, HotKeyAction.Cut),
      new HotKey(true, false, false, Keys.C, HotKeyAction.Copy),
      new HotKey(true, false, false, Keys.Z, HotKeyAction.Paste),

      new HotKey(true, false, false, Keys.O, HotKeyAction.Open),

      new HotKey(true, false, false, Keys.P, HotKeyAction.TogglePin),
      new HotKey(true, false, false, Keys.L, HotKeyAction.ToggleLock),
      new HotKey(true, false, false, Keys.T, HotKeyAction.ToggleFloat),

      new HotKey(true, false, false, Keys.R, HotKeyAction.Recenter),
      new HotKey(true, false, false, Keys.D1, HotKeyAction.ActualSize)
    };
  }
}


