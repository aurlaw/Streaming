using FileStreamDemo.Data.Interfaces;
using FileStreamDemo.Data.Models;
using FileStreamDemo.Data.Utilities;
using Microsoft.Extensions.Logging;

namespace FileStreamDemo.Data.Parsers;

public class PersonParser : IRecordParser<Person>
{
    private readonly ILogger<PersonParser> _logger;

    public PersonParser(ILogger<PersonParser> logger)
    {
        _logger = logger;
    }

    public ValueTask<(Person? Record, ErrorEvent? Error)> TryParseAsync(
        ReadOnlySpan<byte> line,
        int lineNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Make a copy of the line so we can modify it with ref
            var lineCopy = line;

            // Parse first name
            if (!SpanParsingHelpers.TryParseNextField(ref lineCopy, (byte)',', out var firstNameBytes))
            {
                throw new FormatException("Missing first name field");
            }

            // Parse last name
            if (!SpanParsingHelpers.TryParseNextField(ref lineCopy, (byte)',', out var lastNameBytes))
            {
                throw new FormatException("Missing last name field");
            }

            // Parse birth date (remaining field)
            if (!SpanParsingHelpers.TryParseNextField(ref lineCopy, (byte)',', out var birthDateBytes))
            {
                throw new FormatException("Missing birth date field");
            }

            // Trim whitespace from fields
            firstNameBytes = SpanParsingHelpers.Trim(firstNameBytes);
            lastNameBytes = SpanParsingHelpers.Trim(lastNameBytes);
            birthDateBytes = SpanParsingHelpers.Trim(birthDateBytes);

            // Validate fields are not empty
            if (firstNameBytes.Length == 0)
                throw new FormatException("First name cannot be empty");

            if (lastNameBytes.Length == 0)
                throw new FormatException("Last name cannot be empty");

            if (birthDateBytes.Length == 0)
                throw new FormatException("Birth date cannot be empty");

            // Convert to strings
            var firstName = SpanParsingHelpers.GetString(firstNameBytes);
            var lastName = SpanParsingHelpers.GetString(lastNameBytes);

            // Parse date
            if (!SpanParsingHelpers.TryParseDateOnly(birthDateBytes, out var birthDate))
            {
                throw new FormatException($"Invalid birth date format: {SpanParsingHelpers.GetString(birthDateBytes)}");
            }

            var person = new Person
            {
                FirstName = firstName,
                LastName = lastName,
                BirthDate = birthDate
            };

            return new ValueTask<(Person? Record, ErrorEvent? Error)>((person, null));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse line {LineNumber}", lineNumber);

            var error = new ErrorEvent
            {
                Message = $"Failed to parse line {lineNumber}: {ex.Message}",
                LineNumber = lineNumber,
                Exception = ex
            };

            return new ValueTask<(Person? Record, ErrorEvent? Error)>((null, error));
        }
    }
}