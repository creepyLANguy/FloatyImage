using System.IO;

namespace FloatyImage
{
  public sealed partial class Form1
  {
    private static void LoadConfig()
    {
      if (File.Exists(ConfigFile) == false)
      {
        return;
      }

      var configFileContents = File.ReadAllText(ConfigFile);
      //AL.
      //var configJson = configFileContents.toJsonOrSomethingIDunno...
    }
  }
}
