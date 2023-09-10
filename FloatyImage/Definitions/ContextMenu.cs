using System.Windows.Forms;

namespace FloatyImage
{
  public sealed partial class Form1
  {
    private readonly ContextMenu _contextMenu = new();
    private readonly MenuItem _menuItemOpen = new("Open");
    private readonly MenuItem _menuItemCut = new("Cut");
    private readonly MenuItem _menuItemCopy = new("Copy");
    private readonly MenuItem _menuItemPaste = new("Paste");
    private readonly MenuItem _menuItemRecenter = new("Recenter");
    private readonly MenuItem _menuItemOneToOne = new("Actual Size");
    private readonly MenuItem _menuItemTogglePin = new(FloatyStrings.PinString);
    private readonly MenuItem _menuItemToggleLock = new(FloatyStrings.LockString);
    private readonly MenuItem _menuItemToggleFloat = new(FloatyStrings.UnfloatString);
    private readonly MenuItem _menuItemColourDivider = new("-");
    private readonly MenuItem _menuItemColourHex = new();
    private readonly MenuItem _menuItemColourRgb = new();
    private readonly MenuItem _menuItemHelp = new("Help");

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
      _menuItemTogglePin.Click += ToggleTitlebar;
      _menuItemToggleLock.Click += ToggleImagePositionLock;
      _menuItemToggleFloat.Click += ToggleAlwaysOnTop;
      _menuItemColourHex.Click += CopyTextToClipboard;
      _menuItemColourRgb.Click += CopyTextToClipboard;
      _menuItemHelp.Click += LaunchHelp;

      _contextMenu.MenuItems.Add(_menuItemOpen);
      _contextMenu.MenuItems.Add(new MenuItem("-"));
      _contextMenu.MenuItems.Add(_menuItemHelp);
      _contextMenu.MenuItems.Add(new MenuItem("-"));
      _contextMenu.MenuItems.Add(_menuItemCut);
      _contextMenu.MenuItems.Add(_menuItemCopy);
      _contextMenu.MenuItems.Add(_menuItemPaste);
      _contextMenu.MenuItems.Add(new MenuItem("-"));
      _contextMenu.MenuItems.Add(_menuItemRecenter);
      _contextMenu.MenuItems.Add(_menuItemOneToOne);
      _contextMenu.MenuItems.Add(new MenuItem("-"));
      _contextMenu.MenuItems.Add(_menuItemTogglePin);
      _contextMenu.MenuItems.Add(_menuItemToggleLock);
      _contextMenu.MenuItems.Add(_menuItemToggleFloat);
      _contextMenu.MenuItems.Add(_menuItemColourDivider);
      _contextMenu.MenuItems.Add(_menuItemColourHex);
      _contextMenu.MenuItems.Add(_menuItemColourRgb);

      ContextMenu = _contextMenu;
    }
  }
}