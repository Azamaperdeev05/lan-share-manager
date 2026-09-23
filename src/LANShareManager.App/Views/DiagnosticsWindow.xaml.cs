using System.Text;
using System.Windows;
using LANShareManager.Core.Models;

namespace LANShareManager.App.Views;

public partial class DiagnosticsWindow : Window
{
    private readonly DiagnosticReport _report;
    private readonly string _shareName;

    public DiagnosticsWindow(DiagnosticReport report, string shareName)
    {
        InitializeComponent();
        _report = report;
        _shareName = shareName;

        TxtTitle.Text = $"Диагностика ресурса '{shareName}'";
        ListDiagnostics.ItemsSource = report.Items;
    }

    private void BtnCopyReport_Click(object sender, RoutedEventArgs e)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"==================================================");
        sb.AppendLine($" Отчет диагностики SMB ресурса: {_shareName}");
        sb.AppendLine($" Дата: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"==================================================");

        foreach (var item in _report.Items)
        {
            sb.AppendLine($"[{item.StatusSymbol}] {item.Name}: {item.Details}");
            if (!string.IsNullOrWhiteSpace(item.SuggestedFix))
            {
                sb.AppendLine($"    -> Рекомендация: {item.SuggestedFix}");
            }
        }

        Clipboard.SetText(sb.ToString());
        MessageBox.Show(this, "Диагностический отчет скопирован в буфер обмена.", "Скопировано", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
