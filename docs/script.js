// Multilingual Translations Dictionary
const translations = {
  kz: {
    badge: "Windows 10 & 11 • .NET 8 • Ашық бастапқы код",
    heroTitle: "Жергілікті желіде папкаларды <span>1 кликпен</span> ортақ ету",
    heroSubtitle: "Ешқандай күрделі баптауларсыз. Брандмауэрді, NTFS рұқсаттарын және желілік мекенжайларды автоматты реттейтін ақылды Windows утилитасы.",
    copyText: "Көшіру",
    copiedText: "Көшірілді! ✓",
    btnDownload: "ZIP репозиторийден жүктеу",
    btnGithub: "GitHub репозиторийі",
    navFeatures: "Мүмкіндіктер",
    navCompare: "Салыстыру",
    navCli: "CLI Консоль",
    navSecurity: "Қауіпсіздік",
    navFaq: "Сұрақ-жауап",
    secCompareTitle: "Қолмен баптау мен LAN Share Manager айырмашылығы",
    secCompareSubtitle: "Windows стандартты жолы қаншалықты күрделі және утилита қалай жеңілдетеді?",
    secFeaturesTitle: "Неліктен мыңдаған сисадминдер бізді таңдайды?",
    secTerminalTitle: "Интерактивті CLI Симуляторы",
    secSecurityTitle: "Axiom / MAS стиліндегі Қауіпсіздік Архитектурасы",
    secFaqTitle: "Жиі қойылатын сұрақтар"
  },
  ru: {
    badge: "Windows 10 & 11 • .NET 8 • Открытый исходный код",
    heroTitle: "Общий доступ к папкам в локальной сети <span>в 1 клик</span>",
    heroSubtitle: "Никаких сложных ручных настроек. Умная Windows-утилита, автоматически настраивающая Брандмауэр, NTFS разрешения и сетевые пути.",
    copyText: "Копировать",
    copiedText: "Скопировано! ✓",
    btnDownload: "Скачать ZIP архив",
    btnGithub: "Репозиторий на GitHub",
    navFeatures: "Возможности",
    navCompare: "Сравнение",
    navCli: "CLI Консоль",
    navSecurity: "Безопасность",
    navFaq: "Частые вопросы",
    secCompareTitle: "Ручная настройка Windows против LAN Share Manager",
    secCompareSubtitle: "Насколько мучителен стандартный путь Windows и как утилита решает это за 3 секунды?",
    secFeaturesTitle: "Ключевые преимущества и архитектура",
    secTerminalTitle: "Интерактивный терминал CLI",
    secSecurityTitle: "Архитектура безопасности в стиле Axiom / MAS",
    secFaqTitle: "Часто задаваемые вопросы"
  },
  en: {
    badge: "Windows 10 & 11 • .NET 8 • 100% Open Source",
    heroTitle: "Local SMB Network File Sharing <span>in 1 Click</span>",
    heroSubtitle: "Zero manual friction. Automated Windows Firewall rules, NTFS ACL inheritance, and real-time LAN diagnostics.",
    copyText: "Copy",
    copiedText: "Copied! ✓",
    btnDownload: "Download Release (.zip)",
    btnGithub: "View on GitHub",
    navFeatures: "Features",
    navCompare: "Compare",
    navCli: "CLI",
    navSecurity: "Security",
    navFaq: "FAQ",
    secCompareTitle: "Manual Windows Setup vs LAN Share Manager",
    secCompareSubtitle: "Why standard Windows file sharing is painful and how we solve it in 3 seconds.",
    secFeaturesTitle: "Key Capabilities & Architecture",
    secTerminalTitle: "Interactive CLI Terminal",
    secSecurityTitle: "Axiom / MAS-grade Security Architecture",
    secFaqTitle: "Frequently Asked Questions"
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

// Clipboard Copy
function setupCopyButtons() {
  document.querySelectorAll('.copy-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      const targetId = btn.dataset.copyTarget;
      const target = document.getElementById(targetId);
      if (!target) return;

      const text = target.innerText.trim();
      navigator.clipboard.writeText(text).then(() => {
        const originalText = btn.innerHTML;
        btn.classList.add('copied');
        btn.innerHTML = `<span>✓</span> ${translations[currentLang].copiedText}`;
        setTimeout(() => {
          btn.classList.remove('copied');
          btn.innerHTML = originalText;
        }, 2000);
      });
    });
  });
}

// App Window Mockup Tab Switching
function setupShowcaseTabs() {
  const tabBtns = document.querySelectorAll('.tab-btn');
  const tabContents = document.querySelectorAll('.tab-content');

  tabBtns.forEach(btn => {
    btn.addEventListener('click', () => {
      const targetTab = btn.dataset.tab;

      tabBtns.forEach(b => b.classList.remove('active'));
      tabContents.forEach(c => c.classList.remove('active'));

      btn.classList.add('active');
      const activeContent = document.getElementById(`tab-${targetTab}`);
      if (activeContent) activeContent.classList.add('active');
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
  const cliBtns = document.querySelectorAll('.cli-btn');
  const terminalBody = document.getElementById('cli-terminal-body');

  cliBtns.forEach(btn => {
    btn.addEventListener('click', () => {
      cliBtns.forEach(b => b.classList.remove('active'));
      btn.classList.add('active');

      const cmd = btn.dataset.cmd;
      if (cliOutputs[cmd]) {
        terminalBody.innerHTML = formatAnsi(cliOutputs[cmd]);
      }
    });
  });

  // Load default
  if (terminalBody) terminalBody.innerHTML = formatAnsi(cliOutputs.create);
}

// FAQ Accordion
function setupFaq() {
  document.querySelectorAll('.faq-question').forEach(q => {
    q.addEventListener('click', () => {
      const item = q.parentElement;
      item.classList.toggle('active');
    });
  });
}

// Initialize on Load
document.addEventListener('DOMContentLoaded', () => {
  setupCopyButtons();
  setupShowcaseTabs();
  setupCliSimulator();
  setupFaq();

  document.querySelectorAll('.lang-btn').forEach(btn => {
    btn.addEventListener('click', () => setLanguage(btn.dataset.lang));
  });
});
