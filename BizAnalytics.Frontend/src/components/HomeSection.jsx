import { useEffect, useRef, useState } from "react";
import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis
} from "recharts";
import { Chip, EmptyState, Field, GuideOverlay, InlineNotice, Panel } from "./Ui";
import { formatDateValue, formatNumberValue, resolveTemplate } from "../localization";
import { getSectionGuide } from "../analyticsContent";

function getInitialMobileState() {
  if (typeof window === "undefined" || !window.matchMedia) {
    return false;
  }

  return window.matchMedia("(max-width: 900px)").matches;
}

function CurrencySelectField({ label, value, onChange, options, disabled = false }) {
  const [isOpen, setIsOpen] = useState(false);
  const fieldRef = useRef(null);
  const selectedOption = options.find((option) => option.value === value) ?? options[0] ?? null;

  useEffect(() => {
    function handlePointerDown(event) {
      if (fieldRef.current && !fieldRef.current.contains(event.target)) {
        setIsOpen(false);
      }
    }

    function handleEscape(event) {
      if (event.key === "Escape") {
        setIsOpen(false);
      }
    }

    document.addEventListener("mousedown", handlePointerDown);
    document.addEventListener("keydown", handleEscape);

    return () => {
      document.removeEventListener("mousedown", handlePointerDown);
      document.removeEventListener("keydown", handleEscape);
    };
  }, []);

  return (
    <label className="field custom-select-field" ref={fieldRef}>
      <span>{label}</span>
      <button
        aria-expanded={isOpen}
        aria-haspopup="listbox"
        className={`custom-select-trigger ${isOpen ? "is-open" : ""}`}
        disabled={disabled}
        onClick={() => setIsOpen((current) => !current)}
        type="button"
      >
        <span>{selectedOption?.label ?? value}</span>
        <span className="custom-select-caret" aria-hidden="true">
          ▾
        </span>
      </button>
      {isOpen ? (
        <div className="custom-select-menu" role="listbox">
          {options.map((option) => (
            <button
              aria-selected={option.value === value}
              className={`custom-select-option ${option.value === value ? "is-selected" : ""}`}
              key={`${option.value}-${option.label}`}
              onClick={() => {
                onChange(option.value);
                setIsOpen(false);
              }}
              role="option"
              type="button"
            >
              {option.label}
            </button>
          ))}
        </div>
      ) : null}
    </label>
  );
}

function HomeSection({
  copy,
  language,
  organizationForm,
  onOrganizationNameChange,
  onSaveOrganization,
  onCancelEdit,
  isEditingOrganization,
  organizations,
  selectedOrganizationId,
  onSelectOrganization,
  onEditOrganization,
  onDeleteOrganization,
  busyOrganizations,
  formNotice,
  currencyOptions,
  currencyBase,
  currencyQuote,
  currencyPeriod,
  onCurrencyBaseChange,
  onCurrencyPeriodChange,
  onCurrencyQuoteChange,
  currencySeries,
  marketOverview,
  currencyNotice,
  companiesNotice,
  showGuide,
  onDismissGuide
}) {
  const minimumPeriodDate = "2017-01-01";
  const [isMobileView, setIsMobileView] = useState(getInitialMobileState);
  const guide = showGuide ? getSectionGuide("home", language) : null;
  const chartData = currencySeries.map((point) => ({
    ...point,
    label: formatDateValue(point.date, language)
  }));

  const firstPoint = chartData[0];
  const lastPoint = chartData[chartData.length - 1];
  const changePercent =
    firstPoint && lastPoint && Number(firstPoint.rate) !== 0
      ? ((Number(lastPoint.rate) - Number(firstPoint.rate)) / Number(firstPoint.rate)) * 100
      : 0;

  const trendMessage =
    Math.abs(changePercent) < 0.01
      ? copy.home.trendFlat
      : resolveTemplate(changePercent > 0 ? copy.home.trendUp : copy.home.trendDown, {
          base: currencyBase,
          quote: currencyQuote,
          percent: formatNumberValue(Math.abs(changePercent), language)
        });
  const marketSource = marketOverview?.provider?.trim() || "Alpha Vantage";

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
    <div className="section-stack">
      <GuideOverlay guide={guide} onDismiss={onDismissGuide} />

      <div className="panel-grid panel-grid-home">
        <Panel className={showGuide ? "onboarding-focus-card" : ""} title={copy.home.createTitle} description={copy.home.createDescription}>
          <form className="stack" onSubmit={onSaveOrganization}>
            <Field
              label={copy.home.nameLabel}
              value={organizationForm.name}
              onChange={onOrganizationNameChange}
              placeholder={copy.home.namePlaceholder}
            />
            <InlineNotice notice={formNotice} />
            <div className="button-row">
              <button className="primary-button" disabled={busyOrganizations} type="submit">
                {busyOrganizations
                  ? "..."
                  : isEditingOrganization
                    ? copy.home.updateAction
                    : copy.home.createAction}
              </button>
              {isEditingOrganization ? (
                <button className="ghost-button" onClick={onCancelEdit} type="button">
                  {copy.home.cancelEdit}
                </button>
              ) : null}
            </div>
          </form>
        </Panel>

        <Panel
          className={showGuide ? "onboarding-focus-card" : ""}
          title={copy.home.listTitle}
          description={copy.home.listDescription}
          actions={<div className="panel-meta-text">{resolveTemplate(copy.home.companyCount, { count: organizations.length })}</div>}
        >
          {organizations.length === 0 ? (
            <EmptyState title={copy.home.emptyTitle} body={copy.home.emptyBody} />
          ) : (
            <div className="company-list">
              {organizations.map((organization) => (
                <article
                  className={`company-card ${
                    selectedOrganizationId === organization.id ? "is-selected" : ""
                  }`}
                  key={organization.id}
                >
                  <div className="company-card-top">
                    <div className="company-card-head">
                      <strong>{organization.name}</strong>
                    </div>
                    {selectedOrganizationId === organization.id ? (
                      <Chip tone="success">{copy.home.selectedBadge}</Chip>
                    ) : null}
                  </div>
                  <div className="button-row button-row-tight">
                    <button
                      className="ghost-button"
                      onClick={() => onSelectOrganization(organization.id)}
                      type="button"
                    >
                      {copy.home.selectAction}
                    </button>
                    <button
                      className="ghost-button"
                      onClick={() => onEditOrganization(organization)}
                      type="button"
                    >
                      {copy.home.editAction}
                    </button>
                    <button
                      className="danger-button"
                      onClick={() => onDeleteOrganization(organization)}
                      type="button"
                    >
                      {copy.home.deleteAction}
                    </button>
                  </div>
                </article>
              ))}
            </div>
          )}
        </Panel>
      </div>

      <div className="panel-grid panel-grid-home">
        <Panel
          className={showGuide ? "onboarding-focus-card" : ""}
          title={copy.home.marketTitle}
          description={copy.home.marketDescription}
        >
          <div className="filters-row home-market-controls">
            <Field
              label={copy.home.periodStart}
              min={minimumPeriodDate}
              onChange={(value) => onCurrencyPeriodChange("startDate", value)}
              type="date"
              value={currencyPeriod.startDate}
            />
            <Field
              label={copy.home.periodEnd}
              min={minimumPeriodDate}
              onChange={(value) => onCurrencyPeriodChange("endDate", value)}
              type="date"
              value={currencyPeriod.endDate}
            />
            <CurrencySelectField
              label={copy.home.baseCurrency}
              value={currencyBase}
              onChange={onCurrencyBaseChange}
              options={currencyOptions.map((item) => ({
                value: item.code,
                label: `${item.code} - ${item.name}`
              }))}
            />
            <CurrencySelectField
              label={copy.home.quoteCurrency}
              value={currencyQuote}
              onChange={onCurrencyQuoteChange}
              options={currencyOptions.map((item) => ({
                value: item.code,
                label: `${item.code} - ${item.name}`
              }))}
            />
          </div>
          <InlineNotice notice={currencyNotice} />
          <div className="chart-box chart-box-medium">
            {chartData.length === 0 ? (
              <EmptyState title={copy.home.marketTitle} body={copy.home.marketEmpty} />
            ) : (
              <ResponsiveContainer width="100%" height="100%">
                <AreaChart data={chartData}>
                  <defs>
                    <linearGradient id="currencyFill" x1="0" x2="0" y1="0" y2="1">
                      <stop offset="0%" stopColor="#2EA6FF" stopOpacity="0.5" />
                      <stop offset="100%" stopColor="#2EA6FF" stopOpacity="0" />
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="4 4" stroke="rgba(98, 120, 157, 0.18)" />
                  <XAxis dataKey="label" tickLine={false} axisLine={false} />
                  <YAxis
                    tickLine={false}
                    axisLine={false}
                    tickFormatter={(value) => formatNumberValue(value, language)}
                  />
                  <Tooltip
                    formatter={(value) => [formatNumberValue(value, language), copy.home.currencyRateLabel]}
                    labelFormatter={(value) => value}
                  />
                  <Area
                    isAnimationActive={!isMobileView}
                    dataKey="rate"
                    name={copy.home.currencyRateLabel}
                    stroke="#2EA6FF"
                    fill="url(#currencyFill)"
                    strokeWidth={3}
                    type="monotone"
                  />
                </AreaChart>
              </ResponsiveContainer>
            )}
          </div>
          {chartData.length > 1 ? <div className="market-trend-text">{trendMessage}</div> : null}
        </Panel>

        <Panel
          className={`market-pulse-panel ${showGuide ? "onboarding-focus-card" : ""}`.trim()}
          title={copy.home.marketPulseTitle}
          description={resolveTemplate(copy.home.marketPulseDescription, { source: marketSource })}
        >
          <InlineNotice notice={companiesNotice} />
          {!marketOverview?.companies?.length ? (
            <EmptyState title={copy.home.marketPulseTitle} body={copy.home.marketEmpty} />
          ) : (
            <div className="market-grid">
              {marketOverview.companies.map((company) => (
                (() => {
                  const hasQuoteData = Number(company.price) > 0 || Boolean(company.lastUpdatedUtc);
                  const changeTone = hasQuoteData
                    ? company.changePercent >= 0
                      ? "is-positive"
                      : "is-negative"
                    : "";

                  return (
                    <article className="market-card" key={`${company.symbol}-${company.name}`}>
                      <div className="market-card-top">
                        <div className="market-card-head">
                          <strong>{company.name}</strong>
                          <span>{company.symbol}</span>
                        </div>
                        <span className={`market-change-text ${changeTone}`.trim()}>
                          {hasQuoteData
                            ? `${company.changePercent >= 0 ? "+" : ""}${formatNumberValue(company.changePercent, language)}%`
                            : "—"}
                        </span>
                      </div>
                      <div className="market-card-price">
                        {hasQuoteData ? formatNumberValue(company.price, language) : "—"}
                      </div>
                      <div className="market-card-meta">
                        <span>
                          {copy.home.changeLabel}: {hasQuoteData
                            ? formatNumberValue(company.change, language)
                            : "—"}
                        </span>
                        <span>
                          {copy.home.rateLabel}: {company.lastUpdatedUtc
                            ? formatDateValue(company.lastUpdatedUtc, language)
                            : "—"}
                        </span>
                      </div>
                    </article>
                  );
                })()
              ))}
            </div>
          )}
        </Panel>
      </div>
    </div>
  );
}

export default HomeSection;
