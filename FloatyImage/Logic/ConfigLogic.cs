using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace FloatyImage
{
  public sealed partial class Form1
  {
    public List<HotKey> ReadHotKeyConfig()
    {
      try
      {
        var jsonString = File.ReadAllText(ConfigFile);

        return JsonConvert.DeserializeObject<List<HotKey>>(jsonString);
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());

        SaveHotKeyConfig(DefaultHotKeys);

        return DefaultHotKeys;
      }
    }

    public static void SaveHotKeyConfig(List<HotKey> hotKeys)
    {
      var jsonString = JsonConvert.SerializeObject(hotKeys, Formatting.Indented);

      using (var sw = File.CreateText(ConfigFile))
      {
        sw.WriteLine(jsonString);
      }
    }
  }
}
