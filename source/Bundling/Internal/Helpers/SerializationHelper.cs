using System;
using System.Globalization;
using System.IO;

namespace Karambolo.AspNetCore.Bundling.Internal.Helpers
{
#if !NETCOREAPP3_0_OR_GREATER
    using Newtonsoft.Json;
#else
    using System.Text.Json;
    using System.Text.Json.Serialization;
#endif

    internal static class SerializationHelper
    {
#if !NETCOREAPP3_0_OR_GREATER
        private static readonly JsonSerializer s_serializer = JsonSerializer.CreateDefault();

        public static void Serialize<T>(TextWriter writer, T obj)
        {
            s_serializer.Serialize(writer, obj, typeof(T));
        }

        public static T Deserialize<T>(TextReader reader)
        {
            return (T)s_serializer.Deserialize(reader, typeof(T));
        }
#else
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

        public static void Serialize<T>(TextWriter writer, T obj)
        {
            var json = JsonSerializer.Serialize(obj, s_serializerOptions);
            writer.Write(json);
        }

        public static T Deserialize<T>(TextReader reader)
        {
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<T>(json, s_serializerOptions);
        }
#endif
    }
}
