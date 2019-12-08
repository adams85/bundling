using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Karambolo.AspNetCore.Bundling.Internal.Helpers
{
    internal static class SerializationHelper
    {
        private sealed class JsonConverterTimeSpan : JsonConverter<TimeSpan>
        {
            public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return TimeSpan.Parse(reader.GetString(), CultureInfo.InvariantCulture);
            }

            public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString(null, CultureInfo.InvariantCulture));
            }
        }

        private static readonly JsonSerializerOptions s_serializerOptions = new JsonSerializerOptions
        {
            Converters = { new JsonConverterTimeSpan() },
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        public static void Serialize<T>(Stream stream, T obj)
        {
            using (var writer = new Utf8JsonWriter(stream))
                JsonSerializer.Serialize(writer, obj, s_serializerOptions);
        }

        public static T Deserialize<T>(in ReadOnlySpan<byte> data)
        {
            return JsonSerializer.Deserialize<T>(data, s_serializerOptions);
        }
    }
}
