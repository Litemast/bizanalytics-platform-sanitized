using System.Globalization;
using System.Text.RegularExpressions;

namespace BizAnalytics.Api.Services;

public partial class ReportImportProcessingService
{
    private static readonly Dictionary<string, int> MonthAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["january"] = 1, ["jan"] = 1, ["январь"] = 1, ["января"] = 1,
        ["february"] = 2, ["feb"] = 2, ["февраль"] = 2, ["февраля"] = 2,
        ["march"] = 3, ["mar"] = 3, ["март"] = 3, ["марта"] = 3,
        ["april"] = 4, ["apr"] = 4, ["апрель"] = 4, ["апреля"] = 4,
        ["may"] = 5, ["май"] = 5, ["мая"] = 5,
        ["june"] = 6, ["jun"] = 6, ["июнь"] = 6, ["июня"] = 6,
        ["july"] = 7, ["jul"] = 7, ["июль"] = 7, ["июля"] = 7,
        ["august"] = 8, ["aug"] = 8, ["август"] = 8, ["августа"] = 8,
        ["september"] = 9, ["sep"] = 9, ["sept"] = 9, ["сентябрь"] = 9, ["сентября"] = 9,
        ["october"] = 10, ["oct"] = 10, ["октябрь"] = 10, ["октября"] = 10,
        ["november"] = 11, ["nov"] = 11, ["ноябрь"] = 11, ["ноября"] = 11,
        ["december"] = 12, ["dec"] = 12, ["декабрь"] = 12, ["декабря"] = 12
    };

    private DateTime ParsePeriod(object? value, int rowNumber)
    {
        if (value is DateTime dateTime)
        {
            return DateTime.SpecifyKind(dateTime.Date, DateTimeKind.Utc);
        }

        if (value is double excelDate)
        {
            return DateTime.SpecifyKind(DateTime.FromOADate(excelDate).Date, DateTimeKind.Utc);
        }

        var text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ImportProcessingService.ImportValidationException(_texts.CsvFieldMustNotBeEmpty(rowNumber, "Period"));
        }

        if (DateTime.TryParse(text, RussianCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedDate) ||
            DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsedDate))
        {
            return DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Utc);
        }

        var normalized = text.ToLowerInvariant().Replace('ё', 'е');
        var monthYearMatch = Regex.Match(
            normalized,
            @"(?<month>[a-zа-яе]+)\s+(?<year>\d{4})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (monthYearMatch.Success &&
            MonthAliases.TryGetValue(monthYearMatch.Groups["month"].Value, out var month) &&
            int.TryParse(monthYearMatch.Groups["year"].Value, out var year))
        {
            return new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        var numericMonthYearMatch = Regex.Match(
            normalized,
            @"(?<month>\d{1,2})[./-](?<year>\d{4})",
            RegexOptions.CultureInvariant);

        if (numericMonthYearMatch.Success &&
            int.TryParse(numericMonthYearMatch.Groups["month"].Value, out month) &&
            int.TryParse(numericMonthYearMatch.Groups["year"].Value, out year) &&
            month is >= 1 and <= 12)
        {
            return new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        throw new ImportProcessingService.ImportValidationException(_texts.CsvDateHasInvalidFormat(rowNumber, text));
    }

    private string NormalizeHeader(string? value)
    {
        return new string((value ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Replace('\u0451', '\u0435')
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    private string? SanitizeDecimalText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var sanitized = new string(text
            .Where(character =>
                char.IsDigit(character) ||
                character is '.' or ',' or '-' or '+' ||
                char.IsWhiteSpace(character))
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? text.Trim() : sanitized.Trim();
    }
}
