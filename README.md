<div align="center">

# 📁 LAN Share Manager

### Современная утилита Windows для создания и безопасного управления локальными общими папками SMB (Server Message Block) в локальной сети (LAN)

[![Build & Test](https://github.com/Azamaperdeev05/lan-share-manager/actions/workflows/ci.yml/badge.svg)](https://github.com/Azamaperdeev05/lan-share-manager/actions)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011%20%7C%20Server-0078D6?logo=windows&logoColor=white)](https://github.com/Azamaperdeev05/lan-share-manager)
[![Framework](https://img.shields.io/badge/.NET-8.0%20LTS-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Release](https://img.shields.io/badge/Release-v1.0.0-blue.svg)](https://github.com/Azamaperdeev05/lan-share-manager/releases)
[![Language](https://img.shields.io/badge/Language-C%23-239120.svg?logo=c-sharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)

[**Скачать релиз (.exe)**](https://github.com/Azamaperdeev05/lan-share-manager/releases/latest) • [**Быстрый старт**](#-быстрый-старт) • [**Возможности**](#-ключевые-возможности) • [**CLI команды**](#-консольный-режим-cli) • [**English Overview**](#-english-summary)

</div>

---

## 🌟 Зачем нужен LAN Share Manager?

В Windows настройка общего доступа к папке в локальной сети часто требует выполнения десятка разрозненных ручных действий:
1. Открытие свойств папки и переход на вкладку «Доступ».
2. Настройка прав общего ресурса (Share Permissions).
3. Переход на вкладку «Безопасность» и ручная настройка списков управления доступом (NTFS ACL) с выставлением флагов наследования.
4. Открытие «Брандмауэра Защитника Windows» и поиск правил группы «Общий доступ к файлам и принтерам» для порта TCP 445.
5. Проверка типа текущей сети (переключение с Public на Private).
6. Выяснение локального IPv4-адреса через `ipconfig` и имени компьютера.
7. Тестирование, открывается ли ресурс с соседних компьютеров.

**LAN Share Manager делает всё это автоматически в 1 клик.**

```
[ Выбор папки ] ➔ [ Имя ресурса ] ➔ [ Уровень доступа ] ➔ [ Создать общий доступ ]
        │
        ├── 1. Проверка пути и создание папки
        ├── 2. Настройка NTFS ACL (наследование на подпапки и файлы)
        ├── 3. Регистрация SMB-ресурса (нативный PowerShell / CIM)
        ├── 4. Настройка входящего правила Брандмауэра (TCP 445 для Private сети)
        ├── 5. Автоопределение LAN IPv4 и имени ПК
        ├── 6. Сквозная 10-точечная диагностика доступности
        └── 7. Готовые пути для вставки: \\ИМЯ-ПК\SHARE и \\IP-АДРЕС\SHARE
```

---

## 🚀 Быстрый старт (Запуск в 1 строку)

В любой командной строке PowerShell (Windows 10/11) выполните ультра-короткую команду:
```powershell
irm azamaperdeev05.github.io/lsm | iex
```

> **Что делает эта команда?**
> Она загружает проверенный Web Launcher с защитой SHA-256 Hash Pinning, распаковывает официальный релиз в изолированную временную среду, запускает GUI с правами Администратора и полностью очищает временные файлы после завершения.

---

### Альтернативные варианты:
#### Скачать готовый ZIP архив
1. Перейдите в раздел [**Releases**](https://github.com/Azamaperdeev05/lan-share-manager/releases/latest).
2. Скачайте архив `LANShareManager-v1.0.0-win-x64.zip`.
3. Распакуйте в удобную папку и запустите `LANShareManagerGUI.exe`.

#### Запуск CLI через командную строку
Создание сетевого ресурса одной командой:
```cmd
LANShareManager.exe create --path "C:\OBSHAYA" --name "OBSHAYA" --access readwrite
```

---

## 💎 Ключевые возможности

- ⚡ **Создание общего доступа в один клик**: интуитивный интерфейс на русском языке с мгновенным получением сетевых путей.
- 🔒 **Принцип наименьших привилегий (Least Privilege)**:
  - **Только чтение**: `Everyone` → Read (SMB) и Read & Execute (NTFS).
  - **Чтение и запись**: `Everyone` → Change (SMB) и Modify (NTFS).
  - **Полный доступ**: `Everyone` → Full (SMB) и Full Control (NTFS).
- 🛡 **Безопасность системных прав**: учетные записи `SYSTEM`, `Administrators` и текущий Владелец каталога гарантированно сохраняются.
- 🔥 **Безопасность Брандмауэра Windows**: входящий трафик порта 445 разрешается **строго для профиля Private**. Брандмауэр никогда не отключается глобально. Предупреждение и кнопка переключения при нахождении в сети `Public`.
- 🩺 **Встроенная 10-уровневая диагностика**: проверка службы `LanmanServer`, порта 445, статуса брандмауэра, списков ACL и реальных сетевых путей `\\localhost`, `\\COMPUTERNAME` и `\\IP`.
- 💾 **Защита от потери данных**: удаление сетевого ресурса **никогда не удаляет файлы на жестком диске**.
- 🔄 **100% идемпотентность**: повторные запуски не повреждают и не дублируют конфигурацию.
- 📦 **Экспорт и импорт в JSON**: резервное копирование и пакетное развертывание сетевых папок.

---

## 🖼 Интерфейс приложения

### Главное окно
- Отображение текущего имени ПК, локального LAN IPv4 и типа сетевого профиля (Частная/Общедоступная).
- Бейдж статуса UAC (`✓ Администратор: Активен`).
- Таблица всех активных сетевых папок с путями, правами и статусом.
- Панель действий: `[Открыть папку]`, `[Копировать путь]`, `[Изменить доступ]`, `[Диагностика]`, `[Удалить]`.

### Диалог создания
```
Папка:                      [ C:\OBSHAYA                  ] [Обзор...]
Имя общего ресурса:         [ OBSHAYA                     ]

Уровень доступа:
  ( ) Только чтение (Read only)
  (●) Чтение и запись (Read and write)
  ( ) Полный доступ (Full control)

Параметры интеграции:
  [x] Включить правила брандмауэра Windows для SMB (TCP 445)
  [x] Применить разрешения NTFS к подпапкам и файлам (наследование)
  [x] Проверить доступность ресурса после создания (диагностика)

[Отмена]                                    [Создать общий доступ]
```

### Карточка готового ресурса
После создания утилита показывает сетевой путь, IP путь, статус каждого компонента и кнопки быстрого копирования:
```
✓ Общий доступ успешно настроен

Локальный путь:  C:\OBSHAYA
Сетевой путь:    \\DESKTOP-MKVG1ET\OBSHAYA    [Копировать]
IP путь:         \\192.168.1.100\OBSHAYA      [Копировать]

Служба SMB:                       ✓ Включено
Разрешения общего доступа SMB:    ✓ Настроены
Разрешения безопасности NTFS:     ✓ Настроены
Брандмауэр Windows (TCP 445):     ✓ Настроен
Диагностика подключения:          ✓ Проверено

[Открыть папку] [Отчет диагностики] [Копировать сетевой путь] [Закрыть]
```

---

## ⌨ Консольный режим (CLI)

Консольная утилита `LANShareManager.exe` идеально подходит для системных администраторов, батников и автоматизации развертывания:

| Команда | Описание | Пример |
| :--- | :--- | :--- |
| `create` | Создать и настроить общий ресурс | `LANShareManager.exe create --path "C:\OBSHAYA" --name "OBSHAYA" --access readwrite` |
| `list` | Показать список общих папок | `LANShareManager.exe list` (или `--all` со служебными) |
| `test` | Запустить диагностику подключения | `LANShareManager.exe test --name "OBSHAYA"` |
| `remove` | Безопасно удалить доступ (файлы остаются) | `LANShareManager.exe remove --name "OBSHAYA" --force` |
| `export` | Экспортировать конфигурацию в JSON | `LANShareManager.exe export --output "shares.json"` |
| `import` | Импортировать ресурсы из JSON | `LANShareManager.exe import --input "shares.json"` |
| `gui` | Запустить графический интерфейс | `LANShareManager.exe gui` |

---

## 🛠 Инструкция по сборке из исходного кода

### Требования
* Windows 10/11 или Windows Server
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Сборка
```cmd
# 1. Клонировать репозиторий
git clone https://github.com/Azamaperdeev05/lan-share-manager.git
cd lan-share-manager

# 2. Восстановить зависимости
dotnet restore LANShareManager.sln

# 3. Скомпилировать проект
dotnet build LANShareManager.sln -c Release

# 4. Запустить модульные тесты
dotnet test tests/LANShareManager.Tests/LANShareManager.Tests.csproj -c Release
```

### Публикация готовых бинарных файлов (Windows x64)
```cmd
# Публикация GUI
dotnet publish src/LANShareManager.App/LANShareManager.App.csproj -c Release -r win-x64 --self-contained false -o publish/win-x64/

# Публикация CLI
dotnet publish src/LANShareManager.CLI/LANShareManager.CLI.csproj -c Release -r win-x64 --self-contained false -o publish/win-x64/
```
Или запустите готовый скрипт:
```cmd
publish.bat
```

---

## 🌍 English Summary

**LAN Share Manager** is an automated, production-ready Windows 10/11 utility for creating, configuring, and maintaining local SMB network shared folders.

### Why it exists
Configuring Windows LAN file sharing normally requires multiple manual, error-prone steps (sharing tab, NTFS ACL permissions, inheritance flags, Windows Defender Firewall inbound rules for TCP port 445, network profile switching from Public to Private, and IP discovery). 

LAN Share Manager automates the complete workflow:
- **1-Click Share Setup**: Pick a folder, set a share name, pick access mode (`ReadOnly`, `ReadWrite`, `FullControl`).
- **NTFS & SMB Least Privilege**: Granular permissions applied via `System.Security.AccessControl` with proper object & container inheritance without removing SYSTEM or Administrator credentials.
- **Firewall Hardening**: Configures inbound TCP 445 rules strictly scoped to the `Private` network profile. Warns if the current connection is `Public`.
- **Zero Data Loss**: Deleting a share removes only the SMB network registration; directory contents on disk are never deleted.
- **10-Point Diagnostics**: Verifies `LanmanServer` service, TCP 445 socket binding, share registration, NTFS permissions, Windows firewall, and UNC path reachability (`\\localhost`, `\\COMPUTERNAME`, `\\IP`).
- **Modern WPF GUI + Full CLI**: Clean Windows 11 Fluent interface alongside a scriptable CLI.

---

## 📄 Лицензия

Проект распространяется под свободной лицензией **MIT**. Подробности в файле [LICENSE](LICENSE).

---

<div align="center">
Разработано с заботой о системных администраторах и пользователях Windows.
<br>
<b>Поставьте ⭐ репозиторию, если проект оказался вам полезен!</b>
</div>
