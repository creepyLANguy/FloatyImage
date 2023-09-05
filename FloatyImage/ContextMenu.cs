using System;
using System.Windows.Forms;

namespace FloatyImage
{
  public partial class Form1
  {
    private readonly ContextMenu _contextMenu = new ContextMenu();
    private readonly MenuItem _menuItemOpen = new MenuItem("Open");
    private readonly MenuItem _menuItemCut = new MenuItem("Cut");
    private readonly MenuItem _menuItemCopy = new MenuItem("Copy");
    private readonly MenuItem _menuItemPaste = new MenuItem("Paste");
    private readonly MenuItem _menuItemRecenter = new MenuItem("Recenter");
    private readonly MenuItem _menuItemOneToOne = new MenuItem("Actual Size");
    private readonly MenuItem _menuItemAlwaysOnTop = new MenuItem(FloatyStrings.StopPersistingString);
    private readonly MenuItem _menuItemToggleLock = new MenuItem(FloatyStrings.LockString);
    private readonly MenuItem _menuItemColourDivider = new MenuItem("-");
    private readonly MenuItem _menuItemColourHex = new MenuItem();
    private readonly MenuItem _menuItemColourRgb = new MenuItem();

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
  }
}