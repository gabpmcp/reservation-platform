using System.Collections.Immutable;

namespace ReservationPlatform.Validations
{
    public static class Validations
    {
        public static Dictionary<string, List<Validator>> CommandSchema = new()
        {
            { "AuthenticateUser", ValidateUsername.Concat(ValidatePassword).ToList() },
            { "CreateUser", ValidateUsername.Concat(ValidatePassword).Concat(ValidateEmail).Concat(ValidateRoles).ToList() },
            { "UpdateUserProfile", ValidateUserId.Concat(ValidateEmail).Concat(ValidateRoles).ToList() },
            { "DeleteUser", ValidateUserId },
            { "RestoreUser", ValidateUserId },
            { "CheckAvailability", ValidateUserId.Concat(ValidateMoment).ToList() },
            { "CreateReservation", ValidateUserId.Concat(ValidateChannel).Concat(ValidateMoment).Concat(ValidateDetails).ToList() },
            { "MoveReservation", ValidateReservationId.Concat(ValidateUserId).Concat(ValidateMoment).Concat(ValidateChannel).ToList() },
            { "ModifyReservation", ValidateReservationId.Concat(ValidateUserId).Concat(ValidateDetails).Concat(ValidateChannel).ToList() },
            { "ConfirmReservation", ValidateReservationId.Concat(ValidateUserId).Concat(ValidateChannel).ToList() },
            { "HoldReservation", ValidateReservationId.Concat(ValidateUserId).Concat(ValidateHoldUntil).Concat(ValidateChannel).ToList() },
            { "CheckInReservation", ValidateReservationId.Concat(ValidateUserId).Concat(ValidateMoment).Concat(ValidateChannel).ToList() },
            { "CancelReservation", ValidateReservationId.Concat(ValidateUserId).Concat(ValidateChannel).ToList() },
            { "SendReminder", ValidateUserId.Concat(ValidateDetails).Concat(ValidateChannel).ToList() }
        };

        public static ValidationResult ValidateSchema(string kind, ImmutableDictionary<string, object> data, Dictionary<string, List<Validator>> schema)
        {
            return schema.TryGetValue(kind, out var validators)
                ? ValidationResult.Combine(validators.AsParallel().Select(validator => validator(data)))
                : new ValidationResult(false, [$"No schema found for kind: {kind}"]);
        }

        public static List<Validator> ValidateUsername =
        [
            data => Functions.AttrExists("username", data),
            data => Functions.AttrIsNotEmpty("username", data)
        ];

        public static List<Validator> ValidatePassword =
        [
            data => Functions.AttrExists("password", data),
            data => Functions.AttrIsNotEmpty("password", data)
        ];

        public static List<Validator> ValidateEmail =
        [
            data => Functions.AttrExists("email", data),
            data => Functions.AttrIsOfType<string>("email", data),
            data => Functions.AttrIsNotEmpty("email", data),
            data => data.TryGetValue("email", out var email) && email is string mail && mail.Contains("@")
                ? ValidationResult.Success
                : new ValidationResult(false, ["Invalid email format."])
        ];

        public static List<Validator> ValidateRoles =
        [
            data => Functions.AttrExists("roles", data),
            data => Functions.AttrIsOfType<string[]>("roles", data),
            data => Functions.AttrIsNotEmpty("roles", data)
        ];

        public static List<Validator> ValidateUserId =
        [
            data => Functions.AttrExists("userId", data),
            data => Functions.AttrIsOfType<Guid>("userId", data)
        ];

        public static List<Validator> ValidateReservationId =
        [
            data => Functions.AttrExists("reservationId", data),
            data => Functions.AttrIsOfType<Guid>("reservationId", data)
        ];

        public static List<Validator> ValidateMoment =
        [
            data => Functions.AttrExists("moment", data),
            data => Functions.AttrIsOfType<DateTime>("moment", data)
        ];

        public static List<Validator> ValidateChannel =
        [
            data => Functions.AttrExists("channel", data),
            data => Functions.AttrIsNotEmpty("channel", data)
        ];

        public static List<Validator> ValidateDetails =
        [
            data => Functions.AttrExists("details", data),
            data => Functions.AttrIsOfType<Dictionary<string, object>>("details", data)
        ];

        public static List<Validator> ValidateHoldUntil =
        [
            data => Functions.AttrExists("holdUntil", data),
            data => Functions.AttrIsOfType<DateTime>("holdUntil", data)
        ];
    }
}