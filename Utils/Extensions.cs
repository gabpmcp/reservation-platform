using System.Collections;
using System.Collections.Immutable;
using ReservationPlatform.Business;
using ReservationPlatform.CQRS;

namespace ReservationPlatform.Utils
{
    public static class DictionaryExtensions
    {
        public static T Get<T>(this IDictionary<string, object> dictionary, string key)
        {
            if (!dictionary.TryGetValue(key, out object? value))
                throw new KeyNotFoundException($"The key '{key}' was not found in the dictionary.");
            if (value is not T)
                throw new InvalidCastException($"The value associated with the key '{key}' cannot be cast to type '{typeof(T)}'.");

            return (T)value;
        }

        public static Channel GetChannel(this IDictionary<string, object> dictionary, string key)
        {
            if (!dictionary.TryGetValue(key, out object? value))
                throw new KeyNotFoundException($"The key '{key}' was not found in the dictionary.");
            if (value == null)
                throw new InvalidCastException($"The value associated with the key '{key}' is not a valid string.");

            return value switch
            {
                "Web" => new Channel.Web(),
                "Mobile" => new Channel.Mobile(),
                "External" => new Channel.External(),
                "Kiosk" => new Channel.Kiosk(),
                "Phone" => new Channel.Phone(),
                "InPerson" => new Channel.InPerson(),
                _ => throw new ArgumentException($"The value '{value}' is not a valid channel.")
            };
        }

        public static NotificationChannel GetNotificationChannel(this IDictionary<string, object> dictionary, string key)
        {
            if (!dictionary.TryGetValue(key, out object? value))
                throw new KeyNotFoundException($"The key '{key}' was not found in the dictionary.");
            if (value == null)
                throw new InvalidCastException($"The value associated with the key '{key}' is not a valid string.");

            return value switch
            {
                "PushNotification" => new NotificationChannel.PushNotification(),

                "PhoneCall" when dictionary.ContainsKey("PhoneNumber") && dictionary["PhoneNumber"] is string phoneNumber =>
                    new NotificationChannel.PhoneCall(phoneNumber),

                "Mail" when dictionary.ContainsKey("Email") && dictionary.ContainsKey("Message") &&
                            dictionary["Email"] is string email && dictionary["Message"] is string message =>
                    new NotificationChannel.Mail(email, message),

                "WhatsApp" when dictionary.ContainsKey("PhoneNumber") && dictionary.ContainsKey("Message") &&
                                dictionary["PhoneNumber"] is string phoneNum && dictionary["Message"] is string msg =>
                    new NotificationChannel.WhatsApp(phoneNum, msg),

                _ => throw new ArgumentException($"The value '{value}' is not a valid notification channel.")
            };
        }

        public static ImmutableDictionary<string, object> HandleFaultedTask<T, TInput>(this Task<T> task, TInput command) =>
            task.IsFaulted
                ? task.Exception?.Data.Cast<DictionaryEntry>()
                    .Where(entry => entry.Key != null && entry.Value != null)
                    .Select(entry => (entry.Key.ToString()!, entry.Value!))
                    .ToImmutableDictionary(entry => entry.Item1, entry => entry.Item2)
                    .Add("command", command!)
                    .Add("error", task.Exception.Message)
                ?? ImmutableDictionary<string, object>.Empty
                : task.Result as ImmutableDictionary<string, object> ?? ImmutableDictionary<string, object>.Empty;
    }
}