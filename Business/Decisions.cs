using System.Collections.Concurrent;
using System.Collections.Immutable;
using ReservationPlatform.CQRS;
using ReservationPlatform.Utils;
using static ReservationPlatform.Business.ErrorType;
using static ReservationPlatform.Business.Result;

namespace ReservationPlatform.Business
{
    public class Decisions
    {
        public delegate Task<ImmutableDictionary<string, object>> StateRetriever(string userId);

        public static ConcurrentDictionary<string, ImmutableDictionary<string, object>> InMemoryStore = new();

        // Trying to get the dictionary state for local tests. If not exists, it create a immutable new one.
        public static StateRetriever GetLocalState = userId => Task.FromResult(InMemoryStore.GetOrAdd(userId, ImmutableDictionary<string, object>.Empty));

        public static Func<ImmutableDictionary<string, object>, Command, Result> Decide = (state, command) =>
            command.Kind switch
            {
                "CreateUser" => state.ContainsKey("UserId")
                ? new Failure(command, new BusinessError(), ImmutableDictionary<string, object>.Empty.Add("error", "User already exists. It cannot be created again!"))
                    : new Success(ImmutableList.Create(
                        new Event(
                            Kind: "UserCreated",
                            Guid.NewGuid(),
                            command.BusinessKey,
                            Data: ImmutableDictionary<string, object>.Empty
                                .Add("UserId", command.Data["UserId"])
                                .Add("Username", command.Data["Username"])
                                .Add("Email", command.Data["Email"])
                                .Add("Roles", command.Data["Roles"])
                                .Add("Status", "Active")
                        )
                    )),

                "UpdateUserProfile" => state.TryGetValue("Status", out object? value) && value.Equals("Deleted")
                    ? new Failure(command, new BusinessError(), ImmutableDictionary<string, object>.Empty.Add("error", "User is deleted. Profile cannot be updated!"))
                    : new Success(ImmutableList.Create(
                        new Event(
                            Kind: "UserProfileUpdated",
                            Guid.NewGuid(),
                            command.BusinessKey,
                            Data: ImmutableDictionary<string, object>.Empty
                                .Add("UserId", command.Data["UserId"])
                                .Add("NewEmail", command.Data["NewEmail"])
                                .Add("NewRoles", command.Data["NewRoles"])
                        )
                    )),

                "DeleteUser" => state.TryGetValue("Status", out object? value) && value.Equals("Deleted")
                    ? new Failure(command, new BusinessError(), ImmutableDictionary<string, object>.Empty.Add("error", "User already deleted. It cannot be updated again!"))
                    : new Success(ImmutableList.Create(
                        new Event(
                            Kind: "UserDeleted",
                            Guid.NewGuid(),
                            command.BusinessKey,
                            Data: ImmutableDictionary<string, object>.Empty
                                .Add("UserId", command.Data["UserId"])
                        )
                    )),

                "RestoreUser" => !state.TryGetValue("Status", out object? value) || !value.Equals("Deleted")
                    ? new Failure(command, new BusinessError(), ImmutableDictionary<string, object>.Empty.Add("error", "User not deleted. It cannot be restored!"))
                    : new Success(ImmutableList.Create(
                        new Event(
                            Kind: "UserRestored",
                            Guid.NewGuid(),
                            command.BusinessKey,
                            Data: ImmutableDictionary<string, object>.Empty
                                .Add("UserId", command.Data["UserId"])
                                .Add("Username", command.Data["Username"])
                                .Add("Email", command.Data["Email"])
                                .Add("Roles", command.Data["Roles"])
                                .Add("Status", "Active")
                        )
                    )),

                "CreateReservation" => state.ContainsKey("ReservationId")
                    ? new Failure(command, new BusinessError(), ImmutableDictionary<string, object>.Empty.Add("error", "Reservation already exists. It cannot be created again!"))
                    : new Success(ImmutableList.Create(
                        new Event(
                            Kind: "ReservationCreated",
                            Guid.NewGuid(),
                            command.BusinessKey,
                            Data: ImmutableDictionary<string, object>.Empty
                                .Add("ReservationId", command.Data["ReservationId"])
                                .Add("UserId", command.Data["UserId"])
                                .Add("Moment", command.Data["Moment"])
                                .Add("AdditionalDetails", command.Data["AdditionalDetails"])
                                .Add("Status", "Created")
                        )
                    )),

                "UpdateReservation" => state.TryGetValue("Status", out object? value) && value.Equals("Cancelled")
                    ? new Failure(command, new BusinessError(), ImmutableDictionary<string, object>.Empty.Add("error", "Reservation already cancelled. It cannot be updated!"))
                    : new Success(ImmutableList.Create(
                        new Event(
                            Kind: "ReservationUpdated",
                            Guid.NewGuid(),
                            command.BusinessKey,
                            Data: ImmutableDictionary<string, object>.Empty
                                .Add("ReservationId", command.Data["ReservationId"])
                                .Add("Details", command.Data["Details"])
                        )
                    )),

                "CancelReservation" => state.TryGetValue("Status", out object? value) &&
                                    (value.Equals("Cancelled") || value.Equals("Confirmed"))
                    ? new Failure(command, new BusinessError(), ImmutableDictionary<string, object>.Empty.Add("error", "Reservation was canceled or confirmed. It cannot be cancelled again!"))
                    : new Success(ImmutableList.Create(
                        new Event(
                            Kind: "ReservationCancelled",
                            Guid.NewGuid(),
                            command.BusinessKey,
                            Data: ImmutableDictionary<string, object>.Empty
                                .Add("ReservationId", command.Data["ReservationId"])
                                .Add("CancelledOn", DateTime.Now)
                        )
                    )),

                "ConfirmReservation" => state.TryGetValue("Status", out object? value) && value.Equals("Cancelled")
                    ? new Failure(command, new BusinessError(), ImmutableDictionary<string, object>.Empty.Add("error", "Reservation canceled. It cannot be confirmed!"))
                    : new Success(ImmutableList.Create(
                        new Event(
                            Kind: "ReservationConfirmed",
                            Guid.NewGuid(),
                            command.BusinessKey,
                            Data: ImmutableDictionary<string, object>.Empty
                                .Add("ReservationId", command.Data["ReservationId"])
                                .Add("ConfirmedOn", DateTime.Now)
                        )
                    )),

                "HoldReservation" => !state.TryGetValue("Status", out object? value) || !value.Equals("Created")
                    ? new Failure(command, new BusinessError(), ImmutableDictionary<string, object>.Empty.Add("error", "Reservation not created. It cannot be held!"))
                    : new Success(ImmutableList.Create(
                        new Event(
                            Kind: "ReservationHeld",
                            Guid.NewGuid(),
                            command.BusinessKey,
                            Data: ImmutableDictionary<string, object>.Empty
                                .Add("ReservationId", command.Data["ReservationId"])
                                .Add("HoldUntil", command.Data["HoldUntil"])
                        )
                    )),

                "CheckInReservation" => state.TryGetValue("Status", out object? value) && !value.Equals("Confirmed")
                    ? new Failure(command, new BusinessError(), ImmutableDictionary<string, object>.Empty.Add("error", "Reservation not confirmed, unable to check-in!"))
                    : new Success(ImmutableList.Create(
                        new Event(
                            Kind: "ReservationCheckedIn",
                            Guid.NewGuid(),
                            command.BusinessKey,
                            Data: ImmutableDictionary<string, object>.Empty
                                .Add("ReservationId", command.Data["ReservationId"])
                                .Add("CheckInDate", command.Data["CheckInDate"])
                        )
                    )),

                "SendReminder" => state.TryGetValue("LastReminder", out object? value) &&
                                    DateTime.Now - (DateTime)value < TimeSpan.FromHours(24)
                    ? new Failure(command, new BusinessError(), ImmutableDictionary<string, object>.Empty.Add("error", "Reminder already sent recently, don't send another!"))
                    : new Success(ImmutableList.Create(
                        new Event(
                            Kind: "ReminderSent",
                            Guid.NewGuid(),
                            command.BusinessKey,
                            Data: ImmutableDictionary<string, object>.Empty
                                .Add("UserId", command.Data["UserId"])
                                .Add("Message", command.Data["Message"])
                                .Add("Channel", command.Data["Channel"])
                                .Add("Status", "Sent")
                        )
                    )),

                _ => new Failure(command, new BusinessError(), ImmutableDictionary<string, object>.Empty.Add("error", $"Unknown command type: {command.Kind}"))
            };

        public static ImmutableDictionary<string, object> Project(ImmutableDictionary<string, object> currentState, Event newEvent)
        {
            return newEvent.Kind switch
            {
                "UserAuthenticated" => currentState
                    .SetItem("UserId", newEvent.Data["UserId"])
                    .SetItem("Token", newEvent.Data["Token"])
                    .SetItem("Roles", newEvent.Data["Roles"]),

                "AuthenticationFailed" => currentState
                    .SetItem("FailedReason", newEvent.Data["Reason"]),

                "UserCreated" => currentState
                    .SetItem("UserId", newEvent.Data["UserId"])
                    .SetItem("Username", newEvent.Data["Username"])
                    .SetItem("Email", newEvent.Data["Email"])
                    .SetItem("Roles", newEvent.Data["Roles"])
                    .SetItem("Status", "Active"),

                "UserProfileUpdated" => currentState
                    .SetItem("NewEmail", newEvent.Data["NewEmail"])
                    .SetItem("NewRoles", newEvent.Data["NewRoles"]),

                "UserDeleted" => currentState
                    .SetItem("Status", "Deleted"),

                "UserRestored" => currentState
                    .SetItem("UserId", newEvent.Data["UserId"])
                    .SetItem("Username", newEvent.Data["Username"])
                    .SetItem("Email", newEvent.Data["Email"])
                    .SetItem("Roles", newEvent.Data["Roles"])
                    .SetItem("Status", "Active"),

                "AvailabilityChecked" => currentState
                    .SetItem("SpaceId", newEvent.Data["SpaceId"])
                    .SetItem("Date", newEvent.Data["Date"])
                    .SetItem("Time", newEvent.Data["Time"])
                    .SetItem("IsAvailable", newEvent.Data["IsAvailable"]),

                "ReservationCreated" => currentState
                    .SetItem("ReservationId", newEvent.Data["ReservationId"])
                    .SetItem("UserId", newEvent.Data["UserId"])
                    .SetItem("Moment", newEvent.Data["Moment"])
                    .SetItem("AdditionalDetails", newEvent.Data["AdditionalDetails"])
                    .SetItem("Status", "Created"),

                "ReservationUpdated" => currentState
                    .SetItem("Details", newEvent.Data.TryGetValue("Details", out object? value) ? value : currentState["Details"])
                    .SetItem("LastUpdated", DateTime.Now),

                "ReservationCancelled" => currentState
                    .SetItem("Status", "Cancelled")
                    .SetItem("CancelledOn", newEvent.Data["CancelledOn"]),

                "ReservationConfirmed" => currentState
                    .SetItem("Status", "Confirmed")
                    .SetItem("ConfirmedOn", newEvent.Data["ConfirmedOn"]),

                "ReservationHeld" => currentState
                    .SetItem("Status", "Held")
                    .SetItem("HoldUntil", newEvent.Data["HoldUntil"]),

                "ReservationCheckedIn" => currentState
                    .SetItem("Status", "CheckedIn")
                    .SetItem("CheckInDate", newEvent.Data["CheckInDate"]),

                "ReminderSent" => currentState
                    .SetItem("LastReminder", newEvent.Data["Message"])
                    .SetItem("ReminderStatus", newEvent.Data["Status"]),

                _ => ImmutableDictionary<string, object>.Empty.Add("error", $"Unknown event type: {newEvent.Kind} in event: {newEvent.Data}")
            };
        }

        public static Func<Func<Command, StateRetriever>, Func<string, Event, Task>, Func<string, Failure, Task>, Func<string, ImmutableDictionary<string, object>, Task>, Func<string, Command, Task<Result>>> CommandHandler = (getState, produceEvent, produceError, setState) => async (userId, command) =>
        {

            // Obtener el estado actual del agregado usando la función pasada como dependencia
            var currentState = await getState(command)(userId);

            if (currentState.ContainsKey("error"))
            {
                await produceError(userId, new Failure(command, new TechnicalError(), currentState));
            }

            // Procesar el comando y generar un nuevo evento usando la función pasada como dependencia
            var result = Decide(currentState, command);

            if (result is Success orderedEvents)
            {
                // Producir los nuevos eventos usando la función pasada como dependencia
                var tasks = orderedEvents.Events.Select(newEvent => produceEvent(userId, newEvent));
                await Task.WhenAll(tasks);

                // Actualizar el estado intermedio
                var projectionResults = orderedEvents.Events.Select(newEvent => Project(currentState, newEvent))
                .Select(result => result.ContainsKey("error")
                    ? produceError(userId, new Failure(command, new BusinessError(), result))
                    : setState(userId, result));

                await Task.WhenAll(projectionResults);
            }
            else
            {
                await produceError(userId, (result as Failure)!);
            }

            return result;
        };
    }
}