// Multilingual Translations Dictionary (Strictly Mobbin Voice - Sentences with terminal periods)
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
    navCompare: "Салыстыру",
    navCli: "CLI",
    navFaq: "Сұрақ-жауап",
    secShowcaseTitle: "Графикалық және Консольдік орта.",
    secCompareTitle: "Қолмен баптау мен LAN Share Manager айырмашылығы.",
    secCompareSubtitle: "Windows-тың стандартты ортақ ету жолы неліктен қатеге толы және біз оны қалай 3 секундқа түсірдік?",
    secFeaturesTitle: "Негізгі мүмкіндіктер мен архитектура.",
    secTerminalTitle: "Интерактивті CLI Симуляторы.",
    secSecurityTitle: "Axiom / MAS стиліндегі қауіпсіздік.",
    secFaqTitle: "Жиі қойылатын сұрақтар."
  },
  ru: {
    badge: "Windows 10 & 11 • .NET 8 • 100% Open Source.",
    heroTitle: "Общий доступ к папкам в локальной сети в 1 клик.",
    heroSubtitle: "Никаких сложных ручных настроек. Умная Windows-утилита, автоматически настраивающая Брандмауэр, NTFS разрешения и сетевые пути.",
    copyText: "Копировать",
    copiedText: "Скопировано! ✓",
    btnDownload: "Скачать ZIP архив",
    btnGithub: "Репозиторий на GitHub ↗",
    navFeatures: "Возможности",
    navCompare: "Сравнение",
    navCli: "CLI",
    navFaq: "Частые вопросы",
    secShowcaseTitle: "Графический и Консольный интерфейс.",
    secCompareTitle: "Ручная настройка Windows против LAN Share Manager.",
    secCompareSubtitle: "Насколько мучителен стандартный путь Windows и как утилита решает это за 3 секунды?",
    secFeaturesTitle: "Ключевые преимущества и архитектура.",
    secTerminalTitle: "Интерактивный терминал CLI.",
    secSecurityTitle: "Архитектура безопасности в стиле Axiom / MAS.",
    secFaqTitle: "Часто задаваемые вопросы."
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
    navCompare: "Compare",
    navCli: "CLI",
    navFaq: "FAQ",
    secShowcaseTitle: "Graphical and Command-line environment.",
    secCompareTitle: "Manual Windows setup vs LAN Share Manager.",
    secCompareSubtitle: "Why standard Windows file sharing is painful and how we solve it in 3 seconds.",
    secFeaturesTitle: "Key capabilities and architecture.",
    secTerminalTitle: "Interactive CLI Terminal.",
    secSecurityTitle: "Axiom / MAS-grade security architecture.",
    secFaqTitle: "Frequently asked questions."
  }
};

let currentLang = 'kz';

function setLanguage(lang) {
  if (!translations[lang]) return;
  currentLang = lang;

  document.querySelectorAll('.lang-btn').forEach(btn => {
    btn.classList.toggle('active', btn.dataset.lang === lang);
  });

  const t = translations[lang];
  document.querySelectorAll('[data-i18n]').forEach(el => {
    const key = el.dataset.i18n;
    if (t[key]) el.innerHTML = t[key];
  });
}

// Clipboard Copy for Stadium Pills
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
 LAN Share Manager - Управление общими папками SMB
==================================================
Привилегии администратора: \x1b[32m✓ Включены (Administrator)\x1b[0m

Создание общего ресурса 'OBSHAYA' для папки 'C:\\OBSHAYA'...
Уровень доступа: ReadWrite | Брандмауэр: True | Подпапки: True

\x1b[32m✓ Ресурс успешно создан и настроен!\x1b[0m

Локальный путь:  C:\\OBSHAYA
Сетевой путь:    \\\\DESKTOP-MKVG1ET\\OBSHAYA
IP путь:         \\\\192.168.1.100\\OBSHAYA

SMB служба:       \x1b[32m✓ Включено\x1b[0m
Права SMB:        \x1b[32m✓ Настроены (Everyone -> Change)\x1b[0m
Права NTFS:       \x1b[32m✓ Настроены (Everyone -> Modify)\x1b[0m
Брандмауэр:       \x1b[32m✓ Настроен (TCP 445 Private)\x1b[0m`,

  list: `PS C:\\> LANShareManager.exe list
==================================================
 LAN Share Manager - Управление общими папками SMB
==================================================
Привилегии администратора: \x1b[32m✓ Включены (Administrator)\x1b[0m

Запрос списка активных SMB ресурсов...

Имя ресурса      Локальный путь                   Доступ             Статус    
--------------------------------------------------------------------------------
OBSHAYA          C:\\OBSHAYA                       Чтение и запись    Active    
Documents        C:\\Documents                     Только чтение      Active    
Projects         D:\\Work\\Projects                Полный доступ      Active    `,

  test: `PS C:\\> LANShareManager.exe test --name "OBSHAYA"
==================================================
 LAN Share Manager - Управление общими папками SMB
==================================================
Запуск сквозной диагностики для ресурса 'OBSHAYA'...

[\x1b[32m✓\x1b[0m] Служба SMB (LanmanServer / Сервер)        : Служба активна и запущена
[\x1b[32m✓\x1b[0m] Порт TCP 445 (SMB Listener)             : Порт открыт и принимает подключения
[\x1b[32m✓\x1b[0m] Общий ресурс 'OBSHAYA'                    : Зарегистрирован в подсистеме SMB
[\x1b[32m✓\x1b[0m] Локальная папка на диске                  : Папка существует (C:\\OBSHAYA)
[\x1b[32m✓\x1b[0m] Разрешения безопасности NTFS              : Разрешения для группы 'Все' настроены
[\x1b[32m✓\x1b[0m] Брандмауэр Windows (SMB TCP 445)         : Входящий трафик разрешен
[\x1b[32m✓\x1b[0m] Сетевой профиль Windows                  : Текущий профиль: Частная (Private)
[\x1b[32m✓\x1b[0m] Доступ через \\\\localhost                 : Успешный отклик по пути
[\x1b[32m✓\x1b[0m] Доступ через имя ПК (\\\\DESKTOP-MKVG1ET) : Успешный отклик по пути
[\x1b[32m✓\x1b[0m] Доступ через локальный IP (\\\\192.168.1.100) : Успешный отклик по пути

\x1b[32m✓ Все 10 проверок пройдены успешно! Ресурс 100% доступен в локальной сети.\x1b[0m`,

  export: `PS C:\\> LANShareManager.exe export --output "C:\\backup_shares.json"
==================================================
 LAN Share Manager - Управление общими папками SMB
==================================================
Экспорт конфигурации активных ресурсов...

\x1b[32m✓ Экспорт 3 ресурсов успешно сохранен в файл: C:\\backup_shares.json\x1b[0m
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

function setupCliSimulator() {
  const cliBtns = document.querySelectorAll('.cli-tab-btn');
  const screen = document.getElementById('cli-screen-body');
  if (!screen) return;

  cliBtns.forEach(btn => {
    btn.addEventListener('click', () => {
      cliBtns.forEach(b => b.classList.remove('active'));
      btn.classList.add('active');

      const cmd = btn.dataset.cmd;
      if (cliOutputs[cmd]) {
        screen.innerHTML = formatAnsi(cliOutputs[cmd]);
      }
    });
  });

  // Default load
  screen.innerHTML = formatAnsi(cliOutputs.create);
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
});
