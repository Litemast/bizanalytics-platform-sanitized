using BizAnalytics.Api.Contracts.Analytics;
using BizAnalytics.Api.Contracts.Entrepreneurs;
using BizAnalytics.Api.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BizAnalytics.Api.Services;

public class IndividualEntrepreneurRegistryService
{
    private const string TaxpayerSearchApiPath = "/services/isnaportalsync/public/taxpayer-data";
    private const string CounterpartyOpenDataApiPath = "/services/isnaportal/public/get-sur-data";
    private const string RiskDegreeApiPath = "/services/isnaportalsync/public/find-risk-degree";
    private const string SpecialTaxModeApiPath = "/services/isnaportalsync/public/snr-search/search";

    private readonly HttpClient _httpClient;
    private readonly KgdRegistryOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IndividualEntrepreneurRegistryService(
        HttpClient httpClient,
        IOptions<KgdRegistryOptions> options,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IndividualEntrepreneurSearchResponse> SearchAsync(string iin, CancellationToken cancellationToken)
    {
        if (string.Equals(_options.Mode, "demo", StringComparison.OrdinalIgnoreCase))
        {
            return BuildDemoResponse(iin);
        }

        if (string.IsNullOrWhiteSpace(_options.PortalToken))
        {
            return new IndividualEntrepreneurSearchResponse
            {
                Mode = "live",
                Found = false,
                SearchedIin = iin,
                Message = IsEnglishRequested()
                    ? "KGD registry token is not configured. Switch to demo mode or provide X-Portal-Token."
                    : "Токен реестра КГД не настроен. Переключите сервис в demo-режим или укажите X-Portal-Token.",
                OfficialLinks = BuildOfficialLinks()
            };
        }

        return await SearchLiveAsync(iin, cancellationToken);
    }

    private async Task<IndividualEntrepreneurSearchResponse> SearchLiveAsync(string iin, CancellationToken cancellationToken)
    {
        using var taxpayerSearchDocument = await SendGetJsonAsync(
            TaxpayerSearchApiPath,
            new Dictionary<string, string?>
            {
                ["taxpayerCode"] = iin,
                ["taxpayerType"] = "IP",
                ["print"] = "false"
            },
            cancellationToken);

        var taxpayerResult = ExtractTaxpayerSearch(taxpayerSearchDocument);
        if (taxpayerResult is null)
        {
            return new IndividualEntrepreneurSearchResponse
            {
                Mode = "live",
                Found = false,
                SearchedIin = iin,
                Message = IsEnglishRequested()
                    ? "No individual entrepreneur was found in the KGD registry for this IIN."
                    : "По этому ИИН в реестре КГД индивидуальный предприниматель не найден.",
                OfficialLinks = BuildOfficialLinks()
            };
        }

        using var openDataDocument = await TrySendPostJsonAsync(
            CounterpartyOpenDataApiPath,
            $$"""{"xin":"{{iin}}"}""",
            cancellationToken);
        using var riskDegreeDocument = await TrySendGetJsonAsync(
            RiskDegreeApiPath,
            new Dictionary<string, string?>
            {
                ["taxpayerCode"] = iin,
                ["print"] = "false"
            },
            cancellationToken);
        using var specialTaxModeDocument = await TrySendGetJsonAsync(
            SpecialTaxModeApiPath,
            new Dictionary<string, string?>
            {
                ["uin"] = iin
            },
            cancellationToken);

        var profile = BuildLiveProfile(iin, taxpayerResult.Value, openDataDocument, riskDegreeDocument, specialTaxModeDocument);
        var insights = BuildInsights(profile);

        return new IndividualEntrepreneurSearchResponse
        {
            Mode = "live",
            Found = true,
            SearchedIin = iin,
            SourceName = IsEnglishRequested() ? "KGD registry" : "Реестр КГД",
            Registry = profile,
            Insights = insights,
            ReportForms = EntrepreneurTaxFormCatalog.BuildForms(profile, IsEnglishRequested()),
            OfficialLinks = BuildOfficialLinks()
        };
    }

    private IndividualEntrepreneurRegistryProfileResponse BuildLiveProfile(
        string iin,
        (string Name, DateTime? BeginDate, DateTime? EndDate, string EndReason) taxpayerResult,
        JsonDocument? openDataDocument,
        JsonDocument? riskDegreeDocument,
        JsonDocument? specialTaxModeDocument)
    {
        var profile = new IndividualEntrepreneurRegistryProfileResponse
        {
            Iin = iin,
            Name = taxpayerResult.Name,
            TaxpayerBeginDate = taxpayerResult.BeginDate,
            TaxpayerEndDate = taxpayerResult.EndDate,
            TaxpayerEndReason = taxpayerResult.EndReason
        };

        if (openDataDocument is not null)
        {
            var root = openDataDocument.RootElement;
            profile.ActualityDate = ParseDate(root, "actuality");
            profile.Name = GetLocalizedNestedValue(root, "name") ?? profile.Name;
            profile.RegistrationDate = ParseDate(root, "regDate");
            profile.Residency = GetLocalizedNestedValue(root, "residency") ?? string.Empty;
            profile.Oked = GetLocalizedNestedValue(root, "oked") ?? string.Empty;
            profile.OkedName = GetLocalizedNestedValue(root, "okedName") ?? string.Empty;
            profile.OkedDate = ParseDate(root, "okedDate");
            profile.VatInfo = GetLocalizedNestedValue(root, "vatInfo") ?? string.Empty;
            profile.VatDate = ParseDate(root, "vatDate");
            profile.TaxMode = GetLocalizedNestedValue(root, "taxMode") ?? string.Empty;
            profile.TaxModeDate = ParseDate(root, "taxModeDate");
            profile.TaxDebt = ParseDecimal(root, "taxDebt");
            profile.TaxDebt1 = ParseDecimal(root, "taxDebt1");
            profile.TaxDebt2 = ParseDecimal(root, "taxDebt2");
            profile.TaxDebt3 = ParseDecimal(root, "taxDebt3");
            profile.TaxDebt4 = ParseDecimal(root, "taxDebt4");
            profile.Flags = BuildFlags(root);
            profile.Statistics = BuildStatistics(root);
        }

        if (riskDegreeDocument is not null)
        {
            var root = riskDegreeDocument.RootElement;
            profile.RiskDegree = GetString(root, "degree");
            profile.RiskRelevanceDate = ParseDateTimeOffset(root, "relevanceDate")?.UtcDateTime;
            profile.RiskBeginDate = ParseDateTimeOffset(root, "beginDate")?.UtcDateTime;
            profile.RiskEndDate = ParseDateTimeOffset(root, "endDate")?.UtcDateTime;
            if (string.IsNullOrWhiteSpace(profile.Name))
            {
                profile.Name = GetString(root, "name");
            }
        }

        profile.SpecialTaxModes = BuildSpecialTaxModes(specialTaxModeDocument?.RootElement);
        if (string.IsNullOrWhiteSpace(profile.TaxMode))
        {
            profile.TaxMode = profile.SpecialTaxModes.FirstOrDefault()?.TypeName ?? string.Empty;
            profile.TaxModeDate = profile.SpecialTaxModes.FirstOrDefault()?.BeginDate;
        }

        return profile;
    }

    private List<EntrepreneurRegistryFlagResponse> BuildFlags(JsonElement root)
    {
        var knownFlags = new[]
        {
            ("registrationInvalid", IsEnglishRequested() ? "Registration invalid" : "Недействительная регистрация"),
            ("reRegistrationInvalid", IsEnglishRequested() ? "Re-registration invalid" : "Недействительная перерегистрация"),
            ("operationsWOWork", IsEnglishRequested() ? "Operations without actual work" : "Операции без фактического выполнения работ"),
            ("esfRestrinctions", IsEnglishRequested() ? "ESF restrictions" : "Ограничения по ЭСФ"),
            ("inactive", IsEnglishRequested() ? "Inactive taxpayer" : "Бездействующий налогоплательщик"),
            ("regAddressAbsent", IsEnglishRequested() ? "Absent at registration address" : "Отсутствует по адресу регистрации"),
            ("bankrupt", IsEnglishRequested() ? "Bankrupt" : "Банкрот"),
            ("selfRegulatoryRegistry", IsEnglishRequested() ? "Self-regulatory registry" : "Реестр саморегулируемых организаций"),
            ("courtDecisionRegistry", IsEnglishRequested() ? "Court decision registry" : "Судебный реестр")
        };

        var flags = new List<EntrepreneurRegistryFlagResponse>();
        foreach (var (code, label) in knownFlags)
        {
            var value = GetLocalizedNestedValue(root, code);
            if (string.IsNullOrWhiteSpace(value) ||
                value.Contains("Нет данных", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("No data", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            flags.Add(new EntrepreneurRegistryFlagResponse
            {
                Code = code,
                Label = label,
                Value = value
            });
        }

        return flags;
    }

    private List<EntrepreneurStatisticResponse> BuildStatistics(JsonElement root)
    {
        if (!root.TryGetProperty("statistics", out var statisticsElement) ||
            statisticsElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return statisticsElement
            .EnumerateArray()
            .Select(item => new EntrepreneurStatisticResponse
            {
                Year = item.TryGetProperty("year", out var yearElement) && yearElement.TryGetInt32(out var year)
                    ? year
                    : 0,
                WorkersCount = ParseDecimal(item, "workersCount"),
                TaxIn = ParseDecimal(item, "taxIn"),
                Knn = ParseDecimal(item, "knn"),
                KnnAverage = ParseDecimal(item, "knnAvg"),
                VatAmount = ParseDecimal(item, "vatAmount")
            })
            .Where(item => item.Year > 0)
            .OrderBy(item => item.Year)
            .ToList();
    }

    private List<EntrepreneurSpecialTaxModeResponse> BuildSpecialTaxModes(JsonElement? rootElement)
    {
        if (rootElement is null ||
            !rootElement.Value.TryGetProperty("stmList", out var stmListElement) ||
            stmListElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return stmListElement
            .EnumerateArray()
            .Select(item => new EntrepreneurSpecialTaxModeResponse
            {
                Type = GetString(item, "type"),
                TypeName = GetString(item, IsEnglishRequested() ? "typeNameEn" : "typeNameRu"),
                BeginDate = ParseDate(item, "beginDate"),
                EndDate = ParseDate(item, "endDate")
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.TypeName))
            .OrderByDescending(item => item.BeginDate)
            .ToList();
    }

    private List<InsightResponse> BuildInsights(IndividualEntrepreneurRegistryProfileResponse profile)
    {
        var latestStats = profile.Statistics.OrderByDescending(item => item.Year).FirstOrDefault();
        var insights = new List<InsightResponse>
        {
            new()
            {
                Title = IsEnglishRequested() ? "Tax regime" : "Налоговый режим",
                Description = string.IsNullOrWhiteSpace(profile.TaxMode)
                    ? (IsEnglishRequested() ? "The registry did not return an explicit tax mode." : "Реестр не вернул явное значение налогового режима.")
                    : profile.TaxMode,
                Tone = "leader"
            },
            new()
            {
                Title = IsEnglishRequested() ? "Main business activity" : "Основной вид деятельности",
                Description = string.IsNullOrWhiteSpace(profile.OkedName)
                    ? (IsEnglishRequested() ? "OKED information is not available." : "Данные по ОКЭД отсутствуют.")
                    : $"{profile.Oked} {profile.OkedName}".Trim(),
                Tone = "signal"
            }
        };

        if (latestStats is not null)
        {
            insights.Add(new InsightResponse
            {
                Title = IsEnglishRequested() ? "Latest registry statistics" : "Последняя статистика из реестра",
                Description = IsEnglishRequested()
                    ? $"{latestStats.Year}: workers {latestStats.WorkersCount:N0}, taxes paid in {latestStats.TaxIn:N2}."
                    : $"{latestStats.Year}: работников {latestStats.WorkersCount:N0}, налогов поступило {latestStats.TaxIn:N2}.",
                Tone = latestStats.WorkersCount > 0 ? "trend" : "signal"
            });
        }

        insights.Add(new InsightResponse
        {
            Title = IsEnglishRequested() ? "Risk profile" : "Риск-профиль",
            Description = string.IsNullOrWhiteSpace(profile.RiskDegree)
                ? (IsEnglishRequested() ? "Risk degree data is not available in the registry response." : "Данные по степени риска в ответе реестра отсутствуют.")
                : profile.RiskDegree,
            Tone = profile.RiskDegree.Contains("high", StringComparison.OrdinalIgnoreCase) ||
                   profile.RiskDegree.Contains("выс", StringComparison.OrdinalIgnoreCase)
                ? "risk"
                : "trend"
        });

        if (profile.TaxDebt > 0)
        {
            insights.Add(new InsightResponse
            {
                Title = IsEnglishRequested() ? "Tax debt detected" : "Обнаружена налоговая задолженность",
                Description = IsEnglishRequested()
                    ? $"Current registry tax debt: {profile.TaxDebt:N2}."
                    : $"Текущая налоговая задолженность по реестру: {profile.TaxDebt:N2}.",
                Tone = "risk"
            });
        }
        else
        {
            insights.Add(new InsightResponse
            {
                Title = IsEnglishRequested() ? "Debt status" : "Статус задолженности",
                Description = IsEnglishRequested()
                    ? "The registry reports no tax debt at the moment."
                    : "По данным реестра налоговая задолженность не обнаружена.",
                Tone = "leader"
            });
        }

        return insights;
    }

    private IndividualEntrepreneurSearchResponse BuildDemoResponse(string iin)
    {
        var isEnglish = IsEnglishRequested();
        var normalizedIin = iin.Trim();
        var registry = normalizedIin switch
        {
            "123456789876" => BuildSimplifiedDemoProfile(normalizedIin),
            "444444444444" => BuildGeneralDemoProfile(normalizedIin),
            _ => null
        };

        if (registry is null)
        {
            return new IndividualEntrepreneurSearchResponse
            {
                Mode = "demo",
                Found = false,
                SearchedIin = normalizedIin,
                SourceName = isEnglish ? "KGD registry demo" : "Демо-режим реестра КГД",
                Message = isEnglish
                    ? "No entrepreneur was found in demo mode. Try 123456789876 or 444444444444."
                    : "В demo-режиме ИП не найден. Попробуйте ИИН 123456789876 или 444444444444.",
                OfficialLinks = BuildOfficialLinks()
            };
        }

        return new IndividualEntrepreneurSearchResponse
        {
            Mode = "demo",
            Found = true,
            SearchedIin = normalizedIin,
            SourceName = isEnglish ? "KGD registry demo" : "Демо-режим реестра КГД",
            Registry = registry,
            Insights = BuildInsights(registry),
            ReportForms = EntrepreneurTaxFormCatalog.BuildForms(registry, isEnglish),
            OfficialLinks = BuildOfficialLinks()
        };
    }

    private IndividualEntrepreneurRegistryProfileResponse BuildSimplifiedDemoProfile(string iin)
    {
        var isEnglish = IsEnglishRequested();

        return new IndividualEntrepreneurRegistryProfileResponse
        {
            Iin = iin,
            Name = isEnglish ? "Aruzhan Saparova" : "Аружан Сапарова",
            TaxpayerBeginDate = new DateTime(2022, 9, 14, 0, 0, 0, DateTimeKind.Utc),
            ActualityDate = new DateTime(2026, 1, 21, 0, 0, 0, DateTimeKind.Utc),
            RegistrationDate = new DateTime(2022, 9, 14, 0, 0, 0, DateTimeKind.Utc),
            Residency = isEnglish ? "Resident" : "Резидент",
            Oked = "47911",
            OkedName = isEnglish ? "Retail sale via mail order houses or via Internet" : "Розничная торговля через интернет-магазины",
            OkedDate = new DateTime(2022, 9, 14, 0, 0, 0, DateTimeKind.Utc),
            TaxMode = isEnglish ? "Special tax treatment based on a simplified declaration" : "Специальный налоговый режим на основе упрощенной декларации",
            TaxModeDate = new DateTime(2022, 9, 14, 0, 0, 0, DateTimeKind.Utc),
            VatInfo = isEnglish ? "No data" : "Нет данных",
            VatDate = null,
            RiskDegree = isEnglish ? "Low" : "Низкая",
            RiskRelevanceDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            RiskBeginDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            RiskEndDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            Flags =
            [
                new EntrepreneurRegistryFlagResponse
                {
                    Code = "regAddressAbsent",
                    Label = isEnglish ? "Absent at registration address" : "Отсутствует по адресу регистрации",
                    Value = isEnglish ? "No" : "Нет"
                }
            ],
            Statistics =
            [
                new EntrepreneurStatisticResponse { Year = 2022, WorkersCount = 0, TaxIn = 320000, Knn = 1.4m, KnnAverage = 1.7m, VatAmount = 0 },
                new EntrepreneurStatisticResponse { Year = 2023, WorkersCount = 1, TaxIn = 680000, Knn = 1.9m, KnnAverage = 2.1m, VatAmount = 0 },
                new EntrepreneurStatisticResponse { Year = 2024, WorkersCount = 2, TaxIn = 910000, Knn = 2.2m, KnnAverage = 2.3m, VatAmount = 0 },
                new EntrepreneurStatisticResponse { Year = 2025, WorkersCount = 2, TaxIn = 1120000, Knn = 2.5m, KnnAverage = 2.4m, VatAmount = 0 }
            ],
            SpecialTaxModes =
            [
                new EntrepreneurSpecialTaxModeResponse
                {
                    Type = "SNR_SIMPLIFIED_DECLARATION",
                    TypeName = isEnglish ? "Special tax treatment based on a simplified declaration" : "Специальный налоговый режим на основе упрощенной декларации",
                    BeginDate = new DateTime(2022, 9, 14, 0, 0, 0, DateTimeKind.Utc)
                }
            ]
        };
    }

    private IndividualEntrepreneurRegistryProfileResponse BuildGeneralDemoProfile(string iin)
    {
        var isEnglish = IsEnglishRequested();

        return new IndividualEntrepreneurRegistryProfileResponse
        {
            Iin = iin,
            Name = isEnglish ? "Daniyar Kenzhebekov" : "Данияр Кенжебеков",
            TaxpayerBeginDate = new DateTime(2021, 4, 11, 0, 0, 0, DateTimeKind.Utc),
            ActualityDate = new DateTime(2026, 2, 18, 0, 0, 0, DateTimeKind.Utc),
            RegistrationDate = new DateTime(2021, 4, 11, 0, 0, 0, DateTimeKind.Utc),
            Residency = isEnglish ? "Resident" : "Резидент",
            Oked = "62011",
            OkedName = isEnglish ? "Software development" : "Разработка программного обеспечения",
            OkedDate = new DateTime(2021, 4, 11, 0, 0, 0, DateTimeKind.Utc),
            TaxMode = isEnglish ? "General tax treatment" : "Общеустановленный режим",
            TaxModeDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            VatInfo = isEnglish ? "VAT payer" : "Плательщик НДС",
            VatDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            RiskDegree = isEnglish ? "Medium" : "Средняя",
            RiskRelevanceDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            RiskBeginDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            RiskEndDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            TaxDebt = 0,
            Statistics =
            [
                new EntrepreneurStatisticResponse { Year = 2022, WorkersCount = 3, TaxIn = 2450000, Knn = 2.2m, KnnAverage = 2.0m, VatAmount = 180000 },
                new EntrepreneurStatisticResponse { Year = 2023, WorkersCount = 4, TaxIn = 2980000, Knn = 2.6m, KnnAverage = 2.2m, VatAmount = 245000 },
                new EntrepreneurStatisticResponse { Year = 2024, WorkersCount = 5, TaxIn = 3620000, Knn = 2.9m, KnnAverage = 2.4m, VatAmount = 318000 },
                new EntrepreneurStatisticResponse { Year = 2025, WorkersCount = 6, TaxIn = 4110000, Knn = 3.1m, KnnAverage = 2.6m, VatAmount = 364000 }
            ],
            SpecialTaxModes = []
        };
    }

    private List<EntrepreneurLinkResponse> BuildOfficialLinks()
    {
        return
        [
            new EntrepreneurLinkResponse
            {
                Label = IsEnglishRequested() ? "Taxpayer search API" : "API поиска налогоплательщика",
                Url = "https://portal.kgd.gov.kz/ru/pages/info-services/find-taxpayer/_/attachment/download/591204a8-1450-4824-afc1-8298245e6f6c%3A7e918a95b376cd46c2af2f60a387d81996b03f47/ipn_ru%20%281%29.pdf"
            },
            new EntrepreneurLinkResponse
            {
                Label = IsEnglishRequested() ? "Counterparty open data API" : "API сведений по контрагентам",
                Url = "https://portal.kgd.gov.kz/ru/pages/info-services/find-information-for-ip-ul/_/attachment/download/43b4affc-62cc-4614-aab2-9a8750636333%3Ac4710ca462e7e98688f6fc6a82e5c34d99280ba3/API_%D1%81%D0%B5%D1%80%D0%B2%D0%B8%D1%81%D0%B0_%C2%ABApi_c%D0%B2%D0%B5%D0%B4%D0%B5%D0%BD%D0%B8%D1%8F_%D0%BF%D0%BE_%D0%BA%D0%BE%D0%BD%D1%82%D1%80%D0%B0%D0%B3%D0%B5%D0%BD%D1%82%D0%B0%D0%BC_%D0%9E%D1%82%D0%BA%D1%80%D1%8B%D1%82%D1%8B%D0%B5_%D0%B4%D0%B0%D0%BD%D0%BD%D1%8B%D0%B5%C2%BB.pdf"
            },
            new EntrepreneurLinkResponse
            {
                Label = IsEnglishRequested() ? "Special tax modes API" : "API налоговых режимов",
                Url = "https://portal.kgd.gov.kz/ru/pages/api-services/snr/_/attachment/download/de2468fc-45fa-4ce4-aee5-50113d14e395%3A30aa476b6a8a1ced702e79607f937acf71e76628/API%20%D1%81%D0%B5%D1%80%D0%B2%D0%B8%D1%81%D0%B0%20%D0%A1%D0%9D%D0%A0.pdf"
            },
            new EntrepreneurLinkResponse
            {
                Label = IsEnglishRequested() ? "Risk degree API" : "API степени риска",
                Url = "https://portal.kgd.gov.kz/pages/info-services/find-risk-degree/_/attachment/download/81e04b6c-517e-4353-a399-a49ee855694f%3Ad232905e478c74fcdab46f0e61ee9ab7c4a8a80c/rd_ru.pdf"
            }
        ];
    }

    private (string Name, DateTime? BeginDate, DateTime? EndDate, string EndReason)? ExtractTaxpayerSearch(JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("taxpayerPortalSearchResponses", out var responsesElement) ||
            responsesElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var first = responsesElement
            .EnumerateArray()
            .FirstOrDefault(item =>
                string.Equals(GetString(item, "taxpayerType"), "IP", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(GetString(item, "messageResult"), "SUCCESS", StringComparison.OrdinalIgnoreCase));

        if (first.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return (
            Name: GetString(first, "name"),
            BeginDate: ParseDate(first, "beginDate"),
            EndDate: ParseDate(first, "endDate"),
            EndReason: first.TryGetProperty("endReason", out var endReasonElement)
                ? GetLocalizedValue(endReasonElement) ?? string.Empty
                : string.Empty
        );
    }

    private async Task<JsonDocument> SendGetJsonAsync(
        string path,
        IDictionary<string, string?> query,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.SendAsync(BuildGetRequest(path, query), cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private async Task<JsonDocument?> TrySendGetJsonAsync(
        string path,
        IDictionary<string, string?> query,
        CancellationToken cancellationToken)
    {
        try
        {
            return await SendGetJsonAsync(path, query, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private async Task<JsonDocument?> TrySendPostJsonAsync(
        string path,
        string jsonPayload,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.SendAsync(BuildPostRequest(path, jsonPayload), cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private HttpRequestMessage BuildGetRequest(string path, IDictionary<string, string?> query)
    {
        var url = QueryHelpers.AddQueryString(BuildAbsoluteUrl(path), query);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("X-Portal-Token", _options.PortalToken);
        return request;
    }

    private HttpRequestMessage BuildPostRequest(string path, string jsonPayload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, BuildAbsoluteUrl(path));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("X-Portal-Token", _options.PortalToken);
        request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        return request;
    }

    private string BuildAbsoluteUrl(string path)
    {
        return $"{_options.BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }

    private bool IsEnglishRequested()
    {
        var acceptLanguage = _httpContextAccessor.HttpContext?.Request.Headers.AcceptLanguage.ToString();
        if (string.IsNullOrWhiteSpace(acceptLanguage))
        {
            return false;
        }

        var firstLanguage = acceptLanguage
            .Split(',', ';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        return firstLanguage?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return string.Empty;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString() ?? string.Empty,
            JsonValueKind.Number => property.ToString(),
            _ => string.Empty
        };
    }

    private string? GetLocalizedNestedValue(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return GetLocalizedValue(property);
    }

    private string? GetLocalizedValue(JsonElement property)
    {
        if (property.ValueKind == JsonValueKind.String)
        {
            return property.GetString();
        }

        if (property.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var preferredKey = IsEnglishRequested() ? "en" : "ru";
        if (property.TryGetProperty(preferredKey, out var preferredElement) &&
            preferredElement.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(preferredElement.GetString()))
        {
            return preferredElement.GetString();
        }

        foreach (var key in new[] { "ru", "en", "kk" })
        {
            if (property.TryGetProperty(key, out var localizedElement) &&
                localizedElement.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(localizedElement.GetString()))
            {
                return localizedElement.GetString();
            }
        }

        return null;
    }

    private static DateTime? ParseDate(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        var raw = property.GetString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }

        return null;
    }

    private static DateTimeOffset? ParseDateTimeOffset(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        var raw = property.GetString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : null;
    }

    private static decimal ParseDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return 0m;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var decimalValue))
        {
            return decimalValue;
        }

        var raw = property.GetString()?.Replace("%", string.Empty, StringComparison.OrdinalIgnoreCase);
        return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out decimalValue)
            ? decimalValue
            : 0m;
    }
}
