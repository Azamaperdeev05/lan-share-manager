using System.IO;
using System.Windows;
using System.Windows.Controls;
using LANShareManager.Core.Enums;
using LANShareManager.Core.Models;
using LANShareManager.Core.Validation;

namespace LANShareManager.App.Views;

public partial class CreateShareDialog : Window
{
    public ShareCreationRequest? Request { get; private set; }

    public CreateShareDialog(string? initialPath = null)
    {
        InitializeComponent();

        if (!string.IsNullOrWhiteSpace(initialPath))
        {
            TxtFolderPath.Text = initialPath;
        }
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
        // Auto-populate share name if share name is empty or matches previous folder name
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
    }

    private void BtnCreate_Click(object sender, RoutedEventArgs e)
    {
        string path = TxtFolderPath.Text.Trim();
        string shareName = TxtShareName.Text.Trim();

        var pathVal = ShareInputValidator.ValidateFolderPath(path);
        if (!pathVal.IsValid)
        {
            MessageBox.Show(this, pathVal.ErrorMessage, "Проверка папки", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtFolderPath.Focus();
            return;
        }

        var nameVal = ShareInputValidator.ValidateShareName(shareName);
        if (!nameVal.IsValid)
        {
            MessageBox.Show(this, nameVal.ErrorMessage, "Проверка имени ресурса", MessageBoxButton.OK, MessageBoxImage.Warning);
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
