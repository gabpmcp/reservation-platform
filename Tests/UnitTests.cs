using Xunit;
using System.Collections.Immutable;
using ReservationPlatform.CQRS;
using static ReservationPlatform.Validations.Validations;
using static ReservationPlatform.Business.Decisions;
using static ReservationPlatform.Business.Result;

public class UnitTests
{
    // Lambdas que manejan los casos Success y Failure
    Func<IEnumerable<Event>, Func<Success, bool>> AssertSuccess = expectedEvents => success =>
    {
        Assert.Equal(expectedEvents, success.Events);
        return true;
    };

    Func<IImmutableDictionary<string, object>, Func<Failure, bool>> AssertFailure = expectedErrors => failure =>
    {
        Assert.Equal(expectedErrors, failure.Errors);
        return true;
    };

    // Prueba unitaria para la validación de comandos
    [Theory]
    [MemberData(nameof(GetBusinessCases))]
    public void TestValidateCommand(string caseName, Command command, bool expectedIsValid, string[] expectedErrors)
    {
        var validationResult = ValidateSchema(command.Kind ?? string.Empty, command.Data, CommandSchema);
        Assert.Equal(expectedIsValid, validationResult.IsValid);
        Assert.Equal(expectedErrors, validationResult.ErrorMessages);
    }

    // Prueba unitaria para la función de decisión
    [Theory]
    [MemberData(nameof(GetBusinessCases))]
    public async void TestStateMachine(string caseName, Command command, IEnumerable<Event> expectedEvents, IImmutableDictionary<string, object> expectedErrors)
    {
        var result = await CommandHandler(
            command => GetLocalState,
            async (userId, @event) => await Task.FromResult(@event),
            async (userId, failure) => await Task.FromResult(failure),
            async (userId, state) => await Task.FromResult(state)
        )("userId", command);

        var r = result switch
        {
            Success success => AssertSuccess(expectedEvents)(success),
            Failure failure => AssertFailure(expectedErrors)(failure),
            _ => false
        };
    }

    private static readonly string[] value = ["Admin"];
    private static readonly string[] valueArray = ["User"];
    private static readonly string[] valueArray0 = ["Admin", "User"];

    public static IEnumerable<object[]> GetBusinessCases()
    {
        // Caso de Autenticación y Autorización
        yield return new object[]{
            "Successful Authentication",
            new Command("AuthenticateUser", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "username", "testuser" },
                { "password", "password123" }
            }.ToImmutableDictionary()),
            new[]
            {
                Events.UserAuthenticated(Guid.NewGuid(), "sampleToken", ["User"])
            },
            Array.Empty<Event>()
        };

        yield return new object[]{
            "Failed Authentication",
            new Command("AuthenticateUser", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "username", "testuser" },
                { "password", "wrongpassword" }
            }.ToImmutableDictionary()),
            Array.Empty<Event>(),
            new[]
            {
                Events.AuthenticationFailed("Invalid credentials", new { })
            }
        };

        // Caso de Gestión de Usuarios
        yield return new object[]{
            "Create User Successfully",
            new Command("CreateUser", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "username", "newuser" },
                { "email", "newuser@example.com" },
                { "password", "securepassword" },
                { "roles", value }
            }.ToImmutableDictionary()),
            new[]
            {
                Events.UserCreated(Guid.NewGuid(), "newuser", "newuser@example.com", ["Admin"])
            },
            Array.Empty<Event>()
        };

        yield return new object[]{
            "Create User Failed (Email Exists)",
            new Command("CreateUser", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "username", "existinguser" },
                { "email", "existinguser@example.com" },
                { "password", "anotherpassword" },
                { "roles", valueArray }
            }.ToImmutableDictionary()),
            Array.Empty<Event>(),
            ImmutableDictionary<string, object>.Empty.Add("error", "Email already exists!")
        };

        yield return new object[]{
            "Update User Profile Successfully",
            new Command("UpdateUserProfile", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "userId", Guid.NewGuid().ToString() },
                { "newEmail", "updatedemail@example.com" },
                { "newRoles", valueArray0 }
            }.ToImmutableDictionary()),
            new[]
            {
                Events.UserProfileUpdated(Guid.NewGuid(), "updatedemail@example.com", ["Admin", "User"])
            },
            Array.Empty<Event>()
        };

        yield return new object[]{
            "Update User Profile Failed (Invalid Email)",
            new Command("UpdateUserProfile", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "userId", Guid.NewGuid().ToString() },
                { "newEmail", "invalid-email" },
                { "newRoles", valueArray0 }
            }.ToImmutableDictionary()),
            Array.Empty<Event>(),
            ImmutableDictionary<string, object>.Empty.Add("error", "Invalid email format!")
        };

        yield return new object[]{
            "Delete User Successfully",
            new Command("DeleteUser", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "userId", Guid.NewGuid().ToString() }
            }.ToImmutableDictionary()),
            new[]
            {
                Events.UserDeleted(Guid.NewGuid())
            },
            Array.Empty<Event>()
        };

        yield return new object[]{
            "Delete User Failed (User Not Found)",
            new Command("DeleteUser", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "userId", Guid.NewGuid().ToString() }
            }.ToImmutableDictionary()),
            Array.Empty<Event>(),
            ImmutableDictionary<string, object>.Empty.Add("error", "User to delete not found!")
        };

        yield return new object[]{
            "Restore User Successfully",
            new Command("RestoreUser", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "userId", Guid.NewGuid().ToString() }
            }.ToImmutableDictionary()),
            new[]
            {
                Events.UserRestored(Guid.NewGuid(), "restoreduser", "restoreduser@example.com", ["User"])
            },
            Array.Empty<Event>()
        };

        yield return new object[]{
            "Restore User Failed (User Not Found)",
            new Command("RestoreUser", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "userId", Guid.NewGuid().ToString() }
            }.ToImmutableDictionary()),
            Array.Empty<Event>(),
            ImmutableDictionary<string, object>.Empty.Add("error", "User to restore not found!")
        };

        // Caso de Consulta de Disponibilidad
        yield return new object[]{
            "Check Availability Successfully",
            new Command("CheckAvailability", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "spaceId", Guid.NewGuid().ToString() },
                { "date", DateTime.Now.Date },
                { "time", new TimeSpan(14, 0, 0) }
            }.ToImmutableDictionary()),
            new[]
            {
                Events.AvailabilityChecked(Guid.NewGuid(), DateTime.Now.Date, new TimeSpan(14, 0, 0), true)
            },
            Array.Empty<Event>()
        };

        yield return new object[]{
            "Check Availability Failed",
            new Command("CheckAvailability", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "spaceId", Guid.NewGuid().ToString() },
                { "date", DateTime.Now.Date },
                { "time", new TimeSpan(14, 0, 0) }
            }.ToImmutableDictionary()),
            Array.Empty<Event>(),
            ImmutableDictionary<string, object>.Empty.Add("error", "Unable to check availability!")
        };

        // Caso de Reserva de Espacios
        yield return new object[]{
            "Create Reservation Successfully",
            new Command("CreateReservation", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "userId", Guid.NewGuid().ToString() },
                { "channel", "Web" },
                { "moment", DateTime.Now.AddDays(2) },
                { "details", ImmutableDictionary<string, object>.Empty.Add("Description", "Meeting Room Reservation") }
            }.ToImmutableDictionary()),
            new[]
            {
                Events.ReservationCreated(Guid.NewGuid(), Guid.NewGuid(), DateTime.Now.AddDays(2), "Meeting Room Reservation")
            },
            Array.Empty<Event>()
        };

        yield return new object[]{
            "Create Reservation Failed (Space Already Reserved)",
            new Command("CreateReservation", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "userId", Guid.NewGuid().ToString() },
                { "channel", "Web" },
                { "moment", DateTime.Now.AddDays(2) },
                { "details", ImmutableDictionary<string, object>.Empty.Add("Description", "Meeting Room Reservation") }
            }.ToImmutableDictionary()),
            Array.Empty<Event>(),
            new[]
            {
                Events.ReservationFailed("Space already reserved", new { })
            }
        };

        yield return new object[]{
            "Move Reservation Successfully",
            new Command("MoveReservation", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "reservationId", Guid.NewGuid() },
                { "userId", Guid.NewGuid().ToString() },
                { "newMoment", DateTime.Now.AddDays(5) },
                { "channel", "Web" }
            }.ToImmutableDictionary()),
            new[]
            {
                Events.ReservationMoved(Guid.NewGuid(), Guid.NewGuid(), DateTime.Now.AddDays(5))
            },
            Array.Empty<Event>()
        };

        yield return new object[]{
            "Move Reservation Failed (New Date/Time Unavailable)",
            new Command("MoveReservation", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "reservationId", Guid.NewGuid() },
                { "userId", Guid.NewGuid().ToString() },
                { "newMoment", DateTime.Now.AddDays(5) },
                { "channel", "Web" }
            }.ToImmutableDictionary()),
            Array.Empty<Event>(),
            ImmutableDictionary<string, object>.Empty.Add("error", "New date/time unavailable!")
        };

        yield return new object[]{
            "Modify Reservation Successfully",
            new Command("ModifyReservation", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "reservationId", Guid.NewGuid() },
                { "userId", Guid.NewGuid().ToString() },
                { "newDetails", ImmutableDictionary<string, object>.Empty.Add("Description", "Updated Reservation Details") },
                { "channel", "Web" }
            }.ToImmutableDictionary()),
            new[]
            {
                Events.ReservationModified(Guid.NewGuid(), Guid.NewGuid(), "Updated Reservation Details")
            },
            Array.Empty<Event>()
        };

        yield return new object[]{
            "Modify Reservation Failed (Invalid Details)",
            new Command("ModifyReservation", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "reservationId", Guid.NewGuid() },
                { "userId", Guid.NewGuid().ToString() },
                { "newDetails", ImmutableDictionary<string, object>.Empty.Add("Description", "") },
                { "channel", "Web" }
            }.ToImmutableDictionary()),
            Array.Empty<Event>(),
            ImmutableDictionary<string, object>.Empty.Add("error", "Invalid details provided!")
        };

        yield return new object[]{
            "Confirm Reservation Successfully",
            new Command("ConfirmReservation", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "reservationId", Guid.NewGuid() },
                { "userId", Guid.NewGuid().ToString() },
                { "channel", "Web" }
            }.ToImmutableDictionary()),
            new[]
            {
                Events.ReservationConfirmed(Guid.NewGuid(), Guid.NewGuid())
            },
            Array.Empty<Event>()
        };

        yield return new object[]{
            "Confirm Reservation Failed",
            new Command("ConfirmReservation", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "reservationId", Guid.NewGuid() },
                { "userId", Guid.NewGuid().ToString() },
                { "channel", "Web" }
            }.ToImmutableDictionary()),
            Array.Empty<Event>(),
            ImmutableDictionary<string, object>.Empty.Add("error", "Reservation confirmation failed!")
        };

        yield return new object[]{
            "Hold Reservation Successfully",
            new Command("HoldReservation", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "reservationId", Guid.NewGuid() },
                { "userId", Guid.NewGuid().ToString() },
                { "holdUntil", DateTime.Now.AddHours(1) },
                { "channel", "Web" }
            }.ToImmutableDictionary()),
            new[]
            {
                Events.ReservationHeld(Guid.NewGuid(), Guid.NewGuid(), DateTime.Now.AddHours(1))
            },
            Array.Empty<Event>()
        };

        yield return new object[]{
            "Hold Reservation Failed",
            new Command("HoldReservation", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "reservationId", Guid.NewGuid() },
                { "userId", Guid.NewGuid().ToString() },
                { "holdUntil", DateTime.Now.AddHours(1) },
                { "channel", "Web" }
            }.ToImmutableDictionary()),
            Array.Empty<Event>(),
            ImmutableDictionary<string, object>.Empty.Add("error", "Failed to hold reservation until specified time!")
        };

        yield return new object[]{
            "Check In Reservation Successfully",
            new Command("CheckInReservation", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "reservationId", Guid.NewGuid() },
                { "userId", Guid.NewGuid().ToString() },
                { "checkInMoment", DateTime.Now },
                { "channel", "Web" }
            }.ToImmutableDictionary()),
            new[]
            {
                Events.ReservationCheckedIn(Guid.NewGuid(), Guid.NewGuid(), DateTime.Now)
            },
            Array.Empty<Event>()
        };

        yield return new object[]{
            "Check In Reservation Failed",
            new Command("CheckInReservation", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "reservationId", Guid.NewGuid() },
                { "userId", Guid.NewGuid().ToString() },
                { "checkInMoment", DateTime.Now },
                { "channel", "Web" }
            }.ToImmutableDictionary()),
            Array.Empty<Event>(),
            ImmutableDictionary<string, object>.Empty.Add("error", "Check-in failed!")
        };

        yield return new object[]{
            "Cancel Reservation Successfully",
            new Command("CancelReservation", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "reservationId", Guid.NewGuid() },
                { "userId", Guid.NewGuid().ToString() },
                { "channel", "Web" }
            }.ToImmutableDictionary()),
            new[]
            {
                Events.ReservationCancelled(Guid.NewGuid(), Guid.NewGuid())
            },
            Array.Empty<Event>()
        };

        yield return new object[]{
            "Cancel Reservation Failed",
            new Command("CancelReservation", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "reservationId", Guid.NewGuid() },
                { "userId", Guid.NewGuid().ToString() },
                { "channel", "Web" }
            }.ToImmutableDictionary()),
            Array.Empty<Event>(),
            ImmutableDictionary<string, object>.Empty.Add("error", "Failed to cancel reservation!")
        };

        // Caso de Notificaciones
        yield return new object[]{
            "Send Reminder Successfully",
            new Command("SendReminder", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "userId", Guid.NewGuid().ToString() },
                { "message", "Reminder: Your reservation is tomorrow at 2 PM." },
                { "channel", "Email" }
            }.ToImmutableDictionary()),
            new[]
            {
                Events.ReminderSent(Guid.NewGuid(), "Reminder: Your reservation is tomorrow at 2 PM.", "Email", "Success")
            },
            Array.Empty<Event>()
        };

        yield return new object[]{
            "Send Reminder Failed",
            new Command("SendReminder", Guid.NewGuid(), "user123", new Dictionary<string, object>
            {
                { "userId", Guid.NewGuid().ToString() },
                { "message", "Reminder: Your reservation is tomorrow at 2 PM." },
                { "channel", "Email" }
            }.ToImmutableDictionary()),
            Array.Empty<Event>(),
            ImmutableDictionary<string, object>.Empty.Add("error", "Failed to send reminder!")
        };
    }
}
