using System.Collections.Immutable;
using ReservationPlatform.Business;
using ReservationPlatform.Utils;
using ReservationPlatform.Validations;

namespace ReservationPlatform.CQRS
{
    public interface IRecord { }

    public record Command(string Kind, Guid Id, string BusinessKey, ImmutableDictionary<string, object> Data) : IRecord
    {
        public T GetData<T>(string key) => (T)Data[key];
    }

    public record Event(string Kind, Guid Id, string BusinessKey, ImmutableDictionary<string, object> Data) : IRecord
    {
        public T Get<T>(string key) => (T)Data[key];
    }

    public static class Commands
    {
        public static Func<ImmutableDictionary<string, object>, Command> BuildCommand = data =>
            data.Get<string>("kind") switch
            {
                "AuthenticateUser" => AuthenticateUser(data.Get<string>("userId"), data.Get<string>("username") ?? string.Empty, data.Get<string>("password") ?? string.Empty),
                "UpdateUserProfile" => UpdateUserProfile(data.Get<string>("userId"), data.Get<string>("newEmail") ?? string.Empty, data.Get<IEnumerable<string>>("newRoles") ?? []),
                "DeleteUser" => DeleteUser(data.Get<string>("userId")),
                "RestoreUser" => RestoreUser(data.Get<string>("userId")),
                "CheckAvailability" => CheckAvailability(data.Get<string>("userId"), data.Get<DateTime>("moment")),
                "CreateReservation" => CreateReservation(data.Get<string>("userId"), data.GetChannel("channel"), data.Get<DateTime>("moment"), data.Get<ImmutableDictionary<string, object>>("details") ?? ImmutableDictionary<string, object>.Empty),
                "MoveReservation" => MoveReservation(data.Get<string>("userId"), data.Get<Guid>("idReservation"), data.Get<DateTime>("moment"), data.GetChannel("channel")),
                "ModifyReservation" => ModifyReservation(data.Get<string>("userId"), data.Get<Guid>("idReservation"), data.Get<ImmutableDictionary<string, object>>("details") ?? ImmutableDictionary<string, object>.Empty, data.GetChannel("channel")),
                "ConfirmReservation" => ConfirmReservation(data.Get<string>("userId"), data.Get<Guid>("idReservation"), data.GetChannel("channel")),
                "HoldReservation" => HoldReservation(data.Get<string>("userId"), data.Get<Guid>("idReservation"), data.Get<DateTime>("holdUntil"), data.GetChannel("channel")),
                "CheckInReservation" => CheckInReservation(data.Get<string>("userId"), data.Get<Guid>("idReservation"), data.Get<DateTime>("checkInMoment"), data.GetChannel("channel")),
                "CancelReservation" => CancelReservation(data.Get<string>("userId"), data.Get<Guid>("idReservation"), data.GetChannel("channel")),
                "SendReminder" => SendReminder(data.Get<string>("userId"), data.Get<string>("message"), data.GetNotificationChannel("notificationChannel")),

                _ => UnsupportedCommand()
            };

        public static Command AuthenticateUser(string userId, string username, string password) =>
            new("AuthenticateUser", Guid.NewGuid(), userId, ImmutableDictionary<string, object>.Empty
                .Add("UserId", userId)
                .Add("Username", username)
                .Add("Password", password));

        public static Command UpdateUserProfile(string userId, string newEmail, IEnumerable<string> newRoles) =>
            new("UpdateUserProfile", Guid.NewGuid(), userId, ImmutableDictionary<string, object>.Empty
                .Add("UserId", userId)
                .Add("NewEmail", newEmail)
                .Add("NewRoles", newRoles));

        public static Command DeleteUser(string userId) =>
            new("DeleteUser", Guid.NewGuid(), userId, ImmutableDictionary<string, object>.Empty
                .Add("UserId", userId));

        public static Command RestoreUser(string userId) =>
            new("RestoreUser", Guid.NewGuid(), userId, ImmutableDictionary<string, object>.Empty
                .Add("UserId", userId));

        public static Command CheckAvailability(string userId, DateTime moment) =>
            new("CheckAvailability", Guid.NewGuid(), userId, ImmutableDictionary<string, object>.Empty
                .Add("UserId", userId)
                .Add("Moment", moment));

        public static Command CreateReservation(string userId, Channel channel, DateTime moment, ImmutableDictionary<string, object> additionalDetails) =>
            new("CreateReservation", Guid.NewGuid(), userId, ImmutableDictionary<string, object>.Empty
                .Add("UserId", userId)
                .Add("Channel", channel)
                .Add("Moment", moment)
                .Add("Details", additionalDetails));

        public static Command MoveReservation(string userId, Guid reservationId, DateTime newMoment, Channel channel) =>
            new("MoveReservation", Guid.NewGuid(), userId, ImmutableDictionary<string, object>.Empty
                .Add("ReservationId", reservationId)
                .Add("UserId", userId)
                .Add("NewMoment", newMoment)
                .Add("Channel", channel));

        public static Command ModifyReservation(string userId, Guid reservationId, ImmutableDictionary<string, object> additionalDetails, Channel channel) =>
            new("ModifyReservation", Guid.NewGuid(), userId, ImmutableDictionary<string, object>.Empty
                .Add("ReservationId", reservationId)
                .Add("UserId", userId)
                .Add("Details", additionalDetails)
                .Add("Channel", channel));

        public static Command ConfirmReservation(string userId, Guid reservationId, Channel channel) =>
            new("ConfirmReservation", Guid.NewGuid(), userId, ImmutableDictionary<string, object>.Empty
                .Add("ReservationId", reservationId)
                .Add("UserId", userId)
                .Add("Channel", channel));

        public static Command HoldReservation(string userId, Guid reservationId, DateTime holdUntil, Channel channel) =>
            new("HoldReservation", Guid.NewGuid(), userId, ImmutableDictionary<string, object>.Empty
                .Add("ReservationId", reservationId)
                .Add("UserId", userId)
                .Add("HoldUntil", holdUntil)
                .Add("Channel", channel));

        public static Command CheckInReservation(string userId, Guid reservationId, DateTime checkInMoment, Channel channel) =>
            new("CheckInReservation", Guid.NewGuid(), userId, ImmutableDictionary<string, object>.Empty
                .Add("ReservationId", reservationId)
                .Add("UserId", userId)
                .Add("CheckInMoment", checkInMoment)
                .Add("Channel", channel));

        public static Command CancelReservation(string userId, Guid reservationId, Channel channel) =>
            new("CancelReservation", Guid.NewGuid(), userId, ImmutableDictionary<string, object>.Empty
                .Add("ReservationId", reservationId)
                .Add("UserId", userId)
                .Add("Channel", channel));

        public static Command SendReminder(string userId, string message, NotificationChannel channel) =>
            new("SendReminder", Guid.NewGuid(), userId, ImmutableDictionary<string, object>.Empty
                .Add("UserId", userId)
                .Add("Message", message)
                .Add("NotificationChannel", channel));

        public static Command UnsupportedCommand() =>
            new("UnsupportedCommand", Guid.Empty, string.Empty, ImmutableDictionary<string, object>.Empty);
    }

    public static class Events
    {
        public static Event UserAuthenticated(Guid userId, string token, string[] roles) =>
            new("UserAuthenticated", Guid.NewGuid(), userId.ToString(),
                ImmutableDictionary<string, object>.Empty
                    .Add("UserId", userId)
                    .Add("Token", token)
                    .Add("Roles", roles));

        public static Event AuthenticationFailed(string reason, object payload) =>
            new("AuthenticationFailed", Guid.NewGuid(), string.Empty,
                ImmutableDictionary<string, object>.Empty
                    .Add("Reason", reason)
                    .Add("Payload", payload));

        public static Event UserCreated(Guid userId, string username, string email, string[] roles) =>
            new("UserCreated", Guid.NewGuid(), userId.ToString(),
                ImmutableDictionary<string, object>.Empty
                    .Add("UserId", userId)
                    .Add("Username", username)
                    .Add("Email", email)
                    .Add("Roles", roles));

        public static Event UserProfileUpdated(Guid userId, string newEmail, string[] newRoles) =>
            new("UserProfileUpdated", Guid.NewGuid(), userId.ToString(),
                ImmutableDictionary<string, object>.Empty
                    .Add("UserId", userId)
                    .Add("NewEmail", newEmail)
                    .Add("NewRoles", newRoles));

        public static Event UserDeleted(Guid userId) =>
            new("UserDeleted", Guid.NewGuid(), userId.ToString(),
                ImmutableDictionary<string, object>.Empty
                    .Add("UserId", userId));

        public static Event UserRestored(Guid userId, string username, string email, string[] roles) =>
            new("UserRestored", Guid.NewGuid(), userId.ToString(),
                ImmutableDictionary<string, object>.Empty
                    .Add("UserId", userId)
                    .Add("Username", username)
                    .Add("Email", email)
                    .Add("Roles", roles));

        public static Event AvailabilityChecked(Guid spaceId, DateTime date, TimeSpan time, bool isAvailable) =>
            new("AvailabilityChecked", Guid.NewGuid(), spaceId.ToString(),
                ImmutableDictionary<string, object>.Empty
                    .Add("SpaceId", spaceId)
                    .Add("Date", date)
                    .Add("Time", time)
                    .Add("IsAvailable", isAvailable));

        public static Event ReservationCreated(Guid reservationId, Guid userId, DateTime moment, string additionalDetails) =>
            new("ReservationCreated", Guid.NewGuid(), reservationId.ToString(),
                ImmutableDictionary<string, object>.Empty
                    .Add("ReservationId", reservationId)
                    .Add("UserId", userId)
                    .Add("Moment", moment)
                    .Add("AdditionalDetails", additionalDetails));

        public static Event ReservationMoved(Guid reservationId, Guid userId, DateTime newMoment) =>
            new("ReservationMoved", Guid.NewGuid(), reservationId.ToString(),
                ImmutableDictionary<string, object>.Empty
                    .Add("ReservationId", reservationId)
                    .Add("UserId", userId)
                    .Add("NewMoment", newMoment));

        public static Event ReservationModified(Guid reservationId, Guid userId, string newDetails) =>
            new("ReservationModified", Guid.NewGuid(), reservationId.ToString(),
                ImmutableDictionary<string, object>.Empty
                    .Add("ReservationId", reservationId)
                    .Add("UserId", userId)
                    .Add("NewDetails", newDetails));

        public static Event ReservationConfirmed(Guid reservationId, Guid userId) =>
            new("ReservationConfirmed", Guid.NewGuid(), reservationId.ToString(),
                ImmutableDictionary<string, object>.Empty
                    .Add("ReservationId", reservationId)
                    .Add("UserId", userId));

        public static Event ReservationHeld(Guid reservationId, Guid userId, DateTime holdUntil) =>
            new("ReservationHeld", Guid.NewGuid(), reservationId.ToString(),
                ImmutableDictionary<string, object>.Empty
                    .Add("ReservationId", reservationId)
                    .Add("UserId", userId)
                    .Add("HoldUntil", holdUntil));

        public static Event ReservationCheckedIn(Guid reservationId, Guid userId, DateTime checkInDate) =>
            new("ReservationCheckedIn", Guid.NewGuid(), reservationId.ToString(),
                ImmutableDictionary<string, object>.Empty
                    .Add("ReservationId", reservationId)
                    .Add("UserId", userId)
                    .Add("CheckInDate", checkInDate));

        public static Event ReservationCancelled(Guid reservationId, Guid userId) =>
            new("ReservationCancelled", Guid.NewGuid(), reservationId.ToString(),
                ImmutableDictionary<string, object>.Empty
                    .Add("ReservationId", reservationId)
                    .Add("UserId", userId));

        public static Event ReservationFailed(string reason, object payload) =>
            new("ReservationFailed", Guid.NewGuid(), string.Empty,
                ImmutableDictionary<string, object>.Empty
                    .Add("Reason", reason)
                    .Add("Payload", payload));

        public static Event ReminderSent(Guid userId, string message, string channel, string status) =>
            new("ReminderSent", Guid.NewGuid(), userId.ToString(),
                ImmutableDictionary<string, object>.Empty
                    .Add("UserId", userId)
                    .Add("Message", message)
                    .Add("Channel", channel)
                    .Add("Status", status));

        public static Event UnsupportedEvent() =>
            new("UnsupportedEvent", Guid.Empty, string.Empty, ImmutableDictionary<string, object>.Empty);
    }
}