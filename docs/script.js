// ==========================================================================
// LAN Share Manager — Comprehensive Trilingual Script & Mobbin Interactions
// Languages: Kazakh (KZ), Russian (RU), English (EN)
// ==========================================================================

const translations = {
  kz: {
    badge: "Windows 10 & 11 • .NET 8 • 100% Open Source.",
    heroTitle: "Жергілікті желідегі папкаларды 1 кликпен ортақ етіңіз.",
    heroSubtitle: "Ешқандай күрделі қолмен баптауларсыз. Брандмауэрді, NTFS қауіпсіздік ережелерін және желілік мекенжайларды автоматты синхрондайтын заманауи Windows утилитасы.",
    copyText: "Көшіру",
    copiedText: "Көшірілді! ✓",
    btnDownload: "ZIP репозиторийден жүктеу",
    btnGithub: "GitHub репозиторийі ↗",
    navFeatures: "Мүмкіндіктер",
    navWinr: "Win+R Нұсқаулық",
    navCompare: "Салыстыру",
    navShowcase: "Интерфейс",
    navCli: "CLI",
    navFaq: "Сұрақ-жауап",
    
    // Showcase
    secShowcaseTitle: "Графикалық және Консольдік орта.",
    secShowcaseSubtitle: "WPF Windows 11 интерфейсі және автоматтандыруға арналған CLI бірыңғай жүйеде.",
    tabMain: "1. Басты терезе",
    tabCreate: "2. Ресурс жасау",
    tabResult: "3. Нәтиже мен Жолдар",
    tabDiag: "4. 10-деңгейлі Диагностика",
    
    // Win + R Section
    winrBadge: "Басқа компьютерден қосылу • How to Connect from Another PC",
    winrTitle: "Win + R арқылы ортақ папканы лезде ашыңыз.",
    winrSubtitle: "Көрші немесе кеңседегі кез келген Windows компьютерінен ортақ файлдарға 3 қарапайым қадаммен қосылыңыз.",
    winrStep1Title: "Пернетақтаны басыңыз",
    winrStep1Desc: "Басқа компьютерде Win + R басыңыз — Windows-тың «Выполнить / Run» командалық терезесі ашылады.",
    winrStep2Title: "Жолды көшіріп қойыңыз",
    winrStep2Desc: "Ұсынылатын IP мекенжай жолын енгізіңіз. IP арқылы Windows желілік атауды кідіріссіз 100% ашады.",
    winrStep3Title: "Enter басыңыз",
    winrStep3Desc: "«ОК» немесе Enter басыңыз — ортақ папка Проводник (Explorer) терезесінде бірден ашылады!",
    winrDialogTitle: "Выполнить (Run)",
    winrDialogPrompt: "Введите имя программы, папки, документа или ресурса Интернета, которые требуется открыть.",
    winrDialogField: "Открыть:",
    winrTipCredTitle: "Логин немесе пароль сұралса?",
    winrTipCredDesc: "Егер «Ввод сетевых учетных данных» шықса, пайдаланушы атына .\\Пайдаланушы және папка ашылған компьютердің паролін жазыңыз.",
    winrTipDriveTitle: "Тұрақты диск (Z:) ретінде бекіту",
    winrTipDriveDesc: "Басқа компьютердің CMD терезесінде мына пәрменді 1 рет орындасаңыз, папка диск болып сақталып қалады:",

    // Comparison
    secCompareTitle: "Қолмен баптау мен LAN Share Manager айырмашылығы.",
    secCompareSubtitle: "Windows-тың стандартты ортақ ету жолы неліктен қатеге толы және біз оны қалай 3 секундқа түсірдік?",
    compareBadTitle: "Стандартты Windows",
    compareBadBadge: "Қолмен 15 қадам",
    compareGoodTitle: "LAN Share Manager",
    compareGoodBadge: "Ұсынылған ✓",
    compareBad1: "<strong>Шатастыратын 15 терезе</strong>: Папка қасиеттері ➔ Доступ ➔ Безопасность ➔ Дополнительно ➔ Өзгерту.",
    compareBad2: "<strong>NTFS қателіктері</strong>: Тек SMB рұқсатын ашып, файлдық NTFS құқығын ұмытып кетеді ➔ \"Access Denied\" қатесі шығады.",
    compareBad3: "<strong>Брандмауэрді өшіріп тастау</strong>: 445 портын білмегендіктен адамдар антивирусты толық сөндіріп, вирус жұқтырады.",
    compareBad4: "<strong>Public Network қаупі</strong>: Егер желі кездейсоқ \"Общедоступная\" болса, компьютерлер бірін-бірі мүлдем көрмейді.",
    compareGood1: "<strong>Барлығы 1 батырмада</strong>: Папканы таңдайсыз, ат бересіз және басасыз. Барлық баптау 3 секундта бітеді.",
    compareGood2: "<strong>Синхронды SMB + NTFS</strong>: Қос деңгейлі құқықтар бірден толық үйлесімділікпен жазылады.",
    compareGood3: "<strong>Қауіпсіз Брандмауэр</strong>: Брандмауэр сөндірілмейді! Тек TCP 445 порты Private профиль үшін қауіпсіз ашылады.",
    compareGood4: "<strong>Дайын сілтемелер</strong>: Басқа компьютерге жіберетін \\\\ПК\\\\SHARE және \\\\IP\\\\SHARE жолдары бірден көшіріледі.",

    // Features
    secFeaturesTitle: "Негізгі мүмкіндіктер мен архитектура.",
    secFeaturesSubtitle: "Windows ортақ қатынасын сенімді, жылдам әрі қауіпсіз етуге арналған құралдар жиынтығы.",
    feat1Title: "1 кликпен ортақ ету",
    feat1Desc: "Папканы таңдап, рұқсат деңгейін таңдаңыз. Қалған барлық жүйелік операция автоматты түрде жасалады.",
    feat2Title: "Ең аз артықшылық принципі",
    feat2Desc: "SMB және NTFS рұқсаттары толық үйлестіріледі. SYSTEM және Әкімші құқықтары сенімді қорғалған.",
    feat3Title: "Қауіпсіз Брандмауэр",
    feat3Desc: "Брандмауэр сөндірілмейді! Тек TCP 445 порты Private профиль үшін арнайы ашылады.",
    feat4Title: "10-деңгейлі диагностика",
    feat4Desc: "Служба күйі, порт ашықтығы, брандмауэр және кері байланыс UNC жолдары автоматты түрде тексеріледі.",
    feat5Title: "Қателерді автоматты түзеу",
    feat5Desc: "Қателік орын алса, оның себебін түсіндіріп, 1 шертумен жөндеуге мүмкіндік береді.",
    feat6Title: "JSON резервтік көшірме",
    feat6Desc: "Барлық ортақ папкаларды 1 файлға сақтап, кез келген компьютерде бірден қалпына келтіруге болады.",

    // CLI & Security & FAQ
    secTerminalTitle: "Интерактивті CLI Симуляторы.",
    secTerminalSubtitle: "Серверлер мен скрипттер үшін толық консольдік қолдау.",
    secSecurityTitle: "Қауіпсіздік пен Деректерді қорғау.",
    secSecuritySubtitle: "Деректердің жоғалмау кепілдігі және жүйелік қауіпсіздік стандарттары.",
    secFaqTitle: "Жиі қойылатын сұрақтар.",
    secFaqSubtitle: "Желілік ортақ папкалар бойынша ең маңызды сұрақтар мен жауаптар.",
    faq1Q: "Бұл утилита компьютердегі файлдарды өшіріп тастауы мүмкін бе?",
    faq1A: "Жоқ, ешқашан! Желілік ресурсты өшірген кезде тек желілік қолжетімділік тоқтатылады. Қатты дискідегі нақты файлдар мен папкалар 100% өзгеріссіз қалады.",
    faq2Q: "Басқа компьютерден қандай код теріп кіреміз?",
    faq2A: "Басқа компьютерде Win + R басып, \\\\192.168.X.X\\ПапкаАтауы деп тересіз. Enter басқан кезде папка бірден ашылады.",
    faq3Q: "Бағдарлама Windows брандмауэрін сөндіре ме?",
    faq3A: "Жоқ! Брандмауэр ешқашан сөндірілмейді. Тек файл алмасуға қажетті TCP 445 порты жеке (Private) желі үшін ғана ашылады.",
    faq4Q: "Бағдарламаны қалай тез іске қосуға болады?",
    faq4A: "Windows PowerShell терезесінде: irm azamaperdeev05.github.io/lsm | iex деп теріп орындасаңыз болғаны.",
    faq5Q: "Бағдарлама құпия сөздерді сақтай ма?",
    faq5A: "Жоқ! LAN Share Manager ешқашан құпия сөздерді сақтамайды. Барлық аутентификация Windows-тың стандартты қауіпсіздік хаттамасымен жүреді.",
    
    footerTagline: "Windows жүйесінде желілік SMB папкаларын бір батырмамен баптайтын ашық бастапқы кодты утилита.",
    footerColResources: "Ресурстар",
    footerColDocs: "Құжаттама"
  },

  ru: {
    badge: "Windows 10 & 11 • .NET 8 • 100% Open Source.",
    heroTitle: "Общий доступ к папкам в локальной сети в 1 клик.",
    heroSubtitle: "Никаких сложных ручных настроек. Умная Windows-утилита, автоматически настраивающая Брандмауэр, NTFS разрешения и сетевые пути.",
    copyText: "Копировать",
    copiedText: "Скопировано! ✓",
    btnDownload: "Скачать ZIP релиз",
    btnGithub: "Репозиторий на GitHub ↗",
    navFeatures: "Возможности",
    navWinr: "Инструкция Win+R",
    navCompare: "Сравнение",
    navShowcase: "Интерфейс",
    navCli: "CLI",
    navFaq: "Вопросы и ответы",

    // Showcase
    secShowcaseTitle: "Графический и Консольный интерфейс.",
    secShowcaseSubtitle: "Современный WPF Fluent интерфейс Windows 11 и мощный CLI для автоматизации.",
    tabMain: "1. Главное окно",
    tabCreate: "2. Создание ресурса",
    tabResult: "3. Результаты и Пути",
    tabDiag: "4. 10-уровневая Диагностика",

    // Win + R Section
    winrBadge: "Подключение с соседнего ПК • How to Connect from Another PC",
    winrTitle: "Мгновенно открывайте общую папку через Win + R.",
    winrSubtitle: "Подключайтесь к общим файлам с любого офисного или домашнего компьютера за 3 простых шага.",
    winrStep1Title: "Нажмите клавиши",
    winrStep1Desc: "На другом компьютере нажмите сочетание клавиш Win + R — откроется системное окно «Выполнить».",
    winrStep2Title: "Вставьте сетевой путь",
    winrStep2Desc: "Введите рекомендуемый UNC-путь по IP-адресу. По IP Windows находит ресурс мгновенно и без сбоев.",
    winrStep3Title: "Нажмите Enter",
    winrStep3Desc: "Нажмите «ОК» или Enter — общая папка сразу же откроется в Проводнике Windows!",
    winrDialogTitle: "Выполнить (Run)",
    winrDialogPrompt: "Введите имя программы, папки, документа или ресурса Интернета, которые требуется открыть.",
    winrDialogField: "Открыть:",
    winrTipCredTitle: "Если запрошены логин и пароль?",
    winrTipCredDesc: "В окне «Ввод сетевых учетных данных» введите .\\ИмяПользователя и пароль от ПК, на котором открыта папка.",
    winrTipDriveTitle: "Подключение как сетевой диск (Z:)",
    winrTipDriveDesc: "Чтобы папка осталась как постоянный диск, выполните команду в командной строке (CMD):",

    // Comparison
    secCompareTitle: "Ручная настройка Windows против LAN Share Manager.",
    secCompareSubtitle: "Насколько мучителен стандартный путь Windows и как утилита решает это за 3 секунды?",
    compareBadTitle: "Стандартная Windows",
    compareBadBadge: "Вручную 15 шагов",
    compareGoodTitle: "LAN Share Manager",
    compareGoodBadge: "Рекомендуется ✓",
    compareBad1: "<strong>Запутанные 15 окон</strong>: Свойства папки ➔ Доступ ➔ Безопасность ➔ Дополнительно ➔ Изменить разрешения.",
    compareBad2: "<strong>Ошибки NTFS</strong>: Настраивают только SMB доступ, забывая про файловый NTFS ➔ Ошибка \"Access Denied\".",
    compareBad3: "<strong>Отключение Брандмауэра</strong>: Не зная порт 445, пользователи полностью отключают фаервол, рискуя заражением.",
    compareBad4: "<strong>Опасность Public сети</strong>: При профиле сети \"Общедоступная\" компьютеры не видят друг друга в сети.",
    compareGood1: "<strong>Все в 1 кнопке</strong>: Выбираете папку, вводите имя и жмете создать. Вся настройка занимает 3 секунды.",
    compareGood2: "<strong>Синхронные SMB + NTFS</strong>: Двухуровневые права сразу выставляются с правильным наследованием.",
    compareGood3: "<strong>Безопасный Брандмауэр</strong>: Брандмауэр не отключается! Входящий порт TCP 445 открывается строго для Private сети.",
    compareGood4: "<strong>Готовые пути</strong>: Пути \\\\ИМЯ-ПК\\\\SHARE и \\\\IP\\\\SHARE сразу копируются для отправки коллегам.",

    // Features
    secFeaturesTitle: "Ключевые преимущества и архитектура.",
    secFeaturesSubtitle: "Набор инструментов для надежного, быстрого и безопасного совместного доступа к файлам.",
    feat1Title: "Общий доступ в 1 клик",
    feat1Desc: "Выберите каталог и уровень доступа. Все системные операции выполняются автоматически.",
    feat2Title: "Принцип наименьших привилегий",
    feat2Desc: "Права SMB и NTFS синхронизированы. Права SYSTEM и Администраторов гарантированно защищены.",
    feat3Title: "Безопасный Брандмауэр",
    feat3Desc: "Брандмауэр остается включенным! Порт TCP 445 открывается исключительно для профиля Private сети.",
    feat4Title: "10-уровневая диагностика",
    feat4Desc: "Автопроверка службы LanmanServer, порта 445, правил брандмауэра и реального сетевого отклика.",
    feat5Title: "Советник по исправлению",
    feat5Desc: "При ошибке утилита подскажет точную причину и предложит авто-исправление в один клик.",
    feat6Title: "Резервное копирование в JSON",
    feat6Desc: "Экспорт всех сетевых папок в единый файл конфигурации и их быстрое восстановление на любом ПК.",

    // CLI & Security & FAQ
    secTerminalTitle: "Интерактивный терминал CLI.",
    secTerminalSubtitle: "Полнофункциональный консольный интерфейс для серверов и скриптов.",
    secSecurityTitle: "Безопасность и Защита данных.",
    secSecuritySubtitle: "Гарантия сохранности ваших данных и соблюдение стандартов безопасности Windows.",
    secFaqTitle: "Часто задаваемые вопросы.",
    secFaqSubtitle: "Ответы на самые популярные вопросы по настройке сетевого доступа.",
    faq1Q: "Может ли утилита удалить файлы на моем жестком диске?",
    faq1A: "Нет, никогда! При удалении общего ресурса отключается только сетевой доступ. Все локальные файлы и папки на диске остаются нетронутыми.",
    faq2Q: "Какой код вводить на другом компьютере для подключения?",
    faq2A: "На другом компьютере нажмите Win + R и введите \\\\192.168.X.X\\ИмяПапки. После нажатия Enter папка откроется в Проводнике.",
    faq3Q: "Отключает ли программа Брандмауэр Windows?",
    faq3A: "Нет! Брандмауэр всегда остается включенным. Разрешается только входящий трафик порта TCP 445 и только для профиля Частная сеть (Private).",
    faq4Q: "Как быстро запустить утилиту без установки?",
    faq4A: "В PowerShell от имени Администратора выполните: irm azamaperdeev05.github.io/lsm | iex.",
    faq5Q: "Сохраняет ли программа пароли пользователей?",
    faq5A: "Нет! LAN Share Manager не запрашивает и не хранит пароли. Аутентификация выполняется стандартными механизмами Windows (NTLMv2 / Kerberos).",

    footerTagline: "Утилита с открытым исходным кодом для быстрой и безопасной настройки общих папок SMB в Windows.",
    footerColResources: "Ресурсы",
    footerColDocs: "Документация"
  },

  en: {
    badge: "Windows 10 & 11 • .NET 8 • 100% Open Source.",
    heroTitle: "Local SMB network file sharing in 1 click.",
    heroSubtitle: "Zero manual friction. Automated Windows Firewall rules, NTFS ACL inheritance, and real-time LAN diagnostics without touching security settings.",
    copyText: "Copy",
    copiedText: "Copied! ✓",
    btnDownload: "Download Release (.zip)",
    btnGithub: "View on GitHub ↗",
    navFeatures: "Features",
    navWinr: "Win+R Guide",
    navCompare: "Compare",
    navShowcase: "Interface",
    navCli: "CLI",
    navFaq: "FAQ",

    // Showcase
    secShowcaseTitle: "Graphical and Command-line environment.",
    secShowcaseSubtitle: "WPF Windows 11 Fluent interface and high-performance CLI unified in one package.",
    tabMain: "1. Main Window",
    tabCreate: "2. Create Share",
    tabResult: "3. Result & Paths",
    tabDiag: "4. 10-Point Diagnostics",

    // Win + R Section
    winrBadge: "Connect from Another PC • Win + R Quick Guide",
    winrTitle: "Open shared folders instantly via Win + R.",
    winrSubtitle: "Connect to shared files from any other Windows computer on the local network in 3 easy steps.",
    winrStep1Title: "Press the Keys",
    winrStep1Desc: "On the other computer, press the Win + R keys — opens the Windows Run dialog box.",
    winrStep2Title: "Paste Network Path",
    winrStep2Desc: "Type or paste the recommended IP path. Using the local IP ensures 100% immediate resolution without delays.",
    winrStep3Title: "Press Enter",
    winrStep3Desc: "Click OK or hit Enter — the shared folder will open directly in Windows File Explorer!",
    winrDialogTitle: "Run",
    winrDialogPrompt: "Type the name of a program, folder, document, or Internet resource, and Windows will open it for you.",
    winrDialogField: "Open:",
    winrTipCredTitle: "Prompted for username & password?",
    winrTipCredDesc: "If Windows prompts for network credentials, enter .\\HostUsername and the Windows password of the hosting PC.",
    winrTipDriveTitle: "Map as a Permanent Network Drive (Z:)",
    winrTipDriveDesc: "To keep the share permanently mounted as a drive letter, run this in Command Prompt (CMD):",

    // Comparison
    secCompareTitle: "Manual Windows setup vs LAN Share Manager.",
    secCompareSubtitle: "Why standard Windows file sharing is painful and how we solve it in 3 seconds.",
    compareBadTitle: "Standard Windows",
    compareBadBadge: "15 Manual Steps",
    compareGoodTitle: "LAN Share Manager",
    compareGoodBadge: "Recommended ✓",
    compareBad1: "<strong>15 Confusing Windows</strong>: Folder Properties ➔ Sharing ➔ Advanced ➔ Security ➔ Edit ACLs ➔ Inheritance.",
    compareBad2: "<strong>NTFS Permission Mismatches</strong>: SMB permissions are set, but NTFS ACLs are forgotten ➔ \"Access Denied\" error.",
    compareBad3: "<strong>Dangerous Firewall Disabling</strong>: Users don't know port 445, so they disable Windows Firewall entirely.",
    compareBad4: "<strong>Public Network Trap</strong>: If Windows network profile is Public, computers cannot see or reach each other.",
    compareGood1: "<strong>Everything in 1 Button</strong>: Pick folder, name it, and click Create. Full setup completes in under 3 seconds.",
    compareGood2: "<strong>Synchronized SMB + NTFS</strong>: Dual-layer security applied simultaneously with proper child inheritance.",
    compareGood3: "<strong>Surgical Firewall Rule</strong>: Firewall is never turned off! Inbound TCP 445 is opened strictly for Private network.",
    compareGood4: "<strong>Instant Copyable Links</strong>: Ready-to-use \\\\HOSTNAME\\\\SHARE and \\\\IP\\\\SHARE paths generated on the spot.",

    // Features
    secFeaturesTitle: "Key capabilities and architecture.",
    secFeaturesSubtitle: "Complete toolkit engineered for rock-solid, secure, and effortless Windows file sharing.",
    feat1Title: "1-Click Sharing",
    feat1Desc: "Choose folder and desired access level. All underlying Windows OS operations are executed automatically.",
    feat2Title: "Least Privilege Principle",
    feat2Desc: "SMB and NTFS ACLs are perfectly synchronized. SYSTEM and Administrator rights are fully preserved.",
    feat3Title: "Surgical Firewall Safety",
    feat3Desc: "Firewall remains active! Inbound port TCP 445 is enabled exclusively for the Private network profile.",
    feat4Title: "10-Point End-to-End Diagnostics",
    feat4Desc: "Automated verification of LanmanServer service, port 445 socket, firewall rules, and loopback connectivity.",
    feat5Title: "Actionable Remediation Advisor",
    feat5Desc: "When an issue occurs, the app identifies the exact failure point and provides a 1-click Auto-Fix.",
    feat6Title: "JSON Backup & Migration",
    feat6Desc: "Export all active share configurations to a single JSON file and recreate them on any computer in seconds.",

    // CLI & Security & FAQ
    secTerminalTitle: "Interactive CLI Terminal.",
    secTerminalSubtitle: "Scriptable and automated command-line interface for sysadmins and headless servers.",
    secSecurityTitle: "Security & Zero Data Loss Guarantee.",
    secSecuritySubtitle: "Strict adherence to Windows security standards and preservation of physical storage.",
    secFaqTitle: "Frequently Asked Questions.",
    secFaqSubtitle: "Everything you need to know about LAN Share Manager and Windows SMB sharing.",
    faq1Q: "Can this utility delete files on my hard drive?",
    faq1A: "No, never! Removing a share only revokes network access. The physical files and folders on your disk are 100% safe and untouched.",
    faq2Q: "What code should I type on another computer to connect?",
    faq2A: "Press Win + R on the other computer, type \\\\192.168.X.X\\ShareName, and press Enter. The folder opens instantly in File Explorer.",
    faq3Q: "Does the app disable Windows Defender Firewall?",
    faq3A: "No! The firewall remains active at all times. It only opens the necessary TCP 445 port rule for Private networks.",
    faq4Q: "How can I launch it quickly without installing?",
    faq4A: "Open PowerShell as Administrator and run: irm azamaperdeev05.github.io/lsm | iex.",
    faq5Q: "Does the app store or transmit passwords?",
    faq5A: "No! LAN Share Manager never asks for, logs, or stores user passwords. Authentication is handled natively by Windows (NTLMv2 / Kerberos).",

    footerTagline: "Open-source Windows utility for instant, secure local SMB file sharing.",
    footerColResources: "Resources",
    footerColDocs: "Documentation"
  }
};

let currentLang = 'kz';

function setLanguage(lang) {
  if (!translations[lang]) return;
  currentLang = lang;
  try {
    localStorage.setItem('lsm_lang', lang);
  } catch (e) {}

  document.querySelectorAll('.lang-btn').forEach(btn => {
    btn.classList.toggle('active', btn.dataset.lang === lang);
  });

  const t = translations[lang];
  document.querySelectorAll('[data-i18n]').forEach(el => {
    const key = el.dataset.i18n;
    if (t[key] !== undefined) {
      el.innerHTML = t[key];
    }
  });

  // Update CLI simulator screen if active
  const activeCliBtn = document.querySelector('.cli-tab-btn.active');
  if (activeCliBtn) {
    updateCliOutput(activeCliBtn.dataset.cmd);
  }
}

// Clipboard Copy with feedback
function setupCopyButtons() {
  document.querySelectorAll('.copy-pill-btn, .copy-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      const targetId = btn.dataset.copyTarget;
      const target = document.getElementById(targetId);
      if (!target) return;

      const text = target.innerText.trim();
      navigator.clipboard.writeText(text).then(() => {
        const originalHtml = btn.innerHTML;
        btn.classList.add('copied');
        btn.innerHTML = `<span style="color:#fff;">✓</span> ${translations[currentLang].copiedText}`;
        setTimeout(() => {
          btn.classList.remove('copied');
          btn.innerHTML = originalHtml;
        }, 2000);
      });
    });
  });
}

// App Window Mockup Tab Switching (Segmented Control)
function setupShowcaseTabs() {
  const tabBtns = document.querySelectorAll('.segmented-item[data-tab]');
  const tabPanes = document.querySelectorAll('.tab-pane');

  tabBtns.forEach(btn => {
    btn.addEventListener('click', () => {
      const targetTab = btn.dataset.tab;

      tabBtns.forEach(b => b.classList.remove('active'));
      tabPanes.forEach(p => p.style.display = 'none');

      btn.classList.add('active');
      const activeContent = document.getElementById(`tab-${targetTab}`);
      if (activeContent) activeContent.style.display = 'block';
    });
  });
}

// CLI Interactive Terminal Simulator
const cliOutputs = {
  create: `PS C:\\> LANShareManager.exe create --path "C:\\OBSHAYA" --name "OBSHAYA" --access readwrite
==================================================
 LAN Share Manager - SMB Share Orchestrator
==================================================
Administrator Privileges: \x1b[32m✓ Active (Elevated)\x1b[0m

Configuring Share 'OBSHAYA' for folder 'C:\\OBSHAYA'...
Access Mode: ReadWrite | Firewall: Inbound 445 | Propagation: Subfolders

\x1b[32m✓ Share successfully created and published!\x1b[0m

Local Path:     C:\\OBSHAYA
Hostname Path:  \\\\DESKTOP-MKVG1ET\\OBSHAYA
IP Path (Rec):  \\\\192.168.1.100\\OBSHAYA

SMB Service:    \x1b[32m✓ LanmanServer Running\x1b[0m
SMB Rights:     \x1b[32m✓ Everyone -> Change\x1b[0m
NTFS ACL:       \x1b[32m✓ Everyone -> Modify (Inherited)\x1b[0m
Firewall:       \x1b[32m✓ Inbound TCP 445 Allowed (Private)\x1b[0m`,

  list: `PS C:\\> LANShareManager.exe list
==================================================
 LAN Share Manager - SMB Share Orchestrator
==================================================
Administrator Privileges: \x1b[32m✓ Active\x1b[0m

Enumerating active network shares...

Share Name       Path                             Access             Status    
--------------------------------------------------------------------------------
OBSHAYA          C:\\OBSHAYA                       Read & Write       Active    
Documents        C:\\Users\\Admin\\Documents         Read Only          Active    
Projects         D:\\Work\\Projects                Full Control       Active    `,

  test: `PS C:\\> LANShareManager.exe test --name "OBSHAYA"
==================================================
 LAN Share Manager - 10-Point End-to-End Diagnostics
==================================================
Running comprehensive diagnostic suite for 'OBSHAYA'...

[\x1b[32m✓\x1b[0m] Service LanmanServer (Server)             : Running and healthy
[\x1b[32m✓\x1b[0m] TCP Port 445 Socket Listener             : Listening on all interfaces
[\x1b[32m✓\x1b[0m] SMB Share Registration                   : Published in CIM subsystem
[\x1b[32m✓\x1b[0m] Physical Directory Check                 : Present on volume C:
[\x1b[32m✓\x1b[0m] NTFS ACL Propagation                     : Inherited to child objects
[\x1b[32m✓\x1b[0m] Windows Firewall Rule                    : Inbound TCP 445 enabled
[\x1b[32m✓\x1b[0m] Network Profile Security                 : Active profile is Private
[\x1b[32m✓\x1b[0m] Loopback UNC (\\\\localhost\\OBSHAYA)       : Response OK (2ms)
[\x1b[32m✓\x1b[0m] Hostname UNC (\\\\DESKTOP-MKVG1ET\\OBSHAYA) : Response OK (3ms)
[\x1b[32m✓\x1b[0m] IP UNC (\\\\192.168.1.100\\OBSHAYA)         : Response OK (1ms)

\x1b[32m✓ All 10 diagnostic checks passed! 100% accessible across LAN.\x1b[0m`,

  export: `PS C:\\> LANShareManager.exe backup --export "C:\\shares_backup.json"
==================================================
 LAN Share Manager - Backup & Disaster Recovery
==================================================
Exporting active share definitions to JSON format...

\x1b[32m✓ Successfully exported 3 shares to C:\\shares_backup.json\x1b[0m
{
  "shareName": "OBSHAYA",
  "path": "C:\\\\OBSHAYA",
  "access": "ReadWrite",
  "firewall": true
}`
};

function formatAnsi(text) {
  return text
    .replace(/\x1b\[32m/g, '<span style="color:#10B981;font-weight:bold;">')
    .replace(/\x1b\[0m/g, '</span>');
}

function updateCliOutput(cmd) {
  const screen = document.getElementById('cli-screen-body');
  if (!screen) return;
  if (cliOutputs[cmd]) {
    screen.innerHTML = formatAnsi(cliOutputs[cmd]);
  }
}

function setupCliSimulator() {
  const cliBtns = document.querySelectorAll('.cli-tab-btn');
  cliBtns.forEach(btn => {
    btn.addEventListener('click', () => {
      cliBtns.forEach(b => b.classList.remove('active'));
      btn.classList.add('active');
      updateCliOutput(btn.dataset.cmd);
    });
  });

  // Default load
  updateCliOutput('create');
}

// FAQ Accordion (.faq-row at rounded.sm)
function setupFaq() {
  document.querySelectorAll('.faq-row').forEach(row => {
    row.addEventListener('click', () => {
      row.classList.toggle('active');
    });
  });
}

// Initialize on DOM Ready
document.addEventListener('DOMContentLoaded', () => {
  setupCopyButtons();
  setupShowcaseTabs();
  setupCliSimulator();
  setupFaq();

  document.querySelectorAll('.lang-btn').forEach(btn => {
    btn.addEventListener('click', () => setLanguage(btn.dataset.lang));
  });

  let savedLang = 'kz';
  try {
    savedLang = localStorage.getItem('lsm_lang') || 'kz';
  } catch (e) {}
  setLanguage(savedLang);
});
