using System.Windows.Forms;

namespace FloatyImage
{
  public static class FloatyStrings
  {
    public static readonly string ApplicationName = Application.ProductName;

    public const string DefaultTitle = "(Right click on canvas or drag on images/folders to begin)";
    public const string PastedImageTitle = "[Pasted Image]";

    public const string LockString = "Lock";
    public const string UnlockString = "Unlock";

    public const string PersistString = "Float";
    public const string StopPersistingString = "Unfloat";

    public static readonly string FailedToLoadImageCaptionString = ApplicationName + " Error";
    public const string FailedToLoadImageMessageString = "Failed to load image:";
  }
}


