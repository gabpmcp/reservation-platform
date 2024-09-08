using System.Collections.Immutable;
using ReservationPlatform.CQRS;

namespace ReservationPlatform.Business
{
    public abstract record NotificationChannel
    {
        public sealed record PushNotification : NotificationChannel;
        public sealed record PhoneCall(string PhoneNumber) : NotificationChannel;
        public sealed record Mail(string Email, string Message) : NotificationChannel;
        public sealed record WhatsApp(string PhoneNumber, string Message) : NotificationChannel;
    }

    public abstract record Channel
    {
        public sealed record Web : Channel;
        public sealed record Mobile : Channel;
        public sealed record External : Channel;
        public sealed record Kiosk : Channel;
        public sealed record Phone : Channel;
        public sealed record InPerson : Channel;
    }

    public abstract record ErrorType
    {
        public sealed record BusinessError : ErrorType;
        public sealed record TechnicalError : ErrorType;
    }

    public abstract record Result
    {
        public record Success(IImmutableList<Event> Events) : Result;

        public record Failure(IRecord Input, ErrorType ErrorType, IImmutableDictionary<string, object> Errors) : Result, IRecord;
    }
}