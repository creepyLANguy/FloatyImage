using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace FloatyImage
{
  public sealed partial class Form1
  {
    private const int TextPadding = 32;
    private const int TextMinWidth = 320;
    private const int TextMaxWidth = 1200;

    private static Bitmap RenderText(string text)
    {
      using var measureBitmap = new Bitmap(1, 1);
      using var measureGraphics = Graphics.FromImage(measureBitmap);
      using var font = new Font(SystemFonts.MessageBoxFont.FontFamily, 14.0f, FontStyle.Regular, GraphicsUnit.Point);

      measureGraphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
      var format = CreateStringFormat();
      var availableTextWidth = TextMaxWidth - (TextPadding * 2);

      var longestLineWidth = 0.0f;
      foreach (var line in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
      {
        longestLineWidth = Math.Max(longestLineWidth, measureGraphics.MeasureString(line, font, int.MaxValue, format).Width);
      }

      var textWidth = Math.Max(TextMinWidth, Math.Min(availableTextWidth, (int)Math.Ceiling(longestLineWidth)));
      var layoutWidth = textWidth;
      var measuredHeight = measureGraphics.MeasureString(text, font, new SizeF(layoutWidth, float.MaxValue), format).Height;
      var textHeight = Math.Max(font.GetHeight(measureGraphics), measuredHeight);

      var bitmap = new Bitmap(
        textWidth + (TextPadding * 2),
        (int)Math.Ceiling(textHeight) + (TextPadding * 2)
      );

      using var graphics = Graphics.FromImage(bitmap);
      graphics.Clear(Color.Transparent);
      graphics.SmoothingMode = SmoothingMode.HighQuality;
      graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

      using var textBrush = new SolidBrush(Color.Black);
      graphics.DrawString(
        text,
        font,
        textBrush,
        new RectangleF(TextPadding, TextPadding, layoutWidth, textHeight),
        format
      );

      return bitmap;
    }

    private static StringFormat CreateStringFormat()
    {
      return new StringFormat
      {
        Alignment = StringAlignment.Near,
        LineAlignment = StringAlignment.Near,
        FormatFlags = StringFormatFlags.LineLimit,
        Trimming = StringTrimming.None
      };
    }
  }
}
