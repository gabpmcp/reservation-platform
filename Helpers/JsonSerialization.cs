using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ReservationPlatform.Helpers
{
    public static class JsonSerialization
    {
        public static async Task<T> DeserializeAsync<T>(Stream stream)
        {
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            return JsonConvert.DeserializeObject<T>(content);
        }

        public static async Task SerializeAsync<T>(Stream stream, T value)
        {
            using var writer = new StreamWriter(stream);
            var content = JsonConvert.SerializeObject(value);
            await writer.WriteAsync(content);
        }

        public static Func<Func<T>, Func<string, T>> Deserialize<T>() where T : class
        {
            return defaultValueProvider => content => JsonConvert.DeserializeObject<T>(content) ?? defaultValueProvider();
        }

        public static Func<Func<Task<T>>, Func<string, Task<T>>> DeserializeAsync<T>() where T : class
        {
            return defaultValueProvider => async content =>
            {
                var result = await Task.Run(() => JsonConvert.DeserializeObject<T>(content))
                                    .ConfigureAwait(false);
                return result ?? await defaultValueProvider().ConfigureAwait(false);
            };
        }

        public static string Serialize<T>(T obj) => JsonConvert.SerializeObject(obj);
    }
}