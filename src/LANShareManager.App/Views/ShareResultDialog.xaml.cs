using System.Diagnostics;
using System.IO;
using System.Windows;
using LANShareManager.Core.Models;

namespace LANShareManager.App.Views;

public partial class ShareResultDialog : Window
{
    private readonly ShareCreationResult _result;

    public ShareResultDialog(ShareCreationResult result)
    {
        InitializeComponent();
        _result = result;

        TxtLocalPath.Text = result.LocalPath;
        TxtHostnamePath.Text = result.HostnamePath;
        TxtIpPath.Text = result.IpPath;

        TxtSmbStatus.Text = result.SmbEnabled ? "✓ Включено" : "✗ Ошибка";
        TxtSharePermStatus.Text = result.SharePermissionsConfigured ? "✓ Настроены" : "✗ Ошибка";
        TxtNtfsStatus.Text = result.NtfsPermissionsConfigured ? "✓ Настроены" : "✗ Ошибка";
        TxtFirewallStatus.Text = result.FirewallConfigured ? "✓ Настроен" : "- Не требовался";
        TxtConnectivityStatus.Text = result.ConnectivityTested ? "✓ Проверено" : "⚠ Предупреждения";

        if (result.Diagnostics == null)
        {
            BtnShowDiagnostics.Visibility = Visibility.Collapsed;
        }
    }

    private void BtnCopyHostname_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(TxtHostnamePath.Text))
        {
            Clipboard.SetText(TxtHostnamePath.Text);
            MessageBox.Show(this, $"Сетевой путь скопирован в буфер обмена:\n{TxtHostnamePath.Text}", "Скопировано", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnCopyIp_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(TxtIpPath.Text))
        {
            Clipboard.SetText(TxtIpPath.Text);
            MessageBox.Show(this, $"IP путь скопирован в буфер обмена:\n{TxtIpPath.Text}", "Скопировано", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (Directory.Exists(_result.LocalPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _result.LocalPath,
                UseShellExecute = true
            });
        }
    }

    private void BtnShowDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        if (_result.Diagnostics != null)
        {
            var diagWin = new DiagnosticsWindow(_result.Diagnostics, Path.GetFileName(_result.LocalPath));
            diagWin.Owner = this;
            diagWin.ShowDialog();
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
