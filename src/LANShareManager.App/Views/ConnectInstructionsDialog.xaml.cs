using System.Windows;
using LANShareManager.Core.Localization;

namespace LANShareManager.App.Views;

public partial class ConnectInstructionsDialog : Window
{
    private readonly string _shareName;
    private readonly string? _ip;
    private readonly string? _hostname;
    private AppLanguage _currentLang;

    public ConnectInstructionsDialog(string shareName, string? ip, string? hostname, AppLanguage? initialLang = null)
    {
        InitializeComponent();
        _shareName = shareName;
        _ip = ip;
        _hostname = hostname;
        _currentLang = initialLang ?? LocalizationService.CurrentLanguage;

        UpdateUiLanguage();
    }

    private void UpdateUiLanguage()
    {
        var guide = LocalizationService.GetConnectGuide(_shareName, _ip, _hostname, _currentLang);

        TxtIpPath.Text = guide.RecommendedPath;
        TxtHostPath.Text = guide.HostnamePath;
        TxtNetUseCmd.Text = guide.CmdNetUse;

        switch (_currentLang)
        {
            case AppLanguage.Kazakh:
                Title = "Басқа компьютерден қосылу нұсқаулығы";
                TxtHeaderTitle.Text = "🌐 Басқа компьютерден қалай қосылу керек?";
                TxtHeaderSub.Text = "Windows жүйесіндегі Win + R арқылы ортақ папкаға жылдам кіру";
                TxtStep1Title.Text = "Win + R пернелерін басыңыз:";
                TxtStep1Desc.Text = "Басқа компьютердің пернетақтасынан Win + R түймелерін қатар басыңыз. Экранның төменгі сол жағында «Выполнить / Run» терезесі ашылады.";
                TxtStep2Title.Text = "Мына желілік жолды көшіріп қойыңыз (UNC Path):";
                TxtIpLabel.Text = "IP мекенжай арқылы (Ұсынылады):";
                TxtHostLabel.Text = "Компьютер атауы арқылы:";
                TxtStep3Title.Text = "'Enter' немесе 'OK' басыңыз:";
                TxtStep3Desc.Text = "Папка Проводникте бірден ашылады! Сіз файлдарды көріп, өзгертіп немесе жаңа файл сала аласыз.";
                TxtCredTitle.Text = "💡 Егер Windows құпиясөз (пароль) сұраса:";
                TxtCredDesc.Text = "Осы компьютердің пайдаланушы атын (мысалы: .\\Admin) және паролін енгізіп, «Запомнить учетные данные» белгішесін қойыңыз.";
                TxtNetUseTitle.Text = "📌 Тұрақты желілік диск (Z:) қылып қосу пәрмені (CMD):";
                BtnClose.Content = "Түсінікті, жабу";
                break;

            case AppLanguage.English:
                Title = "Network Share Connection Guide";
                TxtHeaderTitle.Text = "🌐 How to connect from another PC?";
                TxtHeaderSub.Text = "Quick access to the shared folder via Windows Win + R";
                TxtStep1Title.Text = "Press Win + R on your keyboard:";
                TxtStep1Desc.Text = "On the other computer, press the Win + R keys together. The Windows 'Run' dialog will pop up.";
                TxtStep2Title.Text = "Paste or type this network UNC path:";
                TxtIpLabel.Text = "Via IP Address (Recommended):";
                TxtHostLabel.Text = "Via Computer Hostname:";
                TxtStep3Title.Text = "Press 'Enter' or click 'OK':";
                TxtStep3Desc.Text = "The folder opens in File Explorer immediately! You can view, edit, and copy files.";
                TxtCredTitle.Text = "💡 If Windows prompts for username & password:";
                TxtCredDesc.Text = "Enter this computer's local username (e.g. .\\Username) and password, then check 'Remember my credentials'.";
                TxtNetUseTitle.Text = "📌 Map as permanent network drive (Z:) in CMD:";
                BtnClose.Content = "Got it, Close";
                break;

            case AppLanguage.Russian:
            default:
                Title = "Инструкция по подключению с другого ПК";
                TxtHeaderTitle.Text = "🌐 Как подключиться с другого компьютера?";
                TxtHeaderSub.Text = "Быстрый доступ к общей папке через сочетание клавиш Win + R";
                TxtStep1Title.Text = "Нажмите Win + R на клавиатуре:";
                TxtStep1Desc.Text = "На другом компьютере одновременно нажмите Win + R. В левом нижнем углу экрана откроется окно «Выполнить».";
                TxtStep2Title.Text = "Вставьте или введите сетевой путь (UNC Path):";
                TxtIpLabel.Text = "По IP адресу (Рекомендуется):";
                TxtHostLabel.Text = "По имени компьютера:";
                TxtStep3Title.Text = "Нажмите 'Enter' или 'ОК':";
                TxtStep3Desc.Text = "Папка мгновенно откроется в Проводнике! Вы сможете просматривать и редактировать файлы.";
                TxtCredTitle.Text = "💡 Если Windows запрашивает логин и пароль:";
                TxtCredDesc.Text = "Введите имя пользователя этого ПК (например: .\\ИмяПользователя) и пароль, затем отметьте галочку «Запомнить учетные данные».";
                TxtNetUseTitle.Text = "📌 Подключить как сетевой диск (Z:) через командную строку:";
                BtnClose.Content = "Понятно, закрыть";
                break;
        }
    }

    private void BtnLangKk_Click(object sender, RoutedEventArgs e)
    {
        _currentLang = AppLanguage.Kazakh;
        LocalizationService.SetLanguage(_currentLang);
        UpdateUiLanguage();
    }

    private void BtnLangRu_Click(object sender, RoutedEventArgs e)
    {
        _currentLang = AppLanguage.Russian;
        LocalizationService.SetLanguage(_currentLang);
        UpdateUiLanguage();
    }

    private void BtnLangEn_Click(object sender, RoutedEventArgs e)
    {
        _currentLang = AppLanguage.English;
        LocalizationService.SetLanguage(_currentLang);
        UpdateUiLanguage();
    }

    private void BtnCopyIp_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(TxtIpPath.Text);
        MessageBox.Show(this, "IP жолы буферге көшірілді:\n" + TxtIpPath.Text, "Көшірілді", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnCopyHost_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(TxtHostPath.Text);
        MessageBox.Show(this, "Желілік жол көшірілді:\n" + TxtHostPath.Text, "Көшірілді", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnCopyNetUse_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(TxtNetUseCmd.Text);
        MessageBox.Show(this, "CMD пәрмені көшірілді:\n" + TxtNetUseCmd.Text, "Көшірілді", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
