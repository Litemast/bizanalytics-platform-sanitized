using BizAnalytics.Api.Contracts.Analytics;
using BizAnalytics.Api.Contracts.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace BizAnalytics.Api.Services;

public class AnalyticsReportService
{
    private static readonly object QuestPdfSync = new();
    private static bool _questPdfConfigured;

    public byte[] GeneratePdf(GenerateAnalyticsReportRequest request)
    {
        EnsureQuestPdfConfigured();

        var isEnglish = request.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        var culture = CultureInfo.GetCultureInfo(isEnglish ? "en-US" : "ru-RU");
        var analytics = request.Analytics;
        var generatedAtUtc = DateTime.UtcNow;
        var sections = BuildSections(analytics, isEnglish, culture);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(10.5f).FontColor("#1D2C45"));

                page.Content().Column(column =>
                {
                    column.Spacing(18);

                    AddReportMetadata(column, request, isEnglish, culture, generatedAtUtc);
                    AddReportOverview(column, request, analytics, isEnglish, sectionCount: sections.Count);

                    if (sections.Count == 0)
                    {
                        AddEmptyState(column, isEnglish);
                    }
                    else
                    {
                        for (var index = 0; index < sections.Count; index++)
                        {
                            if (index > 0)
                            {
                                column.Item().PageBreak();
                            }

                            sections[index](column);
                        }
                    }
                });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span(isEnglish ? "Generated in BizAnalytics" : "Сформировано в BizAnalytics");
                        text.Span(" | ");
                        text.CurrentPageNumber();
                        text.Span("/");
                        text.TotalPages();
                    });
            });
        }).GeneratePdf();
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
            // Linux cloud containers may expose incomplete host font stacks.
            QuestPDF.Settings.UseEnvironmentFonts = false;
            _questPdfConfigured = true;
        }
    }

    private static List<Action<ColumnDescriptor>> BuildSections(
        AnalysisBundleResponse analytics,
        bool isEnglish,
        CultureInfo culture)
    {
        var sections = new List<Action<ColumnDescriptor>>();

        if (HasSalesData(analytics))
        {
            sections.Add(column => AddSalesSection(column, analytics, isEnglish, culture));
        }

        if (analytics.Financial is not null)
        {
            sections.Add(column => AddFinancialSection(column, analytics.Financial, isEnglish, culture));
        }

        if (analytics.Education is not null)
        {
            sections.Add(column => AddEducationSection(column, analytics.Education, isEnglish, culture));
        }

        return sections;
    }

    private static void AddReportMetadata(
        ColumnDescriptor column,
        GenerateAnalyticsReportRequest request,
        bool isEnglish,
        CultureInfo culture,
        DateTime generatedAtUtc)
    {
        column.Item()
            .BorderBottom(1)
            .BorderColor("#DCE5F4")
            .PaddingBottom(16)
            .Column(header =>
            {
                header.Spacing(4);
                header.Item()
                    .Text(isEnglish ? "BizAnalytics Report" : "Отчет BizAnalytics")
                    .FontSize(22)
                    .Bold()
                    .FontColor("#14315C");

                header.Item()
                    .Text(string.Format(
                        culture,
                        isEnglish ? "Company: {0}" : "Компания: {0}",
                        request.OrganizationName))
                    .FontSize(15)
                    .SemiBold();

                if (!string.IsNullOrWhiteSpace(request.AnalysisName))
                {
                    header.Item()
                        .Text(string.Format(
                            culture,
                            isEnglish ? "Analysis: {0}" : "Анализ: {0}",
                            request.AnalysisName))
                        .FontColor("#5E6D86");
                }

                header.Item()
                    .Text(string.Format(
                        culture,
                        isEnglish ? "Generated: {0:g}" : "Сформировано: {0:g}",
                        generatedAtUtc))
                    .FontColor("#5E6D86");

                if (!string.IsNullOrWhiteSpace(request.GeneratedFor))
                {
                    header.Item()
                        .Text(string.Format(
                            culture,
                            isEnglish ? "Prepared for: {0}" : "Подготовлено для: {0}",
                            request.GeneratedFor))
                        .FontColor("#5E6D86");
                }

                if (request.PeriodStart.HasValue || request.PeriodEnd.HasValue)
                {
                    var from = request.PeriodStart?.ToString("d", culture) ?? "-";
                    var to = request.PeriodEnd?.ToString("d", culture) ?? "-";

                    header.Item()
                        .Text(string.Format(
                            culture,
                            isEnglish ? "Period: {0} - {1}" : "Период: {0} - {1}",
                            from,
                            to))
                        .FontColor("#5E6D86");
                }
            });
    }

    private static void AddReportOverview(
        ColumnDescriptor column,
        GenerateAnalyticsReportRequest request,
        AnalysisBundleResponse analytics,
        bool isEnglish,
        int sectionCount)
    {
        AddSectionIntro(
            column,
            isEnglish ? "Report overview" : "Обзор отчета",
            BuildOverviewDescription(request, analytics, isEnglish));

        column.Item()
            .Border(1)
            .BorderColor("#DCE5F4")
            .Background("#F7FAFF")
            .Padding(14)
            .Column(content =>
            {
                content.Spacing(10);
                content.Item()
                    .Text(isEnglish
                        ? "The report structure is assembled automatically based on the analytics blocks available in the selected analysis."
                        : "Структура отчета собирается автоматически по тем аналитическим блокам, которые доступны в выбранном анализе.")
                    .FontColor("#40506A");

                content.Item().Row(row =>
                {
                    row.RelativeItem().Element(cell => MetricCell(
                        cell,
                        isEnglish ? "Report type" : "Тип отчета",
                        GetReportTypeLabel(analytics, isEnglish)));
                    row.RelativeItem().Element(cell => MetricCell(
                        cell,
                        isEnglish ? "Sections included" : "Количество разделов",
                        sectionCount.ToString(CultureInfo.InvariantCulture)));
                    row.RelativeItem().Element(cell => MetricCell(
                        cell,
                        isEnglish ? "Analytics language" : "Язык аналитики",
                        isEnglish ? "English" : "Русский"));
                });

                AddBulletList(content, BuildOverviewItems(analytics, isEnglish));
            });
    }

    private static void AddSalesSection(
        ColumnDescriptor column,
        AnalysisBundleResponse analytics,
        bool isEnglish,
        CultureInfo culture)
    {
        AddSectionIntro(
            column,
            isEnglish ? "Sales analytics" : "Аналитика продаж",
            isEnglish
                ? "This section combines revenue metrics, product leaders, source comparison, price dynamics and sales insights."
                : "Этот раздел объединяет показатели выручки, товарных лидеров, сопоставление источников, динамику средней цены и ключевые выводы по продажам.");

        AddBlockIntro(
            column,
            isEnglish ? "Key sales metrics" : "Ключевые показатели продаж",
            isEnglish
                ? "A compact summary of revenue, number of sales, total quantity and average check."
                : "Краткая сводка по выручке, количеству продаж, общему объему и среднему чеку.");

        column.Item().Row(row =>
        {
            row.RelativeItem().Element(cell => MetricCell(cell, isEnglish ? "Revenue" : "Выручка", FormatNumber(analytics.Summary.TotalRevenue, culture)));
            row.RelativeItem().Element(cell => MetricCell(cell, isEnglish ? "Sales count" : "Количество продаж", FormatNumber(analytics.Summary.TotalSalesCount, culture)));
            row.RelativeItem().Element(cell => MetricCell(cell, isEnglish ? "Units sold" : "Объем продаж", FormatNumber(analytics.Summary.TotalQuantity, culture)));
            row.RelativeItem().Element(cell => MetricCell(cell, isEnglish ? "Average check" : "Средний чек", FormatNumber(analytics.Summary.AverageCheck, culture)));
        });

        if (analytics.Insights.Count > 0)
        {
            AddInsightBlock(
                column,
                isEnglish ? "Key conclusions" : "Ключевые выводы",
                isEnglish
                    ? "The block highlights the strongest signals in sales data: leaders, peaks, volatility and trend direction."
                    : "Блок выделяет самые важные сигналы в данных по продажам: лидеров, пики, колебания и направление тренда.",
                analytics.Insights.Take(8).ToList(),
                isEnglish);
        }

        if (analytics.Revenue.Count > 0)
        {
            AddTableBlock(column, isEnglish ? "Revenue by date" : "Выручка по датам",
                isEnglish ? "Shows how daily revenue changed across the selected period." : "Показывает, как менялась ежедневная выручка в выбранном периоде.",
                table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Date" : "Дата");
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Revenue" : "Выручка");
                    });

                    foreach (var point in analytics.Revenue)
                    {
                        table.Cell().Element(BodyCell).Text(point.Date.ToString("d", culture));
                        table.Cell().Element(BodyCell).Text(FormatNumber(point.Revenue, culture));
                    }
                });
        }

        if (analytics.TopProducts.Count > 0)
        {
            AddTableBlock(column, isEnglish ? "Top products" : "Топ товаров",
                isEnglish ? "Products are sorted by revenue contribution and sold quantity." : "Товары отсортированы по вкладу в выручку и проданному количеству.",
                table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Product" : "Товар");
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Quantity" : "Количество");
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Revenue" : "Выручка");
                    });

                    foreach (var product in analytics.TopProducts.Take(10))
                    {
                        table.Cell().Element(BodyCell).Text(product.ProductName);
                        table.Cell().Element(BodyCell).Text(FormatNumber(product.TotalQuantity, culture));
                        table.Cell().Element(BodyCell).Text(FormatNumber(product.TotalRevenue, culture));
                    }
                });
        }

        if (analytics.SourceComparisons.Count > 0)
        {
            AddTableBlock(column, isEnglish ? "Source comparison" : "Сопоставление источников",
                isEnglish ? "Compares uploaded files by record count, quantity, revenue and average unit price." : "Сравнивает загруженные источники по числу записей, объему, выручке и средней цене единицы.",
                table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Source" : "Источник");
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Records" : "Записи");
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Quantity" : "Количество");
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Revenue" : "Выручка");
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Average price" : "Средняя цена");
                    });

                    foreach (var source in analytics.SourceComparisons.Take(8))
                    {
                        table.Cell().Element(BodyCell).Text(source.SourceName);
                        table.Cell().Element(BodyCell).Text(FormatNumber(source.RecordsCount, culture));
                        table.Cell().Element(BodyCell).Text(FormatNumber(source.TotalQuantity, culture));
                        table.Cell().Element(BodyCell).Text(FormatNumber(source.TotalRevenue, culture));
                        table.Cell().Element(BodyCell).Text(FormatNumber(source.AverageUnitPrice, culture));
                    }
                });
        }

        if (analytics.PriceTrends.Count > 0)
        {
            AddTableBlock(column, isEnglish ? "Average unit price by day" : "Средняя цена единицы по дням",
                isEnglish ? "Helps track price fluctuations and identify unstable periods." : "Помогает отследить колебания цены и увидеть нестабильные периоды.",
                table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Date" : "Дата");
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Average price" : "Средняя цена");
                    });

                    foreach (var point in analytics.PriceTrends)
                    {
                        table.Cell().Element(BodyCell).Text(point.Date.ToString("d", culture));
                        table.Cell().Element(BodyCell).Text(FormatNumber(point.AverageUnitPrice, culture));
                    }
                });
        }
    }

    private static void AddFinancialSection(
        ColumnDescriptor column,
        FinancialAnalysisResponse financial,
        bool isEnglish,
        CultureInfo culture)
    {
        AddSectionIntro(
            column,
            isEnglish ? "Financial analytics" : "Финансовая аналитика",
            isEnglish
                ? "This section summarizes revenue, expenses, profit, profitability, period trend and forecast."
                : "Этот раздел объединяет доходы, расходы, прибыль, рентабельность, динамику по периодам и прогноз.");

        AddBlockIntro(
            column,
            isEnglish ? "Key financial metrics" : "Ключевые финансовые показатели",
            isEnglish
                ? "A short snapshot of the financial result for the selected analysis."
                : "Краткая сводка по финансовому результату выбранного анализа.");

        column.Item().Row(row =>
        {
            row.RelativeItem().Element(cell => MetricCell(cell, isEnglish ? "Revenue" : "Доходы", FormatNumber(financial.TotalRevenue, culture)));
            row.RelativeItem().Element(cell => MetricCell(cell, isEnglish ? "Expenses" : "Расходы", FormatNumber(financial.TotalExpenses, culture)));
            row.RelativeItem().Element(cell => MetricCell(cell, isEnglish ? "Profit" : "Прибыль", FormatNumber(financial.TotalProfit, culture)));
            row.RelativeItem().Element(cell => MetricCell(cell, isEnglish ? "Profitability" : "Рентабельность", $"{FormatNumber(financial.Profitability, culture)}%"));
        });

        if (financial.Insights.Count > 0)
        {
            AddInsightBlock(
                column,
                isEnglish ? "Financial conclusions" : "Финансовые выводы",
                isEnglish
                    ? "Highlights the strongest and weakest periods, expense pressure and the current profit outlook."
                    : "Показывает наиболее сильные и слабые периоды, нагрузку по расходам и текущую оценку прогноза прибыли.",
                financial.Insights.Take(8).ToList(),
                isEnglish);
        }

        if (financial.Periods.Count > 0)
        {
            AddTableBlock(column, isEnglish ? "Trend by periods" : "Тренд по периодам",
                isEnglish ? "Each row shows how revenue, expenses, profit and profitability changed by period." : "Каждая строка показывает, как по периодам менялись доходы, расходы, прибыль и рентабельность.",
                table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Period" : "Период");
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Revenue" : "Доходы");
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Expenses" : "Расходы");
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Profit" : "Прибыль");
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Profitability" : "Рентабельность");
                    });

                    foreach (var point in financial.Periods.Take(24))
                    {
                        table.Cell().Element(BodyCell).Text(FormatPeriodLabel(point.PeriodLabel, point.Period, culture));
                        table.Cell().Element(BodyCell).Text(FormatNumber(point.Revenue, culture));
                        table.Cell().Element(BodyCell).Text(FormatNumber(point.Expenses, culture));
                        table.Cell().Element(BodyCell).Text(FormatNumber(point.Profit, culture));
                        table.Cell().Element(BodyCell).Text($"{FormatNumber(point.Profitability, culture)}%");
                    }
                });
        }

        if (financial.ForecastTrend.Count > 0)
        {
            var nextPoint = financial.ForecastTrend.First();

            AddBlockIntro(
                column,
                isEnglish ? "Forecast summary" : "Сводка по прогнозу",
                isEnglish
                    ? "This block reflects the expected profit trajectory for the next periods based on the current financial trend."
                    : "Этот блок показывает ожидаемую траекторию прибыли на следующие периоды на основе текущей финансовой динамики.");

            column.Item().Row(row =>
            {
                row.RelativeItem().Element(cell => MetricCell(cell, isEnglish ? "Next forecast period" : "Ближайший прогнозный период", nextPoint.Period.ToString("dd.MM.yyyy", culture)));
                row.RelativeItem().Element(cell => MetricCell(cell, isEnglish ? "Expected profit" : "Ожидаемая прибыль", FormatNumber(nextPoint.ForecastProfit, culture)));
            });

            AddTableBlock(column, isEnglish ? "Forecast by periods" : "Прогноз по периодам",
                isEnglish ? "Contains the projected profit values for the upcoming periods." : "Содержит прогнозные значения прибыли для следующих периодов.",
                table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Date" : "Дата");
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Forecast profit" : "Прогноз прибыли");
                    });

                    foreach (var point in financial.ForecastTrend.Take(24))
                    {
                        table.Cell().Element(BodyCell).Text(point.Period.ToString("dd.MM.yyyy", culture));
                        table.Cell().Element(BodyCell).Text(FormatNumber(point.ForecastProfit, culture));
                    }
                });
        }
    }

    private static void AddEducationSection(
        ColumnDescriptor column,
        EducationAnalysisResponse education,
        bool isEnglish,
        CultureInfo culture)
    {
        AddSectionIntro(
            column,
            isEnglish ? "Education analytics" : "Аналитика успеваемости",
            isEnglish
                ? "This section describes the average score, success rate, student and subject leaders, forecast and students who may need attention."
                : "Этот раздел описывает средний балл, процент успеваемости, лидеров по ученикам и предметам, прогноз и учеников, которым может потребоваться внимание.");

        AddBlockIntro(
            column,
            isEnglish ? "Key education metrics" : "Ключевые показатели успеваемости",
            isEnglish
                ? "A compact view of the current academic result."
                : "Компактная сводка по текущему учебному результату.");

        column.Item().Row(row =>
        {
            row.RelativeItem().Element(cell => MetricCell(cell, isEnglish ? "Average score" : "Средний балл", FormatNumber(education.AverageScore, culture)));
            row.RelativeItem().Element(cell => MetricCell(cell, isEnglish ? "Success rate" : "Процент успеваемости", $"{FormatNumber(education.SuccessRate, culture)}%"));
            row.RelativeItem().Element(cell => MetricCell(cell, isEnglish ? "Best student" : "Лучший ученик", education.BestStudent));
            row.RelativeItem().Element(cell => MetricCell(cell, isEnglish ? "Best subject" : "Лучший предмет", education.BestSubject));
        });

        if (education.Insights.Count > 0)
        {
            AddInsightBlock(
                column,
                isEnglish ? "Educational conclusions" : "Выводы по успеваемости",
                isEnglish
                    ? "Shows strong students and subjects as well as areas that may require attention."
                    : "Показывает сильные стороны по ученикам и предметам, а также зоны, которые могут потребовать внимания.",
                education.Insights.Take(8).ToList(),
                isEnglish);
        }

        if (education.GradeDistribution.Count > 0)
        {
            AddTableBlock(column, isEnglish ? "Grade distribution" : "Распределение оценок",
                isEnglish ? "Shows how often each grade appears in the analyzed dataset." : "Показывает, как часто каждая оценка встречается в анализируемом наборе данных.",
                table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Grade" : "Оценка");
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Count" : "Количество");
                    });

                    foreach (var point in education.GradeDistribution)
                    {
                        table.Cell().Element(BodyCell).Text(point.Grade);
                        table.Cell().Element(BodyCell).Text(FormatNumber(point.Count, culture));
                    }
                });
        }

        AddEducationPerformanceTable(
            column,
            isEnglish ? "Student rating" : "Рейтинг учеников",
            isEnglish ? "Students are sorted by average score from highest to lowest." : "Ученики отсортированы по среднему баллу от лучших результатов к более низким.",
            isEnglish ? "Student" : "Ученик",
            education.StudentPerformance,
            isEnglish,
            culture);

        AddEducationPerformanceTable(
            column,
            isEnglish ? "Subject rating" : "Рейтинг предметов",
            isEnglish ? "Subjects are ranked by the average result across the loaded records." : "Предметы отсортированы по среднему результату в загруженных данных.",
            isEnglish ? "Subject" : "Предмет",
            education.SubjectPerformance,
            isEnglish,
            culture);

        if (education.StudentForecasts.Count > 0)
        {
            AddTableBlock(column, isEnglish ? "Average score forecast" : "Прогноз среднего балла",
                isEnglish ? "Shows the current and expected average score for the students with the strongest forecasted result." : "Показывает текущий и ожидаемый средний балл по ученикам с наиболее заметным прогнозом.",
                table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Student" : "Ученик");
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Current average" : "Текущий средний");
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Forecast" : "Прогноз");
                    });

                    foreach (var point in education.StudentForecasts.Take(12))
                    {
                        table.Cell().Element(BodyCell).Text(point.StudentName);
                        table.Cell().Element(BodyCell).Text(FormatNumber(point.CurrentAverage, culture));
                        table.Cell().Element(BodyCell).Text(FormatNumber(point.ForecastAverage, culture));
                    }
                });
        }

        if (education.RiskStudents.Count > 0)
        {
            AddTableBlock(column, isEnglish ? "Students needing attention" : "Ученики, требующие внимания",
                isEnglish ? "Lists students whose current average score places them in the risk zone." : "Перечисляет учеников, чей текущий средний балл относит их к зоне риска.",
                table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Student" : "Ученик");
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Average score" : "Средний балл");
                        header.Cell().Element(HeaderCell).Text(isEnglish ? "Risk level" : "Уровень риска");
                    });

                    foreach (var student in education.RiskStudents.Take(12))
                    {
                        table.Cell().Element(BodyCell).Text(student.StudentName);
                        table.Cell().Element(BodyCell).Text(FormatNumber(student.AverageScore, culture));
                        table.Cell().Element(BodyCell).Text(LocalizeRiskLevel(student.RiskLevel, isEnglish));
                    }
                });
        }
    }

    private static void AddEducationPerformanceTable(
        ColumnDescriptor column,
        string title,
        string description,
        string nameHeader,
        IReadOnlyList<EducationPerformancePointResponse> items,
        bool isEnglish,
        CultureInfo culture)
    {
        if (items.Count == 0)
        {
            return;
        }

        AddTableBlock(column, title, description, table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn();
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text(nameHeader);
                header.Cell().Element(HeaderCell).Text(isEnglish ? "Average score" : "Средний балл");
            });

            foreach (var item in items.Take(12))
            {
                table.Cell().Element(BodyCell).Text(item.Name);
                table.Cell().Element(BodyCell).Text(FormatNumber(item.AverageScore, culture));
            }
        });
    }

    private static void AddSectionIntro(ColumnDescriptor column, string title, string description)
    {
        column.Item()
            .Border(1)
            .BorderColor("#DCE5F4")
            .Background("#F7FAFF")
            .Padding(14)
            .Column(content =>
            {
                content.Spacing(5);
                content.Item().Text(title).FontSize(18).Bold().FontColor("#14315C");
                content.Item().Text(description).FontColor("#47556C");
            });
    }

    private static void AddBlockIntro(ColumnDescriptor column, string title, string description)
    {
        column.Item().Column(content =>
        {
            content.Spacing(4);
            content.Item().Text(title).FontSize(13).SemiBold().FontColor("#14315C");
            content.Item().Text(description).FontColor("#55657E");
        });
    }

    private static void AddTableBlock(
        ColumnDescriptor column,
        string title,
        string description,
        Action<TableDescriptor> tableBuilder)
    {
        AddBlockIntro(column, title, description);
        column.Item().Table(tableBuilder);
    }

    private static void AddInsightBlock(
        ColumnDescriptor column,
        string title,
        string description,
        IReadOnlyList<InsightResponse> insights,
        bool isEnglish)
    {
        if (insights.Count == 0)
        {
            return;
        }

        AddBlockIntro(column, title, description);
        column.Item().Column(insightColumn =>
        {
            insightColumn.Spacing(8);

            foreach (var insight in insights)
            {
                insightColumn.Item()
                    .Border(1)
                    .BorderColor("#DCE5F4")
                    .Background("#FFFFFF")
                    .Padding(10)
                    .Column(card =>
                    {
                        card.Spacing(4);
                        card.Item().Text(insight.Title).SemiBold().FontColor("#14315C");
                        card.Item().Text(insight.Description).FontColor("#45556E");

                        if (!string.IsNullOrWhiteSpace(insight.Tone))
                        {
                            card.Item()
                                .Text(isEnglish
                                    ? $"Focus: {LocalizeInsightTone(insight.Tone, true)}"
                                    : $"Акцент: {LocalizeInsightTone(insight.Tone, false)}")
                                .FontSize(9)
                                .FontColor("#6A7891");
                        }
                    });
            }
        });
    }

    private static void AddEmptyState(ColumnDescriptor column, bool isEnglish)
    {
        column.Item()
            .Border(1)
            .BorderColor("#DCE5F4")
            .Padding(14)
            .Column(content =>
            {
                content.Spacing(6);
                content.Item().Text(isEnglish ? "No analytics data yet" : "Аналитические данные пока отсутствуют").SemiBold().FontColor("#14315C");
                content.Item().Text(
                    isEnglish
                        ? "Run the analysis for the selected workspace first, then return to export a structured report."
                        : "Сначала выполните анализ для выбранного рабочего пространства, после этого можно будет сформировать структурированный отчет.")
                    .FontColor("#47556C");
            });
    }

    private static void AddBulletList(ColumnDescriptor column, IReadOnlyList<string> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        column.Item().Column(list =>
        {
            list.Spacing(5);

            foreach (var item in items)
            {
                list.Item().Row(row =>
                {
                    row.ConstantItem(12).AlignTop().Text("•").FontColor("#2E6BE6");
                    row.RelativeItem().Text(item).FontColor("#45556E");
                });
            }
        });
    }

    private static bool HasSalesData(AnalysisBundleResponse analytics)
    {
        return analytics.Revenue.Count > 0 ||
               analytics.TopProducts.Count > 0 ||
               analytics.Summary.TotalSalesCount > 0;
    }

    private static string BuildOverviewDescription(
        GenerateAnalyticsReportRequest request,
        AnalysisBundleResponse analytics,
        bool isEnglish)
    {
        var sections = BuildOverviewItems(analytics, isEnglish);
        var sectionText = sections.Count == 0
            ? (isEnglish ? "no analytical sections" : "аналитические разделы отсутствуют")
            : string.Join("; ", sections);

        return isEnglish
            ? $"The document is prepared for the selected analysis and includes {sectionText}."
            : $"Документ сформирован по выбранному анализу и включает следующие разделы: {sectionText}.";
    }

    private static List<string> BuildOverviewItems(AnalysisBundleResponse analytics, bool isEnglish)
    {
        var items = new List<string>();

        if (HasSalesData(analytics))
        {
            items.Add(isEnglish
                ? "sales metrics, revenue trend, top products, source comparison and price dynamics"
                : "показатели продаж, динамику выручки, топ товаров, сопоставление источников и динамику цены");
        }

        if (analytics.Financial is not null)
        {
            items.Add(isEnglish
                ? "financial metrics, trend by periods, profit forecast and financial conclusions"
                : "финансовые показатели, тренд по периодам, прогноз прибыли и финансовые выводы");
        }

        if (analytics.Education is not null)
        {
            items.Add(isEnglish
                ? "education metrics, grade distribution, ratings, forecasts and risk group"
                : "показатели успеваемости, распределение оценок, рейтинги, прогнозы и группу риска");
        }

        return items;
    }

    private static string GetReportTypeLabel(AnalysisBundleResponse analytics, bool isEnglish)
    {
        var parts = new List<string>();

        if (HasSalesData(analytics))
        {
            parts.Add(isEnglish ? "sales analytics" : "аналитика продаж");
        }

        if (analytics.Financial is not null)
        {
            parts.Add(isEnglish ? "financial analytics" : "финансовая аналитика");
        }

        if (analytics.Education is not null)
        {
            parts.Add(isEnglish ? "education analytics" : "аналитика успеваемости");
        }

        if (parts.Count == 0)
        {
            return isEnglish ? "No analytical data" : "Нет аналитических данных";
        }

        return string.Join(", ", parts);
    }

    private static string LocalizeRiskLevel(string riskLevel, bool isEnglish)
    {
        return riskLevel.ToLowerInvariant() switch
        {
            "high" => isEnglish ? "High" : "Высокий",
            "medium" => isEnglish ? "Medium" : "Средний",
            "low" => isEnglish ? "Low" : "Низкий",
            _ => string.IsNullOrWhiteSpace(riskLevel)
                ? (isEnglish ? "Not specified" : "Не указан")
                : riskLevel
        };
    }

    private static string LocalizeInsightTone(string tone, bool isEnglish)
    {
        return tone.ToLowerInvariant() switch
        {
            "peak" => isEnglish ? "peak values" : "пиковые показатели",
            "leader" => isEnglish ? "leader signal" : "лидирующий показатель",
            "volatility" => isEnglish ? "volatility" : "колебания",
            "risk" => isEnglish ? "risk area" : "зона риска",
            "trend" => isEnglish ? "trend" : "тренд",
            "signal" => isEnglish ? "notable signal" : "заметный сигнал",
            _ => tone
        };
    }

    private static void MetricCell(IContainer container, string label, string value)
    {
        container
            .Border(1)
            .BorderColor("#DCE5F4")
            .Background("#F7FAFF")
            .Padding(10)
            .Column(column =>
            {
                column.Spacing(2);
                column.Item().Text(label).FontSize(9.5f).FontColor("#58677F");
                column.Item().Text(string.IsNullOrWhiteSpace(value) ? "-" : value).Bold().FontSize(14).FontColor("#14315C");
            });
    }

    private static IContainer HeaderCell(IContainer container)
    {
        return container
            .Background("#EEF4FF")
            .BorderBottom(1)
            .BorderColor("#D4DDEC")
            .PaddingVertical(6)
            .PaddingHorizontal(5);
    }

    private static IContainer BodyCell(IContainer container)
    {
        return container
            .BorderBottom(1)
            .BorderColor("#EEF2F8")
            .PaddingVertical(6)
            .PaddingHorizontal(5);
    }

    private static string FormatNumber(decimal value, CultureInfo culture)
    {
        return value.ToString("N2", culture);
    }

    private static string FormatNumber(int value, CultureInfo culture)
    {
        return value.ToString("N0", culture);
    }

    private static string FormatPeriodLabel(string label, DateTime period, CultureInfo culture)
    {
        return string.IsNullOrWhiteSpace(label)
            ? period.ToString("d", culture)
            : label;
    }
}
