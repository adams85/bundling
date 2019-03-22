using System.IO;
using Newtonsoft.Json;

namespace Karambolo.AspNetCore.Bundling.Internal.Helpers
{
    internal static class SerializationHelper
    {
        private static readonly JsonSerializer s_serializer = JsonSerializer.CreateDefault();

        public static void Serialize<T>(TextWriter writer, T obj)
        {
            s_serializer.Serialize(writer, obj, typeof(T));
        }

        public static T Deserialize<T>(TextReader reader)
        {
            return (T)s_serializer.Deserialize(reader, typeof(T));
        }
    }
}
