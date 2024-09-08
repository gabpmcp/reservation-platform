using ReservationPlatform.CQRS;
using StackExchange.Redis;
using System.Collections.Immutable;
using ReservationPlatform.Utils;
using static ReservationPlatform.Business.Decisions;

namespace ReservationPlatform.Helpers
{
    public static class RedisCache
    {
        private static readonly ConnectionMultiplexer RedisConnection = ConnectionMultiplexer.Connect("localhost"); // Cambia "localhost" por la URL de tu servidor Redis si es necesario.
        private static readonly IDatabase RedisDb = RedisConnection.GetDatabase();

        // Deserializa el estado almacenado en Redis
        public static Func<Func<Command, StateRetriever>> GetAggregate = () => command =>
            userId => JsonSerialization.DeserializeAsync<ImmutableDictionary<string, object>>()(() => Task.FromResult(ImmutableDictionary<string, object>.Empty))(RedisDb.StringGet(userId).ToString())
                .ContinueWith(task => task.HandleFaultedTask(command));

        public async static Task<T> SetState<T>(string userId, T state)
        {
            // Serializa y guarda el estado en Redis
            var serializedState = JsonSerialization.Serialize(state);
            await RedisDb.StringSetAsync(userId, serializedState);
            return state;
        }
    }
}