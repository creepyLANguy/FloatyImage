using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace FloatyImage
{
  public sealed partial class Form1
  {
    public void SetupHotKeys(List<HotKey> fallbackHotKeys)
    {
      try
      {
        var jsonString = File.ReadAllText(ConfigFile);

        _hotKeys = JsonConvert.DeserializeObject<List<HotKey>>(jsonString);
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());

        _hotKeys = fallbackHotKeys;

        SaveHotKeyConfig(fallbackHotKeys);
      }
    }

    public static void SaveHotKeyConfig(List<HotKey> hotKeys)
    {
      var jsonString = JsonConvert.SerializeObject(hotKeys, Formatting.Indented);

      try
      {
        using var sw = File.CreateText(ConfigFile);
        sw.WriteLine(jsonString);
        sw.Close();
      }
      catch (Exception e)
      {
        Console.WriteLine(e);

        //MessageBox.Show(
        //  FailedToHandleConfigUpdatesString,
        //  GenericErrorCaptionString,
        //  MessageBoxButtons.OK,
        //  MessageBoxIcon.Warning,
        //  MessageBoxDefaultButton.Button1,
        //  MessageBoxOptions.ServiceNotification);
      }
      
    }
  }
}
