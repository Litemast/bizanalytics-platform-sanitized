using BizAnalytics.Api.Contracts.Import;
using BizAnalytics.Api.Domain.Entities;
using BizAnalytics.Api.Infrastructure.Localization;
using CsvHelper;
using CsvHelper.Configuration;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BizAnalytics.Api.Services;

public class ImportProcessingService
{
    private static readonly string[] RequiredColumns = ["Date", "ProductName", "Quantity", "Amount"];
    private static readonly Dictionary<string, string[]> HeaderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Date"] =
        [
            "date",
            "period",
            "salesdate",
            "orderdate",
            "documentdate",
            "reportdate",
            "operationdate",
            "postingdate",
            "createddate",
            "invoicedate",
            "transactiondate",
            "\u043f\u0435\u0440\u0438\u043e\u0434",
            "\u0434\u0430\u0442\u0430",
            "\u0434\u0435\u043d\u044c",
            "\u0434\u0430\u0442\u0430\u043f\u0440\u043e\u0434\u0430\u0436\u0438",
            "\u0434\u0430\u0442\u0430\u0437\u0430\u043a\u0430\u0437\u0430",
            "\u0434\u0430\u0442\u0430\u0434\u043e\u043a\u0443\u043c\u0435\u043d\u0442\u0430",
            "\u0434\u043e\u043a\u0443\u043c\u0435\u043d\u0442\u0434\u0430\u0442\u0430",
            "\u0434\u0430\u0442\u0430\u043e\u043f\u0435\u0440\u0430\u0446\u0438\u0438",
            "\u0434\u0430\u0442\u0430\u0442\u0440\u0430\u043d\u0437\u0430\u043a\u0446\u0438\u0438",
            "\u0434\u0430\u0442\u0430\u0440\u0435\u0430\u043b\u0438\u0437\u0430\u0446\u0438\u0438"
        ],
        ["ProductName"] =
        [
            "productname",
            "product",
            "producttitle",
            "item",
            "itemname",
            "sku",
            "article",
            "position",
            "offer",
            "service",
            "\u0442\u043e\u0432\u0430\u0440",
            "\u043f\u0440\u043e\u0434\u0443\u043a\u0442",
            "\u043d\u0430\u0438\u043c\u0435\u043d\u043e\u0432\u0430\u043d\u0438\u0435",
            "\u043d\u043e\u043c\u0435\u043d\u043a\u043b\u0430\u0442\u0443\u0440\u0430",
            "\u043d\u0430\u0438\u043c\u0435\u043d\u043e\u0432\u0430\u043d\u0438\u0435\u0442\u043e\u0432\u0430\u0440\u0430",
            "\u0442\u043e\u0432\u0430\u0440\u043d\u0430\u044f\u043f\u043e\u0437\u0438\u0446\u0438\u044f",
            "\u043f\u043e\u0437\u0438\u0446\u0438\u044f",
            "\u043d\u0430\u0437\u0432\u0430\u043d\u0438\u0435",
            "\u043d\u0430\u0438\u043c\u0435\u043d\u043e\u0432\u0430\u043d\u0438\u0435\u043f\u0440\u043e\u0434\u0443\u043a\u0442\u0430",
            "\u0430\u0440\u0442\u0438\u043a\u0443\u043b",
            "\u0443\u0441\u043b\u0443\u0433\u0430"
        ],
        ["Quantity"] =
        [
            "quantity",
            "qty",
            "quantitysold",
            "sold",
            "unitssold",
            "pieces",
            "pcs",
            "numberofunits",
            "soldqty",
            "salesqty",
            "qtysold",
            "salesvolume",
            "units",
            "count",
            "\u043a\u043e\u043b\u0438\u0447\u0435\u0441\u0442\u0432\u043e",
            "\u043a\u043e\u043b\u0432\u043e",
            "\u043a\u043e\u043b\u0438\u0447",
            "\u043f\u0440\u043e\u0434\u0430\u043d\u043e",
            "\u0440\u0435\u0430\u043b\u0438\u0437\u043e\u0432\u0430\u043d\u043e",
            "\u0448\u0442",
            "\u0448\u0442\u0443\u043a",
            "\u0435\u0434\u0438\u043d\u0438\u0446",
            "\u0435\u0434\u0438\u043d\u0438\u0446\u044b",
            "\u043a\u043e\u043b\u0438\u0447\u0435\u0441\u0442\u0432\u043e\u0448\u0442",
            "\u043e\u0431\u044a\u0435\u043c"
        ],
        ["Amount"] =
        [
            "amount",
            "total",
            "revenue",
            "revenueamount",
            "salesamount",
            "grosssales",
            "grossrevenue",
            "sum",
            "value",
            "salesvalue",
            "linetotal",
            "subtotal",
            "totalsum",
            "\u0441\u0443\u043c\u043c\u0430",
            "\u0432\u044b\u0440\u0443\u0447\u043a\u0430",
            "\u0441\u0442\u043e\u0438\u043c\u043e\u0441\u0442\u044c",
            "\u0441\u0443\u043c\u043c\u0430\u043f\u0440\u043e\u0434\u0430\u0436",
            "\u0441\u0443\u043c\u043c\u0430\u043a\u043e\u043f\u043b\u0430\u0442\u0435",
            "\u0438\u0442\u043e\u0433",
            "\u0438\u0442\u043e\u0433\u043e\u0441\u0443\u043c\u043c\u0430",
            "\u0441\u0443\u043c\u043c\u0430\u0440\u0435\u0430\u043b\u0438\u0437\u0430\u0446\u0438\u0438",
            "totalamount"
        ]
    };

    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");
    private static readonly Dictionary<string, int> MonthAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["january"] = 1,
        ["jan"] = 1,
        ["январь"] = 1,
        ["января"] = 1,
        ["february"] = 2,
        ["feb"] = 2,
        ["февраль"] = 2,
        ["февраля"] = 2,
        ["march"] = 3,
        ["mar"] = 3,
        ["март"] = 3,
        ["марта"] = 3,
        ["april"] = 4,
        ["apr"] = 4,
        ["апрель"] = 4,
        ["апреля"] = 4,
        ["may"] = 5,
        ["май"] = 5,
        ["мая"] = 5,
        ["june"] = 6,
        ["jun"] = 6,
        ["июнь"] = 6,
        ["июня"] = 6,
        ["july"] = 7,
        ["jul"] = 7,
        ["июль"] = 7,
        ["июля"] = 7,
        ["august"] = 8,
        ["aug"] = 8,
        ["август"] = 8,
        ["августа"] = 8,
        ["september"] = 9,
        ["sep"] = 9,
        ["sept"] = 9,
        ["сентябрь"] = 9,
        ["сентября"] = 9,
        ["october"] = 10,
        ["oct"] = 10,
        ["октябрь"] = 10,
        ["октября"] = 10,
        ["november"] = 11,
        ["nov"] = 11,
        ["ноябрь"] = 11,
        ["ноября"] = 11,
        ["december"] = 12,
        ["dec"] = 12,
        ["декабрь"] = 12,
        ["декабря"] = 12
    };
    private readonly IApiTextLocalizer _texts;

    static ImportProcessingService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public ImportProcessingService(IApiTextLocalizer texts)
    {
        _texts = texts;
    }

    public async Task<ImportProcessingResult> ParseFilesAsync(
        IReadOnlyCollection<IFormFile> files,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        if (files.Count == 0)
        {
            throw new ImportValidationException(_texts.AtLeastOneFileMustBeUploaded());
        }

        var records = new List<SalesRecord>();
        var importedFiles = new List<ImportedFileResponse>();

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (file.Length == 0)
            {
                throw new ImportValidationException(_texts.FileWasNotUploaded());
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var parsedRecords = extension switch
            {
                ".csv" => await ParseCsvAsync(file, organizationId, cancellationToken),
                ".xls" or ".xlsx" => await ParseSpreadsheetAsync(file, organizationId, cancellationToken),
                ".docx" => await ParseWordDocumentAsync(file, organizationId, cancellationToken),
                _ => throw new ImportValidationException(_texts.UnsupportedImportFileExtension(extension))
            };

            if (parsedRecords.Count == 0)
            {
                throw new ImportValidationException(_texts.FileDoesNotContainDataRows(file.FileName));
            }

            importedFiles.Add(new ImportedFileResponse
            {
                FileName = file.FileName,
                Extension = extension.TrimStart('.').ToUpperInvariant(),
                ImportedRecords = parsedRecords.Count
            });

            records.AddRange(parsedRecords);
        }

        if (records.Count == 0)
        {
            throw new ImportValidationException(_texts.ImportedFilesDoNotContainDataRows());
        }

        return new ImportProcessingResult(records, importedFiles);
    }

    private async Task<List<SalesRecord>> ParseCsvAsync(
        IFormFile file,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, cancellationToken);

            var text = DecodeCsvText(buffer.ToArray());
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = DetectDelimiter(text),
                IgnoreBlankLines = true,
                PrepareHeaderForMatch = args => args.Header?.Trim() ?? string.Empty
            };

            using var reader = new StringReader(text);
            using var csv = new CsvReader(reader, config);

            if (!await csv.ReadAsync())
            {
                return [];
            }

            csv.ReadHeader();
            var headerMap = ValidateHeaders(csv.HeaderRecord, file.FileName);
            var records = new List<SalesRecord>();

            while (await csv.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rowNumber = csv.Context.Parser?.Row ?? 0;
                var dateValue = ReadRequiredField(csv, headerMap["Date"], rowNumber, "Date");
                var productName = ReadRequiredField(csv, headerMap["ProductName"], rowNumber, "ProductName");
                var quantityValue = ReadRequiredField(csv, headerMap["Quantity"], rowNumber, "Quantity");
                var amountValue = ReadRequiredField(csv, headerMap["Amount"], rowNumber, "Amount");

                records.Add(new SalesRecord
                {
                    OrganizationId = organizationId,
                    SourceFileName = file.FileName,
                    Date = ParseDate(dateValue, rowNumber),
                    ProductName = productName.Trim(),
                    Quantity = ParseQuantity(quantityValue, rowNumber),
                    Amount = ParseAmount(amountValue, rowNumber)
                });
            }

            return records;
        }
        catch (ImportValidationException)
        {
            throw;
        }
        catch (CsvHelperException)
        {
            throw new ImportValidationException(_texts.FailedToReadCsvFile());
        }
    }

    private async Task<List<SalesRecord>> ParseSpreadsheetAsync(
        IFormFile file,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;

            using var reader = ExcelReaderFactory.CreateReader(buffer);
            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                UseColumnDataType = false,
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = false
                }
            });

            var records = new List<SalesRecord>();
            var foundHeader = false;

            foreach (DataTable table in dataSet.Tables)
            {
                var rows = table.Rows
                    .Cast<DataRow>()
                    .Select(row => row.ItemArray.ToList())
                    .ToList();

                var fallbackDate = TryExtractFallbackDate(
                    [table.TableName, .. rows.SelectMany(row => row.Select(cell => Convert.ToString(cell, CultureInfo.InvariantCulture) ?? string.Empty))]);

                var parsed = ParseTabularRows(rows, file.FileName, organizationId, fallbackDate);
                foundHeader |= parsed.FoundHeader;
                records.AddRange(parsed.Records);
            }

            if (!foundHeader)
            {
                throw new ImportValidationException(_texts.TableWithSupportedHeadersWasNotFound(file.FileName));
            }

            return records;
        }
        catch (ImportValidationException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new ImportValidationException(_texts.FailedToReadSpreadsheetFile());
        }
    }

    private async Task<List<SalesRecord>> ParseWordDocumentAsync(
        IFormFile file,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;

            using var document = WordprocessingDocument.Open(buffer, false);
            var body = document.MainDocumentPart?.Document?.Body;
            if (body is null)
            {
                return [];
            }

            var records = new List<SalesRecord>();
            var foundHeader = false;
            var contextTexts = new List<string>();

            foreach (var element in body.Elements())
            {
                if (element is Paragraph paragraph)
                {
                    var paragraphText = paragraph.InnerText?.Trim();
                    if (!string.IsNullOrWhiteSpace(paragraphText))
                    {
                        contextTexts.Add(paragraphText);
                    }

                    continue;
                }

                if (element is not Table table)
                {
                    continue;
                }

                var rows = table.Elements<TableRow>()
                    .Select(row => row.Elements<TableCell>().Select(cell => (object?)cell.InnerText).ToList())
                    .ToList();

                var fallbackDate = TryExtractFallbackDate(
                    [.. contextTexts, .. rows.SelectMany(row => row.Select(cell => Convert.ToString(cell, CultureInfo.InvariantCulture) ?? string.Empty))]);

                var parsed = ParseTabularRows(rows, file.FileName, organizationId, fallbackDate);
                foundHeader |= parsed.FoundHeader;
                records.AddRange(parsed.Records);
            }

            if (!foundHeader)
            {
                throw new ImportValidationException(_texts.TableWithSupportedHeadersWasNotFound(file.FileName));
            }

            return records;
        }
        catch (ImportValidationException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new ImportValidationException(_texts.FailedToReadWordDocument());
        }
    }

    private ParsedTabularRows ParseTabularRows(
        IReadOnlyList<IReadOnlyList<object?>> rows,
        string fileName,
        Guid organizationId,
        DateTime? fallbackDate = null)
    {
        var records = new List<SalesRecord>();
        var foundHeader = false;

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var headerMap = TryResolveHeaderMap(rows[rowIndex], fallbackDate);
            if (headerMap is null)
            {
                continue;
            }

            foundHeader = true;

            for (var dataRowIndex = rowIndex + 1; dataRowIndex < rows.Count; dataRowIndex++)
            {
                var row = rows[dataRowIndex];

                if (TryResolveHeaderMap(row, fallbackDate) is not null)
                {
                    break;
                }

                if (IsBlankRow(row))
                {
                    continue;
                }

                records.Add(CreateRecord(row, headerMap, fileName, organizationId, dataRowIndex + 1, fallbackDate));
            }
        }

        return new ParsedTabularRows(foundHeader, records);
    }

    private SalesRecord CreateRecord(
        IReadOnlyList<object?> row,
        IReadOnlyDictionary<string, int> headerMap,
        string fileName,
        Guid organizationId,
        int rowNumber,
        DateTime? fallbackDate)
    {
        return new SalesRecord
        {
            OrganizationId = organizationId,
            SourceFileName = fileName,
            Date = headerMap.TryGetValue("Date", out var dateIndex)
                ? ParseDate(GetValue(row, dateIndex), rowNumber)
                : fallbackDate ?? throw new ImportValidationException(_texts.CsvFieldMustNotBeEmpty(rowNumber, "Date")),
            ProductName = ReadTextValue(GetValue(row, headerMap["ProductName"]), rowNumber, "ProductName"),
            Quantity = ParseQuantity(GetValue(row, headerMap["Quantity"]), rowNumber),
            Amount = ParseAmount(GetValue(row, headerMap["Amount"]), rowNumber)
        };
    }

    private IReadOnlyDictionary<string, int>? TryResolveHeaderMap(IReadOnlyList<object?> cells, DateTime? fallbackDate = null)
    {
        var normalized = cells
            .Select(cell => NormalizeHeader(Convert.ToString(cell, CultureInfo.InvariantCulture)))
            .ToList();

        if (normalized.All(string.IsNullOrWhiteSpace))
        {
            return null;
        }

        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in RequiredColumns)
        {
            var aliases = HeaderAliases[column];
            var index = normalized.FindIndex(value => aliases.Contains(value, StringComparer.OrdinalIgnoreCase));
            if (index < 0)
            {
                if (column.Equals("Date", StringComparison.OrdinalIgnoreCase) && fallbackDate.HasValue)
                {
                    continue;
                }

                return null;
            }

            result[column] = index;
        }

        return result;
    }

    private IReadOnlyDictionary<string, int> ValidateHeaders(string[]? headers, string fileName)
    {
        if (headers is null)
        {
            throw new ImportValidationException(_texts.CsvFileDoesNotContainHeaders());
        }

        var mapped = TryResolveHeaderMap(headers.Cast<object?>().ToList());
        if (mapped is null)
        {
            throw new ImportValidationException(_texts.TableWithSupportedHeadersWasNotFound(fileName));
        }

        return mapped;
    }

    private string ReadRequiredField(CsvReader csv, int fieldIndex, int rowNumber, string fieldName)
    {
        var value = csv.GetField(fieldIndex);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ImportValidationException(_texts.CsvFieldMustNotBeEmpty(rowNumber, fieldName));
        }

        return value;
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
            throw new ImportValidationException(_texts.CsvFieldMustNotBeEmpty(rowNumber, fieldName));
        }

        return text;
    }

    private DateTime ParseDate(object? value, int rowNumber)
    {
        if (value is DateTime dateTime)
        {
            return DateTime.SpecifyKind(dateTime.Date, DateTimeKind.Utc);
        }

        if (value is double excelDate)
        {
            return DateTime.SpecifyKind(DateTime.FromOADate(excelDate).Date, DateTimeKind.Utc);
        }

        if (value is float floatDate)
        {
            return DateTime.SpecifyKind(DateTime.FromOADate(floatDate).Date, DateTimeKind.Utc);
        }

        var text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ImportValidationException(_texts.CsvFieldMustNotBeEmpty(rowNumber, "Date"));
        }

        if (DateTime.TryParseExact(
                text,
                ["yyyy-MM-dd", "dd.MM.yyyy", "MM/dd/yyyy", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ"],
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsedDate) ||
            DateTime.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out parsedDate) ||
            DateTime.TryParse(
                text,
                RussianCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out parsedDate))
        {
            return DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Utc);
        }

        throw new ImportValidationException(_texts.CsvDateHasInvalidFormat(rowNumber, text));
    }

    private int ParseQuantity(object? value, int rowNumber)
    {
        if (value is int intValue)
        {
            return ValidateQuantity(intValue, rowNumber);
        }

        if (value is double doubleValue && Math.Abs(doubleValue % 1) < 0.000001)
        {
            return ValidateQuantity(Convert.ToInt32(doubleValue), rowNumber);
        }

        if (value is decimal decimalValue && decimalValue % 1 == 0)
        {
            return ValidateQuantity(Convert.ToInt32(decimalValue), rowNumber);
        }

        var text = SanitizeIntegerText(Convert.ToString(value, CultureInfo.InvariantCulture));
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity) &&
            !int.TryParse(text, NumberStyles.Integer, RussianCulture, out quantity))
        {
            throw new ImportValidationException(_texts.CsvQuantityIsNotWholeNumber(rowNumber, text ?? string.Empty));
        }

        return ValidateQuantity(quantity, rowNumber);
    }

    private int ValidateQuantity(int quantity, int rowNumber)
    {
        if (quantity <= 0)
        {
            throw new ImportValidationException(_texts.CsvQuantityMustBeGreaterThanZero(rowNumber));
        }

        return quantity;
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

        throw new ImportValidationException(_texts.CsvAmountIsNotNumber(rowNumber, text ?? string.Empty));
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

    private DateTime? TryExtractFallbackDate(IEnumerable<string> textFragments)
    {
        foreach (var fragment in textFragments)
        {
            var text = fragment?.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (DateTime.TryParse(
                    text,
                    RussianCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsedDate) ||
                DateTime.TryParse(
                    text,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out parsedDate))
            {
                return DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Utc);
            }

            var normalized = text
                .ToLowerInvariant()
                .Replace('ё', 'е');

            var monthYearMatch = Regex.Match(
                normalized,
                @"(?<month>[a-zа-я]+)\s+(?<year>\d{4})",
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
        }

        return null;
    }

    private string? SanitizeIntegerText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var sanitized = new string(text
            .Where(character => char.IsDigit(character) || character is '-' or '+')
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? text.Trim() : sanitized;
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

    private string DecodeCsvText(byte[] bytes)
    {
        var encodings = new Encoding[]
        {
            new UTF8Encoding(false, true),
            Encoding.Unicode,
            Encoding.BigEndianUnicode,
            Encoding.GetEncoding(1251)
        };

        var bestCandidate = (Text: string.Empty, Score: int.MinValue);

        foreach (var encoding in encodings)
        {
            try
            {
                var text = encoding.GetString(bytes);
                var score = ScoreDecodedCsv(text);
                if (score > bestCandidate.Score)
                {
                    bestCandidate = (text, score);
                }
            }
            catch (DecoderFallbackException)
            {
            }
        }

        return bestCandidate.Score == int.MinValue
            ? Encoding.UTF8.GetString(bytes)
            : bestCandidate.Text;
    }

    private int ScoreDecodedCsv(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return int.MinValue;
        }

        var score = 0;

        if (!text.Contains('\uFFFD'))
        {
            score += 100;
        }

        if (text.Contains('\n') || text.Contains('\r'))
        {
            score += 10;
        }

        var firstLine = text
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line))
            ?? string.Empty;

        score += firstLine.Count(character => character == ';') * 6;
        score += firstLine.Count(character => character == ',') * 4;
        score += firstLine.Count(character => character == '\t') * 4;

        var normalizedHeaders = firstLine
            .Split([';', ',', '\t'], StringSplitOptions.None)
            .Select(NormalizeHeader)
            .ToList();

        foreach (var requiredColumn in RequiredColumns)
        {
            if (normalizedHeaders.Any(header => HeaderAliases[requiredColumn].Contains(header, StringComparer.OrdinalIgnoreCase)))
            {
                score += 40;
            }
        }

        score -= text.Count(character => character == '\0') * 20;

        return score;
    }

    private string DetectDelimiter(string text)
    {
        var firstLine = text
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line))
            ?? string.Empty;

        var candidates = new[]
        {
            new { Delimiter = ";", Score = firstLine.Count(character => character == ';') },
            new { Delimiter = ",", Score = firstLine.Count(character => character == ',') },
            new { Delimiter = "\t", Score = firstLine.Count(character => character == '\t') }
        };

        return candidates
            .OrderByDescending(candidate => candidate.Score)
            .First().Delimiter;
    }

    public sealed record ImportProcessingResult(
        List<SalesRecord> Records,
        List<ImportedFileResponse> ImportedFiles);

    private sealed record ParsedTabularRows(
        bool FoundHeader,
        List<SalesRecord> Records);

    public sealed class ImportValidationException : Exception
    {
        public ImportValidationException(string message) : base(message)
        {
        }
    }
}
