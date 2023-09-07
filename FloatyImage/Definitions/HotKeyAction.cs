using System;
using Newtonsoft.Json;

namespace FloatyImage
{
  public enum HotKeyAction
  {
    Clear,
    Cut,
    Copy,
    Paste,
    Open,
    TogglePin,
    ToggleLock,
    ToggleFloat,
    Recenter,
    ActualSize
  }

  public class HotKeyActionConverter : JsonConverter
  {
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
      if (value is HotKeyAction action)
      {
        writer.WriteValue(action.ToString());
      }
      else
      {
        throw new InvalidOperationException("Invalid type for HotKeyActionConverter.");
      }
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      if (reader.TokenType == JsonToken.String)
      {
        if (Enum.TryParse<HotKeyAction>((string)reader.Value, out var action))
        {
          return action;
        }
      }

      throw new InvalidOperationException("Invalid JSON value for HotKeyAction.");
    }

    public override bool CanConvert(Type objectType) => 
      objectType == typeof(HotKeyAction);
  }

}