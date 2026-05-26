using BizAnalytics.Api.Contracts.Entrepreneurs;
using BizAnalytics.Api.Contracts.Reports;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using System.Globalization;

namespace BizAnalytics.Api.Services;

public class EntrepreneurTaxFormReportService
{
    private static readonly object QuestPdfSync = new();
    private static bool _questPdfConfigured;
    private readonly IWebHostEnvironment _environment;

    public EntrepreneurTaxFormReportService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public byte[] GeneratePdf(GenerateEntrepreneurFormPdfRequest request)
    {
        EnsureQuestPdfConfigured();

        var isEnglish = request.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        var form = EntrepreneurTaxFormCatalog.FindForm(request.FormCode, request.Registry, isEnglish)
            ?? throw new InvalidOperationException("Unknown entrepreneur form code.");
        var imageFiles = ResolveOfficialFormPages(request.FormCode);
        if (imageFiles.Count == 0)
        {
            throw new FileNotFoundException($"Official form assets were not found for {request.FormCode}.");
        }

        return Document.Create(container =>
        {
            for (var pageIndex = 0; pageIndex < imageFiles.Count; pageIndex++)
            {
                var imageBytes = BuildFilledPageImage(request.FormCode, request.Registry, imageFiles[pageIndex], pageIndex);

                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.PageColor(Colors.White);
                    page.Content()
                        .Padding(0)
                        .Image(imageBytes)
                        .FitArea();
                });
            }

            container.Page(page => BuildAnalyticsAppendix(page, request, form, isEnglish));
        }).GeneratePdf();
    }

    private byte[] BuildFilledPageImage(
        string formCode,
        IndividualEntrepreneurRegistryProfileResponse registry,
        string imagePath,
        int pageIndex)
    {
        using var inputStream = File.OpenRead(imagePath);
        using var bitmap = SKBitmap.Decode(inputStream)
            ?? throw new InvalidOperationException($"Unable to decode form page image: {imagePath}");
        using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height))
            ?? throw new InvalidOperationException("Unable to create drawing surface for official form rendering.");
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);
        canvas.DrawBitmap(bitmap, 0, 0);

        ApplyOfficialOverlay(formCode, pageIndex, registry, canvas);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private void ApplyOfficialOverlay(
        string formCode,
        int pageIndex,
        IndividualEntrepreneurRegistryProfileResponse registry,
        SKCanvas canvas)
    {
        var reportingDate = DateTime.UtcNow;

        switch (formCode)
        {
            case "910.00" when pageIndex == 0:
                Apply910FirstPage(canvas, registry, reportingDate);
                break;
            case "220.00" when pageIndex == 0:
                Apply220FirstPage(canvas, registry, reportingDate);
                break;
            case "200.00" when pageIndex == 0:
                Apply200FirstPage(canvas, registry, reportingDate);
                break;
        }
    }

    private void Apply910FirstPage(
        SKCanvas canvas,
        IndividualEntrepreneurRegistryProfileResponse registry,
        DateTime reportingDate)
    {
        var (halfYear, year) = GetLatestClosedHalfYear(reportingDate);
        var uppercaseName = NormalizeForOfficialForm(registry.Name);

        DrawTextInBoxes(canvas, registry.Iin, 114, 104, 29, 24, 2, 12, 17f);
        DrawTextAcrossLines(
            canvas,
            uppercaseName,
            [
                new BoxLine(203, 142, 19, 22, 2, 34),
                new BoxLine(38, 178, 19, 22, 2, 34),
                new BoxLine(38, 214, 19, 22, 2, 34)
            ],
            14f);
        DrawTextInBoxes(canvas, halfYear.ToString(CultureInfo.InvariantCulture), 531, 253, 16, 22, 0, 1, 14f);
        DrawTextInBoxes(canvas, year.ToString("0000", CultureInfo.InvariantCulture), 607, 253, 16, 22, 2, 4, 14f);
        DrawCheckMark(canvas, 225, 315, 18, 18);
        DrawTextInBoxes(canvas, "398", 88, 503, 15, 21, 2, 3, 13f);
        DrawCheckMark(canvas, 555, 504, 18, 18);
    }

    private void Apply220FirstPage(
        SKCanvas canvas,
        IndividualEntrepreneurRegistryProfileResponse registry,
        DateTime reportingDate)
    {
        var reportYear = GetLatestClosedYear(reportingDate, registry);
        var uppercaseName = NormalizeForOfficialForm(registry.Name);

        DrawTextInBoxes(canvas, registry.Iin, 228, 428, 48, 64, 8, 12, 33f);
        DrawTextInBoxes(canvas, reportYear.ToString("0000", CultureInfo.InvariantCulture), 1410, 512, 54, 63, 9, 4, 31f);
        DrawTextAcrossLines(
            canvas,
            uppercaseName,
            [
                new BoxLine(468, 595, 47, 61, 8, 21),
                new BoxLine(124, 678, 47, 61, 8, 24),
                new BoxLine(124, 761, 47, 61, 8, 24)
            ],
            31f);
        DrawCheckMark(canvas, 555, 853, 36, 36);
        DrawTextInBoxes(canvas, "398", 624, 1408, 48, 62, 8, 3, 31f);
        DrawCheckMark(canvas, 1640, 1492, 40, 40);
        DrawText(canvas, uppercaseName, 120, 2360, 28f);
        DrawDateDigits(canvas, reportingDate, 255, 2528, 48, 62, 8, 8, 31f);
    }

    private void Apply200FirstPage(
        SKCanvas canvas,
        IndividualEntrepreneurRegistryProfileResponse registry,
        DateTime reportingDate)
    {
        var (quarter, year) = GetLatestClosedQuarter(reportingDate);
        var uppercaseName = NormalizeForOfficialForm(registry.Name);

        DrawTextInBoxes(canvas, registry.Iin, 281, 368, 46, 62, 8, 12, 31f);
        DrawTextInBoxes(canvas, quarter.ToString(CultureInfo.InvariantCulture), 1508, 459, 45, 62, 0, 1, 31f);
        DrawTextInBoxes(canvas, year.ToString("0000", CultureInfo.InvariantCulture), 1686, 459, 53, 62, 8, 4, 31f);
        DrawTextAcrossLines(
            canvas,
            uppercaseName,
            [
                new BoxLine(964, 592, 47, 61, 8, 22),
                new BoxLine(90, 676, 47, 61, 8, 28),
                new BoxLine(90, 759, 47, 61, 8, 28),
                new BoxLine(90, 842, 47, 61, 8, 7)
            ],
            30f);
        DrawCheckMark(canvas, 398, 909, 36, 36);
        DrawTextInBoxes(canvas, "398", 264, 1914, 47, 61, 8, 3, 30f);
        DrawCheckMark(canvas, 1606, 1994, 38, 38);
    }

    private void BuildAnalyticsAppendix(
        PageDescriptor page,
        GenerateEntrepreneurFormPdfRequest request,
        EntrepreneurReportFormResponse form,
        bool isEnglish)
    {
        var reportingDate = DateTime.UtcNow;
        var culture = CultureInfo.GetCultureInfo(isEnglish ? "en-US" : "ru-RU");
        var registry = request.Registry;
        var latestStatistics = registry.Statistics
            .OrderByDescending(item => item.Year)
            .FirstOrDefault();

        page.Size(PageSizes.A4);
        page.Margin(24);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(text => text.FontSize(10));
        page.Content().Column(column =>
        {
            column.Spacing(16);

            column.Item().Text(isEnglish
                ? $"Analytics appendix for official form {form.FormCode}"
                : $"Аналитическое приложение к официальной форме {form.FormCode}")
                .FontSize(18)
                .SemiBold();

            column.Item().Text(isEnglish
                ? "The official form is prefilled only with the fields that are directly available in the KGD-based analytics section. Monetary tax lines that require primary accounting documents remain blank."
                : "Официальный бланк автозаполняется только теми полями, которые напрямую доступны в аналитике на основе данных КГД. Денежные налоговые строки, требующие первичных бухгалтерских документов, остаются пустыми.")
                .FontColor(Colors.Grey.Darken2);

            column.Item().Element(container => BuildKeyValueTable(
                container,
                isEnglish ? "Autofilled official fields" : "Автозаполненные поля официального бланка",
                isEnglish ? "Field" : "Поле",
                isEnglish ? "Value" : "Значение",
                isEnglish ? "How used in the report" : "Как используется в отчете",
                BuildPrefilledFieldRows(request.FormCode, registry, reportingDate, isEnglish, culture)));

            column.Item().Element(container => BuildKeyValueTable(
                container,
                isEnglish ? "Analytics data from the entrepreneur section" : "Данные из раздела аналитики по ИП",
                isEnglish ? "Indicator" : "Показатель",
                isEnglish ? "Value" : "Значение",
                isEnglish ? "Meaning for the report" : "Значение для отчета",
                BuildAnalyticsRows(registry, latestStatistics, isEnglish, culture)));

            if (registry.Statistics.Count > 0)
            {
                column.Item().Element(container => BuildStatisticsTable(
                    container,
                    registry.Statistics.OrderBy(item => item.Year).ToList(),
                    isEnglish,
                    culture));
            }

            column.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(12).Column(note =>
            {
                note.Spacing(6);
                note.Item().Text(isEnglish ? "Source and report notes" : "Источник и примечания по отчету").SemiBold();
                note.Item().Text(isEnglish
                    ? $"Official form source: {form.OfficialSourceUrl}"
                    : $"Официальный источник бланка: {form.OfficialSourceUrl}");
                note.Item().Text(isEnglish
                    ? $"Data prepared for: {request.GeneratedFor ?? "-"}"
                    : $"Отчет сформирован для: {request.GeneratedFor ?? "-"}");
                note.Item().Text(isEnglish
                    ? $"Registry actuality date: {FormatDate(registry.ActualityDate, culture)}"
                    : $"Дата актуальности реестра: {FormatDate(registry.ActualityDate, culture)}");
                note.Item().Text(isEnglish
                    ? "Blank declaration lines must be completed from bookkeeping, payroll and bank data before filing."
                    : "Пустые строки декларации должны быть дополнены по данным бухучета, зарплатных регистров и банковских документов перед подачей.");
            });
        });
    }

    private static void BuildKeyValueTable(
        IContainer container,
        string title,
        string firstColumnTitle,
        string secondColumnTitle,
        string thirdColumnTitle,
        IReadOnlyList<AppendixRow> rows)
    {
        container.Column(column =>
        {
            column.Spacing(8);
            column.Item().Text(title).SemiBold().FontSize(12);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(1.7f);
                });

                table.Header(header =>
                {
                    AddHeaderCell(header.Cell(), firstColumnTitle);
                    AddHeaderCell(header.Cell(), secondColumnTitle);
                    AddHeaderCell(header.Cell(), thirdColumnTitle);
                });

                foreach (var row in rows)
                {
                    AddBodyCell(table, row.Label);
                    AddBodyCell(table, row.Value);
                    AddBodyCell(table, row.Description);
                }
            });
        });
    }

    private static void BuildStatisticsTable(
        IContainer container,
        IReadOnlyList<EntrepreneurStatisticResponse> statistics,
        bool isEnglish,
        CultureInfo culture)
    {
        container.Column(column =>
        {
            column.Spacing(8);
            column.Item().Text(isEnglish
                ? "Registry statistics used in analytics"
                : "Статистика из реестра, используемая в аналитике")
                .SemiBold()
                .FontSize(12);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(56);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    AddHeaderCell(header.Cell(), isEnglish ? "Year" : "Год");
                    AddHeaderCell(header.Cell(), isEnglish ? "Workers" : "Работники");
                    AddHeaderCell(header.Cell(), isEnglish ? "Tax paid" : "Налогов поступило");
                    AddHeaderCell(header.Cell(), isEnglish ? "KNN" : "КНН");
                    AddHeaderCell(header.Cell(), isEnglish ? "VAT" : "НДС");
                });

                foreach (var item in statistics)
                {
                    AddBodyCell(table, item.Year.ToString(CultureInfo.InvariantCulture));
                    AddBodyCell(table, FormatNumber(item.WorkersCount, culture, 0));
                    AddBodyCell(table, FormatNumber(item.TaxIn, culture));
                    AddBodyCell(table, FormatNumber(item.Knn, culture));
                    AddBodyCell(table, FormatNumber(item.VatAmount, culture));
                }
            });
        });
    }

    private static List<AppendixRow> BuildPrefilledFieldRows(
        string formCode,
        IndividualEntrepreneurRegistryProfileResponse registry,
        DateTime reportingDate,
        bool isEnglish,
        CultureInfo culture)
    {
        var periodDescription = formCode switch
        {
            "910.00" => FormatHalfYearPeriod(GetLatestClosedHalfYear(reportingDate), isEnglish),
            "200.00" => FormatQuarterPeriod(GetLatestClosedQuarter(reportingDate), isEnglish),
            "220.00" => GetLatestClosedYear(reportingDate, registry).ToString(CultureInfo.InvariantCulture),
            _ => "-"
        };

        return
        [
            new AppendixRow(
                isEnglish ? "IIN" : "ИИН",
                registry.Iin,
                isEnglish ? "Filled into the taxpayer code cells of the official form." : "Подставляется в ячейки кода налогоплательщика официального бланка."),
            new AppendixRow(
                isEnglish ? "Taxpayer name" : "Наименование / ФИО",
                registry.Name,
                isEnglish ? "Filled into the taxpayer name lines of the official form." : "Подставляется в строки с наименованием налогоплательщика официального бланка."),
            new AppendixRow(
                isEnglish ? "Reporting period" : "Отчетный период",
                periodDescription,
                isEnglish ? "Determined automatically from the current filing cycle and the entrepreneur form type." : "Определяется автоматически по текущему циклу сдачи и типу формы ИП."),
            new AppendixRow(
                isEnglish ? "Currency code" : "Код валюты",
                "398",
                isEnglish ? "Prefilled as Kazakhstan tenge." : "Автозаполняется как казахстанский тенге."),
            new AppendixRow(
                isEnglish ? "Residency status" : "Статус резидентства",
                string.IsNullOrWhiteSpace(registry.Residency) ? "-" : registry.Residency,
                isEnglish ? "Used to mark the resident checkbox in the official form." : "Используется для отметки признака резидента в официальном бланке."),
            new AppendixRow(
                isEnglish ? "Actuality date" : "Дата актуальности",
                FormatDate(registry.ActualityDate, culture),
                isEnglish ? "Included in the appendix as the reference point for registry freshness." : "Показывается в приложении как контрольная дата актуальности реестра.")
        ];
    }

    private static List<AppendixRow> BuildAnalyticsRows(
        IndividualEntrepreneurRegistryProfileResponse registry,
        EntrepreneurStatisticResponse? latestStatistics,
        bool isEnglish,
        CultureInfo culture)
    {
        return
        [
            new AppendixRow(
                isEnglish ? "Tax mode" : "Налоговый режим",
                string.IsNullOrWhiteSpace(registry.TaxMode) ? "-" : registry.TaxMode,
                isEnglish ? "Determines which official entrepreneur forms are suggested in the report section." : "Определяет, какие официальные формы ИП предлагаются в разделе отчетов."),
            new AppendixRow(
                isEnglish ? "Registration date" : "Дата регистрации",
                FormatDate(registry.RegistrationDate, culture),
                isEnglish ? "Shown in analytics and included as reference data in the report package." : "Показывается в аналитике и включается в пакет отчета как справочные данные."),
            new AppendixRow(
                isEnglish ? "Main OKED" : "Основной ОКЭД",
                BuildOkedValue(registry),
                isEnglish ? "Explains the entrepreneur profile in analytics." : "Поясняет профиль деятельности ИП в аналитике."),
            new AppendixRow(
                isEnglish ? "Risk degree" : "Степень риска",
                string.IsNullOrWhiteSpace(registry.RiskDegree) ? "-" : registry.RiskDegree,
                isEnglish ? "Added as an analytical registry indicator." : "Добавляется как аналитический индикатор из реестра."),
            new AppendixRow(
                isEnglish ? "Tax debt" : "Налоговая задолженность",
                FormatNumber(registry.TaxDebt, culture),
                isEnglish ? "Added as an analytical registry indicator." : "Добавляется как аналитический индикатор из реестра."),
            new AppendixRow(
                isEnglish ? "Latest workers count" : "Последнее число работников",
                FormatNumber(latestStatistics?.WorkersCount ?? 0m, culture, 0),
                isEnglish ? "Used to decide whether form 200.00 should be offered." : "Используется для решения, нужно ли предлагать форму 200.00."),
            new AppendixRow(
                isEnglish ? "Latest tax paid indicator" : "Последний показатель налогов",
                FormatNumber(latestStatistics?.TaxIn ?? 0m, culture),
                isEnglish ? "Shown in the entrepreneur analytics trend and carried into this appendix." : "Показывается в тренде аналитики ИП и переносится в это приложение."),
            new AppendixRow(
                isEnglish ? "Latest VAT indicator" : "Последний показатель НДС",
                FormatNumber(latestStatistics?.VatAmount ?? 0m, culture),
                isEnglish ? "Shown in the entrepreneur analytics trend and carried into this appendix." : "Показывается в тренде аналитики ИП и переносится в это приложение.")
        ];
    }

    private static (int HalfYear, int Year) GetLatestClosedHalfYear(DateTime reportingDate)
    {
        if (reportingDate.Month <= 6)
        {
            return (2, reportingDate.Year - 1);
        }

        return (1, reportingDate.Year);
    }

    private static (int Quarter, int Year) GetLatestClosedQuarter(DateTime reportingDate)
    {
        return reportingDate.Month switch
        {
            <= 3 => (4, reportingDate.Year - 1),
            <= 6 => (1, reportingDate.Year),
            <= 9 => (2, reportingDate.Year),
            _ => (3, reportingDate.Year)
        };
    }

    private static int GetLatestClosedYear(
        DateTime reportingDate,
        IndividualEntrepreneurRegistryProfileResponse registry)
    {
        return registry.Statistics
            .OrderByDescending(item => item.Year)
            .Select(item => item.Year)
            .FirstOrDefault(reportingDate.Year - 1);
    }

    private static string BuildOkedValue(IndividualEntrepreneurRegistryProfileResponse registry)
    {
        var oked = registry.Oked?.Trim();
        var okedName = registry.OkedName?.Trim();

        if (string.IsNullOrWhiteSpace(oked) && string.IsNullOrWhiteSpace(okedName))
        {
            return "-";
        }

        return $"{oked} {okedName}".Trim();
    }

    private static string NormalizeForOfficialForm(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalizedWhitespace = string.Join(
            " ",
            value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return normalizedWhitespace
            .ToUpperInvariant()
            .Replace('Ё', 'Е');
    }

    private static string FormatHalfYearPeriod((int HalfYear, int Year) period, bool isEnglish)
    {
        return isEnglish
            ? $"H{period.HalfYear} {period.Year}"
            : $"{period.HalfYear}-е полугодие {period.Year}";
    }

    private static string FormatQuarterPeriod((int Quarter, int Year) period, bool isEnglish)
    {
        return isEnglish
            ? $"Q{period.Quarter} {period.Year}"
            : $"{period.Quarter}-й квартал {period.Year}";
    }

    private static string FormatDate(DateTime? value, CultureInfo culture)
    {
        return value.HasValue
            ? value.Value.ToString("dd.MM.yyyy", culture)
            : "-";
    }

    private static string FormatNumber(decimal value, CultureInfo culture, int decimals = 2)
    {
        return value.ToString($"N{decimals}", culture);
    }

    private static void DrawTextAcrossLines(SKCanvas canvas, string text, IReadOnlyList<BoxLine> lines, float fontSize)
    {
        var characters = text.ToCharArray();
        var offset = 0;

        foreach (var line in lines)
        {
            if (offset >= characters.Length)
            {
                break;
            }

            var sliceLength = Math.Min(line.BoxCount, characters.Length - offset);
            var segment = new string(characters, offset, sliceLength);
            DrawTextInBoxes(canvas, segment, line.StartX, line.StartY, line.BoxWidth, line.BoxHeight, line.Gap, line.BoxCount, fontSize);
            offset += sliceLength;
        }
    }

    private static void DrawDateDigits(
        SKCanvas canvas,
        DateTime date,
        float startX,
        float startY,
        float boxWidth,
        float boxHeight,
        float gap,
        int boxCount,
        float fontSize)
    {
        var digits = date.ToString("ddMMyyyy", CultureInfo.InvariantCulture);
        DrawTextInBoxes(canvas, digits, startX, startY, boxWidth, boxHeight, gap, boxCount, fontSize);
    }

    private static void DrawTextInBoxes(
        SKCanvas canvas,
        string text,
        float startX,
        float startY,
        float boxWidth,
        float boxHeight,
        float gap,
        int boxCount,
        float fontSize)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        using var paint = CreateTextPaint(fontSize);
        var filteredText = text.ToUpperInvariant();
        for (var index = 0; index < Math.Min(filteredText.Length, boxCount); index++)
        {
            var value = filteredText[index].ToString();
            var x = startX + index * (boxWidth + gap) + boxWidth / 2f;
            var y = startY + boxHeight / 2f + fontSize / 2.8f;
            DrawCenteredText(canvas, value, x, y, paint);
        }
    }

    private static void DrawText(SKCanvas canvas, string text, float x, float baselineY, float fontSize)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        using var paint = CreateTextPaint(fontSize);
        canvas.DrawText(text, x, baselineY, paint);
    }

    private static void DrawCheckMark(SKCanvas canvas, float x, float y, float width, float height)
    {
        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Math.Max(2f, Math.Min(width, height) / 8f)
        };

        canvas.DrawLine(x + width * 0.15f, y + height * 0.55f, x + width * 0.42f, y + height * 0.82f, paint);
        canvas.DrawLine(x + width * 0.42f, y + height * 0.82f, x + width * 0.88f, y + height * 0.18f, paint);
    }

    private static void DrawCenteredText(SKCanvas canvas, string value, float centerX, float baselineY, SKPaint paint)
    {
        var bounds = new SKRect();
        paint.MeasureText(value, ref bounds);
        canvas.DrawText(value, centerX - bounds.MidX, baselineY, paint);
    }

    private static SKPaint CreateTextPaint(float fontSize)
    {
        return new SKPaint
        {
            Color = SKColors.Black,
            TextSize = fontSize,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                ?? SKTypeface.Default
        };
    }

    private static void AddHeaderCell(IContainer cell, string text)
    {
        cell
            .Background(Colors.Grey.Lighten3)
            .Border(1)
            .BorderColor(Colors.Grey.Lighten1)
            .Padding(6)
            .Text(text)
            .SemiBold();
    }

    private static void AddBodyCell(TableDescriptor table, string text)
    {
        table.Cell()
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(6)
            .Text(string.IsNullOrWhiteSpace(text) ? "-" : text);
    }

    private List<string> ResolveOfficialFormPages(string formCode)
    {
        var assetDirectory = formCode switch
        {
            "200.00" => Path.Combine(_environment.ContentRootPath, "Assets", "EntrepreneurForms", "200.00"),
            "220.00" => Path.Combine(_environment.ContentRootPath, "Assets", "EntrepreneurForms", "220.00"),
            "910.00" => Path.Combine(_environment.ContentRootPath, "Assets", "EntrepreneurForms", "910.00"),
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(assetDirectory) || !Directory.Exists(assetDirectory))
        {
            return [];
        }

        return Directory.GetFiles(assetDirectory, "*.png", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void EnsureQuestPdfConfigured()
    {
        if (_questPdfConfigured)
        {
            return;
        }

        lock (QuestPdfSync)
        {
            if (_questPdfConfigured)
            {
                return;
            }

            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.UseEnvironmentFonts = false;
            _questPdfConfigured = true;
        }
    }

    private sealed record BoxLine(float StartX, float StartY, float BoxWidth, float BoxHeight, float Gap, int BoxCount);
    private sealed record AppendixRow(string Label, string Value, string Description);
}
