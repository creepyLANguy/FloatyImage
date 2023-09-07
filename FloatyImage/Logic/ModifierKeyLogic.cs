using System;
using System.Windows.Forms;

namespace FloatyImage
{
  public class ModiferKeyUtils
  {
    [Flags]
    public enum ModifierAlias
    {
      None = 0,
      Control = 1,
      Alt = 2,
      Shift = 4
    }

    public static ModifierAlias GetModifierKeyMask(bool control, bool alt, bool shift)
    {
      var mask = ModifierAlias.None;

      if (control)
      {
        mask |= ModifierAlias.Control;
      }

      if (alt)
      {
        mask |= ModifierAlias.Alt;
      }

      if (shift)
      {
        mask |= ModifierAlias.Shift;
      }

      return mask;
    }

    public static ModifierAlias GetModifierKeyMask(KeyEventArgs e)
    {
      return GetModifierKeyMask(e.Control, e.Alt, e.Shift);
    }
  }
}
