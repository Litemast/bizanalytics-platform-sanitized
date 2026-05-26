import { useEffect, useState } from "react";
import { MetricCard } from "./Ui";
import BrandLogo from "./BrandLogo";
import SiteFooter from "./SiteFooter";

const salesChartPoints = "14,124 72,92 130,100 188,58 246,72 304,36";
const salesSources = [
  { key: "erp", share: 0.46, color: "#2EA6FF", ru: "ERP-выгрузка", en: "ERP export" },
  { key: "crm", share: 0.31, color: "#67C587", ru: "CRM-отчет", en: "CRM report" },
  { key: "docs", share: 0.23, color: "#FF8A3D", ru: "DOCX-таблица", en: "DOCX table" }
];

function getInitialMobileState() {
  if (typeof window === "undefined" || !window.matchMedia) {
    return false;
  }

  return window.matchMedia("(max-width: 900px)").matches;
}

function WelcomeDonut({ centerTitle, centerValue, isEnglish = false }) {
  const radius = 58;
  const circumference = 2 * Math.PI * radius;
  const center = 110;
  let offset = 0;

  const computedSegments = salesSources.map((segment) => {
    const dashLength = circumference * segment.share;
    const startOffset = offset;
    offset += dashLength;
    const midRatio = (startOffset + dashLength / 2) / circumference;
    const angle = midRatio * Math.PI * 2 - Math.PI / 2;
    const labelRadius = radius + 26;

    return {
      ...segment,
      dashLength,
      strokeOffset: -startOffset,
      labelX: center + Math.cos(angle) * labelRadius,
      labelY: center + Math.sin(angle) * labelRadius
    };
  });

  return (
    <svg aria-hidden="true" className="welcome-donut-chart" viewBox="0 0 220 220">
      <circle cx={center} cy={center} fill="none" r={radius} stroke="rgba(98, 120, 157, 0.18)" strokeWidth="22" />
      {computedSegments.map((segment) => (
        <circle
          cx={center}
          cy={center}
          fill="none"
          key={segment.key}
          r={radius}
          stroke={segment.color}
          strokeDasharray={`${segment.dashLength} ${circumference - segment.dashLength}`}
          strokeDashoffset={segment.strokeOffset}
          strokeLinecap="round"
          strokeWidth="22"
          transform={`rotate(-90 ${center} ${center})`}
        />
      ))}
      <circle className="welcome-donut-core" cx={center} cy={center} r="46" />
      <text className="welcome-donut-center-title" textAnchor="middle" x={center} y="102">
        {centerTitle}
      </text>
      <text className="welcome-donut-center-value" textAnchor="middle" x={center} y="126">
        {centerValue}
      </text>
      {computedSegments.map((segment) => (
        <text
          className="welcome-donut-label"
          key={`${segment.key}-label`}
          textAnchor={segment.labelX > center ? "start" : "end"}
          x={segment.labelX}
          y={segment.labelY}
        >
          {`${Math.round(segment.share * 100)}%`}
        </text>
      ))}
      <title>
        {isEnglish
          ? "Source comparison preview"
          : "Пример сопоставления источников"}
      </title>
    </svg>
  );
}

function WelcomeLineChart({ compact = false }) {
  return (
    <svg
      aria-hidden="true"
      className={`welcome-sales-chart ${compact ? "is-compact" : ""}`.trim()}
      viewBox="0 0 320 170"
    >
      <defs>
        <linearGradient id={compact ? "welcomeLineFillCompact" : "welcomeLineFill"} x1="0" x2="0" y1="0" y2="1">
          <stop offset="0%" stopColor="#62c5ff" stopOpacity="0.36" />
          <stop offset="100%" stopColor="#62c5ff" stopOpacity="0.04" />
        </linearGradient>
      </defs>
      <path d={`M ${salesChartPoints} L 306 148 L 14 148 Z`} fill={`url(#${compact ? "welcomeLineFillCompact" : "welcomeLineFill"})`} />
      <polyline
        fill="none"
        points={salesChartPoints}
        stroke="#62c5ff"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="5"
      />
      {salesChartPoints.split(" ").map((point) => {
        const [cx, cy] = point.split(",");
        return <circle cx={cx} cy={cy} fill="#ffffff" key={point} r="5" stroke="#2ea6ff" strokeWidth="3" />;
      })}
    </svg>
  );
}

const EN_COPY = {
  title: "Welcome to the BizAnalytics analytics platform",
  subtitle:
    "BizAnalytics helps you quickly analyze reports and turn files into clear metrics, charts, insights, and downloadable PDF reports.",
  startAction: "Start analytics",
  previewTitle: "Sales dashboard example",
  desktop: {
    metrics: [
      { label: "Total revenue", value: "1.48M", accent: "primary" },
      { label: "Sales count", value: "2,640", accent: "secondary" },
      { label: "Sales volume", value: "8,920", accent: "secondary" },
      { label: "Average check", value: "560", accent: "accent" }
    ],
    trendTitle: "Revenue dynamics",
    sourceTitle: "Source comparison",
    sourceCenterTitle: "Sources",
    sourceCenterValue: "3"
  },
  features: {
    visualization: {
      title: "Simple visualization",
      text:
        "Use line charts, bar charts, distributions, source comparison, and top lists to understand sales, finance, and education data faster."
    },
    periods: {
      title: "Analyze selected periods",
      text:
        "Choose the required date range and apply the period to focus analytics only on the interval you need.",
      from: "From",
      to: "To",
      apply: "Apply period"
    },
    reports: {
      title: "Download PDF reports",
      text:
        "Generate a report for the selected analytics: key metrics, charts, comparisons, and final insights are assembled automatically.",
      document: "BizAnalytics report",
      download: "Download PDF"
    },
    files: {
      title: "Files and classification",
      filesLead: "The platform accepts files such as:",
      typesLead: "and supports analytics for classifications:",
      types: ["Sales", "Finance", "Education"]
    }
  }
};

const RU_COPY = {
  title: "Приветствуем вас на платформе аналитики BizAnalytics",
  subtitle:
    "BizAnalytics помогает быстро анализировать отчеты и превращать файлы в понятные метрики, графики, выводы и PDF-отчеты.",
  startAction: "Начать аналитику",
  previewTitle: "Пример дашборда по продажам",
  desktop: {
    metrics: [
      { label: "Общая выручка", value: "1,48 млн", accent: "primary" },
      { label: "Количество продаж", value: "2 640", accent: "secondary" },
      { label: "Объем продаж", value: "8 920", accent: "secondary" },
      { label: "Средний чек", value: "560", accent: "accent" }
    ],
    trendTitle: "Динамика выручки",
    sourceTitle: "Сопоставление источников",
    sourceCenterTitle: "Источники",
    sourceCenterValue: "3"
  },
  features: {
    visualization: {
      title: "Удобная визуализация",
      text:
        "Линейные графики, столбчатые диаграммы, распределения, сравнение источников и топы помогают быстрее понять данные по продажам, финансам и успеваемости."
    },
    periods: {
      title: "Анализ по периодам",
      text:
        "Выбирайте нужный промежуток дат и сразу смотрите аналитику только за выбранный интервал.",
      from: "С",
      to: "По",
      apply: "Применить период"
    },
    reports: {
      title: "Скачивайте PDF-отчеты",
      text:
        "Отчет собирается по выбранной аналитике: ключевые метрики, графики, сравнения и итоговые выводы попадают в PDF автоматически.",
      document: "Отчет BizAnalytics",
      download: "Скачать PDF"
    },
    files: {
      title: "Файлы и классификация",
      text:
        "Платформа принимает CSV, XLS, XLSX и DOCX, автоматически распознает типовые отчеты и может расширяться под новые типы документов.",
      types: ["Продажи", "Финансы", "Успеваемость"]
    }
  }
};

function WelcomeSection({ language, onStart, toolbar }) {
  const copy = language === "en" ? EN_COPY : RU_COPY;
  const filesLead =
    language === "ru"
      ? "Платформа принимает такие файлы как:"
      : copy.features.files.filesLead;
  const typesLead =
    language === "ru"
      ? "и поддерживает аналитику по классификациям:"
      : copy.features.files.typesLead;
  const [isMobileView, setIsMobileView] = useState(getInitialMobileState);

  useEffect(() => {
    if (!window.matchMedia) {
      return undefined;
    }

    const mediaQuery = window.matchMedia("(max-width: 900px)");
    const applyMatch = (event) => setIsMobileView(event.matches);

    setIsMobileView(mediaQuery.matches);

    if (mediaQuery.addEventListener) {
      mediaQuery.addEventListener("change", applyMatch);
      return () => mediaQuery.removeEventListener("change", applyMatch);
    }

    mediaQuery.addListener(applyMatch);
    return () => mediaQuery.removeListener(applyMatch);
  }, []);

  return (
    <section className="welcome-frame welcome-frame-landing">
      <header className="welcome-topbar">
        <div className="topbar-brand">
          <BrandLogo className="brand-logo-header" />
        </div>
        {toolbar}
      </header>

      <div className="welcome-landing-hero">
        <div className="welcome-copy welcome-copy-landing">
          <h1>{copy.title}</h1>
          <p>{copy.subtitle}</p>
          <div className="welcome-actions">
            <button className="primary-button welcome-start-button" onClick={onStart} type="button">
              {copy.startAction}
            </button>
          </div>
        </div>

        {isMobileView ? (
          <div className="welcome-preview-panel">
            <span className="panel-eyebrow">{copy.previewTitle}</span>

            <div className="welcome-preview-metrics">
              {copy.desktop.metrics.map((metric) => (
                <MetricCard
                  accent={metric.accent}
                  key={metric.label}
                  label={metric.label}
                  value={metric.value}
                />
              ))}
            </div>

            <div className="welcome-card welcome-preview-card">
              <div className="welcome-card-head">
                <h3>{copy.desktop.trendTitle}</h3>
              </div>
              <div className="welcome-chart-shell welcome-chart-shell-compact">
                <WelcomeLineChart compact />
              </div>
            </div>

            <div className="welcome-card welcome-preview-card">
              <div className="welcome-card-head">
                <h3>{copy.desktop.sourceTitle}</h3>
              </div>
              <div className="welcome-donut-shell welcome-donut-shell-compact">
                <WelcomeDonut
                  centerTitle={copy.desktop.sourceCenterTitle}
                  centerValue={copy.desktop.sourceCenterValue}
                  isEnglish={language === "en"}
                />
                <div className="welcome-legend">
                  {salesSources.map((segment) => (
                    <div className="welcome-legend-row" key={segment.key}>
                      <span className="welcome-legend-dot" style={{ background: segment.color }} />
                      <span>{language === "en" ? segment.en : segment.ru}</span>
                      <strong>{`${Math.round(segment.share * 100)}%`}</strong>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>
        ) : (
          <div className="welcome-dashboard">
            <span className="panel-eyebrow">{copy.previewTitle}</span>

            <div className="welcome-dashboard-metrics">
              {copy.desktop.metrics.map((metric) => (
                <MetricCard
                  accent={metric.accent}
                  key={metric.label}
                  label={metric.label}
                  value={metric.value}
                />
              ))}
            </div>

            <div className="welcome-dashboard-stack">
              <div className="welcome-card welcome-analytics-card">
                <div className="welcome-card-head">
                  <h3>{copy.desktop.trendTitle}</h3>
                </div>
                <div className="welcome-chart-shell">
                  <WelcomeLineChart />
                </div>
              </div>

              <div className="welcome-card welcome-analytics-card">
                <div className="welcome-card-head">
                  <h3>{copy.desktop.sourceTitle}</h3>
                </div>
                <div className="welcome-donut-shell welcome-donut-shell-desktop">
                  <WelcomeDonut
                    centerTitle={copy.desktop.sourceCenterTitle}
                    centerValue={copy.desktop.sourceCenterValue}
                    isEnglish={language === "en"}
                  />
                  <div className="welcome-legend">
                    {salesSources.map((segment) => (
                      <div className="welcome-legend-row" key={segment.key}>
                        <span className="welcome-legend-dot" style={{ background: segment.color }} />
                        <span className="welcome-legend-label">{language === "en" ? segment.en : segment.ru}</span>
                        <strong className="welcome-legend-value">{`${Math.round(segment.share * 100)}%`}</strong>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>

      <div className="welcome-feature-grid">
        <article className="welcome-card welcome-feature-card">
          <h3>{copy.features.visualization.title}</h3>
          <p>{copy.features.visualization.text}</p>
          <div className="welcome-visual-stack">
            <div className="welcome-bar-row">
              {[44, 66, 52, 82, 72].map((height, index) => (
                <span key={`feature-${height}-${index}`} style={{ height: `${height}%` }} />
              ))}
            </div>
            <div className="welcome-donut-mini" />
          </div>
        </article>

        <article className="welcome-card welcome-feature-card">
          <h3>{copy.features.periods.title}</h3>
          <p>{copy.features.periods.text}</p>
          <div className="welcome-filter-preview">
            <label>
              {copy.features.periods.from}
              <span>01.04.2026</span>
            </label>
            <label>
              {copy.features.periods.to}
              <span>22.04.2026</span>
            </label>
            <button type="button">{copy.features.periods.apply}</button>
          </div>
        </article>

        <article className="welcome-card welcome-feature-card">
          <h3>{copy.features.reports.title}</h3>
          <p>{copy.features.reports.text}</p>
          <div className="welcome-report-preview">
            <span>{copy.features.reports.document}</span>
            <strong>PDF</strong>
            <button type="button">{copy.features.reports.download}</button>
          </div>
        </article>

        <article className="welcome-card welcome-feature-card">
          <h3>{copy.features.files.title}</h3>
          <div className="welcome-feature-caption">{filesLead}</div>
          <div className="welcome-file-preview">
            {["CSV", "XLS", "XLSX", "DOCX"].map((fileType) => (
              <span key={fileType}>{fileType}</span>
            ))}
          </div>
          <div className="welcome-feature-caption welcome-feature-caption-secondary">
            {typesLead}
          </div>
          <div className="welcome-type-preview">
            {copy.features.files.types.map((reportType) => (
              <span key={reportType}>{reportType}</span>
            ))}
          </div>
        </article>
      </div>

      <SiteFooter className="site-footer-embedded" language={language} />
    </section>
  );
}

export default WelcomeSection;
