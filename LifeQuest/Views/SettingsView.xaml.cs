using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using LifeQuest.Models;
using LifeQuest.Services;

namespace LifeQuest.Views;

public partial class SettingsView : UserControl
{
    private readonly MainWindow _main;
    private readonly I18n L = I18n.Instance;
    private bool _loading;

    public SettingsView(MainWindow main)
    {
        InitializeComponent();
        _main = main;
        L.LangChanged += ApplyTexts;
        ApplyTexts();
    }

    private User Me => Session.Instance.CurrentUser!;

    private void ApplyTexts()
    {
        TitleText.Text = L["settingsTitle"];
        AccountLbl.Text = L["account"];
        ChangeAvatarBtn.Content = L["changeAvatar"];
        AvatarHint.Text = L["avatarHint"];
        LangLbl.Text = L["language"];
        LangUk.Content = L["langUk"];
        LangEn.Content = L["langEn"];
        ThemeLbl.Text = L["theme"];
        ThemeLight.Content = L["themeLight"];
        ThemeDark.Content = L["themeDark"];
        DbLbl.Text = L["database"];
        ConnLbl.Text = L["connString"];
        TestConnBtn.Content = L["testConn"];
        LoggedAsText.Text = L["loggedAs"];
    }

    public void Reload()
    {
        _loading = true;
        ApplyTexts();
        UsernameText.Text = Me.Username;
        AvatarInitial.Text = Me.Username.Length > 0 ? Me.Username[..1].ToUpper() : "?";
        SetAvatarPreview();

        LangUk.IsChecked = Me.Lang == "uk";
        LangEn.IsChecked = Me.Lang == "en";
        ThemeLight.IsChecked = Me.Theme != "dark";
        ThemeDark.IsChecked = Me.Theme == "dark";

        ConnBox.Text = Session.Instance.Settings.ConnectionString;
        _loading = false;
    }

    private void SetAvatarPreview()
    {
        if (!string.IsNullOrEmpty(Me.AvatarPath) && File.Exists(Me.AvatarPath))
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(Me.AvatarPath);
                bmp.EndInit();
                AvatarCircle.Fill = new ImageBrush(bmp) { Stretch = Stretch.UniformToFill };
                AvatarInitial.Visibility = Visibility.Collapsed;
                return;
            }
            catch { }
        }
        AvatarCircle.Fill = new SolidColorBrush((Color)FindResource("AccentColor"));
        AvatarInitial.Visibility = Visibility.Visible;
    }

    private async void Lang_Checked(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        if (sender is RadioButton rb && rb.Tag is string lang)
        {
            L.SetLang(lang);
            Me.Lang = lang;
            await App.Users.UpdateAsync(Me);
        }
    }

    private async void Theme_Checked(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        if (sender is RadioButton rb && rb.Tag is string theme)
        {
            ThemeManager.Apply(theme);
            Me.Theme = theme;
            await App.Users.UpdateAsync(Me);
            _main.RefreshHero();
        }
    }

    private async void ChangeAvatar_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Зображення|*.png;*.jpg;*.jpeg;*.gif;*.webp" };
        if (dlg.ShowDialog() != true) return;

        try
        {
            string dir = Path.Combine(AppContext.BaseDirectory, "data", "avatars");
            Directory.CreateDirectory(dir);
            string dest = Path.Combine(dir, $"u{Me.Id}{Path.GetExtension(dlg.FileName)}");
            File.Copy(dlg.FileName, dest, true);
            Me.AvatarPath = dest;
            await App.Users.UpdateAsync(Me);
            SetAvatarPreview();
            _main.RefreshHero();
            _main.Toast(L["avatarUpdated"]);
        }
        catch (Exception ex) { _main.Toast(ex.Message, "error"); }
    }

    private async void TestConn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await App.Db.TestConnectionAsync();
            _main.Toast(L["connOk"]);
        }
        catch (Exception ex)
        {
            _main.Toast($"{L["connFail"]}: {ex.Message}", "error");
        }
    }
}
