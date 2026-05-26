using BizAnalytics.Api.Contracts.Import;
using BizAnalytics.Api.Domain.Entities;
using BizAnalytics.Api.Infrastructure.Localization;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Text;

namespace BizAnalytics.Api.Services;

public partial class ReportImportProcessingService
{
    private static readonly Dictionary<string, string[]> FinancialHeaderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Period"] =
        [
            "period",
            "reportingperiod",
            "statementperiod",
            "fiscalperiod",
            "periodname",
            "date",
            "reportdate",
            "month",
            "quarter",
            "year",
            "\u043f\u0435\u0440\u0438\u043e\u0434",
            "\u043e\u0442\u0447\u0435\u0442\u043d\u044b\u0439\u043f\u0435\u0440\u0438\u043e\u0434",
            "\u0444\u0438\u043d\u0430\u043d\u0441\u043e\u0432\u044b\u0439\u043f\u0435\u0440\u0438\u043e\u0434",
            "\u043f\u0435\u0440\u0438\u043e\u0434\u043e\u0442\u0447\u0435\u0442\u0430",
            "\u0434\u0430\u0442\u0430\u043f\u0435\u0440\u0438\u043e\u0434\u0430",
            "\u0434\u0430\u0442\u0430",
            "\u043c\u0435\u0441\u044f\u0446",
            "\u043a\u0432\u0430\u0440\u0442\u0430\u043b",
            "\u0433\u043e\u0434"
        ],
        ["Revenue"] =
        [
            "revenue",
            "grossrevenue",
            "salesrevenue",
            "income",
            "incomeamount",
            "earnings",
            "sales",
            "turnover",
            "proceeds",
            "\u0434\u043e\u0445\u043e\u0434",
            "\u0434\u043e\u0445\u043e\u0434\u044b",
            "\u0432\u044b\u0440\u0443\u0447\u043a\u0430",
            "\u0432\u044b\u0440\u0443\u0447\u043a\u0430\u043e\u0442\u043f\u0440\u043e\u0434\u0430\u0436",
            "\u043f\u043e\u0441\u0442\u0443\u043f\u043b\u0435\u043d\u0438\u044f",
            "\u043f\u043e\u0441\u0442\u0443\u043f\u043b\u0435\u043d\u0438\u044f\u0434\u0435\u043d\u0435\u0436\u043d\u044b\u0445\u0441\u0440\u0435\u0434\u0441\u0442\u0432",
            "\u0434\u043e\u0445\u043e\u0434\u044b\u043f\u0435\u0440\u0438\u043e\u0434\u0430",
            "\u043e\u0431\u043e\u0440\u043e\u0442"
        ],
        ["Expenses"] =
        [
            "expenses",
            "expense",
            "operatingexpenses",
            "cost",
            "costs",
            "expenditure",
            "spend",
            "outflow",
            "overheads",
            "cogs",
            "\u0440\u0430\u0441\u0445\u043e\u0434",
            "\u0440\u0430\u0441\u0445\u043e\u0434\u044b",
            "\u0437\u0430\u0442\u0440\u0430\u0442\u044b",
            "\u0437\u0430\u0442\u0440\u0430\u0442\u044b\u043f\u0435\u0440\u0438\u043e\u0434\u0430",
            "\u0438\u0437\u0434\u0435\u0440\u0436\u043a\u0438",
            "\u0438\u0437\u0434\u0435\u0440\u0436\u043a\u0438\u043f\u0435\u0440\u0438\u043e\u0434\u0430",
            "\u043e\u043f\u0435\u0440\u0430\u0446\u0438\u043e\u043d\u043d\u044b\u0435\u0440\u0430\u0441\u0445\u043e\u0434\u044b",
            "\u0441\u0435\u0431\u0435\u0441\u0442\u043e\u0438\u043c\u043e\u0441\u0442\u044c"
        ],
        ["Profit"] =
        [
            "profit",
            "netprofit",
            "netincome",
            "netearnings",
            "grossprofit",
            "operatingprofit",
            "profitloss",
            "pnl",
            "margin",
            "\u043f\u0440\u0438\u0431\u044b\u043b\u044c",
            "\u0447\u0438\u0441\u0442\u0430\u044f\u043f\u0440\u0438\u0431\u044b\u043b\u044c",
            "\u0438\u0442\u043e\u0433\u043e\u0432\u0430\u044f\u043f\u0440\u0438\u0431\u044b\u043b\u044c",
            "\u0444\u0438\u043d\u0430\u043d\u0441\u043e\u0432\u044b\u0439\u0440\u0435\u0437\u0443\u043b\u044c\u0442\u0430\u0442",
            "\u043f\u0440\u0438\u0431\u044b\u043b\u044c\u0443\u0431\u044b\u0442\u043e\u043a",
            "\u043c\u0430\u0440\u0436\u0430"
        ]
    };

    private static readonly Dictionary<string, string[]> EducationHeaderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["StudentName"] =
        [
            "student",
            "studentname",
            "studentfullname",
            "pupil",
            "pupilname",
            "learner",
            "learnername",
            "participant",
            "name",
            "fullname",
            "studentfio",
            "\u0443\u0447\u0435\u043d\u0438\u043a",
            "\u0443\u0447\u0435\u043d\u0438\u043a\u0444\u0438\u043e",
            "\u0443\u0447\u0430\u0449\u0438\u0439\u0441\u044f",
            "\u0441\u0442\u0443\u0434\u0435\u043d\u0442",
            "\u0441\u0442\u0443\u0434\u0435\u043d\u0442\u0444\u0438\u043e",
            "\u0444\u0438\u043e",
            "\u0438\u043c\u044f",
            "\u043e\u0431\u0443\u0447\u0430\u044e\u0449\u0438\u0439\u0441\u044f",
            "\u0441\u043b\u0443\u0448\u0430\u0442\u0435\u043b\u044c",
            "\u0444\u0430\u043c\u0438\u043b\u0438\u044f\u0438\u043c\u044f"
        ],
        ["Subject"] =
        [
            "subject",
            "subjectname",
            "course",
            "coursename",
            "discipline",
            "disciplinename",
            "class",
            "module",
            "topic",
            "\u043f\u0440\u0435\u0434\u043c\u0435\u0442",
            "\u043d\u0430\u0438\u043c\u0435\u043d\u043e\u0432\u0430\u043d\u0438\u0435\u043f\u0440\u0435\u0434\u043c\u0435\u0442\u0430",
            "\u043d\u0430\u0437\u0432\u0430\u043d\u0438\u0435\u043f\u0440\u0435\u0434\u043c\u0435\u0442\u0430",
            "\u0434\u0438\u0441\u0446\u0438\u043f\u043b\u0438\u043d\u0430",
            "\u043a\u0443\u0440\u0441",
            "\u0443\u0440\u043e\u043a",
            "\u043c\u043e\u0434\u0443\u043b\u044c",
            "\u0442\u0435\u043c\u0430"
        ],
        ["Grade"] =
        [
            "grade",
            "mark",
            "score",
            "result",
            "assessment",
            "rating",
            "point",
            "points",
            "examscore",
            "testscore",
            "\u043e\u0446\u0435\u043d\u043a\u0430",
            "\u0438\u0442\u043e\u0433\u043e\u0432\u0430\u044f\u043e\u0446\u0435\u043d\u043a\u0430",
            "\u0431\u0430\u043b\u043b",
            "\u0431\u0430\u043b\u043b\u044b",
            "\u0442\u0435\u0441\u0442\u043e\u0432\u044b\u0439\u0431\u0430\u043b\u043b",
            "\u044d\u043a\u0437\u0430\u043c\u0435\u043d\u0430\u0446\u0438\u043e\u043d\u043d\u044b\u0439\u0431\u0430\u043b\u043b",
            "\u0440\u0435\u0437\u0443\u043b\u044c\u0442\u0430\u0442",
            "\u0440\u0435\u0437\u0443\u043b\u044c\u0442\u0430\u0442\u0442\u0435\u0441\u0442\u0430",
            "\u043e\u0442\u043c\u0435\u0442\u043a\u0430",
            "\u0440\u0435\u0439\u0442\u0438\u043d\u0433"
        ],
        ["AverageScore"] =
        [
            "averagescore",
            "average",
            "avgscore",
            "averagegrade",
            "avggrade",
            "meanscore",
            "meangrade",
            "gpa",
            "cumulativeaverage",
            "\u0441\u0440\u0435\u0434\u043d\u0438\u0439\u0431\u0430\u043b\u043b",
            "\u0441\u0440\u0435\u0434\u043d\u044f\u044f\u043e\u0446\u0435\u043d\u043a\u0430",
            "\u0441\u0440\u0435\u0434\u043d\u0438\u0439\u0440\u0435\u0437\u0443\u043b\u044c\u0442\u0430\u0442",
            "\u0441\u0440\u0435\u0434\u043d\u0438\u0439\u0438\u0442\u043e\u0433",
            "\u0438\u0442\u043e\u0433\u043e\u0432\u044b\u0439\u0431\u0430\u043b\u043b",
            "\u0441\u0440\u0431\u0430\u043b\u043b",
            "\u0441\u0440\u0435\u0434\u043d\u0438\u0439"
        ]
    };

    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");
    private readonly IApiTextLocalizer _texts;

    static ReportImportProcessingService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public ReportImportProcessingService(IApiTextLocalizer texts)
    {
        _texts = texts;
    }

    public async Task<ReportImportProcessingResult> ParseFilesAsync(
        IReadOnlyCollection<IFormFile> files,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        if (files.Count == 0)
        {
            throw new ImportProcessingService.ImportValidationException(_texts.AtLeastOneFileMustBeUploaded());
        }

        var result = new ReportImportProcessingResult([], [], []);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (file.Length == 0)
            {
                throw new ImportProcessingService.ImportValidationException(_texts.FileWasNotUploaded());
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var rows = extension switch
            {
                ".csv" => await ReadCsvRowsAsync(file, cancellationToken),
                ".xls" or ".xlsx" => await ReadSpreadsheetRowsAsync(file, cancellationToken),
                ".docx" => await ReadWordRowsAsync(file, cancellationToken),
                _ => throw new ImportProcessingService.ImportValidationException(_texts.UnsupportedImportFileExtension(extension))
            };

            var parsedFile = ParseRows(rows, file.FileName, organizationId);
            if (parsedFile.TotalRecords == 0)
            {
                throw new ImportProcessingService.ImportValidationException(_texts.FileDoesNotContainDataRows(file.FileName));
            }

            result.ImportedFiles.Add(new ImportedFileResponse
            {
                FileName = file.FileName,
                Extension = extension.TrimStart('.').ToUpperInvariant(),
                ImportedRecords = parsedFile.TotalRecords
            });

            result.FinancialRecords.AddRange(parsedFile.FinancialRecords);
            result.EducationRecords.AddRange(parsedFile.EducationRecords);
        }

        if (result.TotalRecords == 0)
        {
            throw new ImportProcessingService.ImportValidationException(_texts.ImportedFilesDoNotContainDataRows());
        }

        return result;
    }

    private ReportImportProcessingResult ParseRows(
        IReadOnlyList<IReadOnlyList<object?>> rows,
        string fileName,
        Guid organizationId)
    {
        var result = new ReportImportProcessingResult([], [], []);

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var header = TryResolveHeader(rows[rowIndex]);
            if (header is null)
            {
                continue;
            }

            for (var dataRowIndex = rowIndex + 1; dataRowIndex < rows.Count; dataRowIndex++)
            {
                var row = rows[dataRowIndex];
                if (TryResolveHeader(row) is not null)
                {
                    break;
                }

                if (IsBlankRow(row))
                {
                    continue;
                }

                if (header.ReportType == "financial_report" &&
                    TryCreateFinancialRecord(row, header.Map, fileName, organizationId, dataRowIndex + 1, out var financialRecord))
                {
                    result.FinancialRecords.Add(financialRecord);
                }

                if (header.ReportType == "education_report" &&
                    TryCreateEducationRecord(row, header.Map, fileName, organizationId, dataRowIndex + 1, out var educationRecord))
                {
                    result.EducationRecords.Add(educationRecord);
                }
            }
        }

        return result;
    }

    private ResolvedReportHeader? TryResolveHeader(IReadOnlyList<object?> cells)
    {
        var financialMap = TryResolveFinancialHeaderMap(cells);
        if (financialMap is not null)
        {
            return new ResolvedReportHeader("financial_report", financialMap);
        }

        var educationMap = TryResolveEducationHeaderMap(cells);
        if (educationMap is not null)
        {
            return new ResolvedReportHeader("education_report", educationMap);
        }

        return null;
    }

    public sealed record ReportImportProcessingResult(
        List<FinancialRecord> FinancialRecords,
        List<EducationRecord> EducationRecords,
        List<ImportedFileResponse> ImportedFiles)
    {
        public int TotalRecords => FinancialRecords.Count + EducationRecords.Count;
    }

    private sealed record ResolvedReportHeader(
        string ReportType,
        IReadOnlyDictionary<string, int> Map);
}
