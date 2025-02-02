using System.Drawing;
using System.Windows.Forms;

namespace FloatyImage
{
  public sealed partial class Form1
  {
    private readonly ContextMenu _contextMenu = new();
    private readonly MenuItem _menuItemNew = new("New");
    private readonly MenuItem _menuItemOpen = new("Open");
    private readonly MenuItem _menuItemCut = new("Cut");
    private readonly MenuItem _menuItemCopy = new("Copy");
    private readonly MenuItem _menuItemPaste = new("Paste");
    private readonly MenuItem _menuItemRotateRight = new("Rotate Right");
    private readonly MenuItem _menuItemRotateLeft = new("Rotate Left");
    private readonly MenuItem _menuItemRecenter = new("Recenter");
    private readonly MenuItem _menuItemOneToOne = new("Actual Size");
    private readonly MenuItem _menuItemTogglePin = new(FloatyStrings.PinString);
    private readonly MenuItem _menuItemToggleLock = new(FloatyStrings.LockString);
    private readonly MenuItem _menuItemToggleFloat = new(FloatyStrings.UnfloatString);
    private readonly MenuItem _menuItemColourDivider = new("-");
    private readonly MenuItem _menuItemColourHex = new();
    private readonly MenuItem _menuItemColourRgb = new();
    private readonly MenuItem _menuItemHelp = new("Help");

    private static ColorConverter _colourConverter;

    private void SetupContextMenu()
    {
      _contextMenu.Popup += ContextMenu_Opening;
      _contextMenu.Collapse += ContextMenu_Closing;

      _menuItemNew.Click += LaunchNewInstance;
      _menuItemOpen.Click += ShowOpenDialog;
      _menuItemCut.Click += Cut;
      _menuItemCopy.Click += Copy;
      _menuItemPaste.Click += Paste;
      _menuItemRotateRight.Click += RotateRight;
      _menuItemRotateLeft.Click += RotateLeft;
      _menuItemRecenter.Click += ResetPictureBoxPosition;
      _menuItemOneToOne.Click += ZoomOneToOne;
      _menuItemTogglePin.Click += ToggleTitlebar;
      _menuItemToggleLock.Click += ToggleImagePositionLock;
      _menuItemToggleFloat.Click += ToggleAlwaysOnTop;
      _menuItemColourHex.Click += CopyTextToClipboard;
      _menuItemColourRgb.Click += CopyTextToClipboard;
      _menuItemHelp.Click += LaunchHelp;

      _contextMenu.MenuItems.Add(_menuItemNew);
      _contextMenu.MenuItems.Add(_menuItemOpen);
      _contextMenu.MenuItems.Add(new MenuItem("-"));
      _contextMenu.MenuItems.Add(_menuItemHelp);
      _contextMenu.MenuItems.Add(new MenuItem("-"));
      _contextMenu.MenuItems.Add(_menuItemCut);
      _contextMenu.MenuItems.Add(_menuItemCopy);
      _contextMenu.MenuItems.Add(_menuItemPaste);
      _contextMenu.MenuItems.Add(new MenuItem("-"));
      _contextMenu.MenuItems.Add(_menuItemRotateRight);
      _contextMenu.MenuItems.Add(_menuItemRotateLeft);
      _contextMenu.MenuItems.Add(_menuItemRecenter);
      _contextMenu.MenuItems.Add(_menuItemOneToOne);
      _contextMenu.MenuItems.Add(new MenuItem("-"));
      _contextMenu.MenuItems.Add(_menuItemTogglePin);
      _contextMenu.MenuItems.Add(_menuItemToggleLock);
      _contextMenu.MenuItems.Add(_menuItemToggleFloat);
      _contextMenu.MenuItems.Add(_menuItemColourDivider);
      _contextMenu.MenuItems.Add(_menuItemColourHex);
      _contextMenu.MenuItems.Add(_menuItemColourRgb);

      _colourConverter = new ColorConverter();

      _menuItemColourHex.OwnerDraw = true;
      _menuItemColourHex.DrawItem += MenuItem_DrawColour;
      _menuItemColourHex.MeasureItem += MenuItem_Measure;
      _menuItemColourRgb.OwnerDraw = true;
      _menuItemColourRgb.DrawItem += MenuItem_DrawColour;
      _menuItemColourRgb.MeasureItem += MenuItem_Measure;

      ContextMenu = _contextMenu;
    }

    private void MenuItem_DrawColour(object sender, DrawItemEventArgs e)
    {
      if (sender is not MenuItem item)
      {
        return;
      }

      var backColour = (Color?)_colourConverter.ConvertFromString(item.Text) ?? SystemColors.Control;

      using (var backgroundBrush = new SolidBrush(backColour))
      {
        e.Graphics.FillRectangle(backgroundBrush, e.Bounds.X, e.Bounds.Y, e.Bounds.Width , e.Bounds.Height - 1); //leave 1px padding at bottom
      }

      DrawColourText(item.Text, backColour, e);

      Cursor = e.State.HasFlag(DrawItemState.Selected) ? Cursors.Cross: Cursors.Default;
    }

    private static void DrawColourText(string text, Color backColour, DrawItemEventArgs e)
    {
      var drawString = e.State.HasFlag(DrawItemState.Selected) ? "Copy: " + text : text;
      var textSize = e.Graphics.MeasureString(drawString, e.Font);
      var textX = e.Bounds.Left + (e.Bounds.Width - textSize.Width) / 2;
      var textY = e.Bounds.Top + (e.Bounds.Height - textSize.Height) / 2;
      using var textBrush = new SolidBrush(GetReadableTextColor(backColour));
      e.Graphics.DrawString(drawString, e.Font, textBrush, textX, textY);
    }

    private static Color GetReadableTextColor(Color backgroundColor)
      => (0.299 * backgroundColor.R) + (0.587 * backgroundColor.G) + (0.114 * backgroundColor.B) < 128
        ? Color.White
        : Color.Black;

    private void MenuItem_Measure(object sender, MeasureItemEventArgs e)
    {
      //AL.//TODO - find some way to cache the intended size of the item and then refer to it here
      e.ItemHeight = 25; // Adjust height
      e.ItemWidth = 100; // Adjust width if needed
    }

  }
}