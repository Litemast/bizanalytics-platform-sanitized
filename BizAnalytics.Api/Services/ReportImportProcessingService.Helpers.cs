using BizAnalytics.Api.Domain.Entities;
using System.Globalization;

namespace BizAnalytics.Api.Services;

public partial class ReportImportProcessingService
{
    private IReadOnlyDictionary<string, int>? TryResolveFinancialHeaderMap(IReadOnlyList<object?> cells)
    {
        var map = ResolveAvailableHeaders(cells, FinancialHeaderAliases);
        if (!map.ContainsKey("Period"))
        {
            return null;
        }

        var metricCount = new[] { "Revenue", "Expenses", "Profit" }.Count(map.ContainsKey);
        return metricCount >= 2 ? map : null;
    }

    private IReadOnlyDictionary<string, int>? TryResolveEducationHeaderMap(IReadOnlyList<object?> cells)
    {
        var map = ResolveAvailableHeaders(cells, EducationHeaderAliases);
        if (!map.ContainsKey("StudentName") || !map.ContainsKey("Subject"))
        {
            return null;
        }

        return map.ContainsKey("Grade") || map.ContainsKey("AverageScore") ? map : null;
    }

    private Dictionary<string, int> ResolveAvailableHeaders(
        IReadOnlyList<object?> cells,
        IReadOnlyDictionary<string, string[]> aliases)
    {
        var normalized = cells
            .Select(cell => NormalizeHeader(Convert.ToString(cell, CultureInfo.InvariantCulture)))
            .ToList();

        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (column, columnAliases) in aliases)
        {
            var index = normalized.FindIndex(value => columnAliases.Contains(value, StringComparer.OrdinalIgnoreCase));
            if (index >= 0)
            {
                result[column] = index;
            }
        }

        return result;
    }

    private bool TryCreateFinancialRecord(
        IReadOnlyList<object?> row,
        IReadOnlyDictionary<string, int> headerMap,
        string fileName,
        Guid organizationId,
        int rowNumber,
        out FinancialRecord record)
    {
        record = new FinancialRecord();

        try
        {
            var periodValue = GetValue(row, headerMap["Period"]);
            var period = ParsePeriod(periodValue, rowNumber);
            var revenue = TryReadDecimal(row, headerMap, "Revenue", rowNumber);
            var expenses = TryReadDecimal(row, headerMap, "Expenses", rowNumber);
            var profit = TryReadDecimal(row, headerMap, "Profit", rowNumber);

            if (!revenue.HasValue && expenses.HasValue && profit.HasValue)
            {
                revenue = expenses.Value + profit.Value;
            }

            if (!expenses.HasValue && revenue.HasValue && profit.HasValue)
            {
                expenses = revenue.Value - profit.Value;
            }

            if (!profit.HasValue && revenue.HasValue && expenses.HasValue)
            {
                profit = revenue.Value - expenses.Value;
            }

            if (!revenue.HasValue || !expenses.HasValue || !profit.HasValue)
            {
                return false;
            }

            var periodLabel = Convert.ToString(periodValue, CultureInfo.InvariantCulture)?.Trim();
            record = new FinancialRecord
            {
                OrganizationId = organizationId,
                SourceFileName = fileName,
                Period = period,
                PeriodLabel = string.IsNullOrWhiteSpace(periodLabel)
                    ? period.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : periodLabel,
                Revenue = revenue.Value,
                Expenses = expenses.Value,
                Profit = profit.Value
            };

            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryCreateEducationRecord(
        IReadOnlyList<object?> row,
        IReadOnlyDictionary<string, int> headerMap,
        string fileName,
        Guid organizationId,
        int rowNumber,
        out EducationRecord record)
    {
        record = new EducationRecord();

        try
        {
            var grade = TryReadDecimal(row, headerMap, "Grade", rowNumber);
            var averageScore = TryReadDecimal(row, headerMap, "AverageScore", rowNumber);
            if (!grade.HasValue && !averageScore.HasValue)
            {
                return false;
            }

            record = new EducationRecord
            {
                OrganizationId = organizationId,
                SourceFileName = fileName,
                StudentName = ReadTextValue(GetValue(row, headerMap["StudentName"]), rowNumber, "StudentName"),
                Subject = ReadTextValue(GetValue(row, headerMap["Subject"]), rowNumber, "Subject"),
                Grade = grade ?? averageScore ?? 0,
                AverageScore = averageScore
            };

            return true;
        }
        catch
        {
            return false;
        }
    }

    private object? GetValue(IReadOnlyList<object?> cells, int index)
    {
        return index >= 0 && index < cells.Count ? cells[index] : null;
    }

    private bool IsBlankRow(IReadOnlyList<object?> row)
    {
        return row.All(cell => string.IsNullOrWhiteSpace(Convert.ToString(cell, CultureInfo.InvariantCulture)));
    }

    private string ReadTextValue(object? value, int rowNumber, string fieldName)
    {
        var text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ImportProcessingService.ImportValidationException(_texts.CsvFieldMustNotBeEmpty(rowNumber, fieldName));
        }

        return text;
    }

    private decimal? TryReadDecimal(
        IReadOnlyList<object?> row,
        IReadOnlyDictionary<string, int> headerMap,
        string column,
        int rowNumber)
    {
        if (!headerMap.TryGetValue(column, out var index))
        {
            return null;
        }

        return ParseAmount(GetValue(row, index), rowNumber);
    }

    private decimal ParseAmount(object? value, int rowNumber)
    {
        if (value is decimal decimalValue)
        {
            return decimalValue;
        }

        if (value is double doubleValue)
        {
            return Convert.ToDecimal(doubleValue);
        }

        if (value is float floatValue)
        {
            return Convert.ToDecimal(floatValue);
        }

        if (value is int intValue)
        {
            return intValue;
        }

        var text = SanitizeDecimalText(Convert.ToString(value, CultureInfo.InvariantCulture));
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) ||
            decimal.TryParse(text, NumberStyles.Number, RussianCulture, out amount))
        {
            return amount;
        }

        throw new ImportProcessingService.ImportValidationException(_texts.CsvAmountIsNotNumber(rowNumber, text ?? string.Empty));
    }
}
