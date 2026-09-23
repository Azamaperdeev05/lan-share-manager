using System.Windows;
using LANShareManager.Core.Enums;

namespace LANShareManager.App.Views;

public partial class EditAccessDialog : Window
{
    public AccessMode SelectedAccess { get; private set; }
    public bool ApplyToNtfs => ChkApplyNtfs.IsChecked == true;

    public EditAccessDialog(string shareName, AccessMode currentAccess)
    {
        InitializeComponent();
        TxtTitle.Text = $"Настройка разрешений для '{shareName}'";
        SelectedAccess = currentAccess;

        switch (currentAccess)
        {
            case AccessMode.ReadOnly:
                RbReadOnly.IsChecked = true;
                break;
            case AccessMode.ReadWrite:
                RbReadWrite.IsChecked = true;
                break;
            case AccessMode.FullControl:
                RbFullControl.IsChecked = true;
                break;
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (RbReadOnly.IsChecked == true) SelectedAccess = AccessMode.ReadOnly;
        else if (RbFullControl.IsChecked == true) SelectedAccess = AccessMode.FullControl;
        else SelectedAccess = AccessMode.ReadWrite;

        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
