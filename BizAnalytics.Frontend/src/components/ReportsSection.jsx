import { Chip, EmptyState, Field, GuideOverlay, InlineNotice, MetricCard, Panel } from "./Ui";
import { formatDateValue, formatNumberValue } from "../localization";
import { getMetricDescription, getSectionGuide } from "../analyticsContent";

function hasSalesData(analysis) {
  return Boolean(
    analysis?.revenue?.length ||
      analysis?.topProducts?.length ||
      analysis?.summary?.totalSalesCount > 0
  );
}

function hasFinancialData(analysis) {
  return Boolean(analysis?.financial?.periods?.length);
}

function hasEducationData(analysis) {
  return Boolean(
    analysis?.education?.gradeDistribution?.length ||
      analysis?.education?.studentPerformance?.length ||
      analysis?.education?.subjectPerformance?.length
  );
}

function buildReportSummaryItems(analysis, language) {
  const isEnglish = language === "en";
  const items = [];

  if (hasSalesData(analysis)) {
    items.push(
      isEnglish
        ? "Sales section: key metrics, revenue by date, top products, source comparison, price dynamics and conclusions."
        : "Раздел по продажам: ключевые метрики, выручка по датам, топ товаров, сопоставление источников, динамика цен и выводы."
    );
  }

  if (hasFinancialData(analysis)) {
    items.push(
      isEnglish
        ? "Financial section: revenue, expenses, profit, profitability, period trend, forecast block and financial conclusions."
        : "Финансовый раздел: доходы, расходы, прибыль, рентабельность, тренд по периодам, прогнозный блок и финансовые выводы."
    );
  }

  if (hasEducationData(analysis)) {
    items.push(
      isEnglish
        ? "Education section: average score, success rate, grade distribution, ratings, forecast and students needing attention."
        : "Раздел по успеваемости: средний балл, процент успеваемости, распределение оценок, рейтинги, прогноз и ученики, требующие внимания."
    );
  }

  if (items.length > 1) {
    items.unshift(
      isEnglish
        ? "Mixed report: all detected analytics sections will be added to the PDF as separate structured blocks."
        : "Смешанный отчет: все найденные разделы аналитики будут добавлены в PDF как отдельные структурированные блоки."
    );
  }

  if (items.length === 0) {
    items.push(
      isEnglish
        ? "Run the analysis first. After that, the report language and structure will adapt automatically."
        : "Сначала выполните анализ. После этого язык и структура отчета подстроятся автоматически."
    );
  }

  return items;
}

function buildReportDescription(analysis, language, fallback) {
  if (!analysis) {
    return fallback;
  }

  const sections = [];

  if (hasSalesData(analysis)) {
    sections.push(language === "en" ? "sales" : "продаж");
  }

  if (hasFinancialData(analysis)) {
    sections.push(language === "en" ? "financial" : "финансовой");
  }

  if (hasEducationData(analysis)) {
    sections.push(language === "en" ? "education" : "аналитики успеваемости");
  }

  if (sections.length === 0) {
    return fallback;
  }

  return language === "en"
    ? `The PDF will be generated in the selected language and will include structured analytics sections: ${sections.join(", ")}.`
    : `PDF будет сформирован на выбранном языке и включит структурированные разделы по аналитике: ${sections.join(", ")}.`;
}

function getEntrepreneurReportsCopy(language) {
  if (language === "en") {
    return {
      eyebrow: "IE reports",
      title: "Tax report forms for the found entrepreneur",
      description: "After a successful IIN lookup, the platform unlocks IP-specific report forms based on the tax mode from the RK registry and prefills the available official fields from the KGD analytics data.",
      registrationDate: "Registration date",
      taxMode: "Tax mode",
      risk: "Risk degree",
      workers: "Workers",
      filing: "Filing deadline",
      payment: "Payment deadline",
      recommended: "Recommended",
      officialSource: "Official source",
      download: "Download official form with analytics data"
    };
  }

  return {
    eyebrow: "Отчеты ИП",
    title: "Формы налоговой отчетности для найденного ИП",
    description: "После успешного поиска по ИИН платформа открывает формы отчетности именно для ИП, подобранные по налоговому режиму из реестра РК.",
    registrationDate: "Дата регистрации",
    taxMode: "Налоговый режим",
    risk: "Степень риска",
    workers: "Работники",
    filing: "Срок сдачи",
    payment: "Срок уплаты",
    recommended: "Рекомендуется",
    officialSource: "Официальный источник",
    download: "Скачать автозаполненный официальный бланк"
  };
}

function ReportsSection({
  copy,
  language,
  analysis,
  analysisWorkspaces,
  selectedAnalysisId,
  selectedAnalysis,
  selectedOrganization,
  busyReport,
  onSelectAnalysis,
  onDownloadReport,
  onDownloadEntrepreneurForm,
  reportNotice,
  showGuide,
  onDismissGuide,
  individualEntrepreneurSearch
}) {
  const summary = analysis?.summary ?? {
    totalRevenue: 0,
    totalSalesCount: 0,
    totalQuantity: 0,
    averageCheck: 0
  };
  const financial = analysis?.financial ?? null;
  const education = analysis?.education ?? null;
  const hasAnyData = Boolean(
    selectedOrganization &&
      selectedAnalysis &&
      (hasSalesData(analysis) || hasFinancialData(analysis) || hasEducationData(analysis))
  );
  const reportSummaryItems = buildReportSummaryItems(analysis, language);
  const reportDescription = buildReportDescription(
    analysis,
    language,
    copy.reports.reportDescription
  );
  const guide = showGuide ? getSectionGuide("reports", language, analysis) : null;
  const entrepreneurCopy = getEntrepreneurReportsCopy(language);
  const entrepreneurRegistry = individualEntrepreneurSearch?.found
    ? individualEntrepreneurSearch.registry ?? null
    : null;
  const entrepreneurForms = individualEntrepreneurSearch?.found
    ? individualEntrepreneurSearch.reportForms ?? []
    : [];
  const entrepreneurLatestStatistics = entrepreneurRegistry?.statistics?.length
    ? entrepreneurRegistry.statistics[entrepreneurRegistry.statistics.length - 1]
    : null;

  return (
    <div className="section-stack">
      <GuideOverlay guide={guide} onDismiss={onDismissGuide} />

      <section className="section-caption">
        <div>
          <h1>{copy.reports.title}</h1>
          <p>{copy.reports.subtitle}</p>
        </div>
      </section>

      <Panel
        className={showGuide ? "onboarding-focus-card" : ""}
        eyebrow={copy.reports.reportEyebrow}
        title={copy.reports.reportTitle}
        description={reportDescription}
      >
        <div className="report-toolbar">
          <Field
            label={copy.reports.analysisSelectLabel}
            value={selectedAnalysisId}
            onChange={onSelectAnalysis}
            as="select"
            options={analysisWorkspaces.map((workspace) => ({
              value: workspace.id,
              label: workspace.name
            }))}
            disabled={!analysisWorkspaces.length}
          />
          <button
            className="primary-button"
            disabled={!hasAnyData || busyReport}
            onClick={onDownloadReport}
            type="button"
          >
            {busyReport ? copy.reports.generating : copy.reports.generateAction}
          </button>
        </div>

        {selectedAnalysis ? (
          <div className="report-company-line">
            <Chip tone="success">{selectedOrganization?.name ?? "-"}</Chip>
            <Chip>{selectedAnalysis.name}</Chip>
          </div>
        ) : null}

        <InlineNotice notice={reportNotice} />

        {!hasAnyData ? (
          <EmptyState
            title={copy.reports.noReportTitle}
            body={copy.reports.noReportBody}
          />
        ) : (
          <section className="metrics-grid metrics-grid-compact">
            {hasSalesData(analysis) ? (
              <>
                <MetricCard
                  description={getMetricDescription("totalRevenue", language)}
                  label={copy.analytics.totalRevenue}
                  value={formatNumberValue(summary.totalRevenue, language)}
                />
                <MetricCard
                  accent="secondary"
                  description={getMetricDescription("salesCount", language)}
                  label={copy.analytics.salesCount}
                  value={formatNumberValue(summary.totalSalesCount, language, {
                    maximumFractionDigits: 0
                  })}
                />
                <MetricCard
                  accent="secondary"
                  description={getMetricDescription("totalQuantity", language)}
                  label={copy.analytics.totalQuantity}
                  value={formatNumberValue(summary.totalQuantity, language, {
                    maximumFractionDigits: 0
                  })}
                />
                <MetricCard
                  accent="accent"
                  description={getMetricDescription("averageCheck", language)}
                  label={copy.analytics.averageCheck}
                  value={formatNumberValue(summary.averageCheck, language)}
                />
              </>
            ) : null}

            {financial ? (
              <>
                <MetricCard
                  description={getMetricDescription("financialRevenue", language)}
                  label={language === "en" ? "Revenue" : "Доходы"}
                  value={formatNumberValue(financial.totalRevenue, language)}
                />
                <MetricCard
                  accent="secondary"
                  description={getMetricDescription("financialExpenses", language)}
                  label={language === "en" ? "Expenses" : "Расходы"}
                  value={formatNumberValue(financial.totalExpenses, language)}
                />
                <MetricCard
                  accent="secondary"
                  description={getMetricDescription("financialProfit", language)}
                  label={language === "en" ? "Profit" : "Прибыль"}
                  value={formatNumberValue(financial.totalProfit, language)}
                />
                <MetricCard
                  accent="accent"
                  description={getMetricDescription("profitability", language)}
                  label={language === "en" ? "Profitability" : "Рентабельность"}
                  value={`${formatNumberValue(financial.profitability, language)}%`}
                />
              </>
            ) : null}

            {education ? (
              <>
                <MetricCard
                  description={getMetricDescription("averageScore", language)}
                  label={language === "en" ? "Average score" : "Средний балл"}
                  value={formatNumberValue(education.averageScore, language)}
                />
                <MetricCard
                  accent="secondary"
                  description={getMetricDescription("bestStudent", language)}
                  label={language === "en" ? "Best student" : "Лучший ученик"}
                  value={education.bestStudent || "-"}
                />
                <MetricCard
                  accent="secondary"
                  description={getMetricDescription("worstStudent", language)}
                  label={language === "en" ? "Weakest student" : "Худший ученик"}
                  value={education.worstStudent || "-"}
                />
                <MetricCard
                  accent="accent"
                  description={getMetricDescription("successRate", language)}
                  label={language === "en" ? "Success rate" : "Процент успеваемости"}
                  value={`${formatNumberValue(education.successRate, language)}%`}
                />
              </>
            ) : null}
          </section>
        )}
      </Panel>

      {entrepreneurRegistry ? (
        <Panel
          eyebrow={entrepreneurCopy.eyebrow}
          title={entrepreneurCopy.title}
          description={entrepreneurCopy.description}
        >
          <div className="entrepreneur-summary-line">
            <Chip tone="success">{entrepreneurRegistry.name || entrepreneurRegistry.iin}</Chip>
            <Chip>{entrepreneurRegistry.iin}</Chip>
            <Chip tone="info">{entrepreneurRegistry.taxMode || "-"}</Chip>
          </div>

          <section className="metrics-grid metrics-grid-compact">
            <MetricCard
              label={entrepreneurCopy.registrationDate}
              value={entrepreneurRegistry.registrationDate ? formatDateValue(entrepreneurRegistry.registrationDate, language) : "-"}
            />
            <MetricCard
              accent="secondary"
              label={entrepreneurCopy.taxMode}
              value={entrepreneurRegistry.taxMode || "-"}
            />
            <MetricCard
              accent="secondary"
              label={entrepreneurCopy.risk}
              value={entrepreneurRegistry.riskDegree || "-"}
            />
            <MetricCard
              accent="accent"
              label={entrepreneurCopy.workers}
              value={formatNumberValue(entrepreneurLatestStatistics?.workersCount ?? 0, language, {
                maximumFractionDigits: 0
              })}
            />
          </section>

          {entrepreneurForms.length === 0 ? (
            <EmptyState
              title={entrepreneurCopy.title}
              body={language === "en" ? "No entrepreneur forms are available yet." : "Формы отчетности для ИП пока не определены."}
            />
          ) : (
            <div className="report-form-grid">
              {entrepreneurForms.map((form) => (
                <article className="report-form-card" key={form.formCode}>
                  <div className="report-form-card-head">
                    <strong>{form.formCode}</strong>
                    {form.isRecommended ? <Chip tone="success">{entrepreneurCopy.recommended}</Chip> : null}
                  </div>
                  <h3>{form.title}</h3>
                  <p>{form.description}</p>
                  <div className="report-form-meta">
                    <span>{entrepreneurCopy.filing}: {form.filingDeadline}</span>
                    <span>{entrepreneurCopy.payment}: {form.paymentDeadline}</span>
                  </div>
                  {form.sections?.length ? (
                    <ul className="report-form-sections">
                      {form.sections.map((section) => (
                        <li key={`${form.formCode}-${section}`}>{section}</li>
                      ))}
                    </ul>
                  ) : null}
                  <div className="report-form-actions">
                    <button
                      className="primary-button"
                      disabled={busyReport}
                      onClick={() => onDownloadEntrepreneurForm(form.formCode)}
                      type="button"
                    >
                      {busyReport ? copy.reports.generating : entrepreneurCopy.download}
                    </button>
                    <a className="report-form-link" href={form.officialSourceUrl} rel="noreferrer" target="_blank">
                      {entrepreneurCopy.officialSource}
                    </a>
                  </div>
                </article>
              ))}
            </div>
          )}
        </Panel>
      ) : null}

      <Panel
        className={showGuide ? "onboarding-focus-card" : ""}
        eyebrow={copy.reports.reportEyebrow}
        title={copy.reports.summaryTitle}
        description={reportDescription}
      >
        <div className="report-list">
          {reportSummaryItems.map((item) => (
            <div className="report-list-item" key={item}>
              <span className="report-dot" />
              <p>{item}</p>
            </div>
          ))}
        </div>
      </Panel>
    </div>
  );
}

export default ReportsSection;
