using System.Text;
using System.Windows;

namespace LANShareManager.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
        catch { }

        base.OnStartup(e);

        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show(
                $"Произошла непредвиденная ошибка:\n{args.Exception.Message}\n\nПодробности в системном журнале.",
                "Ошибка приложения",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };
    }
}
