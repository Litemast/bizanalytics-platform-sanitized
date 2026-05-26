using CsvHelper;
using CsvHelper.Configuration;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using System.Data;
using System.Globalization;
using System.Text;

namespace BizAnalytics.Api.Services;

public partial class ReportImportProcessingService
{
    private async Task<IReadOnlyList<IReadOnlyList<object?>>> ReadCsvRowsAsync(
        IFormFile file,
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
                HasHeaderRecord = false,
                IgnoreBlankLines = true,
                TrimOptions = TrimOptions.Trim
            };

            using var reader = new StringReader(text);
            using var csv = new CsvReader(reader, config);
            var rows = new List<IReadOnlyList<object?>>();

            while (await csv.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var row = new List<object?>();
                for (var index = 0; csv.TryGetField<string>(index, out var value); index++)
                {
                    row.Add(value);
                }

                if (row.Count > 0)
                {
                    rows.Add(row);
                }
            }

            return rows;
        }
        catch (CsvHelperException)
        {
            throw new ImportProcessingService.ImportValidationException(_texts.FailedToReadCsvFile());
        }
    }

    private async Task<IReadOnlyList<IReadOnlyList<object?>>> ReadSpreadsheetRowsAsync(
        IFormFile file,
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

            var rows = new List<IReadOnlyList<object?>>();
            foreach (DataTable table in dataSet.Tables)
            {
                foreach (DataRow row in table.Rows)
                {
                    rows.Add(row.ItemArray.ToList());
                }
            }

            return rows;
        }
        catch (Exception)
        {
            throw new ImportProcessingService.ImportValidationException(_texts.FailedToReadSpreadsheetFile());
        }
    }

    private async Task<IReadOnlyList<IReadOnlyList<object?>>> ReadWordRowsAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;

            using var document = WordprocessingDocument.Open(buffer, false);
            var body = document.MainDocumentPart?.Document?.Body;
            var rows = new List<IReadOnlyList<object?>>();

            if (body is null)
            {
                return rows;
            }

            foreach (var table in body.Descendants<Table>())
            {
                foreach (var row in table.Elements<TableRow>())
                {
                    rows.Add(row.Elements<TableCell>().Select(cell => (object?)cell.InnerText).ToList());
                }
            }

            return rows;
        }
        catch (Exception)
        {
            throw new ImportProcessingService.ImportValidationException(_texts.FailedToReadWordDocument());
        }
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

        var score = text.Contains('\uFFFD') ? 0 : 100;
        var firstLine = text
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line))
            ?? string.Empty;

        score += firstLine.Count(character => character == ';') * 6;
        score += firstLine.Count(character => character == ',') * 4;
        score += firstLine.Count(character => character == '\t') * 4;
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
}
