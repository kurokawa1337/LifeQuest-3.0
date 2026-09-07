using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.IO;
using LifeQuest.Models;
using LifeQuest.Services;
using LifeQuest.Views;

namespace LifeQuest;

public partial class MainWindow : Window
{
    private bool _registerMode = false;
    private readonly I18n L = I18n.Instance;

    private QuestsView? _quests;
    private StatsView? _stats;
    private LeaderboardView? _leaderboard;
    private SettingsView? _settings;
    private AdminView? _admin;
    private DevView? _dev;

    public MainWindow()
    {
        InitializeComponent();
        L.LangChanged += ApplyAuthTexts;
        ApplyAuthTexts();
        UpdateAuthTabs();
    }

    private User Me => Session.Instance.CurrentUser!;


    private void ApplyAuthTexts()
    {
        TaglineText.Text = L["tagline"];
        TabLogin.Content = L["login"];
        TabRegister.Content = L["register"];
        LblUsername.Text = L["username"];
        LblPassword.Text = L["password"];
        LblRepeat.Text = L["repeatPassword"];
        BtnSubmit.Content = _registerMode ? L["createHero"] : L["signIn"];
    }

    private void UpdateAuthTabs()
    {
        RepeatRow.Visibility = _registerMode ? Visibility.Visible : Visibility.Collapsed;
        TabLogin.Style = (Style)FindResource(_registerMode ? "BtnGhost" : "BtnPrimary");
        TabRegister.Style = (Style)FindResource(_registerMode ? "BtnPrimary" : "BtnGhost");
        BtnSubmit.Content = _registerMode ? L["createHero"] : L["signIn"];
        AuthError.Visibility = Visibility.Collapsed;
    }

    private void TabLogin_Click(object s, RoutedEventArgs e) { _registerMode = false; UpdateAuthTabs(); }
    private void TabRegister_Click(object s, RoutedEventArgs e) { _registerMode = true; UpdateAuthTabs(); }

    private void LangUk_Click(object s, RoutedEventArgs e) => L.SetLang("uk");
    private void LangEn_Click(object s, RoutedEventArgs e) => L.SetLang("en");

    private void ShowAuthError(string msg)
    {
        AuthError.Text = msg;
        AuthError.Visibility = Visibility.Visible;
    }

    private async void AuthSubmit_Click(object s, RoutedEventArgs e)
    {
        string username = InpUsername.Text.Trim();
        string password = InpPassword.Password;

        try
        {
            if (_registerMode)
            {
                if (username.Length < 2) { ShowAuthError(L["usernameShort"]); return; }
                if (password.Length < 4) { ShowAuthError(L["passwordShort"]); return; }
                if (InpPassword.Password != InpPassword2.Password) { ShowAuthError(L["passwordsMismatch"]); return; }
                if (await App.Users.ExistsAsync(username)) { ShowAuthError(L["userTaken"]); return; }

                var created = await App.Users.CreateAsync(username, password, L.Lang, ThemeManager.Current);
                Session.Instance.CurrentUser = await App.Users.GetByIdAsync(created.Id);
                EnterApp();
            }
            else
            {
                var user = await App.Users.GetByUsernameAsync(username);
                if (user == null || !PasswordHasher.Verify(password, user.PasswordHash))
                {
                    ShowAuthError(L["badCredentials"]); return;
                }
                Session.Instance.CurrentUser = user;
                EnterApp();
            }
        }
        catch (Exception ex)
        {
            ShowAuthError(ex.Message);
        }
    }

    private void EnterApp()
    {
        L.SetLang(Me.Lang);
        ThemeManager.Apply(Me.Theme);

        AuthScreen.Visibility = Visibility.Collapsed;
        AppShell.Visibility = Visibility.Visible;

        bool admin = Me.IsAdmin;
        NavAdmin.Visibility = admin ? Visibility.Visible : Visibility.Collapsed;
        NavDev.Visibility = admin ? Visibility.Visible : Visibility.Collapsed;

        _quests = null; _stats = null; _leaderboard = null; _settings = null; _admin = null; _dev = null;

        RefreshHero();
        NavQuests.IsChecked = true;
        SwitchView("quests");

        L.LangChanged -= ApplyNavTexts;
        L.LangChanged += ApplyNavTexts;
        ApplyNavTexts();

        InpPassword.Clear(); InpPassword2.Clear(); InpUsername.Clear();
    }

    private void ApplyNavTexts()
    {
        NavQuests.Content = L["quests"];
        NavStats.Content = L["stats"];
        NavLeaders.Content = L["leaderboard"];
        NavSettings.Content = L["settings"];
        NavAdmin.Content = L["admin"];
        NavDev.Content = L["dev"];
        LogoutBtn.Content = L["logout"];
    }

    private void Logout_Click(object s, RoutedEventArgs e)
    {
        Session.Instance.Logout();
        AppShell.Visibility = Visibility.Collapsed;
        AuthScreen.Visibility = Visibility.Visible;
        _registerMode = false;
        UpdateAuthTabs();
    }


    public void RefreshHero()
    {
        var p = Me.Profile;
        HeroName.Text = Me.Username;
        HeroLevelRank.Text = $"{L["statLevel"]} {p.Level} · {GameLogic.RankForLevel(p.Level)}";
        HeroMult.Text = $"×{p.Multiplier:0.00}";
        HeroAvatarInitial.Text = Me.Username.Length > 0 ? Me.Username[..1].ToUpper() : "?";

        SetAvatar();

        DevBadge.Visibility = Me.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
        PrestigeBadge.Visibility = p.Prestige > 0 ? Visibility.Visible : Visibility.Collapsed;
        PrestigeBadgeText.Text = $"P{p.Prestige}";

        var (need, cur, pct) = GameLogic.Progress(p);
        bool atMax = p.Level >= GameLogic.MaxLevel;
        XpText.Text = atMax ? $"{L["max"]} · {cur} XP" : $"{cur} / {need} XP";
        XpFill.Width = Math.Max(0, 250 * pct / 100.0);
    }

    private void SetAvatar()
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
                HeroAvatar.Fill = new ImageBrush(bmp) { Stretch = Stretch.UniformToFill };
                HeroAvatarInitial.Visibility = Visibility.Collapsed;
                return;
            }
            catch { }
        }
        HeroAvatar.Fill = new SolidColorBrush((Color)FindResource("AccentColor"));
        HeroAvatarInitial.Visibility = Visibility.Visible;
    }


    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded && ContentHost == null) return;
        if (sender is RadioButton rb && rb.Tag is string tag) SwitchView(tag);
    }

    private void SwitchView(string view)
    {
        if (ContentHost == null || !Session.Instance.IsLoggedIn) return;

        switch (view)
        {
            case "quests":
                _quests ??= new QuestsView(this);
                _quests.Reload();
                ContentHost.Content = _quests;
                break;
            case "stats":
                _stats ??= new StatsView(this);
                _stats.Reload();
                ContentHost.Content = _stats;
                break;
            case "leaderboard":
                _leaderboard ??= new LeaderboardView();
                _leaderboard.Reload();
                ContentHost.Content = _leaderboard;
                break;
            case "settings":
                _settings ??= new SettingsView(this);
                _settings.Reload();
                ContentHost.Content = _settings;
                break;
            case "admin":
                _admin ??= new AdminView(this);
                _admin.Reload();
                ContentHost.Content = _admin;
                break;
            case "dev":
                _dev ??= new DevView(this);
                _dev.Reload();
                ContentHost.Content = _dev;
                break;
        }
    }

    public void GoToStats()
    {
        NavStats.IsChecked = true;
    }


    public void Toast(string message, string type = "success")
    {
        Brush bg = type switch
        {
            "error" => (Brush)FindResource("DangerBrush"),
            "xp" => (Brush)FindResource("GoldBrush"),
            _ => (Brush)FindResource("GreenBrush")
        };

        var border = new Border
        {
            Background = bg,
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 8, 0, 0),
            Child = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 320
            }
        };
        ToastHost.Children.Add(border);

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180));
        border.BeginAnimation(OpacityProperty, fadeIn);

        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2.6) };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
            fadeOut.Completed += (_, __) => ToastHost.Children.Remove(border);
            border.BeginAnimation(OpacityProperty, fadeOut);
        };
        timer.Start();
    }


    public void ShowLevelUp(int level, bool rankChanged)
    {
        LevelUpTitle.Text = rankChanged ? L["newRank"] : L["lvlUp"];
        LevelUpSub.Text = $"{L["statLevel"]} {level} · {GameLogic.RankForLevel(level)}";
        LevelUpFx.Visibility = Visibility.Visible;
        LevelUpFx.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)));

        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1.8) };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
            fade.Completed += (_, __) => LevelUpFx.Visibility = Visibility.Collapsed;
            LevelUpFx.BeginAnimation(OpacityProperty, fade);
        };
        timer.Start();
    }

    public async void RefreshMe()
    {
        var fresh = await App.Users.GetByIdAsync(Me.Id);
        if (fresh != null) Session.Instance.CurrentUser = fresh;
        RefreshHero();
    }
}
