﻿using System.Drawing.Drawing2D;
using System.Drawing;
using System;
using System.Windows.Forms;

namespace FloatyImage
{
  public sealed partial class Form1
  {
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

    private static readonly Color BackgroundColor1 = Color.White;
    private static readonly Color BackgroundColor2 = Color.LightGray;
    private const HatchStyle BackgroundStyle = HatchStyle.LargeGrid;
    private static readonly Color OverlayColor = Color.FromArgb(128, Color.MediumTurquoise);
  }
}


