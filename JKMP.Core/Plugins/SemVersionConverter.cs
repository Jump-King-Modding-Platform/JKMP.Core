using System;
using Newtonsoft.Json;
using Semver;

namespace JKMP.Core.Plugins
{
    public class SemVersionConverter : JsonConverter<SemVersion>
    {
        public override void WriteJson(JsonWriter writer, SemVersion? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            serializer.Serialize(writer, value.ToString());
        }

        public override SemVersion? ReadJson(JsonReader reader, Type objectType, SemVersion? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null || reader.Value == null)
                return null;

            if (reader.TokenType != JsonToken.String)
                throw new JsonSerializationException($"Unexpected token type, expected string but got {reader.TokenType}. Value: {reader.Value}");

            try
            {
                return SemVersion.Parse((string)reader.Value, true);
            }
            catch (Exception ex)
            {
                throw new JsonSerializationException($"Could not parse SemVersion string: {reader.Value}", ex);
            }
        }
    }
}