using System.Collections.Generic;
using System.Windows.Forms;

namespace FloatyImage
{
  internal class HotKey
  {
    public bool Ctrl;
    public bool Alt;
    public bool Shift;
    public bool Win;

    public List<Keys> Keys;
    
    public List<HotKeyAction> Actions;

    public HotKey(bool ctrl, bool alt, bool shift, bool win, List<Keys> keys, List<HotKeyAction> actions)
    {
      Ctrl = ctrl;
      Alt = alt;
      Shift = shift;
      Win = win;
      Keys = keys;
      Actions = actions;
    }
  }
}
