using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Karambolo.AspNetCore.Bundling.Internal.Helpers
{
    static class SerializationHelper
    {
        static readonly JsonSerializer serializer = JsonSerializer.CreateDefault();

        public static void Serialize<T>(TextWriter writer, T obj)
        {
            serializer.Serialize(writer, obj, typeof(T));
        }

        public static T Deserialize<T>(TextReader reader)
        {
            return (T)serializer.Deserialize(reader, typeof(T));
        }
    }
}
