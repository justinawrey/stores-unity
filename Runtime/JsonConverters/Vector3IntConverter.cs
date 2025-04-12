using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Stores
{
    public class Vector3IntConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(Vector3Int);

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer
        )
        {
            var t = serializer.Deserialize(reader);
            var iv = JsonConvert.DeserializeObject<Vector3Int>(t.ToString());
            return iv;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Vector3Int v = (Vector3Int)value;

            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(v.x);
            writer.WritePropertyName("y");
            writer.WriteValue(v.y);
            writer.WritePropertyName("z");
            writer.WriteValue(v.z);
            writer.WriteEndObject();
        }
    }
}
