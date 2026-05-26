import BrandLogo from "./BrandLogo";

const FOOTER_COPY = {
  ru: {
    description: "Аналитика отчетов и структурированные PDF-отчеты в одном интерфейсе.",
    rights: "Все права защищены.",
    openWelcome: "Приветственная",
    contacts: {
      emailLabel: "Почта",
      emailValue: "support@bizanalytics.app",
      supportLabel: "Поддержка",
      supportValue: "+7 (800) 555-24-17",
      telegramLabel: "Telegram",
      telegramValue: "@bizanalytics_support",
      vkLabel: "VK",
      vkValue: "bizanalytics"
    }
  },
  en: {
    description: "Report analytics and structured PDF exports in one interface.",
    rights: "All rights reserved.",
    openWelcome: "Welcome",
    contacts: {
      emailLabel: "Email",
      emailValue: "support@bizanalytics.app",
      supportLabel: "Support",
      supportValue: "+7 (800) 555-24-17",
      telegramLabel: "Telegram",
      telegramValue: "@bizanalytics_support",
      vkLabel: "VK",
      vkValue: "bizanalytics"
    }
  }
};

function SiteFooter({
  language = "ru",
  className = "",
  navItems = [],
  activeSection = "",
  onNavigate,
  onOpenWelcome
}) {
  const copy = FOOTER_COPY[language] ?? FOOTER_COPY.ru;
  const currentYear = new Date().getFullYear();
  const showNavigation = navItems.length > 0 && typeof onNavigate === "function";

  return (
    <footer className={`site-footer ${className}`.trim()}>
      <div className="site-footer-main">
        <div className="site-footer-brand">
          <BrandLogo className="brand-logo-footer" />
          <p>{copy.description}</p>
        </div>

        <div className="site-footer-meta">
          <a className="site-footer-meta-item" href={`mailto:${copy.contacts.emailValue}`}>
            <strong>{copy.contacts.emailLabel}</strong>
            <span>{copy.contacts.emailValue}</span>
          </a>
          <a className="site-footer-meta-item" href={`tel:${copy.contacts.supportValue.replace(/[^\d+]/g, "")}`}>
            <strong>{copy.contacts.supportLabel}</strong>
            <span>{copy.contacts.supportValue}</span>
          </a>
          <div className="site-footer-meta-item is-static">
            <strong>{copy.contacts.telegramLabel}</strong>
            <span>{copy.contacts.telegramValue}</span>
          </div>
          <div className="site-footer-meta-item is-static">
            <strong>{copy.contacts.vkLabel}</strong>
            <span>{copy.contacts.vkValue}</span>
          </div>
        </div>

        {showNavigation ? (
          <div className="site-footer-nav">
            {navItems.map((item) => (
              <button
                className={`site-footer-link ${activeSection === item.id ? "is-active" : ""}`.trim()}
                key={item.id}
                onClick={() => onNavigate(item.id)}
                type="button"
              >
                {item.label}
              </button>
            ))}
            {typeof onOpenWelcome === "function" ? (
              <button className="site-footer-link" onClick={onOpenWelcome} type="button">
                {copy.openWelcome}
              </button>
            ) : null}
          </div>
        ) : null}
      </div>

      <div className="site-footer-bottom">
        <span>{`© ${currentYear} BizAnalytics. ${copy.rights}`}</span>
      </div>
    </footer>
  );
}

export default SiteFooter;
