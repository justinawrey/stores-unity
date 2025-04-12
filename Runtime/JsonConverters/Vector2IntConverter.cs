using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Stores
{
    public class Vector2IntConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(Vector2Int);

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer
        )
        {
            var t = serializer.Deserialize(reader);
            var iv = JsonConvert.DeserializeObject<Vector2Int>(t.ToString());
            return iv;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Vector2Int v = (Vector2Int)value;

            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(v.x);
            writer.WritePropertyName("y");
            writer.WriteValue(v.y);
            writer.WriteEndObject();
        }
    }
}
