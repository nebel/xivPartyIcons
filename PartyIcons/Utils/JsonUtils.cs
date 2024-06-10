using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PartyIcons.Utils;

public class EnumKeyConverter<TEnum, TValue> : JsonConverter where TEnum : Enum
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Dictionary<TEnum, TValue>);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer ser)
    {
        if (value == null) {
            writer.WriteNull();
            return;
        }

        var jObject = new JObject();
        foreach (var pair in (Dictionary<TEnum, TValue>)value) {
            if (pair.Value is { } val) {
                jObject.Add(Convert.ToInt32(pair.Key).ToString(), JToken.FromObject(val, ser));
            }
        }

        jObject.WriteTo(writer);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existing, JsonSerializer ser)
    {
        var dict = new Dictionary<TEnum, TValue>();
        foreach (var pair in JObject.Load(reader)) {
            var key = (TEnum)Enum.ToObject(typeof(TEnum), int.Parse(pair.Key));
            if (pair.Value is { } tokenValue) {
                if (tokenValue.ToObject<TValue>(ser) is { } val) {
                    dict.Add(key, val);
                }
            }
        }

        return dict;
    }
}