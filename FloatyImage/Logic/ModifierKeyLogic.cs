using System.Windows.Forms;

namespace FloatyImage
{
  public class ModiferKeyUtils
  {
    public struct ModifierAliases
    {
      public static int Control = 1;
      public static int Alt = 2;
      public static int Shift = 4;
    }

    public static int GetModifierKeyMask(bool control, bool alt, bool shift)
    {
      var mask = 0;

      if (control)
      {
        mask += ModifierAliases.Control;
      }

      if (alt)
      {
        mask += ModifierAliases.Alt;
      }

      if (shift)
      {
        mask += ModifierAliases.Shift;
      }

      return mask;
    }

    public static int GetModifierKeyMask(KeyEventArgs e)
    {
      return GetModifierKeyMask(e.Control, e.Alt, e.Shift);
    }
  }
}
