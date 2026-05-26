import { lazy, Suspense, useEffect, useMemo, useRef, useState } from "react";
import axios from "axios";
import { Field, InlineNotice } from "./components/Ui";
import BrandLogo from "./components/BrandLogo";
import SiteFooter from "./components/SiteFooter";
import { LANGUAGE_OPTIONS, messages, resolveTemplate } from "./localization";

const HomeSection = lazy(() => import("./components/HomeSection"));
const IpSection = lazy(() => import("./components/IpSection"));
const AnalyticsSection = lazy(() => import("./components/AnalyticsSection"));
const ReportsSection = lazy(() => import("./components/ReportsSection"));
const WelcomeSection = lazy(() => import("./components/WelcomeSection"));

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000/api";
const STORAGE_TOKEN_KEY = "bizanalytics.token";
const STORAGE_EMAIL_KEY = "bizanalytics.email";
const STORAGE_LANGUAGE_KEY = "bizanalytics.language";
const STORAGE_THEME_KEY = "bizanalytics.theme";
const STORAGE_ANALYSIS_KEY = "bizanalytics.latest-analysis";
const STORAGE_SECTION_GUIDE_KEY = "bizanalytics.section-guides";
const MAX_IMPORT_FILES = 10;
const MARKET_AUTO_REFRESH_INTERVAL_MS = 10000;

function createNotice(tone, message) {
  return message
    ? {
        tone,
        message,
        id: `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`
      }
    : null;
}

function SectionLoadingLegacy({ language }) {
  return <div className="panel-card section-loading">{language === "en" ? "Loading..." : "Загрузка..."}</div>;
}

function SectionLoading({ language }) {
  return (
    <div aria-label={language === "en" ? "Loading" : "Загрузка"} className="panel-card section-loading">
      <BrandLogo className="brand-logo-loader" />
    </div>
  );
}

function IntroSplash({ language }) {
  return (
    <div aria-label={language === "en" ? "Opening BizAnalytics" : "Открытие BizAnalytics"} className="intro-splash-screen">
      <BrandLogo className="brand-logo-splash" />
    </div>
  );
}

function getInitialTheme() {
  const storedTheme = localStorage.getItem(STORAGE_THEME_KEY);
  if (storedTheme === "light" || storedTheme === "dark") {
    return storedTheme;
  }

  return window.matchMedia?.("(max-width: 820px)").matches ? "dark" : "light";
}

function toInputDateValue(date) {
  return new Date(date).toISOString().slice(0, 10);
}

function getDefaultCurrencyPeriod() {
  const endDate = new Date();
  endDate.setHours(0, 0, 0, 0);

  const startDate = new Date(endDate);
  startDate.setDate(startDate.getDate() - 29);

  return {
    startDate: toInputDateValue(startDate),
    endDate: toInputDateValue(endDate)
  };
}

function getSectionGuideStorageKey(emailValue) {
  return `${STORAGE_SECTION_GUIDE_KEY}.${(emailValue || "guest").trim().toLowerCase()}`;
}

function getDefaultGuideState() {
  return {
    home: true,
    analytics: true,
    reports: true
  };
}

function parseGuideState(rawValue) {
  if (!rawValue) {
    return getDefaultGuideState();
  }

  try {
    const parsed = JSON.parse(rawValue);
    return {
      home: parsed?.home !== false,
      analytics: parsed?.analytics !== false,
      reports: parsed?.reports !== false
    };
  } catch {
    return getDefaultGuideState();
  }
}

function App() {
  const [token, setToken] = useState(() => localStorage.getItem(STORAGE_TOKEN_KEY) ?? "");
  const [email, setEmail] = useState(() => localStorage.getItem(STORAGE_EMAIL_KEY) ?? "");
  const [language, setLanguage] = useState(() => {
    const storedLanguage = localStorage.getItem(STORAGE_LANGUAGE_KEY);
    return LANGUAGE_OPTIONS.includes(storedLanguage) ? storedLanguage : "ru";
  });
  const [theme, setTheme] = useState(getInitialTheme);
  const [authMode, setAuthMode] = useState("login");
  const [authScreen, setAuthScreen] = useState("welcome");
  const [showInitialPage, setShowInitialPage] = useState(false);
  const [showIntroSplash, setShowIntroSplash] = useState(() => !localStorage.getItem(STORAGE_TOKEN_KEY));
  const [sectionGuides, setSectionGuides] = useState(getDefaultGuideState);
  const [activeSection, setActiveSection] = useState("home");
  const [authForm, setAuthForm] = useState({ email: "", password: "" });
  const [organizationForm, setOrganizationForm] = useState({ name: "" });
  const [editingOrganizationId, setEditingOrganizationId] = useState("");
  const [organizations, setOrganizations] = useState([]);
  const [selectedOrganizationId, setSelectedOrganizationId] = useState("");
  const [filters, setFilters] = useState({ startDate: "", endDate: "" });
  const [analysis, setAnalysis] = useState(() => {
    const raw = localStorage.getItem(STORAGE_ANALYSIS_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw);
    } catch {
      return null;
    }
  });
  const [analysisWorkspaces, setAnalysisWorkspaces] = useState([]);
  const [selectedAnalysisId, setSelectedAnalysisId] = useState("");
  const [analysisCache, setAnalysisCache] = useState({});
  const [importFiles, setImportFiles] = useState([]);
  const [uploadInputKey, setUploadInputKey] = useState(0);
  const [currencyOptions, setCurrencyOptions] = useState([]);
  const [currencyBase, setCurrencyBase] = useState("USD");
  const [currencyQuote, setCurrencyQuote] = useState("RUB");
  const [currencyPeriod, setCurrencyPeriod] = useState(getDefaultCurrencyPeriod);
  const [currencySeries, setCurrencySeries] = useState([]);
  const [marketOverview, setMarketOverview] = useState(null);
  const [entrepreneurIin, setEntrepreneurIin] = useState("");
  const [entrepreneurSearch, setEntrepreneurSearch] = useState(null);
  const [notices, setNotices] = useState(() => ({
    auth: createNotice("info", messages[language]?.status.initial ?? messages.ru.status.initial),
    home: null,
    ip: null,
    analytics: null,
    import: null,
    currencyMarket: null,
    companiesMarket: null,
    report: null
  }));
  const [busy, setBusy] = useState({
    auth: false,
    organizations: false,
    analytics: false,
    analysisWorkspaces: false,
    comparison: false,
    ipRegistry: false,
    import: false,
    currencyMarket: false,
    companiesMarket: false,
    report: false,
    entrepreneurReport: false
  });
  const noticeTimeoutsRef = useRef({});

  const copy = messages[language] ?? messages.ru;
  const navItems = [
    { id: "home", label: copy.nav.home },
    { id: "ip", label: language === "en" ? "IE Registry" : "ИП" },
    { id: "analytics", label: copy.nav.analytics },
    { id: "reports", label: copy.nav.reports }
  ];

  const selectedOrganization = useMemo(
    () => organizations.find((organization) => organization.id === selectedOrganizationId) ?? null,
    [organizations, selectedOrganizationId]
  );
  const selectedAnalysisWorkspace = useMemo(
    () => analysisWorkspaces.find((workspace) => workspace.id === selectedAnalysisId) ?? null,
    [analysisWorkspaces, selectedAnalysisId]
  );

  useEffect(() => {
    document.documentElement.lang = language;
    document.documentElement.dataset.theme = theme;
    document.title = copy.meta.pageTitle;
    localStorage.setItem(STORAGE_LANGUAGE_KEY, language);
    localStorage.setItem(STORAGE_THEME_KEY, theme);
  }, [copy.meta.pageTitle, language, theme]);

  useEffect(() => {
    if (token) localStorage.setItem(STORAGE_TOKEN_KEY, token);
    else localStorage.removeItem(STORAGE_TOKEN_KEY);
  }, [token]);

  useEffect(() => {
    if (email) localStorage.setItem(STORAGE_EMAIL_KEY, email);
    else localStorage.removeItem(STORAGE_EMAIL_KEY);
  }, [email]);

  useEffect(() => {
    if (analysis) localStorage.setItem(STORAGE_ANALYSIS_KEY, JSON.stringify(analysis));
    else localStorage.removeItem(STORAGE_ANALYSIS_KEY);
  }, [analysis]);

  useEffect(() => {
    if (!token) {
      setOrganizations([]);
      setSelectedOrganizationId("");
      setAnalysis(null);
      setAnalysisWorkspaces([]);
      setSelectedAnalysisId("");
      setAnalysisCache({});
      setEntrepreneurIin("");
      setEntrepreneurSearch(null);
      setCurrencySeries([]);
      setMarketOverview(null);
      setImportFiles([]);
      setNotices((current) => ({
        ...current,
        home: null,
        ip: null,
        analytics: null,
        import: null,
        currencyMarket: null,
        companiesMarket: null,
        report: null
      }));
      setSectionGuides(getDefaultGuideState());
      return;
    }
    loadOrganizations();
    loadCurrencyOptions();
  }, [token, language]);

  useEffect(() => {
    if (!token || !email) {
      setSectionGuides(getDefaultGuideState());
      return;
    }

    const storageKey = getSectionGuideStorageKey(email);
    setSectionGuides(parseGuideState(localStorage.getItem(storageKey)));
  }, [email, token]);

  useEffect(() => {
    if (!token || !selectedOrganizationId) {
      setAnalysis(null);
      setAnalysisWorkspaces([]);
      setSelectedAnalysisId("");
      setAnalysisCache({});
      return;
    }
    loadAnalysisWorkspaces();
  }, [token, selectedOrganizationId, language]);

  useEffect(() => {
    if (!token || !selectedOrganizationId || !selectedAnalysisId) {
      setAnalysis(null);
      return;
    }
    loadAnalytics(true, selectedAnalysisId);
  }, [token, selectedOrganizationId, selectedAnalysisId, language]);

  useEffect(() => {
    if (!token || currencyOptions.length === 0 || activeSection !== "home") return;
    loadCurrencySeries(true);
  }, [token, activeSection, currencyBase, currencyQuote, currencyPeriod.startDate, currencyPeriod.endDate, currencyOptions.length, language]);

  useEffect(() => {
    if (!token || activeSection !== "home") return;
    loadCompaniesOverview(true);
  }, [token, activeSection, language]);

  useEffect(() => {
    if (!token || activeSection !== "home") {
      return undefined;
    }

    function refreshHomeMarketData() {
      if (currencyOptions.length > 0) {
        loadCurrencySeries(true);
      }

      loadCompaniesOverview(true);
    }

    const intervalId = window.setInterval(refreshHomeMarketData, MARKET_AUTO_REFRESH_INTERVAL_MS);

    return () => {
      window.clearInterval(intervalId);
    };
  }, [
    activeSection,
    currencyBase,
    currencyOptions.length,
    currencyPeriod.endDate,
    currencyPeriod.startDate,
    currencyQuote,
    language,
    token
  ]);

  useEffect(() => {
    Object.entries(notices).forEach(([scope, notice]) => {
      const existingTimeout = noticeTimeoutsRef.current[scope];

      if (!notice?.id) {
        if (existingTimeout) {
          window.clearTimeout(existingTimeout.timeoutId);
          delete noticeTimeoutsRef.current[scope];
        }
        return;
      }

      if (existingTimeout?.id === notice.id) {
        return;
      }

      if (existingTimeout) {
        window.clearTimeout(existingTimeout.timeoutId);
      }

      noticeTimeoutsRef.current[scope] = {
        id: notice.id,
        timeoutId: window.setTimeout(() => {
          setNotices((current) =>
            current[scope]?.id === notice.id
              ? { ...current, [scope]: null }
              : current
          );
        }, 5000)
      };
    });
  }, [notices]);

  useEffect(() => {
    return () => {
      Object.values(noticeTimeoutsRef.current).forEach((entry) => {
        window.clearTimeout(entry.timeoutId);
      });
      noticeTimeoutsRef.current = {};
    };
  }, []);

  useEffect(() => {
    if (!showIntroSplash) {
      return undefined;
    }

    const timeoutId = window.setTimeout(() => {
      setShowIntroSplash(false);
    }, 2200);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [showIntroSplash]);

  function createApiClient() {
    return axios.create({
      baseURL: API_BASE_URL,
      headers: { "Accept-Language": language, ...(token ? { Authorization: `Bearer ${token}` } : {}) }
    });
  }

  function setBusyFlag(key, value) {
    setBusy((current) => ({ ...current, [key]: value }));
  }

  function setNotice(scope, tone, message) {
    setNotices((current) => ({ ...current, [scope]: createNotice(tone, message) }));
  }

  function setLocalizedNotice(scope, tone, group, key, params) {
    setNotice(scope, tone, resolveTemplate(copy[group]?.[key] ?? "", params));
  }

  function setErrorNotice(error, fallbackKey, scope) {
    if (error?.response?.status === 401) {
      setAuthScreen("auth");
      setToken("");
      setEmail("");
      setLocalizedNotice("auth", "error", "status", "sessionExpired");
      return;
    }
    const apiMessage = error?.response?.data?.message ?? error?.response?.data?.title;
    if (apiMessage) {
      setNotice(scope, "error", apiMessage);
      return;
    }
    setLocalizedNotice(scope, "error", "errors", fallbackKey);
  }

  function buildDateParams() {
    const params = {};
    if (filters.startDate) params.startDate = filters.startDate;
    if (filters.endDate) params.endDate = filters.endDate;
    return params;
  }

  async function loadOrganizations(preferredOrganizationId) {
    setBusyFlag("organizations", true);
    try {
      const response = await createApiClient().get("/organizations");
      const items = response.data ?? [];
      setOrganizations(items);
      setSelectedOrganizationId((current) => {
        if (preferredOrganizationId && items.some((item) => item.id === preferredOrganizationId)) return preferredOrganizationId;
        if (current && items.some((item) => item.id === current)) return current;
        return items[0]?.id ?? "";
      });
      if (items.length === 0) setLocalizedNotice("home", "info", "status", "createFirstCompany");
    } catch (error) {
      setErrorNotice(error, "loadCompanies", "home");
    } finally {
      setBusyFlag("organizations", false);
    }
  }

  async function loadAnalysisWorkspaces(preferredAnalysisId) {
    if (!selectedOrganizationId) return;
    setBusyFlag("analysisWorkspaces", true);
    try {
      const response = await createApiClient().get("/analysisworkspaces", { params: { organizationId: selectedOrganizationId } });
      const items = response.data ?? [];
      setAnalysisWorkspaces(items);
      setSelectedAnalysisId((current) => {
        if (preferredAnalysisId && items.some((item) => item.id === preferredAnalysisId)) return preferredAnalysisId;
        if (current && items.some((item) => item.id === current)) return current;
        return items[0]?.id ?? "";
      });
    } catch (error) {
      setErrorNotice(error, "loadAnalytics", "analytics");
    } finally {
      setBusyFlag("analysisWorkspaces", false);
    }
  }

  async function loadAnalytics(silent = false, analysisIdOverride) {
    const activeAnalysisId = analysisIdOverride ?? selectedAnalysisId;
    if (!selectedOrganizationId || !activeAnalysisId) {
      setAnalysis(null);
      return;
    }
    setBusyFlag("analytics", true);
    try {
      const response = await createApiClient().get("/analytics/deep-dive", {
        params: { organizationId: selectedOrganizationId, analysisWorkspaceId: activeAnalysisId, ...buildDateParams() }
      });
      setAnalysis(response.data ?? null);
      setAnalysisCache((current) => ({ ...current, [activeAnalysisId]: response.data ?? null }));
      setNotice("analytics", null, null);
      if (!silent) setNotice("import", null, null);
    } catch (error) {
      setErrorNotice(error, "loadAnalytics", "analytics");
    } finally {
      setBusyFlag("analytics", false);
    }
  }

  async function loadCurrencyOptions() {
    setBusyFlag("currencyMarket", true);
    try {
      const response = await createApiClient().get("/market/currencies");
      const options = response.data ?? [];
      setCurrencyOptions(options);
      if (!options.some((item) => item.code === currencyBase)) {
        setCurrencyBase(options.find((item) => item.code === "USD")?.code ?? options[0]?.code ?? "USD");
      }
      if (!options.some((item) => item.code === currencyQuote)) {
        setCurrencyQuote(options.find((item) => item.code === "RUB")?.code ?? options[1]?.code ?? "EUR");
      }
      setNotice("currencyMarket", null, null);
    } catch (error) {
      setErrorNotice(error, "loadMarket", "currencyMarket");
    } finally {
      setBusyFlag("currencyMarket", false);
    }
  }

  async function loadCurrencySeries(silent = false) {
    if (!currencyBase || !currencyQuote) return;
    setBusyFlag("currencyMarket", true);
    try {
      const params = {
        baseCurrency: currencyBase,
        quoteCurrency: currencyQuote
      };

      if (currencyPeriod.startDate) {
        params.startDate = currencyPeriod.startDate;
      }

      if (currencyPeriod.endDate) {
        params.endDate = currencyPeriod.endDate;
      }

      const response = await createApiClient().get("/market/currency-series", {
        params
      });
      setCurrencySeries(response.data?.points ?? []);
      setNotice("currencyMarket", null, null);
      if (!silent) setLocalizedNotice("currencyMarket", "success", "status", "currencyLoaded");
    } catch (error) {
      setErrorNotice(error, "loadMarket", "currencyMarket");
    } finally {
      setBusyFlag("currencyMarket", false);
    }
  }

  async function loadCompaniesOverview(silent = false) {
    setBusyFlag("companiesMarket", true);
    try {
      const response = await createApiClient().get("/market/companies");
      setMarketOverview(response.data ?? null);
      setNotice("companiesMarket", null, null);
      if (!silent) setLocalizedNotice("companiesMarket", "success", "status", "companiesLoaded");
    } catch (error) {
      setErrorNotice(error, "loadMarket", "companiesMarket");
    } finally {
      setBusyFlag("companiesMarket", false);
    }
  }

  async function handleAuthSubmit(event) {
    event.preventDefault();
    setBusyFlag("auth", true);
    try {
      const api = createApiClient();
      if (authMode === "register") await api.post("/auth/register", authForm);
      const response = await api.post("/auth/login", authForm);
      setToken(response.data.token);
      setEmail(response.data.email);
      setActiveSection("home");
      setAuthScreen("auth");
      setNotice("auth", null, null);
      setAuthForm((current) => ({ ...current, password: "" }));
    } catch (error) {
      setErrorNotice(error, "auth", "auth");
    } finally {
      setBusyFlag("auth", false);
    }
  }

  async function handleSaveOrganization(event) {
    event.preventDefault();
    const name = organizationForm.name.trim();
    if (!name) {
      setLocalizedNotice("home", "warning", "status", "enterCompanyName");
      return;
    }
    setBusyFlag("organizations", true);
    try {
      const api = createApiClient();
      if (editingOrganizationId) {
        await api.put(`/organizations/${editingOrganizationId}`, { name });
        await loadOrganizations(editingOrganizationId);
        setLocalizedNotice("home", "success", "status", "companyUpdated", { name });
      } else {
        const response = await api.post("/organizations", { name });
        await loadOrganizations(response.data.id);
        setLocalizedNotice("home", "success", "status", "companyCreated", { name });
      }
      setOrganizationForm({ name: "" });
      setEditingOrganizationId("");
    } catch (error) {
      setErrorNotice(error, "saveCompany", "home");
    } finally {
      setBusyFlag("organizations", false);
    }
  }

  function handleEditOrganization(organization) {
    setEditingOrganizationId(organization.id);
    setOrganizationForm({ name: organization.name });
    setNotice("home", null, null);
  }

  function handleCancelEdit() {
    setEditingOrganizationId("");
    setOrganizationForm({ name: "" });
  }

  async function handleDeleteOrganization(organization) {
    if (!window.confirm(copy.home.deleteConfirm)) return;
    setBusyFlag("organizations", true);
    try {
      await createApiClient().delete(`/organizations/${organization.id}`);
      await loadOrganizations();
      if (editingOrganizationId === organization.id) handleCancelEdit();
      setLocalizedNotice("home", "success", "status", "companyDeleted");
    } catch (error) {
      setErrorNotice(error, "deleteCompany", "home");
    } finally {
      setBusyFlag("organizations", false);
    }
  }

  async function handleCreateAnalysisWorkspace() {
    if (!selectedOrganizationId) {
      setLocalizedNotice("analytics", "warning", "status", "selectCompanyFirst");
      return null;
    }
    setBusyFlag("analysisWorkspaces", true);
    try {
      const response = await createApiClient().post("/analysisworkspaces", { organizationId: selectedOrganizationId });
      const createdWorkspace = response.data;
      setAnalysisWorkspaces((current) => [...current, createdWorkspace]);
      setSelectedAnalysisId(createdWorkspace.id);
      setAnalysis(null);
      setImportFiles([]);
      setUploadInputKey((current) => current + 1);
      setFilters({ startDate: "", endDate: "" });
      setNotice("analytics", null, null);
      return createdWorkspace;
    } catch (error) {
      setErrorNotice(error, "loadAnalytics", "analytics");
      return null;
    } finally {
      setBusyFlag("analysisWorkspaces", false);
    }
  }

  async function handleRenameAnalysisWorkspace(workspaceId, name) {
    if (!name.trim()) return false;
    setBusyFlag("analysisWorkspaces", true);
    try {
      const response = await createApiClient().patch(`/analysisworkspaces/${workspaceId}`, { name });
      setAnalysisWorkspaces((current) => current.map((workspace) => (workspace.id === workspaceId ? response.data : workspace)));
      return true;
    } catch (error) {
      setErrorNotice(error, "loadAnalytics", "analytics");
      return false;
    } finally {
      setBusyFlag("analysisWorkspaces", false);
    }
  }

  async function handleDeleteAnalysisWorkspace(workspaceId) {
    setBusyFlag("analysisWorkspaces", true);
    try {
      const response = await createApiClient().delete(`/analysisworkspaces/${workspaceId}`);
      const payload = response.data ?? {};
      setAnalysisWorkspaces(payload.remainingWorkspaces ?? []);
      setAnalysisCache((current) => {
        const next = { ...current };
        delete next[workspaceId];
        return next;
      });
      setSelectedAnalysisId(payload.replacementWorkspaceId ?? payload.remainingWorkspaces?.[0]?.id ?? "");
      if (selectedAnalysisId === workspaceId) {
        setAnalysis(null);
        setImportFiles([]);
        setUploadInputKey((current) => current + 1);
      }
      return true;
    } catch (error) {
      setErrorNotice(error, "loadAnalytics", "analytics");
      return false;
    } finally {
      setBusyFlag("analysisWorkspaces", false);
    }
  }

  async function handleCompareAnalyses(workspaceIds) {
    if (!selectedOrganizationId || workspaceIds.length < 2) return [];
    setBusyFlag("comparison", true);
    try {
      const api = createApiClient();
      const cacheUpdates = {};
      const results = await Promise.all(
        workspaceIds.map(async (workspaceId) => {
          const workspace = analysisWorkspaces.find((item) => item.id === workspaceId);
          const cachedAnalysis = workspaceId === selectedAnalysisId ? analysis : analysisCache[workspaceId];
          if (cachedAnalysis) {
            return { id: workspaceId, name: workspace?.name ?? workspaceId, analytics: cachedAnalysis };
          }
          const response = await api.get("/analytics/deep-dive", {
            params: { organizationId: selectedOrganizationId, analysisWorkspaceId: workspaceId }
          });
          cacheUpdates[workspaceId] = response.data ?? null;
          return { id: workspaceId, name: workspace?.name ?? workspaceId, analytics: response.data ?? null };
        })
      );
      if (Object.keys(cacheUpdates).length > 0) {
        setAnalysisCache((current) => ({ ...current, ...cacheUpdates }));
      }
      return results.filter((item) => item.analytics);
    } catch (error) {
      setErrorNotice(error, "loadAnalytics", "analytics");
      return [];
    } finally {
      setBusyFlag("comparison", false);
    }
  }

  function handleCurrencyPeriodChange(key, value) {
    setCurrencyPeriod((current) => {
      const nextPeriod = { ...current, [key]: value };

      if (nextPeriod.startDate && nextPeriod.endDate && nextPeriod.startDate > nextPeriod.endDate) {
        if (key === "startDate") {
          nextPeriod.endDate = value;
        } else if (key === "endDate") {
          nextPeriod.startDate = value;
        }
      }

      return nextPeriod;
    });
  }

  function handleFilesSelected(files) {
    const map = new Map(importFiles.map((file) => [`${file.name}-${file.size}-${file.lastModified}`, file]));
    files.forEach((file) => {
      map.set(`${file.name}-${file.size}-${file.lastModified}`, file);
    });

    const nextFiles = Array.from(map.values());
    if (nextFiles.length > MAX_IMPORT_FILES) {
      setImportFiles(nextFiles.slice(0, MAX_IMPORT_FILES));
      setNotice("import", "warning", resolveTemplate(copy.analytics.maxFiles, { count: MAX_IMPORT_FILES }));
      return;
    }

    setImportFiles(nextFiles);
    setNotice("import", null, null);
  }

  function handleRemoveFile(fileToRemove) {
    setImportFiles((current) =>
      current.filter(
        (file) =>
          `${file.name}-${file.size}-${file.lastModified}` !==
          `${fileToRemove.name}-${fileToRemove.size}-${fileToRemove.lastModified}`
      )
    );
  }

  async function handleRunImport() {
    if (!selectedOrganizationId) {
      setLocalizedNotice("import", "warning", "status", "selectCompanyFirst");
      return;
    }
    if (!selectedAnalysisId) {
      setLocalizedNotice("analytics", "warning", "status", "selectCompanyFirst");
      return;
    }
    if (!importFiles.length) {
      setLocalizedNotice("import", "warning", "status", "chooseFiles");
      return;
    }
    if (importFiles.length > MAX_IMPORT_FILES) {
      setNotice("import", "warning", resolveTemplate(copy.analytics.maxFiles, { count: MAX_IMPORT_FILES }));
      return;
    }
    setBusyFlag("import", true);
    try {
      const formData = new FormData();
      formData.append("organizationId", selectedOrganizationId);
      formData.append("analysisWorkspaceId", selectedAnalysisId);
      importFiles.forEach((file) => formData.append("files", file));
      const response = await createApiClient().post("/import/files", formData);
      setAnalysis(response.data.analytics ?? null);
      setAnalysisCache((current) => ({ ...current, [selectedAnalysisId]: response.data.analytics ?? null }));
      setImportFiles([]);
      setUploadInputKey((current) => current + 1);
      setLocalizedNotice("import", "success", "status", "importSuccess", {
        files: response.data.fileCount ?? importFiles.length,
        count: response.data.count ?? 0
      });
      setNotice("report", null, null);
    } catch (error) {
      setErrorNotice(error, "importFiles", "import");
    } finally {
      setBusyFlag("import", false);
    }
  }

  async function handleDownloadReport() {
    if (!selectedOrganization || !selectedAnalysisWorkspace || !analysis) {
      setLocalizedNotice("report", "warning", "status", "selectCompanyFirst");
      return;
    }
    setBusyFlag("report", true);
    try {
      const response = await createApiClient().post(
        "/reports/analytics-pdf",
        {
          organizationName: selectedOrganization.name,
          analysisName: selectedAnalysisWorkspace.name,
          generatedFor: email,
          language,
          periodStart: filters.startDate || null,
          periodEnd: filters.endDate || null,
          analytics: analysis
        },
        { responseType: "blob" }
      );
      const contentDisposition = response.headers["content-disposition"] ?? "";
      const fileNameMatch = contentDisposition.match(/filename="?([^"]+)"?/i);
      const fileName = fileNameMatch?.[1] ?? "BizAnalitics report.pdf";
      const blobUrl = window.URL.createObjectURL(response.data);
      const link = document.createElement("a");
      link.href = blobUrl;
      link.download = fileName;
      document.body.append(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(blobUrl);
      setLocalizedNotice("report", "success", "status", "reportReady");
    } catch (error) {
      setErrorNotice(error, "downloadReport", "report");
    } finally {
      setBusyFlag("report", false);
    }
  }

  async function handleSearchEntrepreneur() {
    const normalizedIin = entrepreneurIin.replace(/\D/g, "");
    if (normalizedIin.length !== 12) {
      setNotice("ip", "warning", language === "en" ? "Enter a valid 12-digit Kazakhstan IIN." : "Введите корректный 12-значный ИИН Казахстана.");
      return;
    }

    setBusyFlag("ipRegistry", true);
    try {
      const response = await createApiClient().get("/individualentrepreneurs/search", {
        params: { iin: normalizedIin }
      });
      const payload = response.data ?? null;
      setEntrepreneurSearch(payload);

      if (payload?.found) {
        setNotice("ip", "success", payload.message ?? (language === "en" ? "Entrepreneur found in the registry." : "ИП найден в реестре."));
        setNotice("analytics", "success", language === "en" ? "Entrepreneur data has been loaded into analytics." : "Данные ИП загружены в аналитику.");
        setActiveSection("analytics");
      } else {
        setNotice("ip", "warning", payload?.message ?? (language === "en" ? "Entrepreneur not found." : "ИП не найден."));
      }
    } catch (error) {
      setEntrepreneurSearch(null);
      setErrorNotice(error, "loadAnalytics", "ip");
    } finally {
      setBusyFlag("ipRegistry", false);
    }
  }

  async function handleDownloadEntrepreneurForm(formCode) {
    if (!entrepreneurSearch?.registry) {
      setNotice("report", "warning", language === "en" ? "Search for an entrepreneur first." : "Сначала найдите ИП по ИИН.");
      return;
    }

    setBusyFlag("entrepreneurReport", true);
    try {
      const response = await createApiClient().post(
        "/reports/entrepreneur-form-pdf",
        {
          formCode,
          language,
          generatedFor: email,
          registry: entrepreneurSearch.registry
        },
        { responseType: "blob" }
      );
      const contentDisposition = response.headers["content-disposition"] ?? "";
      const fileNameMatch = contentDisposition.match(/filename=\"?([^\"]+)\"?/i);
      const fileName = fileNameMatch?.[1] ?? `Entrepreneur-${formCode}.pdf`;
      const blobUrl = window.URL.createObjectURL(response.data);
      const link = document.createElement("a");
      link.href = blobUrl;
      link.download = fileName;
      document.body.append(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(blobUrl);
      setLocalizedNotice("report", "success", "status", "reportReady");
    } catch (error) {
      setErrorNotice(error, "downloadReport", "report");
    } finally {
      setBusyFlag("entrepreneurReport", false);
    }
  }

  async function handleResetAnalytics() {
    if (!selectedOrganizationId || !selectedAnalysisId) {
      setLocalizedNotice("analytics", "warning", "status", "selectCompanyFirst");
      return;
    }
    setBusyFlag("analytics", true);
    try {
      const response = await createApiClient().delete("/analytics/reset", {
        params: { organizationId: selectedOrganizationId, analysisWorkspaceId: selectedAnalysisId }
      });
      setAnalysis(null);
      setAnalysisCache((current) => ({ ...current, [selectedAnalysisId]: null }));
      setImportFiles([]);
      setUploadInputKey((current) => current + 1);
      setFilters({ startDate: "", endDate: "" });
      setNotice(
        "analytics",
        "success",
        response.data?.message ??
          (language === "en"
            ? "Analytics was reset. You can upload new files."
            : "Аналитика сброшена. Можно загружать новые файлы.")
      );
      setNotice(
        "import",
        "info",
        language === "en"
          ? "Previous records were removed from the selected analysis. Upload new files for a fresh result."
          : "Предыдущие записи удалены из выбранного анализа. Загрузите новые файлы для нового результата."
      );
      setNotice("report", null, null);
    } catch (error) {
      setErrorNotice(error, "loadAnalytics", "analytics");
    } finally {
      setBusyFlag("analytics", false);
    }
  }

  function handleLogout() {
    setShowInitialPage(false);
    setAuthScreen("welcome");
    setToken("");
    setEmail("");
    setOrganizations([]);
    setSelectedOrganizationId("");
      setAnalysis(null);
      setAnalysisWorkspaces([]);
      setSelectedAnalysisId("");
      setAnalysisCache({});
      setEntrepreneurIin("");
      setEntrepreneurSearch(null);
      setImportFiles([]);
      setEditingOrganizationId("");
      setOrganizationForm({ name: "" });
    setActiveSection("home");
      setNotices({
        auth: createNotice("info", copy.status.loggedOut),
        home: null,
        ip: null,
        analytics: null,
        import: null,
        currencyMarket: null,
      companiesMarket: null,
      report: null
    });
  }

  function openAuth(mode = "login") {
    setAuthMode(mode);
    setAuthScreen("auth");
    setNotice("auth", "info", copy.status.initial);
  }

  function renderLanguageButton() {
    const nextLanguage = language === "ru" ? "en" : "ru";
    return (
      <button aria-label={copy.meta.language} className="lang-button" onClick={() => setLanguage(nextLanguage)} title={copy.meta.language} type="button">
        {language.toUpperCase()}
      </button>
    );
  }

  function renderThemeButton() {
    return (
      <button aria-label={copy.meta.theme} className="icon-button" onClick={() => setTheme((current) => (current === "light" ? "dark" : "light"))} title={copy.meta.theme} type="button">
        {theme === "light" ? "\u2600" : "\u263E"}
      </button>
    );
  }

  function handleDismissSectionGuide(section) {
    setSectionGuides((current) => {
      const next = {
        ...current,
        [section]: false
      };

      if (email) {
        localStorage.setItem(getSectionGuideStorageKey(email), JSON.stringify(next));
      }

      return next;
    });
  }

  if (!token) {
    return (
      <div className="auth-shell">
        <div className="auth-backdrop" />
        {authScreen === "welcome" && showIntroSplash ? (
          <IntroSplash language={language} />
        ) : authScreen === "welcome" ? (
          <Suspense fallback={<SectionLoading language={language} />}>
            <WelcomeSection language={language} onStart={() => openAuth("login")} toolbar={<div className="compact-toolbar">{renderThemeButton()}{renderLanguageButton()}</div>} />
          </Suspense>
        ) : (
          <section className="auth-card auth-card-centered">
            <div className="auth-toolbar auth-toolbar-end"><div className="compact-toolbar">{renderThemeButton()}{renderLanguageButton()}</div></div>
            <div className="auth-content">
              <div className="auth-copy">
                <h1>{copy.auth.title}</h1>
                <p>{copy.auth.subtitle}</p>
                <div className="auth-note">{copy.auth.backgroundNote}</div>
              </div>
              <div className="stack">
                <form className="stack" onSubmit={handleAuthSubmit}>
                  <Field label={copy.auth.emailLabel} placeholder={copy.auth.emailPlaceholder} type="email" value={authForm.email} onChange={(value) => setAuthForm((current) => ({ ...current, email: value }))} />
                  <Field label={copy.auth.passwordLabel} placeholder={copy.auth.passwordPlaceholder} type="password" value={authForm.password} onChange={(value) => setAuthForm((current) => ({ ...current, password: value }))} />
                  <button className="primary-button primary-button-large" disabled={busy.auth} type="submit">
                    {busy.auth ? copy.auth.submitting : authMode === "login" ? copy.auth.loginAction : copy.auth.registerAction}
                  </button>
                </form>
                <InlineNotice notice={notices.auth} />
                <button className="text-button" onClick={() => setAuthMode((current) => (current === "login" ? "register" : "login"))} type="button">
                  {authMode === "login" ? copy.auth.switchToRegister : copy.auth.switchToLogin}
                </button>
                <button className="text-button" onClick={() => setAuthScreen("welcome")} type="button">
                  {language === "en" ? "Back to preview" : "Вернуться к приветственной странице"}
                </button>
              </div>
            </div>
          </section>
        )}
      </div>
    );
  }

  if (showInitialPage) {
    return (
      <div className="auth-shell">
        <div className="auth-backdrop" />
        <Suspense fallback={<SectionLoading language={language} />}>
          <WelcomeSection
            language={language}
            onStart={() => {
              setShowInitialPage(false);
              setActiveSection("analytics");
            }}
            toolbar={<div className="compact-toolbar">{renderThemeButton()}{renderLanguageButton()}</div>}
          />
        </Suspense>
      </div>
    );
  }

  return (
    <div className="workspace-shell">
      <header className="topbar topbar-compact">
        <button
          aria-label={language === "en" ? "Open welcome page" : "Открыть приветственную страницу"}
          className="topbar-brand topbar-logo-button"
          onClick={() => setShowInitialPage(true)}
          type="button"
        >
          <BrandLogo className="brand-logo-header" />
        </button>
        <nav className="topbar-nav">
          {navItems.map((item) => (
            <button key={item.id} className={`nav-button ${activeSection === item.id ? "is-active" : ""}`} onClick={() => setActiveSection(item.id)} type="button">
              {item.label}
            </button>
          ))}
        </nav>
        <div className="topbar-actions">{renderThemeButton()}{renderLanguageButton()}<button className="ghost-button ghost-button-compact" onClick={handleLogout} type="button">{copy.layout.logout}</button></div>
      </header>
      <main className="workspace-main">
        {activeSection === "home" ? (
          <Suspense fallback={<SectionLoading language={language} />}>
            <HomeSection busyOrganizations={busy.organizations} companiesNotice={notices.companiesMarket} copy={copy} currencyBase={currencyBase} currencyNotice={notices.currencyMarket} currencyOptions={currencyOptions} currencyPeriod={currencyPeriod} currencyQuote={currencyQuote} currencySeries={currencySeries} formNotice={notices.home} isEditingOrganization={Boolean(editingOrganizationId)} language={language} marketOverview={marketOverview} onCancelEdit={handleCancelEdit} onCurrencyBaseChange={setCurrencyBase} onCurrencyPeriodChange={handleCurrencyPeriodChange} onCurrencyQuoteChange={setCurrencyQuote} onDeleteOrganization={handleDeleteOrganization} onDismissGuide={() => handleDismissSectionGuide("home")} onEditOrganization={handleEditOrganization} onOrganizationNameChange={(value) => setOrganizationForm({ name: value })} onSaveOrganization={handleSaveOrganization} onSelectOrganization={setSelectedOrganizationId} organizationForm={organizationForm} organizations={organizations} selectedOrganizationId={selectedOrganizationId} showGuide={sectionGuides.home} />
          </Suspense>
        ) : null}
        {activeSection === "ip" ? (
          <Suspense fallback={<SectionLoading language={language} />}>
            <IpSection
              busy={busy.ipRegistry}
              iin={entrepreneurIin}
              language={language}
              notice={notices.ip}
              onIinChange={setEntrepreneurIin}
              onOpenAnalytics={() => setActiveSection("analytics")}
              onSearch={handleSearchEntrepreneur}
              result={entrepreneurSearch}
            />
          </Suspense>
        ) : null}
        {activeSection === "analytics" ? (
          <Suspense fallback={<SectionLoading language={language} />}>
            <AnalyticsSection analysis={analysis} analysisWorkspaces={analysisWorkspaces} analyticsNotice={notices.analytics} busy={busy} comparisonBusy={busy.comparison} copy={copy} filters={filters} importFiles={importFiles} importNotice={notices.import} individualEntrepreneurSearch={entrepreneurSearch} language={language} onCompareAnalyses={handleCompareAnalyses} onCreateAnalysis={handleCreateAnalysisWorkspace} onDeleteAnalysis={handleDeleteAnalysisWorkspace} onDismissOnboarding={() => handleDismissSectionGuide("analytics")} onFilesSelect={handleFilesSelected} onFilterChange={(key, value) => setFilters((current) => ({ ...current, [key]: value }))} onRefresh={() => loadAnalytics()} onRenameAnalysis={handleRenameAnalysisWorkspace} onResetAnalytics={handleResetAnalytics} onRemoveFile={handleRemoveFile} onRunImport={handleRunImport} onSelectAnalysis={setSelectedAnalysisId} selectedAnalysis={selectedAnalysisWorkspace} selectedAnalysisId={selectedAnalysisId} selectedOrganization={selectedOrganization} selectedOrganizationId={selectedOrganizationId} showOnboarding={sectionGuides.analytics} uploadInputKey={uploadInputKey} />
          </Suspense>
        ) : null}
        {activeSection === "reports" ? (
          <Suspense fallback={<SectionLoading language={language} />}>
            <ReportsSection analysis={analysis} analysisWorkspaces={analysisWorkspaces} busyReport={busy.report || busy.entrepreneurReport} copy={copy} individualEntrepreneurSearch={entrepreneurSearch} language={language} onDismissGuide={() => handleDismissSectionGuide("reports")} onDownloadEntrepreneurForm={handleDownloadEntrepreneurForm} onDownloadReport={handleDownloadReport} onSelectAnalysis={setSelectedAnalysisId} reportNotice={notices.report} selectedAnalysis={selectedAnalysisWorkspace} selectedAnalysisId={selectedAnalysisId} selectedOrganization={selectedOrganization} showGuide={sectionGuides.reports} />
          </Suspense>
        ) : null}
      </main>
      <SiteFooter
        activeSection={activeSection}
        className="site-footer-shell"
        language={language}
        navItems={navItems}
        onNavigate={setActiveSection}
        onOpenWelcome={() => setShowInitialPage(true)}
      />
    </div>
  );
}

export default App;
