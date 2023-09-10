using Newtonsoft.Json;
using System;
using System.Windows.Forms;

namespace FloatyImage
{
  public class HotKey
  {
    public bool Ctrl;
    public bool Alt;
    public bool Shift;

    [JsonIgnore]
    public ModiferKeyUtils.ModifierAlias ModifierMask;

    [JsonConverter(typeof(KeysConverter))]
    public Keys Key { get; set; }

    [JsonConverter(typeof(HotKeyActionConverter))]
    public HotKeyAction Action { get; set; }

    public HotKey(bool ctrl, bool alt, bool shift, Keys key, HotKeyAction action)
    {
      Ctrl = ctrl;
      Alt = alt;
      Shift = shift;
      Key = key;
      Action = action;

      ModifierMask = ModiferKeyUtils.GetModifierKeyMask(ctrl, alt, shift);
    }
  }

  public class KeysConverter : JsonConverter
  {
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
      if (value is Keys key)
      {
        writer.WriteValue(key.ToString());
      }
      else
      {
        throw new InvalidOperationException("Invalid type for KeysConverter.");
      }
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      if (reader.TokenType == JsonToken.String)
      {
        if (Enum.TryParse<Keys>((string)reader.Value, out var key))
        {
          return key;
        }
      }

      throw new InvalidOperationException("Invalid JSON value for Keys.");
    }

    public override bool CanConvert(Type objectType) =>
      objectType == typeof(Keys);
  }
}
