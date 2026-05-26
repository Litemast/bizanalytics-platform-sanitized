import { Chip, EmptyState, Field, InlineNotice, MetricCard, Panel } from "./Ui";
import { formatDateValue, formatNumberValue } from "../localization";

function getCopy(language) {
  if (language === "en") {
    return {
      title: "Individual entrepreneur registry",
      subtitle: "Search the Kazakhstan IIN in the KGD registry, detect whether an individual entrepreneur exists, and send the result straight into analytics and reporting.",
      searchTitle: "IIN search in the KGD registry",
      searchDescription: "In live mode the page requests KGD registry APIs. In demo mode you can test the flow with prepared sample IINs.",
      iinLabel: "Kazakhstan IIN",
      iinPlaceholder: "Enter 12-digit IIN",
      searchAction: "Check IIN",
      searching: "Checking...",
      openAnalytics: "Open entrepreneur analytics",
      noResultTitle: "No entrepreneur selected yet",
      noResultBody: "Enter an IIN to check whether the person is registered as an entrepreneur and to unlock the analytics and tax form cards.",
      quickFacts: "Registry snapshot",
      detailsTitle: "What the registry returned",
      modeDemo: "Demo mode",
      modeLive: "Live KGD",
      recommended: "Recommended reports",
      officialLinks: "Official sources",
      registrationDate: "Registration date",
      taxMode: "Tax mode",
      oked: "OKED",
      risk: "Risk degree",
      debt: "Tax debt",
      actuality: "Actuality",
      employees: "Workers",
      taxesIn: "Taxes paid in",
      vat: "VAT",
      demoHint: "Demo IINs: 123456789876 (simplified declaration), 444444444444 (general tax mode)."
    };
  }

  return {
    title: "Раздел ИП",
    subtitle: "Проверьте ИИН Казахстана по реестру КГД, определите наличие ИП и сразу передайте найденные данные в аналитику и отчеты.",
    searchTitle: "Поиск ИП по ИИН в реестре КГД",
    searchDescription: "В live-режиме раздел обращается к API КГД. В demo-режиме можно проверить сценарий на подготовленных тестовых ИИН.",
    iinLabel: "ИИН Казахстана",
    iinPlaceholder: "Введите 12-значный ИИН",
    searchAction: "Проверить ИИН",
    searching: "Проверяем...",
    openAnalytics: "Открыть аналитику ИП",
    noResultTitle: "ИП пока не выбран",
    noResultBody: "Введите ИИН, чтобы проверить наличие ИП в реестре и открыть аналитические карточки и налоговые формы.",
    quickFacts: "Сводка по реестру",
    detailsTitle: "Что вернул реестр",
    modeDemo: "Demo-режим",
    modeLive: "Live КГД",
    recommended: "Рекомендуемые формы",
    officialLinks: "Официальные источники",
    registrationDate: "Дата регистрации",
    taxMode: "Налоговый режим",
    oked: "ОКЭД",
    risk: "Степень риска",
    debt: "Налоговая задолженность",
    actuality: "Актуальность",
    employees: "Работники",
    taxesIn: "Поступление налогов",
    vat: "НДС",
    demoHint: "Demo ИИН: 123456789876 (упрощенная декларация), 444444444444 (общеустановленный режим)."
  };
}

function getLatestStatistics(result) {
  const statistics = result?.registry?.statistics ?? [];
  return statistics.length ? statistics[statistics.length - 1] : null;
}

function IpSection({
  language,
  iin,
  onIinChange,
  onSearch,
  busy,
  notice,
  result,
  onOpenAnalytics
}) {
  const copy = getCopy(language);
  const latestStatistics = getLatestStatistics(result);
  const forms = result?.reportForms ?? [];
  const links = result?.officialLinks ?? [];
  const registry = result?.registry ?? null;

  return (
    <div className="section-stack">
      <section className="section-caption">
        <div>
          <h1>{copy.title}</h1>
          <p>{copy.subtitle}</p>
        </div>
      </section>

      <div className="panel-grid panel-grid-home">
        <Panel title={copy.searchTitle} description={copy.searchDescription}>
          <form
            className="stack"
            onSubmit={(event) => {
              event.preventDefault();
              onSearch();
            }}
          >
            <Field
              label={copy.iinLabel}
              value={iin}
              onChange={onIinChange}
              placeholder={copy.iinPlaceholder}
              inputMode="numeric"
              pattern="[0-9]*"
              maxLength={12}
            />
            <InlineNotice notice={notice} />
            <div className="button-row">
              <button className="primary-button" disabled={busy} type="submit">
                {busy ? copy.searching : copy.searchAction}
              </button>
              {result?.found ? (
                <button className="ghost-button" onClick={onOpenAnalytics} type="button">
                  {copy.openAnalytics}
                </button>
              ) : null}
            </div>
            <div className="report-list-item">
              <span className="report-dot" />
              <p>{copy.demoHint}</p>
            </div>
          </form>
        </Panel>

        <Panel
          title={copy.quickFacts}
          description={registry ? copy.detailsTitle : copy.noResultBody}
          actions={result?.mode ? <Chip tone={result.mode === "live" ? "success" : "default"}>{result.mode === "live" ? copy.modeLive : copy.modeDemo}</Chip> : null}
        >
          {!registry ? (
            <EmptyState title={copy.noResultTitle} body={copy.noResultBody} />
          ) : (
            <div className="metrics-grid metrics-grid-compact">
              <MetricCard label={copy.registrationDate} value={registry.registrationDate ? formatDateValue(registry.registrationDate, language) : "-"} />
              <MetricCard accent="secondary" label={copy.taxMode} value={registry.taxMode || "-"} />
              <MetricCard accent="secondary" label={copy.oked} value={registry.oked ? `${registry.oked} ${registry.okedName}`.trim() : "-"} />
              <MetricCard accent="accent" label={copy.risk} value={registry.riskDegree || "-"} />
              <MetricCard label={copy.debt} value={formatNumberValue(registry.taxDebt ?? 0, language)} />
              <MetricCard accent="secondary" label={copy.actuality} value={registry.actualityDate ? formatDateValue(registry.actualityDate, language) : "-"} />
              <MetricCard accent="secondary" label={copy.vat} value={registry.vatInfo || "-"} />
              <MetricCard accent="accent" label={copy.employees} value={formatNumberValue(latestStatistics?.workersCount ?? 0, language, { maximumFractionDigits: 0 })} />
            </div>
          )}
        </Panel>
      </div>

      {registry ? (
        <div className="panel-grid panel-grid-home">
          <Panel title={copy.recommended} description={copy.detailsTitle}>
            {!forms.length ? (
              <EmptyState title={copy.recommended} body={copy.noResultBody} />
            ) : (
              <div className="report-form-grid">
                {forms.map((form) => (
                  <article className="report-form-card" key={form.formCode}>
                    <div className="report-form-card-head">
                      <strong>{form.formCode}</strong>
                      {form.isRecommended ? <Chip tone="success">{copy.recommended}</Chip> : null}
                    </div>
                    <h3>{form.title}</h3>
                    <p>{form.description}</p>
                    <div className="report-form-meta">
                      <span>{form.filingDeadline}</span>
                      <span>{form.paymentDeadline}</span>
                    </div>
                  </article>
                ))}
              </div>
            )}
          </Panel>

          <Panel title={copy.officialLinks} description={copy.detailsTitle}>
            {!links.length ? (
              <EmptyState title={copy.officialLinks} body={copy.noResultBody} />
            ) : (
              <div className="report-list">
                {links.map((link) => (
                  <a className="report-list-item report-link-item" href={link.url} key={link.url} rel="noreferrer" target="_blank">
                    <span className="report-dot" />
                    <p>{link.label}</p>
                  </a>
                ))}
              </div>
            )}
          </Panel>
        </div>
      ) : null}
    </div>
  );
}

export default IpSection;
