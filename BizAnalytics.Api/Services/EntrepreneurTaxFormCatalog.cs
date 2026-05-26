using BizAnalytics.Api.Contracts.Entrepreneurs;

namespace BizAnalytics.Api.Services;

public static class EntrepreneurTaxFormCatalog
{
    public const string OfficialIncomeFormsUrl = "https://portal.kgd.gov.kz/ru/pages/page-kgd/FNO%202026/%D0%A4%D0%9D%D0%9E%202026-10";
    public const string Official910PdfUrl = "https://portal.kgd.gov.kz/pages/taxonomy/content_tax_324/12901/_/attachment/inline/be6b025c-5db1-49f5-92f3-5f01f4e50015%3A46da50bf2065647832bef240e50c27ceadffbe6a/910.00.pdf";
    public const string Official200TemplateUrl = "https://portal.kgd.gov.kz/ru/pages/page-kgd/FNO%202026/%D0%A4%D0%9D%D0%9E%202026-10/_/attachment/download/31e937d9-e030-4483-b454-abf4f52b8be1:a945e56939be9a32ea475c856fce375dd14c73d7/200.00_2026_%D1%80%D1%83%D1%81_pdf-6832146015813231401.rar";
    public const string Official220TemplateUrl = "https://portal.kgd.gov.kz/ru/pages/page-kgd/FNO%202026/%D0%A4%D0%9D%D0%9E%202026-10/_/attachment/download/cce0a14e-95e1-48ef-bc03-aaf1eb3fcb3e:b51c18c42d9d2c236760187b02fe1b7eb6c58a2c/220.00_2026_%D1%80%D1%83%D1%81_pdf-4832710994941587275.rar";
    public const string Official910TemplateUrl = "https://portal.kgd.gov.kz/ru/pages/taxonomy/content_tax_730/78751/_/attachment/download/2f8237bc-4f0e-4ebb-aab6-0ebcd144d96b:6d0018cb8eb0fa13ae9d6f8fc9d018fa8c6a5595/910.00_na_2021_rus_pdf.rar";

    public static List<EntrepreneurReportFormResponse> BuildForms(
        IndividualEntrepreneurRegistryProfileResponse profile,
        bool isEnglish)
    {
        var forms = new List<EntrepreneurReportFormResponse>();
        var latestWorkers = profile.Statistics
            .OrderByDescending(item => item.Year)
            .FirstOrDefault()
            ?.WorkersCount ?? 0m;
        var taxMode = profile.TaxMode.ToLowerInvariant();
        var hasSimplifiedMode = taxMode.Contains("упрощ", StringComparison.OrdinalIgnoreCase) ||
                                taxMode.Contains("simplified", StringComparison.OrdinalIgnoreCase) ||
                                profile.SpecialTaxModes.Any(item => item.Type.Contains("SIMPLIFIED", StringComparison.OrdinalIgnoreCase));
        var hasGeneralMode = !hasSimplifiedMode;
        var hasEmployees = latestWorkers > 0m;

        if (hasSimplifiedMode)
        {
            forms.Add(new EntrepreneurReportFormResponse
            {
                FormCode = "910.00",
                Title = isEnglish ? "Form 910.00 simplified declaration" : "Форма 910.00 Упрощенная декларация",
                Description = isEnglish
                    ? "Semiannual simplified declaration for small business entities using the simplified tax treatment."
                    : "Полугодовая упрощенная декларация для субъектов малого бизнеса на специальном налоговом режиме на основе упрощенной декларации.",
                FilingPeriodicity = isEnglish ? "Twice a year" : "2 раза в год",
                FilingDeadline = isEnglish ? "By August 15 and February 15" : "До 15 августа и до 15 февраля",
                PaymentDeadline = isEnglish ? "By August 25 and February 25" : "До 25 августа и до 25 февраля",
                Applicability = isEnglish
                    ? "Recommended because the registry shows a simplified declaration regime."
                    : "Рекомендуется, так как в реестре указан режим на основе упрощенной декларации.",
                IsRecommended = true,
                Sections =
                [
                    isEnglish ? "General taxpayer information" : "Общая информация о налогоплательщике",
                    isEnglish ? "Income and tax calculation" : "Доход и исчисление налогов",
                    isEnglish ? "Social contributions and pension contributions" : "Социальные отчисления и пенсионные взносы",
                    isEnglish ? "Worker payroll taxes (if employees exist)" : "Налоги и отчисления по работникам (если есть сотрудники)"
                ],
                HighlightFields =
                [
                    "910.00.001",
                    "910.00.003",
                    "910.00.005",
                    "910.00.007",
                    "910.00.008",
                    "910.00.009",
                    "910.00.010",
                    "910.00.011",
                    "910.00.012",
                    "910.00.013",
                    "910.00.014",
                    "910.00.015",
                    "910.00.016",
                    "910.00.017",
                    "910.00.018",
                    "910.00.019",
                    "910.00.020",
                    "910.00.021"
                ],
                OfficialSourceUrl = Official910TemplateUrl
            });
        }

        if (hasGeneralMode)
        {
            forms.Add(new EntrepreneurReportFormResponse
            {
                FormCode = "220.00",
                Title = isEnglish ? "Form 220.00 annual individual income tax declaration" : "Форма 220.00 Декларация по индивидуальному подоходному налогу",
                Description = isEnglish
                    ? "Annual declaration for entrepreneurs on the general tax treatment. It reflects annual income, deductible expenses, taxable income and the final 10% tax calculation."
                    : "Годовая декларация для ИП на общеустановленном режиме. Отражает доход за год, вычеты и расходы, налогооблагаемый доход и итоговый ИПН 10%.",
                FilingPeriodicity = isEnglish ? "Once a year" : "1 раз в год",
                FilingDeadline = isEnglish ? "By March 31 of the following year" : "До 31 марта следующего года",
                PaymentDeadline = isEnglish ? "No later than April 10" : "Не позднее 10 апреля",
                Applicability = isEnglish
                    ? "Recommended because the registry does not show a simplified declaration regime."
                    : "Рекомендуется, если ИП работает не на упрощенной декларации, а на общеустановленном режиме.",
                IsRecommended = true,
                Sections =
                [
                    isEnglish ? "Taxpayer details and reporting year" : "Реквизиты налогоплательщика и отчетный год",
                    isEnglish ? "Annual business income" : "Годовой доход от предпринимательской деятельности",
                    isEnglish ? "Deductible expenses and adjustments" : "Расходы, вычеты и корректировки",
                    isEnglish ? "Taxable income and 10% individual income tax" : "Налогооблагаемый доход и ИПН 10%",
                    isEnglish ? "Advance and final tax amounts" : "Авансовые и итоговые суммы налога"
                ],
                HighlightFields =
                [
                    isEnglish ? "Annual gross income" : "Годовой валовый доход",
                    isEnglish ? "Documented deductible expenses" : "Подтвержденные вычеты и расходы",
                    isEnglish ? "Taxable income base" : "Налоговая база",
                    isEnglish ? "10% individual income tax" : "ИПН 10%"
                ],
                OfficialSourceUrl = Official220TemplateUrl
            });
        }

        if (hasGeneralMode && hasEmployees)
        {
            forms.Add(new EntrepreneurReportFormResponse
            {
                FormCode = "200.00",
                Title = isEnglish ? "Form 200.00 payroll tax declaration" : "Форма 200.00 Декларация по ИПН и социальному налогу",
                Description = isEnglish
                    ? "Quarterly declaration for entrepreneurs with employees. It covers payroll individual income tax, social tax and related social payments."
                    : "Ежеквартальная декларация для ИП с работниками. Отражает начисленные доходы работников, удержанный ИПН, социальный налог и связанные социальные платежи.",
                FilingPeriodicity = isEnglish ? "Quarterly" : "Ежеквартально",
                FilingDeadline = isEnglish ? "By the 15th day of the second month after each quarter" : "До 15 числа второго месяца после отчетного квартала",
                PaymentDeadline = isEnglish ? "By the 25th day after the payroll month" : "Как правило, до 25 числа месяца, следующего за месяцем выплаты дохода",
                Applicability = isEnglish
                    ? "Shown because the latest registry statistics indicate employees."
                    : "Показывается, потому что в последней статистике реестра есть работники.",
                IsRecommended = true,
                Sections =
                [
                    isEnglish ? "Tax agent details and quarter" : "Реквизиты налогового агента и квартал",
                    isEnglish ? "Payroll income of employees" : "Начисленные доходы работников",
                    isEnglish ? "Withheld individual income tax" : "Удержанный индивидуальный подоходный налог",
                    isEnglish ? "Social tax and social payments" : "Социальный налог и социальные платежи",
                    isEnglish ? "Pension contributions and social contributions" : "Пенсионные взносы и социальные отчисления"
                ],
                HighlightFields =
                [
                    isEnglish ? "Employee taxable payroll" : "Облагаемый фонд оплаты труда",
                    isEnglish ? "IIT withheld from employees" : "Удержанный ИПН",
                    isEnglish ? "Social tax payable" : "Социальный налог к уплате",
                    isEnglish ? "Pension and social contributions" : "ОПВ и социальные отчисления"
                ],
                OfficialSourceUrl = Official200TemplateUrl
            });
        }

        return forms;
    }

    public static EntrepreneurReportFormResponse? FindForm(
        string formCode,
        IndividualEntrepreneurRegistryProfileResponse profile,
        bool isEnglish)
    {
        return BuildForms(profile, isEnglish)
            .FirstOrDefault(item => string.Equals(item.FormCode, formCode, StringComparison.OrdinalIgnoreCase));
    }
}
