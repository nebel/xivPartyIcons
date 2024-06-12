using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PartyIcons.Utils;

// Serialize/deserialize dictionaries with enum keys using the integer-as-a-string for the key, rather than the enum
// variant name. Pro: allows us to rename enum variants. Con: we can't change enum order.
public class EnumKeyConverter<TEnum, TValue> : JsonConverter<Dictionary<TEnum, TValue>> where TEnum : Enum
{
    public override Dictionary<TEnum, TValue> ReadJson(JsonReader reader, Type objectType,
        Dictionary<TEnum, TValue>? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var dict = new Dictionary<TEnum, TValue>();
        foreach (var (jsonKey, jsonVal) in JObject.Load(reader)) {
            var key = (TEnum)Enum.ToObject(typeof(TEnum), int.Parse(jsonKey));
            if (jsonVal != null && jsonVal.ToObject<TValue>(serializer) is { } val) {
                dict.Add(key, val);
            }
        }

        return dict;
    }

    public override void WriteJson(JsonWriter writer, Dictionary<TEnum, TValue>? value, JsonSerializer serializer)
    {
        if (value == null) {
            writer.WriteNull();
            return;
        }

        var jObject = new JObject();
        foreach (var (key, val) in value) {
            jObject.Add(Convert.ToInt32(key).ToString(), val != null ? JToken.FromObject(val, serializer) : null);
        }

        jObject.WriteTo(writer);
    }
}