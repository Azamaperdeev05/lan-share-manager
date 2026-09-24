<div align="center">

# 📁 LAN Share Manager

### Modern, enterprise-grade Windows utility for creating, diagnosing, and securely managing SMB network shares in peer-to-peer and office LANs.

<p align="center">
  <b><a href="README.md">🇬🇧 English</a></b> •
  <b><a href="README.kz.md">🇰🇿 Қазақша</a></b> •
  <b><a href="README.ru.md">🇷🇺 Русский</a></b>
</p>

[![Build & Test](https://github.com/Azamaperdeev05/lan-share-manager/actions/workflows/ci.yml/badge.svg)](https://github.com/Azamaperdeev05/lan-share-manager/actions)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011%20%7C%20Server-0078D6?logo=windows&logoColor=white)](https://github.com/Azamaperdeev05/lan-share-manager)
[![Framework](https://img.shields.io/badge/.NET-8.0%20LTS-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Release](https://img.shields.io/badge/Release-v1.0.0-blue.svg)](https://github.com/Azamaperdeev05/lan-share-manager/releases)
[![Language](https://img.shields.io/badge/Localization-Trilingual%20(KZ%20%7C%20RU%20%7C%20EN)-brightgreen.svg)](https://github.com/Azamaperdeev05/lan-share-manager)

[**Download Release (.zip)**](https://github.com/Azamaperdeev05/lan-share-manager/releases/latest) • [**Quick Start (1-Line)**](#-instant-launch-one-liner) • [**How to Connect (Win + R)**](#-how-to-connect-from-another-computer-win--r) • [**Key Features**](#-key-features) • [**CLI Reference**](#-command-line-interface-cli)

</div>

---

## 🌟 Why LAN Share Manager?

Configuring folder sharing in Windows often requires 7+ disconnected, error-prone manual steps:
1. Opening folder Properties and finding the "Sharing" tab.
2. Setting SMB Share Permissions.
3. Switching to the "Security" tab and manually configuring NTFS Access Control Lists (ACLs) and inheritance flags.
4. Opening Windows Defender Firewall and locating TCP port 445 rules under "File and Printer Sharing".
5. Verifying network profile type (switching from Public to Private network).
6. Determining the machine's local LAN IPv4 address and NetBIOS name.
7. Testing connectivity and troubleshooting cryptic Windows network errors.

**LAN Share Manager automates the entire process in a single click with built-in actionable diagnostics.**

```text
[ Pick Folder ] ➔ [ Share Name ] ➔ [ Access Level ] ➔ [ One-Click Share ]
       │
       ├── 1. Validate paths & handle non-existent folders
       ├── 2. Configure NTFS ACL with proper propagation & inheritance
       ├── 3. Register SMB Share (Native Windows CIM / PowerShell)
       ├── 4. Configure Firewall rule (TCP port 445 for Private profile only)
       ├── 5. Auto-detect LAN IPv4 and Hostname
       ├── 6. Run comprehensive 10-point diagnostic verification
       └── 7. Formatted Win + R copyable paths (\\IP\Share & \\Hostname\Share)
```

---

## 🚀 Instant Launch (One-Liner)

Open Windows PowerShell (Run as Administrator) and run:

```powershell
irm azamaperdeev05.github.io/lsm | iex
```

> **What does this do?**  
> It downloads the verified Web Launcher with pinned SHA-256 integrity verification, extracts the official binary to an isolated temporary workspace, launches the trilingual GUI with Administrator privileges, and safely cleans up all temporary artifacts upon exit.

---

## 💡 How to Connect from Another Computer (`Win + R`)

To access shared files from another Windows computer on the same local network:

```text
┌────────────────────────────────────────────────────────────────────────┐
│  Run (Выполнить)                                                  [X]  │
├────────────────────────────────────────────────────────────────────────┤
│  Type the name of a program, folder, document, or Internet resource... │
│                                                                        │
│  Open: [ \\192.168.1.55\SharedFolder                         ▼]        │
│                                                                        │
│               [  OK  ]          [ Cancel ]          [ Browse... ]      │
└────────────────────────────────────────────────────────────────────────┘
```

### Step-by-Step Instructions:

1. **Step 1:** On the **other** computer, press the keyboard shortcut **`Win + R`** (opens the Windows **Run** dialog).
2. **Step 2:** Type or paste the network UNC path:
   ```cmd
   \\192.168.X.X\YourShareName
   ```
   *(e.g., `\\192.168.1.55\Data` or `\\192.168.1.55\SharedFolder`)*

   > **💡 Why IP address is recommended over Hostname:**  
   > While Hostname (`\\DESKTOP-ABC\Data`) works, peer-to-peer Windows networks often suffer from NetBIOS/mDNS discovery delays across subnets or modern Windows 11 updates. Connecting directly via **local IP opens the share instantly without delay**.

3. **Step 3:** Press **`Enter`** (or click **OK**). The shared folder will immediately open in Windows File Explorer!

### Additional Connection Tips:

* **If prompted for Windows Credentials:**  
  If Windows displays *"Enter network credentials"*:
  * **Username:** `.\HostUsername` (e.g., `.\Admin` or `.\Azamat`)
  * **Password:** The Windows account password of the computer hosting the shared folder.
* **Map as a Permanent Network Drive (Drive Letter `Z:`):**  
  To keep the folder permanently visible in *"This PC"*, run this single command in Command Prompt (CMD):
  ```cmd
  net use Z: \\192.168.X.X\YourShareName /persistent:yes
  ```

---

## 💎 Key Features

* 🌐 **100% Trilingual UI & CLI:** Seamlessly switch between **Kazakh (Қазақша 🇰🇿)**, **Russian (Русский 🇷🇺)**, and **English (🇬🇧)** on the fly without restarting.
* ⚡ **1-Click Share Wizard:** Create fully accessible network shares in seconds with instant copy buttons for UNC paths.
* 🔒 **Dual-Layer Principle of Least Privilege:**
  * **Read Only:** `Everyone` → Read (SMB) and Read & Execute (NTFS).
  * **Read & Write (Recommended):** `Everyone` → Change (SMB) and Modify (NTFS).
  * **Full Control:** `Everyone` → Full (SMB) and Full Control (NTFS).
* 🛡️ **System Accounts Protected:** `SYSTEM`, `Administrators`, and directory Owner ACL permissions are strictly preserved.
* 🔥 **Surgical Firewall Automation:** Inbound TCP port 445 is enabled **strictly for the Private network profile**. The firewall is never globally disabled.
* 🩺 **10-Point End-to-End Diagnostics:** Verifies `LanmanServer` service, TCP port 445 listener, firewall rules, NTFS inheritance, and real loopback UNC resolution (`\\localhost`, `\\IP`, `\\Hostname`).
* 💡 **Actionable Remediation Advisor:** When an error occurs, the app doesn't just display cryptic codes — it identifies the exact failure point, underlying cause, and offers a 1-click **Auto-Fix** or copyable command.
* 💾 **Data Loss Prevention:** Removing a network share **never deletes local files or directories** on disk.
* 📦 **JSON Backup & Migration:** Export and restore all share configurations with a single command.

---

## 🖥️ Graphical Interface (GUI)

The modern WPF application provides a sleek, responsive dark-themed dashboard:
* **Real-time Status Bar:** Displays Hostname, Local LAN IPv4, Network Profile badge (`Private ✓` vs `Public ⚠`), and UAC status.
* **One-Click Language Switcher:** Instant pills in the top-right corner `[ 🇰🇿 KZ | 🇷🇺 RU | 🇬🇧 EN ]`.
* **Share Inventory Grid:** Live table of all shared folders, physical paths, access modes, and connection status.
* **Integrated Win + R Guide Dialog:** Click **`🌐 Win+R Guide`** for instant access to copyable paths and setup instructions.

---

## ⌨️ Command Line Interface (CLI)

LAN Share Manager includes both a full interactive menu and a scriptable CLI:

### Interactive CLI Menu
```cmd
LANShareManager.exe
```
Provides an interactive numeric menu for creating shares, running diagnostics, auto-repairing firewall/network settings, switching languages, and viewing Win+R instructions.

### Scriptable Commands

```cmd
# Create a Read/Write share with firewall rules and subfolder inheritance
LANShareManager.exe create --path "C:\Shares\ProjectDocs" --name "ProjectDocs" --access readwrite

# Create a Read-Only share
LANShareManager.exe create --path "D:\Media" --name "Media" --access readonly

# List all active network shares
LANShareManager.exe list

# Run full diagnostics on a specific share
LANShareManager.exe test --name "ProjectDocs"

# Remove network sharing safely (preserves local disk files)
LANShareManager.exe remove --name "ProjectDocs"

# Automatically fix network profile and open SMB firewall rules
LANShareManager.exe autofix

# Export all shares to JSON backup
LANShareManager.exe backup --export "shares_backup.json"

# Import and recreate shares from JSON backup
LANShareManager.exe backup --import "shares_backup.json"
```

---

## 🏗️ Architecture & Project Structure

```text
lan-share-manager/
├── src/
│   ├── LANShareManager.Core/           # Core domain models, interfaces & trilingual engine
│   │   ├── Diagnostics/                # 10-point diagnostic service & remediation engine
│   │   ├── Enums/                      # AccessMode, DiagnosticStatus, NetworkCategory
│   │   ├── Interfaces/                 # ISmbService, IFirewallService, INetworkService
│   │   ├── Localization/               # LocalizationService & ConnectGuide (KZ, RU, EN)
│   │   ├── Models/                     # ShareCreationRequest, ShareResult, NetworkInfo
│   │   └── Validation/                 # ShareInputValidator & RemediationAdvisor
│   ├── LANShareManager.Infrastructure/ # Native Windows OS implementations
│   │   ├── Firewall/                   # Windows Defender Firewall (netsh & PowerShell)
│   │   ├── Network/                    # Network profile and IPv4 detection
│   │   ├── NTFS/                       # System.Security.AccessControl ACL management
│   │   ├── Orchestration/              # Transactional ShareOrchestrator
│   │   ├── Serialization/              # JSON ShareConfigSerializer
│   │   └── SMB/                        # Windows SMB management via CIM / WMI
│   ├── LANShareManager.App/            # WPF Modern Graphical User Interface
│   │   ├── Views/                      # MainWindow, ShareResultDialog, ConnectInstructionsDialog
│   │   └── ViewModels/                 # MVVM bindings & trilingual updates
│   └── LANShareManager.CLI/            # Terminal interface & interactive wizard
├── tests/
│   └── LANShareManager.Tests/          # 61 automated xUnit tests (Validation, Logic, I18N)
├── install.ps1                         # Zero-install Web Launcher with SHA-256 Pinning
└── README.md                           # Documentation (English primary)
```

---

## 🧪 Testing & Reliability

The solution maintains 100% test pass rate across 61 comprehensive unit tests:
```bash
dotnet test tests/LANShareManager.Tests/LANShareManager.Tests.csproj -c Release
```
```text
Passed!  - Failed: 0, Passed: 61, Skipped: 0, Total: 61.
```

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
Created with ❤️ for system administrators, office teams, and everyday Windows users.
