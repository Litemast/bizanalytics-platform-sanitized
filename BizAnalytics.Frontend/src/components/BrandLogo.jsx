function BrandLogo({ className = "" }) {
  const classes = ["brand-logo-image", className].filter(Boolean).join(" ");

  return (
    <svg
      aria-label="BizAnalytics"
      className={classes}
      preserveAspectRatio="xMidYMid meet"
      role="img"
      viewBox="0 0 660 180"
      xmlns="http://www.w3.org/2000/svg"
    >
      <title>BizAnalytics</title>
      <defs>
        <linearGradient id="brandCircleStroke" x1="20" x2="150" y1="145" y2="25">
          <stop offset="0%" stopColor="#2D6DFF" />
          <stop offset="55%" stopColor="#5AB7FF" />
          <stop offset="100%" stopColor="#C9F1FF" />
        </linearGradient>
        <linearGradient id="brandIconFill" x1="28" x2="130" y1="132" y2="38">
          <stop offset="0%" stopColor="#102756" />
          <stop offset="52%" stopColor="#1E5EA0" />
          <stop offset="100%" stopColor="#7ED4FF" />
        </linearGradient>
        <linearGradient id="brandBarFill" x1="45" x2="108" y1="132" y2="54">
          <stop offset="0%" stopColor="#2449B8" />
          <stop offset="100%" stopColor="#89DBFF" />
        </linearGradient>
        <linearGradient id="brandArrowStroke" x1="18" x2="150" y1="125" y2="42">
          <stop offset="0%" stopColor="#D8F3FF" />
          <stop offset="44%" stopColor="#EAF9FF" />
          <stop offset="100%" stopColor="#F7FDFF" />
        </linearGradient>
        <linearGradient id="brandAnalyticsText" x1="310" x2="640" y1="40" y2="128">
          <stop offset="0%" stopColor="#72B3FF" />
          <stop offset="100%" stopColor="#4B8BE8" />
        </linearGradient>
      </defs>

      <g className="brand-logo-icon" transform="translate(18 20)">
        <circle className="brand-logo-icon-backplate" cx="72" cy="72" r="58" fill="url(#brandIconFill)" />
        <path
          className="brand-logo-circle-stroke"
          d="M131 66a59 59 0 0 1-12 42 63 63 0 0 1-47 24 62 62 0 0 1-55-31 60 60 0 0 1 2-61A62 62 0 0 1 74 12a64 64 0 0 1 44 18"
          fill="none"
          stroke="url(#brandCircleStroke)"
          strokeLinecap="round"
          strokeWidth="7"
        />
        <rect className="brand-logo-bar brand-logo-bar-1" fill="url(#brandBarFill)" height="36" rx="5" width="20" x="34" y="83" />
        <rect className="brand-logo-bar brand-logo-bar-2" fill="url(#brandBarFill)" height="50" rx="5" width="20" x="61" y="69" />
        <rect className="brand-logo-bar brand-logo-bar-3" fill="url(#brandBarFill)" height="68" rx="5" width="20" x="88" y="51" />
        <path
          className="brand-logo-arrow-line"
          d="M22 110c26-24 42-28 64-23 25 6 43-12 69-43"
          fill="none"
          stroke="url(#brandArrowStroke)"
          strokeLinecap="round"
          strokeWidth="10"
        />
        <path className="brand-logo-arrow-head" d="M136 39 166 31l-7 31Z" fill="#F7FDFF" />
      </g>

      <text className="brand-logo-wordmark" dominantBaseline="middle" fontFamily="Segoe UI, Arial, sans-serif" fontSize="62" fontWeight="800" x="185" y="94">
        <tspan className="brand-logo-text-biz">Biz</tspan>
        <tspan className="brand-logo-text-analytics" fill="url(#brandAnalyticsText)">Analytics</tspan>
      </text>
    </svg>
  );
}

export default BrandLogo;
