using System.Collections.Immutable;
using System.Text.RegularExpressions;
using ReservationPlatform.CQRS;

namespace ReservationPlatform.Validations
{
    public delegate ValidationResult Validator(ImmutableDictionary<string, object> data);

    public record ValidationResult(bool IsValid, List<string> ErrorMessages) : IRecord
    {
        public static ValidationResult Success => new(true, []);

        public static ValidationResult Combine(IEnumerable<ValidationResult> results)
        {
            var allErrors = results
                .Where(r => !r.IsValid)
                .SelectMany(r => r.ErrorMessages)
                .ToList();

            return allErrors.Count != 0
                ? new ValidationResult(false, allErrors)
                : Success;
        }
    }

    public static class Functions
    {
        public static ValidationResult AttrExists(string fieldName, ImmutableDictionary<string, object> data) =>
        data.ContainsKey(fieldName)
            ? ValidationResult.Success
            : new ValidationResult(false, [$"Attr '{fieldName}' is missing."]);

        public static ValidationResult AttrIsNotEmpty(string fieldName, ImmutableDictionary<string, object> data) =>
            data.TryGetValue(fieldName, out var value) && !string.IsNullOrWhiteSpace(value?.ToString())
                ? ValidationResult.Success
                : new ValidationResult(false, [$"Attr '{fieldName}' cannot be empty."]);

        public static ValidationResult AttrIsOfType<T>(string fieldName, ImmutableDictionary<string, object> data) =>
            data.TryGetValue(fieldName, out var value) && value is T
                ? ValidationResult.Success
                : new ValidationResult(false, [$"Attr '{fieldName}' must be of type {typeof(T).Name}."]);

        public static ValidationResult AttrMatchesPattern(string fieldName, string pattern, ImmutableDictionary<string, object> data) =>
            data.TryGetValue(fieldName, out var value) && Regex.IsMatch(value?.ToString() ?? string.Empty, pattern)
                ? ValidationResult.Success
                : new ValidationResult(false, [$"Attr '{fieldName}' does not match the required pattern."]);
    }
}

