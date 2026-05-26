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

export function getMetricDescription(metricId, language) {
  const isEnglish = language === "en";

  const descriptions = {
    totalRevenue: isEnglish
      ? "The total revenue is the sum of all sales amounts included in the current analysis and selected period."
      : "Общая выручка рассчитывается как сумма всех денежных значений продаж, попавших в текущий анализ и выбранный период.",
    salesCount: isEnglish
      ? "Sales count shows how many sales records were included after import, classification and period filtering."
      : "Количество продаж показывает, сколько записей о продажах попало в анализ после импорта, классификации и фильтра по периоду.",
    totalQuantity: isEnglish
      ? "Total quantity is the sum of all quantity values across the selected sales records."
      : "Общий объем рассчитывается как сумма всех количественных значений по выбранным записям продаж.",
    averageCheck: isEnglish
      ? "Average check is calculated as total revenue divided by the number of sales records in the current analysis."
      : "Средний чек рассчитывается как общая выручка, деленная на количество записей о продажах в текущем анализе.",
    financialRevenue: isEnglish
      ? "Revenue is the sum of all income values extracted from the detected financial periods."
      : "Доходы рассчитываются как сумма всех значений дохода, извлеченных из найденных финансовых периодов.",
    financialExpenses: isEnglish
      ? "Expenses are the sum of all cost values detected in the financial report."
      : "Расходы рассчитываются как сумма всех значений затрат, найденных в финансовом отчете.",
    financialProfit: isEnglish
      ? "Profit is the difference between revenue and expenses across all detected periods."
      : "Прибыль рассчитывается как разница между доходами и расходами по всем найденным периодам.",
    profitability: isEnglish
      ? "Profitability is calculated as profit divided by revenue and then converted into a percentage."
      : "Рентабельность рассчитывается как прибыль, деленная на доходы, после чего результат переводится в проценты.",
    averageScore: isEnglish
      ? "Average score is the mean value of all detected grades or scores included in the education analysis."
      : "Средний балл рассчитывается как среднее значение всех найденных оценок или баллов, попавших в анализ успеваемости.",
    bestStudent: isEnglish
      ? "Best student is the learner with the highest average score in the current dataset."
      : "Лучший ученик определяется как ученик с самым высоким средним баллом в текущем наборе данных.",
    worstStudent: isEnglish
      ? "Weakest student is the learner with the lowest average score in the current dataset."
      : "Худший ученик определяется как ученик с самым низким средним баллом в текущем наборе данных.",
    successRate: isEnglish
      ? "Success rate shows the share of grades above the passing threshold. The platform uses 3+ for five-point grading and 60+ for hundred-point grading."
      : "Процент успеваемости показывает долю оценок выше порогового уровня. Для пятибалльной шкалы используется порог 3 и выше, для стобалльной шкалы — 60 и выше."
  };

  return descriptions[metricId] ?? "";
}

export function getAnalyticsText(language) {
  const isEnglish = language === "en";

  return {
    salesPanels: {
      revenue: isEnglish
        ? "The chart shows revenue by dates to help spot peaks, declines and the general pace of sales."
        : "График показывает выручку по датам, чтобы быстро увидеть пики, спады и общий темп продаж.",
      price: isEnglish
        ? "This trend tracks the average unit price by date and helps reveal price fluctuations inside the selected analysis."
        : "Этот тренд показывает среднюю цену единицы товара по датам и помогает заметить колебания цены внутри выбранного анализа.",
      sources: isEnglish
        ? "The pie chart shows which source files or datasets contributed the most to total revenue."
        : "Круговая диаграмма показывает, какой вклад файлы или источники данных внесли в общую выручку.",
      insights: isEnglish
        ? "Insight cards collect the main signals from the sales analysis: leaders, peaks, fluctuations and noticeable shifts."
        : "Карточки выводов собирают главные сигналы из аналитики продаж: лидеров, пики, колебания и заметные изменения.",
      products: isEnglish
        ? "The table highlights products with the highest revenue and volume so the leading positions are visible immediately."
        : "Таблица выделяет товары с наибольшей выручкой и объемом, чтобы лидирующие позиции были видны сразу."
    },
    financialPanels: {
      trend: isEnglish
        ? "This block compares revenue, expenses and profit by periods so the financial balance is easy to read."
        : "Этот блок сравнивает доходы, расходы и прибыль по периодам, чтобы финансовый баланс читался сразу.",
      forecast: isEnglish
        ? "The forecast extends the current profit dynamics to the next period and helps estimate the expected direction."
        : "Прогноз продолжает текущую динамику прибыли на следующий период и помогает оценить ожидаемое направление.",
      insights: isEnglish
        ? "The conclusion cards summarize the key financial signals: margin strength, unstable periods and growth points."
        : "Карточки выводов собирают ключевые финансовые сигналы: силу маржи, нестабильные периоды и точки роста."
    },
    educationPanels: {
      distribution: isEnglish
        ? "The pie chart shows how grades are distributed inside the report and which levels dominate the overall picture."
        : "Круговая диаграмма показывает, как распределены оценки в отчете и какие уровни преобладают в общей картине.",
      students: isEnglish
        ? "The student rating compares average scores so leaders and lagging students are visible at a glance."
        : "Рейтинг учеников сравнивает средние баллы, чтобы сразу были видны лидеры и отстающие.",
      subjects: isEnglish
        ? "The subject rating helps identify the strongest and weakest academic areas based on the average score."
        : "Рейтинг предметов помогает определить самые сильные и самые слабые учебные направления по среднему баллу.",
      forecast: isEnglish
        ? "The forecast table compares the current and expected average score for each student."
        : "Таблица прогноза сопоставляет текущий и ожидаемый средний балл по каждому ученику.",
      risks: isEnglish
        ? "The risk block highlights students whose current average score puts them in the attention zone."
        : "Блок риска выделяет учеников, чей текущий средний балл относит их в зону внимания.",
      insights: isEnglish
        ? "These recommendation cards summarize where additional attention can improve performance."
        : "Эти карточки рекомендаций показывают, где дополнительное внимание может улучшить успеваемость."
    },
    onboarding: {
      badge: isEnglish ? "Quick start" : "Быстрый старт",
      title: isEnglish ? "How to work with the analytics platform" : "Как работать с аналитической платформой",
      description: isEnglish
        ? "The platform becomes easier when you start from the main sections and the analyses sidebar."
        : "Платформа становится понятнее, если сразу ориентироваться на основные разделы и боковой список анализов.",
      noteTitle: isEnglish ? "Main focus" : "Главный акцент",
      noteBody: isEnglish
        ? "Use the analyses sidebar as the main workspace switcher: create new analyses there, rename them, compare them and keep different scenarios separate."
        : "Используйте боковой список анализов как главный переключатель рабочих сценариев: здесь создаются новые анализы, меняются их названия, запускается сравнение и хранится история отдельных сценариев.",
      steps: [
        {
          title: isEnglish ? "Main sections" : "Основные разделы",
          body: isEnglish
            ? "Home contains companies, currencies and market widgets. Analytics is the working area for files and dashboards. Reports is used to export the selected analysis to PDF."
            : "На главной находятся компании, валюты и рыночные блоки. Аналитика — это рабочая зона для файлов и дашбордов. Отчеты нужны для выгрузки выбранного анализа в PDF."
        },
        {
          title: isEnglish ? "Analyses sidebar" : "Боковой список анализов",
          body: isEnglish
            ? "Open Analyses in the upper left corner to create a new workspace, switch between analyses, rename them, compare them or prepare items for deletion."
            : "Откройте кнопку «Анализы» в левом верхнем углу, чтобы создать новое рабочее пространство, переключаться между анализами, переименовывать их, сравнивать или готовить к удалению."
        },
        {
          title: isEnglish ? "File upload and period" : "Загрузка файлов и период",
          body: isEnglish
            ? "Upload one or several files, then set the period and refresh the analytics to focus on the exact time range you need."
            : "Загрузите один или несколько файлов, затем задайте период и обновите аналитику, чтобы сосредоточиться на нужном временном диапазоне."
        },
        {
          title: isEnglish ? "Cards, charts and reports" : "Карточки, графики и отчеты",
          body: isEnglish
            ? "Summary cards explain the key figures, charts reveal trends, and the Reports section exports the current analysis in the selected language."
            : "Карточки сводки объясняют ключевые показатели, графики показывают динамику, а раздел отчетов выгружает текущий анализ на выбранном языке."
        }
      ],
      confirm: isEnglish ? "Start working" : "Начать работу"
    }
  };
}

export function buildAnalyticsOverview(analysis, language) {
  const isEnglish = language === "en";
  const items = [];

  if (hasSalesData(analysis)) {
    items.push(
      isEnglish
        ? "Sales analytics: revenue dynamics, price trend, source comparison, top products and insight cards."
        : "Аналитика продаж: динамика выручки, тренд цены, сопоставление источников, топ товаров и карточки выводов."
    );
  }

  if (hasFinancialData(analysis)) {
    items.push(
      isEnglish
        ? "Financial analytics: revenue, expenses, profit, profitability, period trend and next-period forecast."
        : "Финансовая аналитика: доходы, расходы, прибыль, рентабельность, тренд по периодам и прогноз на следующий период."
    );
  }

  if (hasEducationData(analysis)) {
    items.push(
      isEnglish
        ? "Education analytics: average score, grade structure, ratings, forecast and students in the attention zone."
        : "Аналитика успеваемости: средний балл, структура оценок, рейтинги, прогноз и ученики в зоне внимания."
    );
  }

  if (items.length === 0) {
    items.push(
      isEnglish
        ? "Sales files are shown with revenue, products, sources and price dynamics."
        : "Отчеты по продажам показываются через выручку, товары, источники и динамику цены."
    );
    items.push(
      isEnglish
        ? "Financial files are shown with revenue, expenses, profit, profitability and forecast."
        : "Финансовые отчеты показываются через доходы, расходы, прибыль, рентабельность и прогноз."
    );
    items.push(
      isEnglish
        ? "Education files are shown with average score, grade distribution, ratings, forecast and risk zone."
        : "Отчеты по успеваемости показываются через средний балл, распределение оценок, рейтинги, прогноз и зону риска."
    );
  }

  items.push(
    isEnglish
      ? "The analyses sidebar helps keep separate scenarios apart and switch between them without mixing data."
      : "Боковой список анализов помогает хранить отдельные сценарии раздельно и переключаться между ними без смешивания данных."
  );

  return {
    eyebrow: isEnglish ? "Current analysis" : "Текущий анализ",
    title: isEnglish ? "What this analytics section shows" : "Что показывает этот раздел аналитики",
    description: isEnglish
      ? "The dashboard adapts to the detected document types and shows only the relevant analytics blocks."
      : "Панель автоматически подстраивается под найденные типы документов и показывает только те блоки аналитики, которые относятся к текущему анализу.",
    items
  };
}

export function getSectionGuide(section, language, analysis) {
  const isEnglish = language === "en";
  const analyticsOverview = buildAnalyticsOverview(analysis, language);

  if (section === "home") {
    return {
      badge: isEnglish ? "Quick start" : "Быстрый старт",
      title: isEnglish ? "What is on the home page" : "Что находится на главной странице",
      description: isEnglish
        ? "Home is the entry point for companies, currency dynamics and market widgets."
        : "Главная страница — это точка входа для работы с компаниями, валютной динамикой и рыночными блоками.",
      noteTitle: isEnglish ? "What to do first" : "С чего начать",
      noteBody: isEnglish
        ? "Start by creating or selecting a company. After that, the same company becomes the basis for analytics and report generation."
        : "Сначала создайте или выберите компанию. После этого именно она станет основой для аналитики и формирования отчетов.",
      steps: [
        {
          title: isEnglish ? "Companies" : "Компании",
          body: isEnglish
            ? "The upper cards let you create, edit, select and delete companies. The active company determines the workspace context."
            : "Верхние карточки позволяют создавать, редактировать, выбирать и удалять компании. Активная компания задает рабочий контекст платформы."
        },
        {
          title: isEnglish ? "Currency analysis" : "Анализ валют",
          body: isEnglish
            ? "The currency block shows exchange-rate dynamics for the selected period and helps compare the base and quote currencies."
            : "Блок валют показывает динамику курса за выбранный период и помогает сравнивать базовую и котируемую валюту."
        },
        {
          title: isEnglish ? "Market pulse" : "Топ компаний мира",
          body: isEnglish
            ? "This widget displays current market leaders and lets you manually refresh quotes to watch the market movement."
            : "Этот блок показывает текущих лидеров рынка и позволяет вручную обновлять котировки, чтобы следить за движением рынка."
        },
        {
          title: isEnglish ? "Next step" : "Следующий шаг",
          body: isEnglish
            ? "Once the company is ready, move to Analytics to upload files and build the dashboard."
            : "Когда компания готова, переходите в раздел аналитики, чтобы загрузить файлы и построить дашборд."
        }
      ],
      confirm: isEnglish ? "Continue" : "Продолжить"
    };
  }

  if (section === "reports") {
    return {
      badge: isEnglish ? "Quick start" : "Быстрый старт",
      title: isEnglish ? "How reports are exported" : "Как формируются отчеты",
      description: isEnglish
        ? "Reports are generated from the selected analysis and downloaded as a structured PDF."
        : "Отчеты формируются на основе выбранного анализа и скачиваются в виде структурированного PDF.",
      noteTitle: isEnglish ? "Important" : "Важно",
      noteBody: isEnglish
        ? "The PDF language follows the current site language, and the report content depends on the analytics inside the selected analysis."
        : "Язык PDF следует за текущим языком сайта, а состав отчета зависит от того, какая аналитика содержится в выбранном анализе.",
      steps: [
        {
          title: isEnglish ? "Choose analysis" : "Выберите анализ",
          body: isEnglish
            ? "Use the dropdown at the top to select the analysis whose data should be exported."
            : "Используйте выпадающий список сверху, чтобы выбрать анализ, данные которого нужно выгрузить."
        },
        {
          title: isEnglish ? "Check the composition" : "Проверьте состав",
          body: isEnglish
            ? "The page preview shows which sections and key indicators will be included in the PDF."
            : "Предпросмотр на странице показывает, какие разделы и ключевые показатели будут включены в PDF."
        },
        {
          title: isEnglish ? "Language and structure" : "Язык и структура",
          body: isEnglish
            ? "The report automatically adapts to sales, financial and education analytics found in the chosen workspace."
            : "Отчет автоматически подстраивается под продажи, финансы и успеваемость, найденные в выбранном рабочем пространстве."
        },
        {
          title: isEnglish ? "Download" : "Скачивание",
          body: isEnglish
            ? "Use the main button to generate the file after the preview and metrics are shown."
            : "Используйте основную кнопку для генерации файла после того, как на странице появился предпросмотр и ключевые показатели."
        }
      ],
      confirm: isEnglish ? "Understood" : "Понятно"
    };
  }

  return {
    badge: isEnglish ? "Quick start" : "Быстрый старт",
    title: isEnglish ? "How to work with analytics" : "Как работать с аналитикой",
    description: isEnglish
      ? "Analytics is the main workspace for imports, filters, the analyses sidebar and dashboards."
      : "Аналитика — это основная рабочая зона для импорта файлов, фильтров, бокового списка анализов и дашбордов.",
    noteTitle: isEnglish ? "Main focus" : "Главный акцент",
    noteBody: isEnglish
      ? "Use the analyses sidebar in the upper left corner as the main switcher between workspaces and independent analysis scenarios."
      : "Используйте список анализов в левом верхнем углу как главный переключатель между рабочими пространствами и независимыми сценариями анализа.",
    steps: [
      {
        title: isEnglish ? "Upload files" : "Загрузка файлов",
        body: isEnglish
          ? "Upload one or several files in the left block. The platform will classify them and build the relevant analytics."
          : "В левом блоке загружайте один или несколько файлов. Платформа классифицирует их и построит подходящую аналитику."
      },
      {
        title: isEnglish ? "Period and refresh" : "Период и обновление",
        body: isEnglish
          ? "In the right block set the dates, apply the period and refresh analytics when you need a recalculation."
          : "В правом блоке задавайте даты, применяйте период и обновляйте аналитику, когда нужен пересчет."
      },
      {
        title: isEnglish ? "Analyses sidebar" : "Боковой список анализов",
        body: isEnglish
          ? "Create new analyses, rename them, compare them and keep different scenarios separated from each other."
          : "Создавайте новые анализы, переименовывайте их, сравнивайте и храните разные сценарии отдельно друг от друга."
      },
      {
        title: isEnglish ? "Dashboards and tips" : "Дашборды и подсказки",
        body: isEnglish
          ? "Hover summary cards to understand the metrics, then use the charts and tables to read the trend in detail."
          : "Наводите курсор на карточки сводки, чтобы понять метрики, а затем используйте графики и таблицы для детального чтения динамики."
      }
    ],
    extraTitle: analyticsOverview.title,
    extraItems: analyticsOverview.items,
    confirm: isEnglish ? "Start analytics" : "Перейти к аналитике"
  };
}
