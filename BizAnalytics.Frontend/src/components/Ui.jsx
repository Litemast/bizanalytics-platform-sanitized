import { useEffect, useRef, useState } from "react";

export function Panel({ eyebrow, title, description, actions, children, className = "" }) {
  return (
    <section className={`panel-card ${className}`.trim()}>
      {(eyebrow || title || description || actions) && (
        <header className="panel-header">
          <div>
            {eyebrow ? <div className="panel-eyebrow">{eyebrow}</div> : null}
            {title ? <h2>{title}</h2> : null}
            {description ? <p className="panel-description">{description}</p> : null}
          </div>
          {actions}
        </header>
      )}
      {children}
    </section>
  );
}

function SelectField({ disabled, label, onChange, options, value }) {
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
        <span className="custom-select-caret" aria-hidden="true">▼</span>
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

export function Field({
  label,
  value,
  onChange,
  placeholder,
  type = "text",
  as = "input",
  options = [],
  disabled = false,
  ...inputProps
}) {
  if (as === "select") {
    return (
      <SelectField
        disabled={disabled}
        label={label}
        onChange={onChange}
        options={options}
        value={value}
      />
    );
  }

  return (
    <label className="field">
      <span>{label}</span>
      {as === "textarea" ? (
        <textarea
          disabled={disabled}
          placeholder={placeholder}
          rows="4"
          value={value}
          onChange={(event) => onChange(event.target.value)}
          {...inputProps}
        />
      ) : (
        <input
          disabled={disabled}
          placeholder={placeholder}
          type={type}
          value={value}
          onChange={(event) => onChange(event.target.value)}
          {...inputProps}
        />
      )}
    </label>
  );
}

export function MetricCard({ label, value, accent = "primary", description = "" }) {
  return (
    <article
      className={`metric-card metric-card-${accent} ${description ? "has-description" : ""}`.trim()}
      tabIndex={description ? 0 : undefined}
      title={description || undefined}
    >
      {description ? (
        <span aria-hidden="true" className="metric-card-info">
          i
        </span>
      ) : null}
      <span>{label}</span>
      <strong>{value}</strong>
      {description ? (
        <div className="metric-card-tooltip" role="note">
          {description}
        </div>
      ) : null}
    </article>
  );
}

export function EmptyState({ title, body, action }) {
  return (
    <div className="empty-state">
      <strong>{title}</strong>
      <p>{body}</p>
      {action}
    </div>
  );
}

export function Chip({ children, tone = "default" }) {
  return <span className={`chip chip-${tone}`}>{children}</span>;
}

export function InlineNotice({ notice }) {
  const [renderedNotice, setRenderedNotice] = useState(notice);
  const [isVisible, setIsVisible] = useState(Boolean(notice?.message));

  useEffect(() => {
    if (notice?.message) {
      setRenderedNotice(notice);
      const frameId = window.requestAnimationFrame(() => {
        setIsVisible(true);
      });

      return () => {
        window.cancelAnimationFrame(frameId);
      };
    }

    if (!renderedNotice?.message) {
      setIsVisible(false);
      return undefined;
    }

    setIsVisible(false);
    const timeoutId = window.setTimeout(() => {
      setRenderedNotice(null);
    }, 220);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [notice, renderedNotice?.message]);

  if (!renderedNotice?.message) {
    return null;
  }

  return (
    <div
      className={`inline-notice tone-${renderedNotice.tone ?? "info"} ${isVisible ? "is-visible" : "is-hiding"}`}
    >
      {renderedNotice.message}
    </div>
  );
}

export function GuideOverlay({ guide, onDismiss }) {
  if (!guide) {
    return null;
  }

  return (
    <div className="analytics-onboarding-overlay" role="dialog" aria-modal="true" aria-label={guide.title}>
      <div className="analytics-onboarding">
        <div className="analytics-onboarding-header">
          <div className="analytics-onboarding-header-top">
            <span className="panel-eyebrow">{guide.badge}</span>
            <button
              aria-label={guide.closeLabel ?? "Close"}
              className="ghost-button ghost-button-compact analytics-onboarding-close"
              onClick={onDismiss}
              type="button"
            >
              ×
            </button>
          </div>
          <h2>{guide.title}</h2>
          <p>{guide.description}</p>
        </div>

        <div className="analytics-onboarding-content">
          {guide.noteTitle || guide.noteBody ? (
            <div className="analytics-onboarding-note">
              {guide.noteTitle ? <strong>{guide.noteTitle}</strong> : null}
              {guide.noteBody ? <p>{guide.noteBody}</p> : null}
            </div>
          ) : null}

          {guide.steps?.length ? (
            <div className="analytics-onboarding-grid">
              {guide.steps.map((step, index) => (
                <article className="analytics-onboarding-card" key={`${step.title}-${index}`}>
                  <span className="analytics-onboarding-icon">{index + 1}</span>
                  <strong>{step.title}</strong>
                  <p>{step.body}</p>
                </article>
              ))}
            </div>
          ) : null}

          {guide.extraItems?.length ? (
            <div className="analytics-onboarding-summary">
              {guide.extraTitle ? <strong>{guide.extraTitle}</strong> : null}
              <ul className="insight-bullet-list">
                {guide.extraItems.map((item) => (
                  <li key={item}>{item}</li>
                ))}
              </ul>
            </div>
          ) : null}
        </div>

        <div className="analytics-onboarding-actions">
          <button className="primary-button" onClick={onDismiss} type="button">
            {guide.confirm}
          </button>
        </div>
      </div>
    </div>
  );
}
