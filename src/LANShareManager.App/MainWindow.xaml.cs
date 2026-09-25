using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LANShareManager.App.Views;
using LANShareManager.Core.Enums;
using LANShareManager.Core.Interfaces;
using LANShareManager.Core.Localization;
using LANShareManager.Core.Models;
using LANShareManager.Core.Serialization;
using LANShareManager.Infrastructure.Diagnostics;
using LANShareManager.Infrastructure.Firewall;
using LANShareManager.Infrastructure.Logging;
using LANShareManager.Infrastructure.Network;
using LANShareManager.Infrastructure.NTFS;
using LANShareManager.Infrastructure.Orchestration;
using LANShareManager.Infrastructure.OS;
using LANShareManager.Infrastructure.Process;
using LANShareManager.Infrastructure.SMB;
using Microsoft.Win32;

namespace LANShareManager.App;

public partial class MainWindow : Window
{
    private readonly ILoggerService _logger;
    private readonly ISmbService _smbService;
    private readonly INtfsPermissionService _ntfsService;
    private readonly IFirewallService _firewallService;
    private readonly INetworkService _networkService;
    private readonly IOsService _osService;
    private readonly IDiagnosticsService _diagnosticsService;
    private readonly IShareOrchestrator _orchestrator;
    private readonly IShareConfigSerializer _serializer;

    private NetworkInfo? _currentNetworkInfo;
    private List<SmbShareInfo> _currentShares = new();

    public MainWindow()
    {
        InitializeComponent();

        _logger = new FileLoggerService();
        var processRunner = new PowerShellProcessRunner(_logger);
        _smbService = new WindowsSmbService(_logger, processRunner);
#pragma warning disable CA1416
        _ntfsService = new NtfsPermissionService(_logger, processRunner);
#pragma warning restore CA1416
        _firewallService = new WindowsFirewallService(_logger, processRunner);
        _osService = new WindowsOsService(_logger);
        _networkService = new WindowsNetworkService(_logger, processRunner, _osService);
        _diagnosticsService = new WindowsDiagnosticsService(_smbService, _ntfsService, _firewallService, _networkService, _logger, _osService);
        _orchestrator = new ShareOrchestrator(_smbService, _ntfsService, _firewallService, _networkService, _diagnosticsService, _logger);
        _serializer = new ShareConfigSerializer();

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyLanguage(LocalizationService.CurrentLanguage);
        CheckAdministratorPrivileges();
        await RefreshNetworkInfoAsync();
        await RefreshSharesListAsync();
    }

    private void CheckAdministratorPrivileges()
    {
        bool isAdmin = false;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                isAdmin = false;
            }
        }
        else
        {
            isAdmin = true;
        }

        if (isAdmin)
        {
            TxtAdminStatus.Text = LocalizationService.GetString("AdminActive");
            TxtAdminStatus.Foreground = (Brush)FindResource("SuccessBrush");
            AdminBadgeBorder.Background = (Brush)FindResource("SuccessBgBrush");
        }
        else
        {
            TxtAdminStatus.Text = LocalizationService.GetString("AdminRequired");
            TxtAdminStatus.Foreground = (Brush)FindResource("DangerBrush");
            AdminBadgeBorder.Background = new SolidColorBrush(Color.FromRgb(255, 235, 235));

            MessageBox.Show(
                this,
                "Приложение запущено без прав Администратора.\nДля настройки SMB, NTFS разрешений и брандмауэра требуются права администратора.",
                "Предупреждение UAC",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private async Task RefreshNetworkInfoAsync()
    {
        TxtStatus.Text = "Определение сетевых параметров...";
        try
        {
            _currentNetworkInfo = await _networkService.GetNetworkInfoAsync();
            TxtComputerName.Text = _currentNetworkInfo.ComputerName;
            TxtOsVersion.Text = _currentNetworkInfo.OsInfo?.FullDescription ?? "Windows";
            TxtLocalIp.Text = _currentNetworkInfo.LocalIPv4;
            TxtNetworkProfile.Text = _currentNetworkInfo.NetworkProfileDisplay;

            if (_currentNetworkInfo.NetworkProfile == NetworkCategory.Public)
            {
                TxtNetworkProfile.Foreground = (Brush)FindResource("DangerBrush");
                BtnSwitchPrivate.Visibility = Visibility.Visible;
            }
            else
            {
                TxtNetworkProfile.Foreground = (Brush)FindResource("TextPrimaryBrush");
                BtnSwitchPrivate.Visibility = Visibility.Collapsed;
            }
            TxtStatus.Text = "Сетевые параметры обновлены";
        }
        catch (Exception ex)
        {
            _logger.LogError("Ошибка получения сетевой информации", ex);
            TxtStatus.Text = "Ошибка определения параметров сети";
        }
    }

    private async Task RefreshSharesListAsync()
    {
        TxtStatus.Text = "Загрузка списка общих ресурсов...";
        bool includeSpecial = ChkShowAdminShares.IsChecked == true;

        try
        {
            var shares = await _smbService.GetSharesAsync(includeSpecial);
            _currentShares = shares.ToList();
            GridShares.ItemsSource = _currentShares;
            TxtStatus.Text = $"Найдено ресурсов: {_currentShares.Count}";
            UpdateActionButtonsState();
        }
        catch (Exception ex)
        {
            _logger.LogError("Ошибка при получении списка ресурсов", ex);
            TxtStatus.Text = "Не удалось обновить список ресурсов";
            MessageBox.Show(this, $"Ошибка получения списка SMB ресурсов:\n{ex.Message}", "Ошибка SMB", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateActionButtonsState()
    {
        bool hasSelection = GridShares.SelectedItem is SmbShareInfo;
        BtnOpenFolder.IsEnabled = hasSelection;
        BtnCopyPath.IsEnabled = hasSelection;
        BtnShareInstructions.IsEnabled = hasSelection;
        BtnEditAccess.IsEnabled = hasSelection;
        BtnDiagnostics.IsEnabled = hasSelection;
        BtnRemoveShare.IsEnabled = hasSelection;
    }

    private void GridShares_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateActionButtonsState();
    }

    private void GridShares_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (GridShares.SelectedItem is SmbShareInfo share)
        {
            OpenFolder(share);
        }
    }

    private async void BtnCreateNewShare_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new CreateShareDialog(_currentShares) { Owner = this };
        if (dlg.ShowDialog() == true && dlg.Request != null)
        {
            TxtStatus.Text = $"Создание ресурса '{dlg.Request.ShareName}'...";
            BtnCreateNewShare.IsEnabled = false;

            try
            {
                var result = await _orchestrator.CreateOrConfigureShareAsync(dlg.Request);
                if (result.Success)
                {
                    var resultDlg = new ShareResultDialog(result) { Owner = this };
                    resultDlg.ShowDialog();
                    await RefreshSharesListAsync();
                }
                else
                {
                    MessageBox.Show(this, result.Message, "Ошибка создания ресурса", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Критическая ошибка:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnCreateNewShare.IsEnabled = true;
                TxtStatus.Text = "Готово";
            }
        }
    }

    private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (GridShares.SelectedItem is SmbShareInfo share)
        {
            OpenFolder(share);
        }
    }

    private void OpenFolder(SmbShareInfo share)
    {
        string target = !string.IsNullOrWhiteSpace(share.Path) && Directory.Exists(share.Path)
            ? share.Path
            : $"\\\\{Environment.MachineName}\\{share.Name}";

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Не удалось открыть папку: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnCopyPath_Click(object sender, RoutedEventArgs e)
    {
        if (GridShares.SelectedItem is SmbShareInfo share)
        {
            string hostPath = $"\\\\{Environment.MachineName}\\{share.Name}";
            string ipPath = _currentNetworkInfo != null && !string.IsNullOrWhiteSpace(_currentNetworkInfo.LocalIPv4) && _currentNetworkInfo.LocalIPv4 != "127.0.0.1"
                ? $"\\\\{_currentNetworkInfo.LocalIPv4}\\{share.Name}"
                : hostPath;

            Clipboard.SetText(hostPath);
            MessageBox.Show(
                this,
                $"Пути сетевого ресурса:\nИмя ПК: {hostPath}\nЛокальный IP: {ipPath}\n\nСетевой путь скопирован в буфер обмена.",
                "Сетевой путь",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private async void BtnEditAccess_Click(object sender, RoutedEventArgs e)
    {
        if (GridShares.SelectedItem is SmbShareInfo share)
        {
            var dlg = new EditAccessDialog(share.Name, share.Access) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                TxtStatus.Text = $"Обновление прав для '{share.Name}'...";
                bool success = await _orchestrator.UpdateShareAccessAsync(share.Name, dlg.SelectedAccess, dlg.ApplyToNtfs);
                if (success)
                {
                    MessageBox.Show(this, $"Разрешения для ресурса '{share.Name}' успешно обновлены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    await RefreshSharesListAsync();
                }
                else
                {
                    MessageBox.Show(this, "Не удалось обновить разрешения. Подробности в журнале логов.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                TxtStatus.Text = "Готово";
            }
        }
    }

    private async void BtnDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        if (GridShares.SelectedItem is SmbShareInfo share)
        {
            TxtStatus.Text = $"Выполняется диагностика ресурса '{share.Name}'...";
            var report = await _diagnosticsService.RunFullDiagnosticsAsync(share.Name, share.Path);
            TxtStatus.Text = "Диагностика завершена";

            var diagWin = new DiagnosticsWindow(report, share.Name) { Owner = this };
            diagWin.ShowDialog();
        }
    }

    private async void BtnRemoveShare_Click(object sender, RoutedEventArgs e)
    {
        if (GridShares.SelectedItem is SmbShareInfo share)
        {
            var res = MessageBox.Show(
                this,
                $"Вы действительно хотите удалить общий доступ к ресурсу '{share.Name}'?\n\nВАЖНО:\nБудет удален только сетевой общий ресурс SMB.\nСама папка '{share.Path}' и все файлы на жестком диске останутся нетронутыми.",
                "Удалить только общий доступ SMB?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (res == MessageBoxResult.Yes)
            {
                TxtStatus.Text = $"Удаление общего доступа '{share.Name}'...";
                bool success = await _orchestrator.RemoveShareSafelyAsync(share.Name);
                if (success)
                {
                    MessageBox.Show(this, $"Общий сетевой доступ '{share.Name}' успешно удален.\nФайлы сохранены на диске.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    await RefreshSharesListAsync();
                }
                else
                {
                    MessageBox.Show(this, $"Не удалось удалить ресурс '{share.Name}'.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                TxtStatus.Text = "Готово";
            }
        }
    }

    private async void BtnSwitchPrivate_Click(object sender, RoutedEventArgs e)
    {
        var res = MessageBox.Show(
            this,
            "Переключить текущее сетевое подключение в режим 'Частная сеть' (Private)?\nЭто позволит безопасно обнаруживать папки в локальной сети.",
            "Настройка профиля сети",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (res == MessageBoxResult.Yes)
        {
            TxtStatus.Text = "Переключение профиля сети...";
            bool ok = await _networkService.SwitchNetworkToPrivateAsync();
            if (ok)
            {
                await RefreshNetworkInfoAsync();
            }
            else
            {
                MessageBox.Show(this, "Не удалось изменить профиль сети. Пожалуйста, откройте Параметры Windows -> Сеть и интернет.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            TxtStatus.Text = "Готово";
        }
    }

    private async void BtnRefreshNetwork_Click(object sender, RoutedEventArgs e)
    {
        await RefreshNetworkInfoAsync();
        await RefreshSharesListAsync();
    }

    private async void ChkShowAdminShares_Changed(object sender, RoutedEventArgs e)
    {
        await RefreshSharesListAsync();
    }

    private async void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        var sfd = new SaveFileDialog
        {
            Filter = "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*",
            FileName = "smb_shares_backup.json",
            Title = "Экспорт конфигурации сетевых ресурсов"
        };

        if (sfd.ShowDialog(this) == true)
        {
            var configs = _currentShares.Where(s => !s.IsSpecial).Select(s => new ShareExportConfig
            {
                ShareName = s.Name,
                Path = s.Path,
                Access = s.Access.ToString(),
                Firewall = true,
                ApplyToSubfolders = true
            }).ToList();

            string json = _serializer.Serialize(configs);
            await File.WriteAllTextAsync(sfd.FileName, json, Encoding.UTF8);
            MessageBox.Show(this, $"Конфигурация {configs.Count} ресурсов успешно экспортирована.", "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void BtnImport_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Filter = "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*",
            Title = "Импорт конфигурации сетевых ресурсов"
        };

        if (ofd.ShowDialog(this) == true)
        {
            string json = await File.ReadAllTextAsync(ofd.FileName, Encoding.UTF8);
            var configs = _serializer.Deserialize(json);
            if (configs.Count == 0)
            {
                MessageBox.Show(this, "В файле не найдено корректных настроек общих ресурсов.", "Ошибка импорта", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int successCount = 0;
            foreach (var c in configs)
            {
                Enum.TryParse<AccessMode>(c.Access, true, out var access);
                var req = new ShareCreationRequest
                {
                    FolderPath = c.Path,
                    ShareName = c.ShareName,
                    Access = access,
                    EnableFirewallRules = c.Firewall,
                    ApplyPermissionsToSubfolders = c.ApplyToSubfolders,
                    TestShareAfterCreation = false
                };
                var result = await _orchestrator.CreateOrConfigureShareAsync(req);
                if (result.Success) successCount++;
            }

            MessageBox.Show(this, $"Успешно импортировано и создано ресурсов: {successCount} из {configs.Count}", "Импорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
            await RefreshSharesListAsync();
        }
    }

    private void BtnOpenLogs_Click(object sender, RoutedEventArgs e)
    {
        string logDir = _logger.GetLogDirectory();
        if (Directory.Exists(logDir))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = logDir,
                UseShellExecute = true
            });
        }
        else
        {
            MessageBox.Show(this, $"Папка журналов: {logDir}", "Журнал логов", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ApplyLanguage(AppLanguage lang)
    {
        LocalizationService.SetLanguage(lang);

        Title = LocalizationService.GetString("AppTitle", lang);
        TxtAppTitle.Text = "LAN Share Manager";
        TxtAppSub.Text = LocalizationService.GetString("AppSubtitle", lang);

        TxtHostLabel.Text = LocalizationService.GetString("HostName", lang);
        TxtOsLabel.Text = LocalizationService.GetString("OsVersion", lang);
        TxtIpLabel.Text = LocalizationService.GetString("LocalIp", lang);
        TxtProfileLabel.Text = LocalizationService.GetString("NetworkProfile", lang);

        BtnSwitchPrivate.Content = LocalizationService.GetString("BtnSwitchPrivate", lang);
        BtnRefreshNetwork.Content = LocalizationService.GetString("BtnRefreshNetwork", lang);

        TxtSharesTitle.Text = LocalizationService.GetString("SharesTitle", lang);
        BtnConnectGuide.Content = LocalizationService.GetString("BtnConnectGuide", lang);
        ChkShowAdminShares.Content = LocalizationService.GetString("ShowAdminShares", lang);
        BtnCreateNewShare.Content = LocalizationService.GetString("BtnCreateShare", lang);

        ColShareName.Header = LocalizationService.GetString("ColShareName", lang);
        ColLocalPath.Header = LocalizationService.GetString("ColLocalPath", lang);
        ColAccess.Header = LocalizationService.GetString("ColAccess", lang);
        ColStatus.Header = LocalizationService.GetString("ColStatus", lang);

        BtnOpenFolder.Content = LocalizationService.GetString("BtnOpenFolder", lang);
        BtnCopyPath.Content = LocalizationService.GetString("BtnCopyPath", lang);
        BtnShareInstructions.Content = LocalizationService.GetString("BtnHowToConnect", lang);
        BtnEditAccess.Content = LocalizationService.GetString("BtnEditAccess", lang);
        BtnDiagnostics.Content = LocalizationService.GetString("BtnDiagnostics", lang);
        BtnRemoveShare.Content = LocalizationService.GetString("BtnRemove", lang);

        BtnExport.Content = LocalizationService.GetString("BtnExport", lang);
        BtnImport.Content = LocalizationService.GetString("BtnImport", lang);
        BtnLogs.Content = LocalizationService.GetString("BtnLogs", lang);
        TxtStatus.Text = LocalizationService.GetString("StatusReady", lang);

        CheckAdministratorPrivileges();
    }

    private void BtnLangKk_Click(object sender, RoutedEventArgs e) => ApplyLanguage(AppLanguage.Kazakh);
    private void BtnLangRu_Click(object sender, RoutedEventArgs e) => ApplyLanguage(AppLanguage.Russian);
    private void BtnLangEn_Click(object sender, RoutedEventArgs e) => ApplyLanguage(AppLanguage.English);

    private void BtnConnectGuide_Click(object sender, RoutedEventArgs e)
    {
        string shareName = (GridShares.SelectedItem as SmbShareInfo)?.Name ?? "SharedFolder";
        var dlg = new ConnectInstructionsDialog(shareName, _currentNetworkInfo?.LocalIPv4, _currentNetworkInfo?.ComputerName)
        {
            Owner = this
        };
        dlg.ShowDialog();
    }

    private void BtnShareInstructions_Click(object sender, RoutedEventArgs e)
    {
        if (GridShares.SelectedItem is SmbShareInfo share)
        {
            var dlg = new ConnectInstructionsDialog(share.Name, _currentNetworkInfo?.LocalIPv4, _currentNetworkInfo?.ComputerName)
            {
                Owner = this
            };
            dlg.ShowDialog();
        }
    }
}
