using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LANShareManager.Core.Enums;
using LANShareManager.Core.Models;
using LANShareManager.Core.Validation;
using CoreValidationResult = LANShareManager.Core.Validation.ValidationResult;

namespace LANShareManager.App.Views;

public partial class CreateShareDialog : Window
{
    private static readonly Brush ErrorBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
    private static readonly Brush WarningBrush = new SolidColorBrush(Color.FromRgb(217, 119, 6));
    private static readonly Brush InfoBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235));
    private static readonly Brush SuccessBrush = new SolidColorBrush(Color.FromRgb(5, 150, 105));

    private static readonly Brush ErrorBgBrush = new SolidColorBrush(Color.FromRgb(254, 242, 242));
    private static readonly Brush ErrorBorderBrush = new SolidColorBrush(Color.FromRgb(252, 165, 165));
    private static readonly Brush ErrorTextBrush = new SolidColorBrush(Color.FromRgb(153, 27, 27));

    private static readonly Brush WarningBgBrush = new SolidColorBrush(Color.FromRgb(255, 251, 235));
    private static readonly Brush WarningBorderBrush = new SolidColorBrush(Color.FromRgb(253, 230, 138));
    private static readonly Brush WarningTextBrush = new SolidColorBrush(Color.FromRgb(180, 83, 9));

    private readonly List<SmbShareInfo> _existingShares = new();
    public ShareCreationRequest? Request { get; private set; }

    public CreateShareDialog(IEnumerable<SmbShareInfo>? existingShares = null, string? initialPath = null)
    {
        InitializeComponent();

        if (existingShares != null)
        {
            _existingShares.AddRange(existingShares);
        }

        if (!string.IsNullOrWhiteSpace(initialPath))
        {
            TxtFolderPath.Text = initialPath;
        }

        ValidateInputsRealtime();
    }

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Выберите папку для общего доступа",
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            TxtFolderPath.Text = dialog.FolderName;
        }
    }

    private void TxtFolderPath_TextChanged(object sender, TextChangedEventArgs e)
    {
        string path = TxtFolderPath.Text.Trim();
        if (!string.IsNullOrWhiteSpace(path))
        {
            try
            {
                string folderName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (string.IsNullOrWhiteSpace(TxtShareName.Text) || TxtShareName.Tag?.ToString() == "auto")
                {
                    TxtShareName.Text = folderName;
                    TxtShareName.Tag = "auto";
                }
            }
            catch
            {
                // Ignore path parsing errors during typing
            }
        }

        ValidateInputsRealtime();
    }

    private void TxtShareName_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (TxtShareName.IsFocused)
        {
            TxtShareName.Tag = null; // User manually edited share name
        }

        ValidateInputsRealtime();
    }

    private void ValidateInputsRealtime()
    {
        if (TxtFolderPath == null || TxtShareName == null || BtnCreate == null) return;

        string path = TxtFolderPath.Text.Trim();
        string shareName = TxtShareName.Text.Trim();

        var existingNames = _existingShares.Select(s => s.Name);
        var existingTuples = _existingShares.Select(s => (s.Name, s.Path));

        bool hasPath = !string.IsNullOrWhiteSpace(path);
        bool hasName = !string.IsNullOrWhiteSpace(shareName);

        CoreValidationResult? pathVal = hasPath ? ShareInputValidator.ValidateFolderPath(path, existingTuples) : null;
        CoreValidationResult? nameVal = hasName ? ShareInputValidator.ValidateShareName(shareName, existingNames) : null;

        // Render Path feedback
        if (pathVal != null)
        {
            TxtPathFeedback.Visibility = Visibility.Visible;
            ApplyFeedback(TxtPathFeedback, pathVal);
        }
        else
        {
            TxtPathFeedback.Visibility = Visibility.Collapsed;
        }

        // Render Share feedback
        if (nameVal != null)
        {
            TxtShareFeedback.Visibility = Visibility.Visible;
            ApplyFeedback(TxtShareFeedback, nameVal);
        }
        else
        {
            TxtShareFeedback.Visibility = Visibility.Collapsed;
        }

        // Summary banner & Button state
        bool hasError = (pathVal != null && !pathVal.IsValid) || (nameVal != null && !nameVal.IsValid);
        bool hasWarning = (pathVal != null && pathVal.Severity == ValidationSeverity.Warning) ||
                          (nameVal != null && nameVal.Severity == ValidationSeverity.Warning);

        if (hasError)
        {
            BorderValidationSummary.Visibility = Visibility.Visible;
            BorderValidationSummary.Background = ErrorBgBrush;
            BorderValidationSummary.BorderBrush = ErrorBorderBrush;
            TxtValidationSummary.Foreground = ErrorTextBrush;

            string errText = (pathVal != null && !pathVal.IsValid) ? pathVal.Message! : nameVal!.Message!;
            TxtValidationSummary.Text = $"✕ Ошибка: {errText}";
            BtnCreate.IsEnabled = false;
        }
        else if (hasWarning && hasPath && hasName)
        {
            BorderValidationSummary.Visibility = Visibility.Visible;
            BorderValidationSummary.Background = WarningBgBrush;
            BorderValidationSummary.BorderBrush = WarningBorderBrush;
            TxtValidationSummary.Foreground = WarningTextBrush;

            string warnText = (pathVal != null && pathVal.Severity == ValidationSeverity.Warning)
                ? $"{pathVal.Message} {pathVal.Details}".Trim()
                : $"{nameVal!.Message} {nameVal.Details}".Trim();
            TxtValidationSummary.Text = $"⚠ Предупреждение: {warnText}";
            BtnCreate.IsEnabled = true;
        }
        else
        {
            BorderValidationSummary.Visibility = Visibility.Collapsed;
            BtnCreate.IsEnabled = hasPath && hasName;
        }
    }

    private static void ApplyFeedback(TextBlock tb, CoreValidationResult result)
    {
        switch (result.Severity)
        {
            case ValidationSeverity.Error:
                tb.Foreground = ErrorBrush;
                tb.Text = $"✕ {result.Message}";
                break;
            case ValidationSeverity.Warning:
                tb.Foreground = WarningBrush;
                tb.Text = $"⚠ {result.Message}";
                break;
            case ValidationSeverity.Info:
                tb.Foreground = InfoBrush;
                tb.Text = $"ℹ {result.Message}";
                break;
            case ValidationSeverity.Success:
                tb.Foreground = SuccessBrush;
                tb.Text = $"✓ {result.Message}";
                break;
        }
    }

    private void BtnCreate_Click(object sender, RoutedEventArgs e)
    {
        string path = TxtFolderPath.Text.Trim();
        string shareName = TxtShareName.Text.Trim();

        var existingNames = _existingShares.Select(s => s.Name);
        var existingTuples = _existingShares.Select(s => (s.Name, s.Path));

        var pathVal = ShareInputValidator.ValidateFolderPath(path, existingTuples);
        if (!pathVal.IsValid)
        {
            MessageBox.Show(this, pathVal.Message, "Проверка папки", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtFolderPath.Focus();
            return;
        }

        var nameVal = ShareInputValidator.ValidateShareName(shareName, existingNames);
        if (!nameVal.IsValid)
        {
            MessageBox.Show(this, nameVal.Message, "Проверка имени ресурса", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtShareName.Focus();
            return;
        }

        AccessMode access = AccessMode.ReadWrite;
        if (RbReadOnly.IsChecked == true) access = AccessMode.ReadOnly;
        else if (RbFullControl.IsChecked == true) access = AccessMode.FullControl;

        Request = new ShareCreationRequest
        {
            FolderPath = path,
            ShareName = shareName,
            Access = access,
            EnableFirewallRules = ChkFirewall.IsChecked == true,
            ApplyPermissionsToSubfolders = ChkSubfolders.IsChecked == true,
            TestShareAfterCreation = ChkTestAfterCreation.IsChecked == true
        };

        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
