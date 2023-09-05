using System.Windows.Forms;

namespace FloatyImage
{
  public sealed partial class Form1
  {
    private static readonly string ApplicationName = Application.ProductName;

    private const string DefaultTitle = "(Right click on canvas or drag on images/folders to begin)";
    private const string PastedImageTitle = "[Pasted Image]";

    private const string LockString = "Lock";
    private const string UnlockString = "Unlock";

    private const string PersistString = "Persist";
    private const string StopPersistingString = "Unpersist";

    private const string FailedToLoadImageMessageString = "Failed to load image:";
    private static readonly string FailedToLoadImageCaptionString = ApplicationName + " Error";

    private const string PossibleErrorMessageString = "Possible error encountered while loading:";
    private static readonly string PossibleErrorCaptionString = ApplicationName + " Warning";
  }

}


