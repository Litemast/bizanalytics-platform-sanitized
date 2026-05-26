import { useEffect, useMemo, useState } from "react";
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Line,
  LineChart,
  Pie,
  PieChart,
  ResponsiveContainer,
  Sector,
  Tooltip,
  XAxis,
  YAxis
} from "recharts";
import { Chip, EmptyState, Field, GuideOverlay, InlineNotice, MetricCard, Panel } from "./Ui";
import { formatDateValue, formatNumberValue, resolveTemplate } from "../localization";
import { getAnalyticsText, getMetricDescription, getSectionGuide } from "../analyticsContent";

const PIE_COLORS = ["#2EA6FF", "#67C587", "#FF8A3D", "#9B7CFF", "#F4B33F", "#EF5B73", "#5AD68D"];
const CHART_PALETTES = [
  { start: "#2EA6FF", end: "#8BE4FF", glow: "rgba(46, 166, 255, 0.34)" },
  { start: "#67C587", end: "#B4F2CA", glow: "rgba(103, 197, 135, 0.3)" },
  { start: "#FF8A3D", end: "#FFD18D", glow: "rgba(255, 138, 61, 0.34)" },
  { start: "#9B7CFF", end: "#D2C5FF", glow: "rgba(155, 124, 255, 0.32)" },
  { start: "#F4B33F", end: "#FFE38D", glow: "rgba(244, 179, 63, 0.3)" },
  { start: "#EF5B73", end: "#FFB0BE", glow: "rgba(239, 91, 115, 0.32)" },
  { start: "#5AD68D", end: "#B8F6CE", glow: "rgba(90, 214, 141, 0.3)" }
];
const RADIAN = Math.PI / 180;
const HEAT_COLORS = ["#EF5B73", "#FF8A3D", "#F4B33F", "#67C587"];

function getInitialMobileState() {
  if (typeof window === "undefined" || !window.matchMedia) {
    return false;
  }

  return window.matchMedia("(max-width: 900px)").matches;
}

function getChartPalette(index) {
  return CHART_PALETTES[index % CHART_PALETTES.length];
}

function formatPiePercent(percent, language) {
  return `${formatNumberValue((percent ?? 0) * 100, language, { maximumFractionDigits: 0 })}%`;
}

function shortenChartLabel(value, maxLength = 16) {
  const text = String(value ?? "").trim();
  if (text.length <= maxLength) {
    return text;
  }

  return `${text.slice(0, maxLength - 1)}…`;
}

function clamp(value, min, max) {
  return Math.min(max, Math.max(min, value));
}

function hexToRgb(hexColor) {
  const normalized = hexColor.replace("#", "");
  const value = normalized.length === 3
    ? normalized.split("").map((char) => `${char}${char}`).join("")
    : normalized;

  return {
    r: Number.parseInt(value.slice(0, 2), 16),
    g: Number.parseInt(value.slice(2, 4), 16),
    b: Number.parseInt(value.slice(4, 6), 16)
  };
}

function mixHexColors(startColor, endColor, ratio) {
  const start = hexToRgb(startColor);
  const end = hexToRgb(endColor);
  const safeRatio = clamp(ratio, 0, 1);
  const toHex = (value) => Math.round(value).toString(16).padStart(2, "0");

  return `#${toHex(start.r + (end.r - start.r) * safeRatio)}${toHex(start.g + (end.g - start.g) * safeRatio)}${toHex(start.b + (end.b - start.b) * safeRatio)}`;
}

function getHeatColor(value, min, max) {
  if (!Number.isFinite(Number(value))) {
    return "#2EA6FF";
  }

  if (max <= min) {
    return HEAT_COLORS[HEAT_COLORS.length - 1];
  }

  const normalized = clamp((Number(value) - min) / (max - min), 0, 1);
  const scaled = normalized * (HEAT_COLORS.length - 1);
  const lowerIndex = Math.floor(scaled);
  const upperIndex = Math.min(HEAT_COLORS.length - 1, lowerIndex + 1);
  const blend = scaled - lowerIndex;

  return mixHexColors(HEAT_COLORS[lowerIndex], HEAT_COLORS[upperIndex], blend);
}

function getSeriesRange(data, dataKey) {
  const values = data
    .map((item) => Number(item?.[dataKey]))
    .filter((value) => Number.isFinite(value));

  if (!values.length) {
    return { min: 0, max: 0 };
  }

  return {
    min: Math.min(...values),
    max: Math.max(...values)
  };
}

function buildLineGradientStops(data, dataKey) {
  const { min, max } = getSeriesRange(data, dataKey);

  if (!data.length) {
    return [];
  }

  if (data.length === 1) {
    const color = getHeatColor(Number(data[0]?.[dataKey] ?? 0), min, max);
    return [
      { offset: "0%", color },
      { offset: "100%", color }
    ];
  }

  return data.map((item, index) => ({
    offset: `${(index / (data.length - 1)) * 100}%`,
    color: getHeatColor(Number(item?.[dataKey] ?? 0), min, max)
  }));
}

function renderActivePieShape(props) {
  const {
    cx,
    cy,
    innerRadius,
    outerRadius,
    startAngle,
    endAngle,
    fill
  } = props;

  return (
    <Sector
      cx={cx}
      cy={cy}
      innerRadius={innerRadius - 2}
      outerRadius={outerRadius + 10}
      startAngle={startAngle}
      endAngle={endAngle}
      fill={fill}
      stroke="rgba(255, 255, 255, 0.82)"
      strokeWidth={2}
      cornerRadius={12}
    />
  );
}

function renderPieLabel(props, formatter) {
  const { cx, cy, midAngle, outerRadius, percent, payload } = props;
  const radius = outerRadius + 22;
  const x = cx + radius * Math.cos(-midAngle * RADIAN);
  const y = cy + radius * Math.sin(-midAngle * RADIAN);
  const formatted = formatter(payload, percent);
  const label =
    typeof formatted === "string"
      ? { primary: formatted, secondary: "" }
      : {
          primary: formatted?.primary ?? "",
          secondary: formatted?.secondary ?? ""
        };
  const hasSecondary = Boolean(label.secondary);

  return (
    <text
      className="analytics-pie-label"
      dominantBaseline="middle"
      textAnchor={x > cx ? "start" : "end"}
      x={x}
      y={y}
    >
      <tspan
        className="analytics-pie-label-percent"
        dy={hasSecondary ? "-0.38rem" : "0"}
        x={x}
      >
        {label.primary}
      </tspan>
      {hasSecondary ? (
        <tspan className="analytics-pie-label-caption" dy="1rem" x={x}>
          {label.secondary}
        </tspan>
      ) : null}
    </text>
  );
}

function TrendDot({ active = false, color, cx, cy }) {
  if (!Number.isFinite(cx) || !Number.isFinite(cy)) {
    return null;
  }

  const glowColor = mixHexColors(color, "#FFFFFF", active ? 0.46 : 0.22);

  return (
    <g>
      <circle
        cx={cx}
        cy={cy}
        fill={glowColor}
        opacity={active ? 0.42 : 0.24}
        r={active ? 8 : 6}
      />
      <circle
        className="analytics-trend-dot-core"
        cx={cx}
        cy={cy}
        fill={color}
        r={active ? 4.4 : 3.4}
        stroke="rgba(255, 255, 255, 0.86)"
        strokeWidth={active ? 2 : 1.5}
      />
    </g>
  );
}

function DecoratedPieChart({
  data,
  dataKey,
  nameKey,
  language,
  idPrefix,
  labelFormatter,
  tooltipFormatter,
  centerTitle,
  centerValue,
  isAnimationActive = true
}) {
  const [activeIndex, setActiveIndex] = useState(null);
  const safeActiveIndex =
    data.length && activeIndex !== null
      ? Math.min(activeIndex, data.length - 1)
      : undefined;

  useEffect(() => {
    if (!data.length) {
      setActiveIndex(null);
      return;
    }

    if (activeIndex !== null && activeIndex > data.length - 1) {
      setActiveIndex(null);
    }
  }, [activeIndex, data.length]);

  return (
    <ResponsiveContainer width="100%" height="100%">
      <PieChart className="analytics-pie-chart">
        <defs>
          {data.map((entry, index) => {
            const palette = getChartPalette(index);
            return (
              <linearGradient id={`${idPrefix}-pie-${index}`} key={`${idPrefix}-pie-gradient-${entry[nameKey]}-${index}`} x1="0" x2="1" y1="0" y2="1">
                <stop offset="0%" stopColor={palette.start} />
                <stop offset="100%" stopColor={palette.end} />
              </linearGradient>
            );
          })}
          <filter id={`${idPrefix}-pie-glow`} height="160%" width="160%" x="-30%" y="-30%">
            <feDropShadow dx="0" dy="0" floodColor="rgba(46, 166, 255, 0.28)" floodOpacity="0.5" stdDeviation="6" />
          </filter>
        </defs>
        <Pie
          activeIndex={safeActiveIndex}
          activeShape={renderActivePieShape}
          animationDuration={isAnimationActive ? 320 : 0}
          data={data}
          dataKey={dataKey}
          innerRadius={54}
          isAnimationActive={isAnimationActive}
          label={(props) => renderPieLabel(props, labelFormatter)}
          labelLine={false}
          nameKey={nameKey}
          onMouseEnter={(_, index) => setActiveIndex(index)}
          onMouseLeave={() => setActiveIndex(null)}
          outerRadius={100}
          paddingAngle={4}
        >
          {data.map((entry, index) => (
            <Cell
              fill={`url(#${idPrefix}-pie-${index})`}
              filter={index === safeActiveIndex ? `url(#${idPrefix}-pie-glow)` : undefined}
              key={`${idPrefix}-pie-cell-${entry[nameKey]}-${index}`}
              stroke={index === safeActiveIndex ? "rgba(255, 255, 255, 0.78)" : "rgba(255, 255, 255, 0.34)"}
              strokeWidth={index === safeActiveIndex ? 2 : 1}
            />
          ))}
        </Pie>
        <text className="analytics-pie-center-title" textAnchor="middle" x="50%" y="46%">
          {centerTitle}
        </text>
        <text className="analytics-pie-center-value" textAnchor="middle" x="50%" y="57%">
          {centerValue}
        </text>
        <Tooltip
          formatter={(value, name, details) => tooltipFormatter(value, name, details)}
          labelFormatter={(value) => value}
        />
      </PieChart>
    </ResponsiveContainer>
  );
}

function DecoratedBarChart({
  data,
  dataKey,
  name,
  nameKey,
  language,
  idPrefix,
  tooltipLabel,
  maxBarSize = 64,
  isAnimationActive = true
}) {
  const [activeIndex, setActiveIndex] = useState(null);
  const { min, max } = useMemo(() => getSeriesRange(data, dataKey), [data, dataKey]);

  return (
    <ResponsiveContainer width="100%" height="100%">
      <BarChart barCategoryGap="22%" className="analytics-bar-chart" data={data}>
        <defs>
          {data.map((entry, index) => {
            const baseColor = getHeatColor(Number(entry?.[dataKey] ?? 0), min, max);
            return (
              <linearGradient id={`${idPrefix}-bar-${index}`} key={`${idPrefix}-bar-gradient-${entry[nameKey]}-${index}`} x1="0" x2="0" y1="0" y2="1">
                <stop offset="0%" stopColor={mixHexColors(baseColor, "#FFFFFF", 0.34)} />
                <stop offset="100%" stopColor={mixHexColors(baseColor, "#0E1A2B", 0.08)} />
              </linearGradient>
            );
          })}
          <filter id={`${idPrefix}-bar-glow`} height="160%" width="180%" x="-40%" y="-30%">
            <feDropShadow dx="0" dy="8" floodColor="rgba(46, 166, 255, 0.18)" floodOpacity="0.45" stdDeviation="8" />
          </filter>
        </defs>
        <CartesianGrid strokeDasharray="4 4" stroke="rgba(98, 120, 157, 0.18)" vertical={false} />
        <XAxis
          axisLine={false}
          dataKey={nameKey}
          tickFormatter={(value) => shortenChartLabel(value, 12)}
          tickLine={false}
        />
        <YAxis tickLine={false} axisLine={false} tickFormatter={(value) => formatNumberValue(value, language)} />
        <Tooltip formatter={(value) => [formatNumberValue(value, language), tooltipLabel]} />
        <Bar
          animationDuration={isAnimationActive ? 900 : 0}
          animationEasing="ease-out"
          dataKey={dataKey}
          isAnimationActive={isAnimationActive}
          maxBarSize={maxBarSize}
          name={name}
          onMouseEnter={(_, index) => setActiveIndex(index)}
          onMouseLeave={() => setActiveIndex(null)}
          radius={[16, 16, 6, 6]}
        >
          {data.map((entry, index) => (
            <Cell
              fill={`url(#${idPrefix}-bar-${index})`}
              filter={activeIndex === index ? `url(#${idPrefix}-bar-glow)` : undefined}
              key={`${idPrefix}-bar-cell-${entry[nameKey]}-${index}`}
              opacity={activeIndex === null || activeIndex === index ? 1 : 0.8}
              stroke={activeIndex === index ? "rgba(255, 255, 255, 0.7)" : "rgba(255, 255, 255, 0.16)"}
              strokeWidth={activeIndex === index ? 1.5 : 0.8}
            />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}

function DecoratedLineChart({
  data,
  series,
  xKey,
  language,
  idPrefix,
  xTickFormatter = (value) => value,
  tooltipFormatter,
  labelFormatter = (value) => value,
  isAnimationActive = true
}) {
  const preparedSeries = useMemo(
    () =>
      series.map((item) => ({
        ...item,
        range: getSeriesRange(data, item.dataKey),
        stops: item.color
          ? [
              { offset: "0%", color: item.color },
              { offset: "100%", color: item.color }
            ]
          : buildLineGradientStops(data, item.dataKey)
      })),
    [data, series]
  );

  return (
    <ResponsiveContainer width="100%" height="100%">
      <LineChart className="analytics-line-chart" data={data}>
        <defs>
          {preparedSeries.map((item) => (
            <linearGradient id={`${idPrefix}-line-${item.dataKey}`} key={`${idPrefix}-line-gradient-${item.dataKey}`} x1="0" x2="1" y1="0" y2="0">
              {item.stops.map((stop, index) => (
                <stop key={`${idPrefix}-line-stop-${item.dataKey}-${index}`} offset={stop.offset} stopColor={stop.color} />
              ))}
            </linearGradient>
          ))}
          {preparedSeries.map((item) => (
            <filter id={`${idPrefix}-line-glow-${item.dataKey}`} key={`${idPrefix}-line-filter-${item.dataKey}`} height="180%" width="180%" x="-40%" y="-40%">
              <feDropShadow dx="0" dy="0" floodColor="rgba(46, 166, 255, 0.3)" floodOpacity="0.4" stdDeviation="6" />
            </filter>
          ))}
        </defs>
        <CartesianGrid strokeDasharray="4 4" stroke="rgba(98, 120, 157, 0.18)" vertical={false} />
        <XAxis axisLine={false} dataKey={xKey} tickFormatter={xTickFormatter} tickLine={false} />
        <YAxis tickLine={false} axisLine={false} tickFormatter={(value) => formatNumberValue(value, language)} />
        <Tooltip
          formatter={(value, name, details) =>
            tooltipFormatter
              ? tooltipFormatter(value, name, details)
              : [formatNumberValue(value, language), name]
          }
          labelFormatter={labelFormatter}
        />
        {preparedSeries.map((item) => (
          <Line
            activeDot={(props) => (
              <TrendDot
                active
                color={item.color ?? getHeatColor(Number(props?.payload?.[item.dataKey] ?? 0), item.range.min, item.range.max)}
                cx={props.cx}
                cy={props.cy}
              />
            )}
            animationDuration={isAnimationActive ? 780 : 0}
            dataKey={item.dataKey}
            dot={(props) => (
              <TrendDot
                color={item.color ?? getHeatColor(Number(props?.payload?.[item.dataKey] ?? 0), item.range.min, item.range.max)}
                cx={props.cx}
                cy={props.cy}
              />
            )}
            filter={`url(#${idPrefix}-line-glow-${item.dataKey})`}
            isAnimationActive={isAnimationActive}
            key={`${idPrefix}-line-${item.dataKey}`}
            name={item.name}
            stroke={`url(#${idPrefix}-line-${item.dataKey})`}
            strokeWidth={3.4}
            type="monotone"
          />
        ))}
      </LineChart>
    </ResponsiveContainer>
  );
}

function getInsightMeta(insight, language) {
  const source = `${insight?.title ?? ""} ${insight?.description ?? ""}`.toLowerCase();
  const explicitTone = String(insight?.tone ?? "").toLowerCase();
  const labels =
    language === "en"
      ? {
          peak: "Peak",
          leader: "Leader",
          volatility: "Volatility",
          trend: "Trend",
          risk: "Risk",
          signal: "Insight"
        }
      : {
          peak: "Пик",
          leader: "Лидер",
          volatility: "Колебания",
          trend: "Тренд",
          risk: "Риск",
          signal: "Вывод"
        };

  if (explicitTone === "peak" || /пик|максим|лучший день|record|peak|best day|сильн/.test(source)) {
    return { tone: "peak", badge: labels.peak, icon: "bolt" };
  }

  if (explicitTone === "leader" || /лидер|драйвер|ведущ|leader|driver|main|лучший ученик|рентабельность/.test(source)) {
    return { tone: "leader", badge: labels.leader, icon: "bolt" };
  }

  if (explicitTone === "volatility" || /цен|колеб|volat|swing|price|расход/.test(source)) {
    return { tone: "signal", badge: labels.volatility, icon: "spark" };
  }

  if (explicitTone === "trend" || /рост|динам|trend|increase|decline|прогноз|успеваемости/.test(source)) {
    return { tone: "trend", badge: labels.trend, icon: "dollar" };
  }

  if (explicitTone === "risk" || explicitTone === "warning" || /убыт|риск|внимания|attention|risk/.test(source)) {
    return { tone: "risk", badge: labels.risk, icon: "pulse" };
  }

  return { tone: "signal", badge: labels.signal, icon: "spark" };
}

function InsightCards({ insights, language }) {
  return (
    <div className="sales-insight-grid">
      {insights.map((insight, index) => {
        const meta = getInsightMeta(insight, language);
        return (
          <article
            className={`sales-insight-card sales-insight-card-${meta.tone}`}
            key={`${insight.title}-${insight.description}`}
            style={{ animationDelay: `${index * 80}ms` }}
          >
            <div className="sales-insight-card-head">
              <span className={`sales-insight-badge sales-insight-badge-${meta.tone}`}>{meta.badge}</span>
              <span
                aria-hidden="true"
                className={`sales-insight-icon sales-insight-icon-${meta.icon}`}
              />
            </div>
            <strong>{insight.title}</strong>
            <p>{insight.description}</p>
          </article>
        );
      })}
    </div>
  );
}

function buildComparisonInsights(items, copy, language) {
  if (items.length < 2) {
    return [];
  }

  const leaderByRevenue = [...items].sort(
    (left, right) => right.analytics.summary.totalRevenue - left.analytics.summary.totalRevenue
  )[0];
  const leaderBySales = [...items].sort(
    (left, right) => right.analytics.summary.totalSalesCount - left.analytics.summary.totalSalesCount
  )[0];
  const leaderByCheck = [...items].sort(
    (left, right) => right.analytics.summary.averageCheck - left.analytics.summary.averageCheck
  )[0];

  const insights = [
    resolveTemplate(copy.analytics.compareRevenueLeader, {
      name: leaderByRevenue.name,
      value: formatNumberValue(leaderByRevenue.analytics.summary.totalRevenue, language)
    }),
    resolveTemplate(copy.analytics.compareSalesLeader, {
      name: leaderBySales.name,
      value: formatNumberValue(leaderBySales.analytics.summary.totalSalesCount, language, {
        maximumFractionDigits: 0
      })
    }),
    resolveTemplate(copy.analytics.compareCheckLeader, {
      name: leaderByCheck.name,
      value: formatNumberValue(leaderByCheck.analytics.summary.averageCheck, language)
    })
  ];

  if (items.length === 2) {
    const [first, second] = items;
    const baseRevenue = Math.max(Math.abs(second.analytics.summary.totalRevenue), 1);
    const revenueGap =
      ((first.analytics.summary.totalRevenue - second.analytics.summary.totalRevenue) / baseRevenue) * 100;

    insights.push(
      resolveTemplate(copy.analytics.compareRevenueGap, {
        first: first.name,
        second: second.name,
        percent: formatNumberValue(Math.abs(revenueGap), language)
      })
    );
  }

  return insights;
}

function getSpecializedLabels(language) {
  if (language === "en") {
    return {
      reportTypes: {
        sales_report: "Sales report",
        financial_report: "Financial report",
        education_report: "Education report",
        mixed_report: "Mixed report",
        unknown: "Unknown report"
      },
      sales: {
        title: "Sales analytics"
      },
      financial: {
        title: "Financial analytics",
        description: "Revenue, expenses, profit, profitability, trend and simple next-period forecasts.",
        revenue: "Revenue",
        expenses: "Expenses",
        profit: "Profit",
        profitability: "Profitability",
        trendTitle: "Trend by periods",
        trendEyebrow: "Income and expense dynamics",
        forecastTitle: "Next period forecast",
        forecastEyebrow: "Profit forecast",
        linearForecast: "Linear regression",
        movingAverage: "Moving average",
        trendForecast: "Trend extrapolation",
        insightsEyebrow: "Financial conclusions",
        insightsTitle: "Financial insights"
      },
      education: {
        title: "Student performance analytics",
        description: "Average score, best and weakest areas, grade distribution, forecasts and risk students.",
        averageScore: "Average score",
        bestStudent: "Best student",
        worstStudent: "Weakest student",
        bestSubject: "Best subject",
        worstSubject: "Weakest subject",
        successRate: "Success rate",
        gradeDistribution: "Grade distribution",
        gradeDistributionEyebrow: "Grade structure",
        studentRating: "Student rating",
        studentRatingEyebrow: "Student comparison",
        subjectRating: "Subject rating",
        subjectRatingEyebrow: "Subject comparison",
        forecastTitle: "Average score forecast",
        forecastEyebrow: "Performance forecast",
        riskTitle: "Risk of performance decline",
        riskEyebrow: "Risk zone",
        noRisks: "No obvious risk students found.",
        insightsEyebrow: "Learning focus",
        insightsTitle: "Learning recommendations"
      }
    };
  }

  return {
    reportTypes: {
      sales_report: "Отчет по продажам",
      financial_report: "Финансовый отчет",
      education_report: "Отчет по успеваемости",
      mixed_report: "Смешанный отчет",
      unknown: "Тип не определен"
    },
    sales: {
      title: "Аналитика продаж"
    },
    financial: {
      title: "Финансовая аналитика",
      description: "Доходы, расходы, прибыль, рентабельность, тренд и простые прогнозы на следующий период.",
      revenue: "Доходы",
      expenses: "Расходы",
      profit: "Прибыль",
      profitability: "Рентабельность",
      trendTitle: "Тренд по периодам",
      trendEyebrow: "Динамика доходов и расходов",
      forecastTitle: "Прогноз на следующий период",
      forecastEyebrow: "Прогноз прибыли",
      linearForecast: "Линейная регрессия",
      movingAverage: "Moving average",
      trendForecast: "Экстраполяция тренда",
      insightsEyebrow: "Финансовые выводы",
      insightsTitle: "Финансовые выводы"
    },
    education: {
      title: "Аналитика успеваемости",
      description: "Средний балл, сильные и слабые стороны, распределение оценок, прогнозы и ученики в зоне риска.",
      averageScore: "Средний балл",
      bestStudent: "Лучший ученик",
      worstStudent: "Худший ученик",
      bestSubject: "Лучший предмет",
      worstSubject: "Худший предмет",
      successRate: "Процент успеваемости",
      gradeDistribution: "Распределение оценок",
      gradeDistributionEyebrow: "Структура оценок",
      studentRating: "Рейтинг учеников",
      studentRatingEyebrow: "Сравнение учеников",
      subjectRating: "Рейтинг предметов",
      subjectRatingEyebrow: "Сравнение предметов",
      forecastTitle: "Прогноз среднего балла",
      forecastEyebrow: "Прогноз успеваемости",
      riskTitle: "Риск снижения успеваемости",
      riskEyebrow: "Зона риска",
      noRisks: "Явных учеников в зоне риска не найдено.",
      insightsEyebrow: "Фокус обучения",
      insightsTitle: "Рекомендации к обучению"
    }
  };
}

function formatDotDateValue(value) {
  if (!value) {
    return "";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "";
  }

  const day = String(date.getUTCDate()).padStart(2, "0");
  const month = String(date.getUTCMonth() + 1).padStart(2, "0");
  const year = date.getUTCFullYear();

  return `${day}.${month}.${year}`;
}

function FinancialAnalyticsBlock({ financial, language, chartAnimationActive }) {
  const labels = getSpecializedLabels(language).financial;
  const analyticsText = getAnalyticsText(language);
  const periodData = (financial?.periods ?? []).map((item) => ({
    ...item,
    label: item.periodLabel || formatDateValue(item.period, language)
  }));
  const forecastProfitLabel = labels.forecastProfit ?? (language === "en" ? "Profit forecast" : "Прогноз прибыли");
  const forecastData = (financial?.forecastTrend ?? []).map((item) => ({
    ...item,
    label: formatDotDateValue(item.period) || item.periodLabel
  }));
  const insights = financial?.insights ?? [];
  return (
    <>
      <section className="metrics-grid">
        <MetricCard
          description={getMetricDescription("financialRevenue", language)}
          label={labels.revenue}
          value={formatNumberValue(financial?.totalRevenue, language)}
        />
        <MetricCard
          accent="secondary"
          description={getMetricDescription("financialExpenses", language)}
          label={labels.expenses}
          value={formatNumberValue(financial?.totalExpenses, language)}
        />
        <MetricCard
          accent="secondary"
          description={getMetricDescription("financialProfit", language)}
          label={labels.profit}
          value={formatNumberValue(financial?.totalProfit, language)}
        />
        <MetricCard
          accent="accent"
          description={getMetricDescription("profitability", language)}
          label={labels.profitability}
          value={`${formatNumberValue(financial?.profitability, language)}%`}
        />
      </section>

      <div className="panel-grid panel-grid-charts">
        <Panel eyebrow={labels.title} title={labels.trendTitle} description={analyticsText.financialPanels.trend}>
          <div className="chart-box chart-box-fancy chart-box-fancy-line">
            <DecoratedLineChart
              data={periodData}
              idPrefix="financial-periods"
              isAnimationActive={chartAnimationActive}
              labelFormatter={(value) => value}
              language={language}
              series={[
                { dataKey: "revenue", name: labels.revenue, color: "#2EA6FF" },
                { dataKey: "expenses", name: labels.expenses, color: "#FF8A3D" },
                { dataKey: "profit", name: labels.profit, color: "#67C587" }
              ]}
              xKey="label"
            />
          </div>
        </Panel>

        <Panel eyebrow={labels.title} title={labels.forecastTitle} description={analyticsText.financialPanels.forecast}>
          <div className="chart-box chart-box-fancy chart-box-fancy-line">
            <DecoratedLineChart
              data={forecastData}
              idPrefix="financial-forecast"
              isAnimationActive={chartAnimationActive}
              labelFormatter={(value) => value}
              language={language}
              series={[{ dataKey: "forecastProfit", name: forecastProfitLabel, color: "#9B7CFF" }]}
              xKey="label"
            />
          </div>
        </Panel>
      </div>

      <Panel eyebrow={labels.title} title={labels.insightsTitle} description={analyticsText.financialPanels.insights}>
        {insights.length === 0 ? (
          <EmptyState title={labels.insightsTitle} body={analyticsText.financialPanels.insights} />
        ) : (
          <InsightCards insights={insights} language={language} />
        )}
      </Panel>

    </>
  );
}

function EducationAnalyticsBlock({ education, language, chartAnimationActive }) {
  const labels = getSpecializedLabels(language).education;
  const analyticsText = getAnalyticsText(language);
  const distributionData = education?.gradeDistribution ?? [];
  const studentData = (education?.studentPerformance ?? []).slice(0, 12);
  const subjectData = (education?.subjectPerformance ?? []).slice(0, 12);
  const forecasts = education?.studentForecasts ?? [];
  const risks = education?.riskStudents ?? [];
  const insights = education?.insights ?? [];
  return (
    <>
      <section className="metrics-grid">
        <MetricCard
          description={getMetricDescription("averageScore", language)}
          label={labels.averageScore}
          value={formatNumberValue(education?.averageScore, language)}
        />
        <MetricCard
          accent="secondary"
          description={getMetricDescription("bestStudent", language)}
          label={labels.bestStudent}
          value={education?.bestStudent || "-"}
        />
        <MetricCard
          accent="secondary"
          description={getMetricDescription("worstStudent", language)}
          label={labels.worstStudent}
          value={education?.worstStudent || "-"}
        />
        <MetricCard
          accent="accent"
          description={getMetricDescription("successRate", language)}
          label={labels.successRate}
          value={`${formatNumberValue(education?.successRate, language)}%`}
        />
      </section>

      <div className="panel-grid panel-grid-charts">
        <Panel eyebrow={labels.title} title={labels.gradeDistribution} description={analyticsText.educationPanels.distribution}>
          <div className="chart-box chart-box-fancy chart-box-fancy-pie">
            <DecoratedPieChart
              centerTitle={language === "en" ? "Grades" : "Оценки"}
              centerValue={formatNumberValue(
                distributionData.reduce((total, item) => total + Number(item.count ?? 0), 0),
                language,
                { maximumFractionDigits: 0 }
              )}
              data={distributionData}
              dataKey="count"
              idPrefix="education-grade-distribution"
              isAnimationActive={chartAnimationActive}
              labelFormatter={(item, percent) => ({
                primary: formatPiePercent(percent, language),
                secondary: shortenChartLabel(item.grade, 14)
              })}
              language={language}
              nameKey="grade"
              tooltipFormatter={(value, name) => [
                formatNumberValue(value, language, { maximumFractionDigits: 0 }),
                `${labels.gradeDistribution}: ${name}`
              ]}
            />
          </div>
        </Panel>

        <Panel eyebrow={labels.title} title={labels.studentRating} description={analyticsText.educationPanels.students}>
          <div className="chart-box chart-box-fancy chart-box-fancy-bar">
            <DecoratedBarChart
              data={studentData}
              dataKey="averageScore"
              idPrefix="education-students"
              isAnimationActive={chartAnimationActive}
              language={language}
              name={labels.averageScore}
              nameKey="name"
              tooltipLabel={labels.averageScore}
            />
          </div>
        </Panel>
      </div>

      <div className="panel-grid panel-grid-charts">
        <Panel eyebrow={labels.title} title={labels.subjectRating} description={analyticsText.educationPanels.subjects}>
          <div className="chart-box chart-box-fancy chart-box-fancy-bar">
            <DecoratedBarChart
              data={subjectData}
              dataKey="averageScore"
              idPrefix="education-subjects"
              isAnimationActive={chartAnimationActive}
              language={language}
              name={labels.averageScore}
              nameKey="name"
              tooltipLabel={labels.averageScore}
            />
          </div>
        </Panel>

        <Panel eyebrow={labels.title} title={labels.forecastTitle} description={analyticsText.educationPanels.forecast}>
          <div className="table-shell">
            <table>
              <thead>
                <tr>
                  <th>{labels.bestStudent}</th>
                  <th>{labels.averageScore}</th>
                  <th>{labels.forecastTitle}</th>
                </tr>
              </thead>
              <tbody>
                {forecasts.map((item) => (
                  <tr key={item.studentName}>
                    <td>{item.studentName}</td>
                    <td>{formatNumberValue(item.currentAverage, language)}</td>
                    <td>{formatNumberValue(item.forecastAverage, language)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Panel>
      </div>

      <Panel eyebrow={labels.title} title={labels.riskTitle} description={analyticsText.educationPanels.risks}>
        {risks.length === 0 ? (
          <EmptyState title={labels.riskTitle} body={labels.noRisks} />
        ) : (
          <ul className="insight-bullet-list">
            {risks.map((item) => (
              <li key={item.studentName}>
                <strong>{item.studentName}.</strong> {labels.averageScore}: {formatNumberValue(item.averageScore, language)}
              </li>
            ))}
          </ul>
        )}
      </Panel>

      <Panel eyebrow={labels.title} title={labels.insightsTitle} description={analyticsText.educationPanels.insights}>
        {insights.length === 0 ? (
          <EmptyState title={labels.insightsTitle} body={analyticsText.educationPanels.insights} />
        ) : (
          <InsightCards insights={insights} language={language} />
        )}
      </Panel>

    </>
  );
}

function getEntrepreneurAnalyticsCopy(language) {
  if (language === "en") {
    return {
      registryLabel: "KGD registry",
      modeDemo: "Demo registry",
      modeLive: "Live registry",
      summaryEyebrow: "Entrepreneur registry",
      summaryTitle: "Entrepreneur profile from the RK registry",
      summaryDescription: "The block shows key entrepreneur details returned directly by KGD registry services.",
      registrationDate: "Registration date",
      taxMode: "Tax mode",
      oked: "OKED",
      risk: "Risk degree",
      taxDebt: "Tax debt",
      workers: "Workers",
      taxIn: "Taxes paid in",
      vatAmount: "VAT amount",
      taxTrendTitle: "Taxes by year",
      taxTrendDescription: "Trend based on annual registry statistics.",
      workersTitle: "Workers by year",
      workersDescription: "Shows whether the entrepreneur works alone or with employees.",
      vatTitle: "VAT by year",
      vatDescription: "Useful when the entrepreneur is a VAT payer.",
      insightsTitle: "Registry insights",
      insightsDescription: "Auto-generated conclusions from the KGD registry response.",
      flagsTitle: "Registry flags",
      flagsDescription: "Special statuses and warnings returned by the registry.",
      flagsEmpty: "The registry did not return special flags for this entrepreneur.",
      formsTitle: "Recommended IP tax forms",
      formsDescription: "These forms are selected from the detected tax mode and employee statistics.",
      recommended: "Recommended",
      officialSource: "Official source"
    };
  }

  return {
    registryLabel: "Реестр КГД",
    modeDemo: "Демо-реестр",
    modeLive: "Живой реестр",
    summaryEyebrow: "Реестр ИП",
    summaryTitle: "Профиль ИП из реестра РК",
    summaryDescription: "Блок показывает ключевые сведения по ИП, полученные напрямую из сервисов реестра КГД.",
    registrationDate: "Дата регистрации",
    taxMode: "Налоговый режим",
    oked: "ОКЭД",
    risk: "Степень риска",
    taxDebt: "Налоговая задолженность",
    workers: "Работники",
    taxIn: "Поступление налогов",
    vatAmount: "Сумма НДС",
    taxTrendTitle: "Налоги по годам",
    taxTrendDescription: "Динамика построена по ежегодной статистике из реестра.",
    workersTitle: "Работники по годам",
    workersDescription: "Показывает, работает ли ИП без сотрудников или с наймом.",
    vatTitle: "НДС по годам",
    vatDescription: "Полезно для ИП, состоящих на учете по НДС.",
    insightsTitle: "Ключевые выводы из реестра",
    insightsDescription: "Автоматически собранные выводы на основе ответа КГД.",
    flagsTitle: "Флаги и статусы реестра",
    flagsDescription: "Специальные статусы и предупреждения, которые вернул реестр.",
    flagsEmpty: "Реестр не вернул специальных флагов по данному ИП.",
    formsTitle: "Рекомендуемые формы отчетности ИП",
    formsDescription: "Формы подобраны по найденному налоговому режиму и статистике по работникам.",
    recommended: "Рекомендуется",
    officialSource: "Официальный источник"
  };
}

function AnalyticsSection({
  busy,
  comparisonBusy,
  copy,
  filters,
  language,
  selectedOrganizationId,
  selectedOrganization,
  selectedAnalysisId,
  selectedAnalysis,
  analysisWorkspaces,
  onSelectAnalysis,
  onCreateAnalysis,
  onRenameAnalysis,
  onDeleteAnalysis,
  onCompareAnalyses,
  onFilterChange,
  onRefresh,
  onResetAnalytics,
  onFilesSelect,
  onRemoveFile,
  importFiles,
  onRunImport,
  analysis,
  uploadInputKey,
  analyticsNotice,
  importNotice,
  showOnboarding,
  onDismissOnboarding,
  individualEntrepreneurSearch
}) {
  const [isMobileView, setIsMobileView] = useState(getInitialMobileState);
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [sidebarClosing, setSidebarClosing] = useState(false);
  const [compareMode, setCompareMode] = useState(false);
  const [compareSelection, setCompareSelection] = useState([]);
  const [deleteMode, setDeleteMode] = useState(false);
  const [deleteSelection, setDeleteSelection] = useState([]);
  const [deleteHelpVisible, setDeleteHelpVisible] = useState(false);
  const [deleteHelpSeen, setDeleteHelpSeen] = useState(false);
  const [comparisonItems, setComparisonItems] = useState([]);
  const [editingAnalysisId, setEditingAnalysisId] = useState("");
  const [editingName, setEditingName] = useState("");

  const revenueData = (analysis?.revenue ?? []).map((item) => ({
    ...item,
    label: formatDateValue(item.date, language)
  }));
  const priceData = (analysis?.priceTrends ?? []).map((item) => ({
    ...item,
    label: formatDateValue(item.date, language)
  }));
  const sourceData = analysis?.sourceComparisons ?? [];
  const topProducts = analysis?.topProducts ?? [];
  const insights = analysis?.insights ?? [];
  const reportType = analysis?.reportType ?? "unknown";
  const specializedLabels = getSpecializedLabels(language);
  const analyticsText = getAnalyticsText(language);
  const entrepreneurCopy = getEntrepreneurAnalyticsCopy(language);
  const guide = showOnboarding ? getSectionGuide("analytics", language, analysis) : null;
  const chartAnimationActive = !isMobileView;
  const financialAnalysis = analysis?.financial ?? null;
  const educationAnalysis = analysis?.education ?? null;
  const entrepreneurRegistry = individualEntrepreneurSearch?.found
    ? individualEntrepreneurSearch.registry ?? null
    : null;
  const entrepreneurInsights = individualEntrepreneurSearch?.found
    ? individualEntrepreneurSearch.insights ?? []
    : [];
  const entrepreneurForms = individualEntrepreneurSearch?.found
    ? individualEntrepreneurSearch.reportForms ?? []
    : [];
  const entrepreneurFlags = entrepreneurRegistry?.flags ?? [];
  const entrepreneurStatistics = entrepreneurRegistry?.statistics ?? [];
  const entrepreneurTrendData = entrepreneurStatistics.map((item) => ({
    label: String(item.year),
    year: item.year,
    taxIn: Number(item.taxIn ?? 0),
    workersCount: Number(item.workersCount ?? 0),
    vatAmount: Number(item.vatAmount ?? 0)
  }));
  const entrepreneurLatestStatistics = entrepreneurStatistics.length
    ? entrepreneurStatistics[entrepreneurStatistics.length - 1]
    : null;
  const entrepreneurMode = individualEntrepreneurSearch?.mode ?? "demo";
  const summary = analysis?.summary ?? {
    totalRevenue: 0,
    totalSalesCount: 0,
    totalQuantity: 0,
    averageCheck: 0
  };
  const importActionLabel =
    importFiles.length > 1 ? copy.analytics.multiAnalyzeAction : copy.analytics.analyzeAction;
  const hasSalesData = revenueData.length > 0 || topProducts.length > 0;
  const hasFinancialData = Boolean(financialAnalysis?.periods?.length);
  const hasEducationData = Boolean(
    educationAnalysis?.gradeDistribution?.length ||
      educationAnalysis?.studentPerformance?.length ||
      educationAnalysis?.subjectPerformance?.length
  );
  const hasImportedData = hasSalesData || hasFinancialData || hasEducationData;
  const hasEntrepreneurData = Boolean(entrepreneurRegistry);
  const hasData = hasImportedData || hasEntrepreneurData;
  const comparisonInsights = useMemo(
    () => buildComparisonInsights(comparisonItems, copy, language),
    [comparisonItems, copy, language]
  );

  useEffect(() => {
    setCompareSelection((current) =>
      current.filter((item) => analysisWorkspaces.some((workspace) => workspace.id === item))
    );
    setDeleteSelection((current) =>
      current.filter((item) => analysisWorkspaces.some((workspace) => workspace.id === item))
    );

    if (editingAnalysisId && !analysisWorkspaces.some((workspace) => workspace.id === editingAnalysisId)) {
      setEditingAnalysisId("");
      setEditingName("");
    }
  }, [analysisWorkspaces, editingAnalysisId]);

  useEffect(() => {
    if (!sidebarClosing) {
      return undefined;
    }

    const timerId = window.setTimeout(() => {
      setSidebarOpen(false);
      setSidebarClosing(false);
    }, 180);

    return () => window.clearTimeout(timerId);
  }, [sidebarClosing]);

  useEffect(() => {
    if (!showOnboarding) {
      return;
    }

    setSidebarClosing(false);
    setSidebarOpen(true);
  }, [showOnboarding]);

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

  function openSidebar() {
    setSidebarClosing(false);
    setSidebarOpen(true);
  }

  function closeSidebar() {
    setSidebarClosing(true);
  }

  function enterCompareMode() {
    setCompareMode((current) => {
      const next = !current;
      if (next) {
        setDeleteMode(false);
        setDeleteSelection([]);
        setDeleteHelpVisible(false);
      }
      return next;
    });
    setCompareSelection([]);
  }

  function enterDeleteMode() {
    setDeleteMode((current) => {
      const next = !current;
      if (next) {
        setCompareMode(false);
        setCompareSelection([]);
        setDeleteSelection([]);
        setEditingAnalysisId("");
        setEditingName("");
        if (!deleteHelpSeen) {
          setDeleteHelpVisible(true);
          setDeleteHelpSeen(true);
        } else {
          setDeleteHelpVisible(false);
        }
      } else {
        setDeleteSelection([]);
        setDeleteHelpVisible(false);
      }
      return next;
    });
  }

  async function handleRunComparison() {
    const items = await onCompareAnalyses(compareSelection);
    if (items.length > 1) {
      setComparisonItems(items);
      setCompareMode(false);
      closeSidebar();
    }
  }

  async function handleConfirmBulkDelete() {
    if (deleteSelection.length === 0) {
      return;
    }

    const ids = [...deleteSelection];
    let allDeleted = true;
    for (const workspaceId of ids) {
      const deleted = await onDeleteAnalysis(workspaceId);
      if (!deleted) {
        allDeleted = false;
        break;
      }
    }

    if (allDeleted) {
      setComparisonItems((current) => current.filter((item) => !ids.includes(item.id)));
      setCompareSelection((current) => current.filter((item) => !ids.includes(item)));
      setDeleteSelection([]);
      setDeleteMode(false);
      setDeleteHelpVisible(false);
    }
  }

  async function handleSaveRename() {
    if (!editingAnalysisId) {
      return;
    }

    const saved = await onRenameAnalysis(editingAnalysisId, editingName);
    if (saved) {
      setEditingAnalysisId("");
      setEditingName("");
    }
  }

  function toggleCompareSelection(workspaceId) {
    setCompareSelection((current) =>
      current.includes(workspaceId)
        ? current.filter((item) => item !== workspaceId)
        : [...current, workspaceId]
    );
  }

  function toggleDeleteSelection(workspaceId) {
    setDeleteHelpVisible(false);
    setDeleteSelection((current) =>
      current.includes(workspaceId)
        ? current.filter((item) => item !== workspaceId)
        : [...current, workspaceId]
    );
  }

  function selectAllForDelete() {
    setDeleteHelpVisible(false);
    setDeleteSelection(analysisWorkspaces.map((workspace) => workspace.id));
  }

  return (
    <div className="section-stack analytics-page-shell">
      <GuideOverlay guide={guide} onDismiss={onDismissOnboarding} />

      <section className="section-caption section-caption-split">
        <div className="analytics-caption-group">
          <button
            className={`ghost-button analysis-drawer-button ${showOnboarding ? "is-highlighted" : ""}`.trim()}
            onClick={openSidebar}
            type="button"
          >
            {copy.analytics.analysesAction}
          </button>
          <div>
            <h1>{copy.analytics.title}</h1>
            <p>{copy.analytics.subtitle}</p>
          </div>
        </div>

        {selectedAnalysis || entrepreneurRegistry ? (
          <div className="analysis-caption-chip">
            {selectedAnalysis ? (
              <>
                <span>{copy.analytics.currentAnalysisLabel}</span>
                <Chip tone="success">{selectedAnalysis.name}</Chip>
                {hasImportedData ? (
                  <Chip tone="info">{specializedLabels.reportTypes[reportType] ?? specializedLabels.reportTypes.unknown}</Chip>
                ) : null}
              </>
            ) : null}
            {entrepreneurRegistry ? (
              <>
                <span>{entrepreneurCopy.registryLabel}</span>
                <Chip tone="success">{entrepreneurRegistry.name || entrepreneurRegistry.iin}</Chip>
                <Chip tone="info">{entrepreneurMode === "live" ? entrepreneurCopy.modeLive : entrepreneurCopy.modeDemo}</Chip>
              </>
            ) : null}
          </div>
        ) : null}
      </section>

      {sidebarOpen ? (
        <aside
          className={`analysis-drawer ${compareMode ? "is-compare-mode" : ""} ${deleteMode ? "is-delete-mode" : ""} ${sidebarClosing ? "is-closing" : ""}`}
          onMouseLeave={closeSidebar}
        >
          <div className="analysis-drawer-head">
            <strong>{copy.analytics.drawerTitle}</strong>
            <button className="ghost-button ghost-button-compact analysis-drawer-close" onClick={closeSidebar} type="button">
              ×
            </button>
          </div>

          <div className="analysis-drawer-actions">
            <button
              className="primary-button"
              disabled={!selectedOrganizationId || busy.analysisWorkspaces}
              onClick={onCreateAnalysis}
              type="button"
            >
              {copy.analytics.newAnalysisAction}
            </button>
            <button
              className={`ghost-button ${compareMode ? "is-active-secondary" : ""}`}
              disabled={analysisWorkspaces.length < 2}
              onClick={enterCompareMode}
              type="button"
            >
              {copy.analytics.compareAnalysesAction}
            </button>
            <button
              className={`danger-button ${deleteMode ? "is-active-danger" : ""}`}
              disabled={analysisWorkspaces.length === 0 || busy.analysisWorkspaces}
              onClick={enterDeleteMode}
              type="button"
            >
              {copy.analytics.deleteAnalysesAction}
            </button>
          </div>

          {compareMode || deleteMode ? (
            <div
              className={`analysis-drawer-hint-slot ${
                compareMode || (deleteMode && deleteHelpVisible) ? "info-banner" : ""
              }`}
            >
              {compareMode ? copy.analytics.compareHint : deleteHelpVisible ? copy.analytics.deleteHint : null}
            </div>
          ) : null}

          <div className="analysis-drawer-list">
            {analysisWorkspaces.map((workspace, index) => {
              const isSelected = selectedAnalysisId === workspace.id;
              const isMarked = compareSelection.includes(workspace.id);
              const isDeleteMarked = deleteSelection.includes(workspace.id);
              const isEditing = editingAnalysisId === workspace.id;

              return (
                <div className={`analysis-drawer-item ${isSelected ? "is-selected" : ""} ${isDeleteMarked ? "is-delete-marked" : ""}`} key={workspace.id}>
                  {isEditing ? (
                    <div className="analysis-drawer-edit">
                      <Field
                        label={copy.analytics.renameFieldLabel}
                        value={editingName}
                        onChange={setEditingName}
                        placeholder={copy.analytics.renamePlaceholder}
                      />
                      <div className="button-row button-row-tight">
                        <button className="primary-button" onClick={handleSaveRename} type="button">
                          {copy.analytics.saveAnalysisName}
                        </button>
                        <button
                          className="ghost-button"
                          onClick={() => {
                            setEditingAnalysisId("");
                            setEditingName("");
                          }}
                          type="button"
                        >
                          {copy.analytics.cancelAnalysisRename}
                        </button>
                      </div>
                    </div>
                  ) : (
                    <button
                      className="analysis-drawer-link"
                      onClick={() => {
                        if (compareMode) {
                          toggleCompareSelection(workspace.id);
                          return;
                        }

                        if (deleteMode) {
                          toggleDeleteSelection(workspace.id);
                          return;
                        }

                        onSelectAnalysis(workspace.id);
                        setComparisonItems([]);
                        closeSidebar();
                      }}
                      onDoubleClick={(event) => {
                        if (!deleteMode) {
                          return;
                        }

                        event.preventDefault();
                        selectAllForDelete();
                      }}
                      type="button"
                    >
                      <div className="analysis-drawer-link-main">
                        {compareMode || deleteMode ? (
                          <span className={`analysis-marker ${isMarked || isDeleteMarked ? "is-marked" : ""}`} aria-hidden="true" />
                        ) : null}
                        <div>
                          <strong>
                            {index + 1}. {workspace.name}
                          </strong>
                        </div>
                      </div>

                      {!compareMode && !deleteMode ? (
                        <div className="analysis-drawer-item-tools">
                          <button
                            aria-label={copy.analytics.renameFieldLabel}
                            className="icon-button"
                            onClick={(event) => {
                              event.stopPropagation();
                              setEditingAnalysisId(workspace.id);
                              setEditingName(workspace.name);
                            }}
                            type="button"
                          >
                            ✎
                          </button>
                            🗑
                        </div>
                      ) : null}
                    </button>
                  )}
                </div>
              );
            })}
          </div>

          {compareMode && compareSelection.length > 1 ? (
            <button
              className="primary-button analysis-drawer-footer-action"
              disabled={comparisonBusy}
              onClick={handleRunComparison}
              type="button"
            >
              {comparisonBusy ? copy.analytics.comparingAction : copy.analytics.compareSelectedAction}
            </button>
          ) : null}

          {deleteMode && deleteSelection.length > 0 ? (
            <div className="analysis-delete-box analysis-delete-box-sticky">
              <p>{resolveTemplate(copy.analytics.deleteSelectedConfirm, { count: deleteSelection.length })}</p>
              <div className="button-row button-row-tight">
                <button
                  className="danger-button"
                  disabled={busy.analysisWorkspaces}
                  onClick={handleConfirmBulkDelete}
                  type="button"
                >
                  {copy.analytics.deleteYes}
                </button>
                <button
                  className="ghost-button"
                  onClick={() => {
                    setDeleteSelection([]);
                    setDeleteMode(false);
                    setDeleteHelpVisible(false);
                  }}
                  type="button"
                >
                  {copy.analytics.deleteNo}
                </button>
              </div>
            </div>
          ) : null}
        </aside>
      ) : null}

      {comparisonItems.length > 1 ? (
        <>
          <Panel
            eyebrow={copy.analytics.comparisonEyebrow}
            title={copy.analytics.comparisonTitle}
            description={copy.analytics.comparisonDescription}
            actions={
              <button className="ghost-button" onClick={() => setComparisonItems([])} type="button">
                {copy.analytics.closeComparisonAction}
              </button>
            }
          >
            <div className="comparison-grid">
              {comparisonItems.map((item) => (
                <article className="comparison-card" key={item.id}>
                  <div className="comparison-card-head">
                    <strong>{item.name}</strong>
                    <Chip tone="success">{formatDateValue(item.analytics.generatedAtUtc, language)}</Chip>
                  </div>

                  <div className="comparison-metrics">
                    <MetricCard
                      description={getMetricDescription("totalRevenue", language)}
                      label={copy.analytics.totalRevenue}
                      value={formatNumberValue(item.analytics.summary.totalRevenue, language)}
                    />
                    <MetricCard
                      accent="secondary"
                      description={getMetricDescription("salesCount", language)}
                      label={copy.analytics.salesCount}
                      value={formatNumberValue(item.analytics.summary.totalSalesCount, language, {
                        maximumFractionDigits: 0
                      })}
                    />
                    <MetricCard
                      accent="secondary"
                      description={getMetricDescription("totalQuantity", language)}
                      label={copy.analytics.totalQuantity}
                      value={formatNumberValue(item.analytics.summary.totalQuantity, language, {
                        maximumFractionDigits: 0
                      })}
                    />
                    <MetricCard
                      accent="accent"
                      description={getMetricDescription("averageCheck", language)}
                      label={copy.analytics.averageCheck}
                      value={formatNumberValue(item.analytics.summary.averageCheck, language)}
                    />
                  </div>

                  <div className="comparison-section">
                    <strong>{copy.analytics.productsTitle}</strong>
                    <ul className="comparison-list">
                      {(item.analytics.topProducts ?? []).slice(0, 3).map((product) => (
                        <li key={`${item.id}-${product.productName}`}>
                          {product.productName}: {formatNumberValue(product.totalRevenue, language)}
                        </li>
                      ))}
                    </ul>
                  </div>

                  <div className="comparison-section">
                    <strong>{copy.analytics.insightsTitle}</strong>
                    <ul className="comparison-list">
                      {(item.analytics.insights ?? []).slice(0, 3).map((insight) => (
                        <li key={`${item.id}-${insight.title}`}>{insight.title}</li>
                      ))}
                    </ul>
                  </div>
                </article>
              ))}
            </div>
          </Panel>

          <Panel
            eyebrow={copy.analytics.comparisonEyebrow}
            title={copy.analytics.comparisonResultTitle}
            description={copy.analytics.comparisonResultDescription}
          >
            <ul className="insight-bullet-list">
              {comparisonInsights.map((insight) => (
                <li key={insight}>{insight}</li>
              ))}
            </ul>
          </Panel>
        </>
      ) : null}

      <div className="panel-grid panel-grid-analytics-top">
        <Panel
          className={showOnboarding ? "onboarding-focus-card" : ""}
          eyebrow={copy.analytics.uploadEyebrow}
          title={copy.analytics.uploadTitle}
          description={copy.analytics.uploadDescription}
        >
          <form
            className="stack"
            onSubmit={(event) => {
              event.preventDefault();
              onRunImport();
            }}
          >
            <label className="upload-field upload-field-large">
              <span>{copy.analytics.fileLabel}</span>
              <span className="file-upload-control">
                <span className="file-upload-button">{copy.analytics.chooseFilesAction}</span>
                <span className="file-upload-status">
                  {importFiles.length
                    ? resolveTemplate(copy.analytics.readyFiles, { count: importFiles.length })
                    : copy.analytics.noFiles}
                </span>
              </span>
              <input
                className="file-upload-input"
                key={uploadInputKey}
                accept=".csv,.xls,.xlsx,.docx"
                multiple
                onChange={(event) => {
                  onFilesSelect(Array.from(event.target.files ?? []));
                  event.target.value = "";
                }}
                type="file"
              />
            </label>

            <div className="upload-hint">
              {importFiles.length
                ? resolveTemplate(copy.analytics.readyFiles, { count: importFiles.length })
                : copy.analytics.noFiles}
            </div>

            <InlineNotice notice={importNotice} />

            {importFiles.length ? (
              <div className="file-chip-list">
                {importFiles.map((file) => (
                  <div className="file-chip-card" key={`${file.name}-${file.size}-${file.lastModified}`}>
                    <Chip>{file.name}</Chip>
                    <button className="file-chip-remove" onClick={() => onRemoveFile(file)} type="button">
                      {copy.analytics.removeFile}
                    </button>
                  </div>
                ))}
              </div>
            ) : null}

            <button
              className="primary-button"
              disabled={!selectedOrganizationId || !selectedAnalysisId || busy.import}
              type="submit"
            >
              {busy.import ? copy.analytics.importing : importActionLabel}
            </button>
          </form>
        </Panel>

        <Panel
          className={showOnboarding ? "onboarding-focus-card" : ""}
          eyebrow={copy.analytics.filtersEyebrow}
          title={copy.analytics.filtersTitle}
          description={copy.analytics.filtersDescription}
          actions={
            <div className="button-row button-row-tight analytics-actions">
              <button
                className="ghost-button analytics-filter-button"
                disabled={!selectedOrganizationId || !selectedAnalysisId || busy.analytics}
                onClick={onRefresh}
                type="button"
              >
                {copy.analytics.applyPeriodAction}
              </button>
              <button
                className="analytics-reset-button"
                disabled={!selectedOrganizationId || !selectedAnalysisId || busy.analytics}
                onClick={onResetAnalytics}
                type="button"
              >
                {copy.analytics.refreshAction}
              </button>
            </div>
          }
        >
          <div className="filters-row-compact">
            <div className="current-company-chip">
              <span>{copy.analytics.currentCompany}</span>
              <Chip tone={selectedOrganization ? "success" : "default"}>
                {selectedOrganization?.name ?? "-"}
              </Chip>
            </div>
          </div>

          <div className="filters-row">
            <Field
              label={copy.analytics.startDate}
              value={filters.startDate}
              onChange={(value) => onFilterChange("startDate", value)}
              type="date"
            />
            <Field
              label={copy.analytics.endDate}
              value={filters.endDate}
              onChange={(value) => onFilterChange("endDate", value)}
              type="date"
            />
          </div>

          <InlineNotice notice={analyticsNotice} />
        </Panel>
      </div>

      {!hasData ? (
        <Panel
          eyebrow={copy.analytics.summaryTitle}
          title={copy.analytics.noDataTitle}
          description={copy.analytics.noDataBody}
        >
          <EmptyState title={copy.analytics.noDataTitle} body={copy.analytics.noDataBody} />
        </Panel>
      ) : (
        <>
          {hasEntrepreneurData ? (
            <>
              <section className="metrics-grid">
                <MetricCard
                  label={entrepreneurCopy.registrationDate}
                  value={entrepreneurRegistry?.registrationDate ? formatDateValue(entrepreneurRegistry.registrationDate, language) : "-"}
                />
                <MetricCard
                  accent="secondary"
                  label={entrepreneurCopy.taxMode}
                  value={entrepreneurRegistry?.taxMode || "-"}
                />
                <MetricCard
                  accent="secondary"
                  label={entrepreneurCopy.oked}
                  value={entrepreneurRegistry?.oked ? `${entrepreneurRegistry.oked} ${entrepreneurRegistry.okedName}`.trim() : "-"}
                />
                <MetricCard
                  accent="accent"
                  label={entrepreneurCopy.risk}
                  value={entrepreneurRegistry?.riskDegree || "-"}
                />
                <MetricCard
                  label={entrepreneurCopy.taxDebt}
                  value={formatNumberValue(entrepreneurRegistry?.taxDebt ?? 0, language)}
                />
                <MetricCard
                  accent="secondary"
                  label={entrepreneurCopy.workers}
                  value={formatNumberValue(entrepreneurLatestStatistics?.workersCount ?? 0, language, {
                    maximumFractionDigits: 0
                  })}
                />
                <MetricCard
                  accent="secondary"
                  label={entrepreneurCopy.taxIn}
                  value={formatNumberValue(entrepreneurLatestStatistics?.taxIn ?? 0, language)}
                />
                <MetricCard
                  accent="accent"
                  label={entrepreneurCopy.vatAmount}
                  value={formatNumberValue(entrepreneurLatestStatistics?.vatAmount ?? 0, language)}
                />
              </section>

              <div className="panel-grid panel-grid-charts">
                <Panel
                  eyebrow={entrepreneurCopy.summaryEyebrow}
                  title={entrepreneurCopy.taxTrendTitle}
                  description={entrepreneurCopy.taxTrendDescription}
                >
                  <div className="chart-box chart-box-fancy chart-box-fancy-line">
                    {entrepreneurTrendData.length === 0 ? (
                      <EmptyState title={entrepreneurCopy.taxTrendTitle} body={copy.analytics.noDataBody} />
                    ) : (
                      <DecoratedLineChart
                        data={entrepreneurTrendData}
                        idPrefix="entrepreneur-tax-trend"
                        isAnimationActive={chartAnimationActive}
                        labelFormatter={(value) => value}
                        language={language}
                        series={[{ dataKey: "taxIn", name: entrepreneurCopy.taxIn }]}
                        xKey="label"
                      />
                    )}
                  </div>
                </Panel>

                <Panel
                  eyebrow={entrepreneurCopy.summaryEyebrow}
                  title={entrepreneurCopy.workersTitle}
                  description={entrepreneurCopy.workersDescription}
                >
                  <div className="chart-box chart-box-fancy chart-box-fancy-bar">
                    {entrepreneurTrendData.length === 0 ? (
                      <EmptyState title={entrepreneurCopy.workersTitle} body={copy.analytics.noDataBody} />
                    ) : (
                      <DecoratedBarChart
                        data={entrepreneurTrendData}
                        dataKey="workersCount"
                        idPrefix="entrepreneur-workers"
                        isAnimationActive={chartAnimationActive}
                        language={language}
                        name={entrepreneurCopy.workers}
                        nameKey="label"
                        tooltipLabel={entrepreneurCopy.workers}
                      />
                    )}
                  </div>
                </Panel>
              </div>

              <div className="panel-grid panel-grid-charts">
                <Panel
                  eyebrow={entrepreneurCopy.summaryEyebrow}
                  title={entrepreneurCopy.vatTitle}
                  description={entrepreneurCopy.vatDescription}
                >
                  <div className="chart-box chart-box-fancy chart-box-fancy-line">
                    {entrepreneurTrendData.length === 0 ? (
                      <EmptyState title={entrepreneurCopy.vatTitle} body={copy.analytics.noDataBody} />
                    ) : (
                      <DecoratedLineChart
                        data={entrepreneurTrendData}
                        idPrefix="entrepreneur-vat"
                        isAnimationActive={chartAnimationActive}
                        labelFormatter={(value) => value}
                        language={language}
                        series={[{ dataKey: "vatAmount", name: entrepreneurCopy.vatAmount }]}
                        xKey="label"
                      />
                    )}
                  </div>
                </Panel>

                <Panel
                  eyebrow={entrepreneurCopy.summaryEyebrow}
                  title={entrepreneurCopy.insightsTitle}
                  description={entrepreneurCopy.insightsDescription}
                >
                  {entrepreneurInsights.length === 0 ? (
                    <EmptyState title={entrepreneurCopy.insightsTitle} body={copy.analytics.noDataBody} />
                  ) : (
                    <InsightCards insights={entrepreneurInsights} language={language} />
                  )}
                </Panel>
              </div>

              <div className="panel-grid panel-grid-home">
                <Panel
                  eyebrow={entrepreneurCopy.summaryEyebrow}
                  title={entrepreneurCopy.flagsTitle}
                  description={entrepreneurCopy.flagsDescription}
                >
                  {entrepreneurFlags.length === 0 ? (
                    <EmptyState title={entrepreneurCopy.flagsTitle} body={entrepreneurCopy.flagsEmpty} />
                  ) : (
                    <div className="registry-flag-list">
                      {entrepreneurFlags.map((flag) => (
                        <article className="registry-flag-card" key={`${flag.code}-${flag.value}`}>
                          <div className="report-form-card-head">
                            <strong>{flag.label}</strong>
                            <Chip tone="warning">{flag.code}</Chip>
                          </div>
                          <p>{flag.value}</p>
                        </article>
                      ))}
                    </div>
                  )}
                </Panel>

                <Panel
                  eyebrow={entrepreneurCopy.summaryEyebrow}
                  title={entrepreneurCopy.formsTitle}
                  description={entrepreneurCopy.formsDescription}
                >
                  {entrepreneurForms.length === 0 ? (
                    <EmptyState title={entrepreneurCopy.formsTitle} body={copy.analytics.noDataBody} />
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
                            <span>{form.filingDeadline}</span>
                            <span>{form.paymentDeadline}</span>
                          </div>
                          <a className="report-form-link" href={form.officialSourceUrl} rel="noreferrer" target="_blank">
                            {entrepreneurCopy.officialSource}
                          </a>
                        </article>
                      ))}
                    </div>
                  )}
                </Panel>
              </div>
            </>
          ) : null}

          {hasFinancialData && reportType === "financial_report" ? (
            <FinancialAnalyticsBlock
              chartAnimationActive={chartAnimationActive}
              financial={financialAnalysis}
              language={language}
            />
          ) : null}

          {hasEducationData && reportType === "education_report" ? (
            <EducationAnalyticsBlock
              chartAnimationActive={chartAnimationActive}
              education={educationAnalysis}
              language={language}
            />
          ) : null}

          {hasSalesData ? (
            <>
          <section className="metrics-grid">
            <MetricCard
              description={getMetricDescription("totalRevenue", language)}
              label={copy.analytics.totalRevenue}
              value={formatNumberValue(summary.totalRevenue, language)}
            />
            <MetricCard
              accent="secondary"
              description={getMetricDescription("salesCount", language)}
              label={copy.analytics.salesCount}
              value={formatNumberValue(summary.totalSalesCount, language, { maximumFractionDigits: 0 })}
            />
            <MetricCard
              accent="secondary"
              description={getMetricDescription("totalQuantity", language)}
              label={copy.analytics.totalQuantity}
              value={formatNumberValue(summary.totalQuantity, language, { maximumFractionDigits: 0 })}
            />
            <MetricCard
              accent="accent"
              description={getMetricDescription("averageCheck", language)}
              label={copy.analytics.averageCheck}
              value={formatNumberValue(summary.averageCheck, language)}
            />
          </section>

          <div className="panel-grid panel-grid-charts">
            <Panel
              eyebrow={specializedLabels.sales.title}
              title={copy.analytics.revenueTitle}
              description={analyticsText.salesPanels.revenue}
            >
              <div className="chart-box chart-box-fancy chart-box-fancy-line">
                <DecoratedLineChart
                  data={revenueData}
                  idPrefix="sales-revenue"
                  isAnimationActive={chartAnimationActive}
                  labelFormatter={(value) => value}
                  language={language}
                  series={[{ dataKey: "revenue", name: copy.analytics.totalRevenue }]}
                  xKey="label"
                />
              </div>
            </Panel>

            <Panel
              eyebrow={specializedLabels.sales.title}
              title={copy.analytics.priceTitle}
              description={analyticsText.salesPanels.price}
            >
              <div className="chart-box chart-box-fancy chart-box-fancy-line">
                <DecoratedLineChart
                  data={priceData}
                  idPrefix="sales-price"
                  isAnimationActive={chartAnimationActive}
                  labelFormatter={(value) => value}
                  language={language}
                  series={[{ dataKey: "averageUnitPrice", name: copy.analytics.avgPrice }]}
                  xKey="label"
                />
              </div>
            </Panel>
          </div>

          <div className="panel-grid panel-grid-charts">
            <Panel
              eyebrow={specializedLabels.sales.title}
              title={copy.analytics.sourceTitle}
              description={analyticsText.salesPanels.sources}
            >
              <div className="chart-box chart-box-fancy chart-box-fancy-pie">
                {sourceData.length === 0 ? (
                  <EmptyState title={copy.analytics.sourceTitle} body={copy.analytics.noDataBody} />
                ) : (
                  <DecoratedPieChart
                    centerTitle={language === "en" ? "Sources" : "Источники"}
                    centerValue={formatNumberValue(
                      sourceData.reduce((total, item) => total + Number(item.totalRevenue ?? 0), 0),
                      language
                    )}
                    data={sourceData}
                    dataKey="totalRevenue"
                    idPrefix="sales-source-comparison"
                    isAnimationActive={chartAnimationActive}
                    labelFormatter={(item, percent) => ({
                      primary: formatPiePercent(percent, language),
                      secondary: shortenChartLabel(item.sourceName, 18)
                    })}
                    language={language}
                    nameKey="sourceName"
                    tooltipFormatter={(value, name) => [
                      formatNumberValue(value, language),
                      `${copy.analytics.totalRevenue}: ${name}`
                    ]}
                  />
                )}
              </div>
            </Panel>

            <Panel
              eyebrow={specializedLabels.sales.title}
              title={copy.analytics.insightsTitle}
              description={analyticsText.salesPanels.insights}
            >
              {insights.length === 0 ? (
                <EmptyState title={copy.analytics.insightsTitle} body={copy.analytics.noDataBody} />
              ) : (
                <InsightCards insights={insights} language={language} />
              )}
            </Panel>
          </div>

          <Panel
            eyebrow={specializedLabels.sales.title}
            title={copy.analytics.productsTitle}
            description={analyticsText.salesPanels.products}
          >
            <div className="table-shell">
              <table>
                <thead>
                  <tr>
                    <th>{copy.analytics.productColumn}</th>
                    <th>{copy.analytics.quantityColumn}</th>
                    <th>{copy.analytics.revenueColumn}</th>
                  </tr>
                </thead>
                <tbody>
                  {topProducts.map((product) => (
                    <tr key={product.productName}>
                      <td>{product.productName}</td>
                      <td>
                        {formatNumberValue(product.totalQuantity, language, {
                          maximumFractionDigits: 0
                        })}
                      </td>
                      <td>{formatNumberValue(product.totalRevenue, language)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </Panel>
            </>
          ) : null}

          {hasFinancialData && reportType !== "financial_report" ? (
            <FinancialAnalyticsBlock financial={financialAnalysis} language={language} />
          ) : null}

          {hasEducationData && reportType !== "education_report" ? (
            <EducationAnalyticsBlock education={educationAnalysis} language={language} />
          ) : null}
        </>
      )}
    </div>
  );
}

export default AnalyticsSection;
