using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using LifeQuest.Models;
using LifeQuest.Services;

namespace LifeQuest.Views;

public partial class QuestsView : UserControl
{
    private readonly MainWindow _main;
    private readonly I18n L = I18n.Instance;

    private string _filter = "all";
    private string _newDiff = "easy";
    private int _proofQuestId = -1;
    private string? _proofFilePath;
    private DailyBrief? _brief;
    private bool _briefFailed;
    private bool _briefLoading;
    private int _renderSeq;

    public QuestsView(MainWindow main)
    {
        InitializeComponent();
        _main = main;
        L.LangChanged += ApplyTexts;
        ApplyTexts();
    }

    private User Me => Session.Instance.CurrentUser!;

    private void ApplyTexts()
    {
        TitleText.Text = L["questBoard"];
        SubText.Text = L["questBoardSub"];
        AddBtn.Content = "+ " + L["newQuest"];
        FAll.Content = L["all"];
        FActive.Content = L["active"];
        FPending.Content = L["pending"];
        FDone.Content = L["done"];
        ModalTitle.Text = L["newQuest"];
        MLblTitle.Text = L["title"];
        MLblDesc.Text = L["description"];
        MLblDiff.Text = L["difficulty"];
        DEasy.Content = L["easy"];
        DMedium.Content = L["medium"];
        DHard.Content = L["hard"];
        MCancel.Content = L["cancel"];
        MSave.Content = L["create"];
        PTitle.Text = L["proofTitle"];
        PWarn.Text = L["proofWarn"];
        PPick.Content = L["pickFile"];
        PSubmit.Content = L["submitProof"];
        PSkip.Content = L["skipProof"];
        PClose.Content = L["cancel"];
        BriefTitleText.Text = L["briefTitle"];
        CityLbl.Text = L["city"] + ":";
        RefreshBtn.Content = L["refresh"];
        IdeasText.Text = L["ideasToday"];
        if (Session.Instance.CurrentUser != null)
        {
            _ = RenderAsync();
            _ = LoadBriefAsync(force: false);
        }
    }

    public void Reload()
    {
        if (Session.Instance.CurrentUser == null) return;
        _ = RenderAsync();
        _ = LoadBriefAsync(force: false);
    }

    private static string DifficultyColorKey(string diff) => diff switch
    {
        "hard" => "DangerBrush",
        "medium" => "GoldBrush",
        _ => "GreenBrush"
    };

    private async Task RenderAsync()
    {
        int seq = ++_renderSeq;
        string day = DateTime.Now.ToString("yyyy-MM-dd");
        int usedToday = Me.QuestDay == day ? Me.QuestDayCount : 0;
        int left = Math.Max(0, GameLogic.DailyQuestLimit - usedToday);
        DailyText.Text = $"{L["dailyLeft"]}: {left}/{GameLogic.DailyQuestLimit}";
        if (CityBox.Text.Length == 0 && !string.IsNullOrEmpty(Me.City)) CityBox.Text = Me.City;

        var quests = await App.Quests.ForUserAsync(Me.Id);
        if (seq != _renderSeq || Session.Instance.CurrentUser == null) return;

        var filtered = _filter == "all" ? quests : quests.FindAll(q => q.Status == _filter);

        QuestList.Items.Clear();
        EmptyText.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyText.Text = L["noQuests"];

        foreach (var q in filtered)
            QuestList.Items.Add(BuildQuestCard(q));
    }

    private Border BuildQuestCard(Quest q)
    {
        var card = new Border
        {
            Style = (Style)FindResource("Card"),
            Margin = new Thickness(0, 0, 0, 12)
        };

        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var stripe = new Border
        {
            Width = 6, CornerRadius = new CornerRadius(3),
            Background = (System.Windows.Media.Brush)FindResource(DifficultyColorKey(q.Difficulty)),
            Margin = new Thickness(0, 0, 14, 0)
        };
        Grid.SetColumn(stripe, 0);
        root.Children.Add(stripe);

        var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        info.Children.Add(new TextBlock
        {
            Text = q.Title, FontSize = 16, FontWeight = FontWeights.SemiBold,
            Foreground = (System.Windows.Media.Brush)FindResource("TextBrush"), TextWrapping = TextWrapping.Wrap
        });
        if (!string.IsNullOrWhiteSpace(q.Description))
            info.Children.Add(new TextBlock
            {
                Text = q.Description, Style = (Style)FindResource("Muted"),
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 0)
            });

        string statusLabel = q.Status switch
        {
            "done" => $"✓ {L["done"]} · +{q.AwardedXp} XP",
            "pending" => $"⏳ {L["pending"]}",
            _ => $"{L[q.Difficulty]} · {GameLogic.BaseXp(q.Difficulty)} XP"
        };
        info.Children.Add(new TextBlock
        {
            Text = statusLabel, Style = (Style)FindResource("Muted"),
            Margin = new Thickness(0, 6, 0, 0), FontWeight = FontWeights.SemiBold
        });
        Grid.SetColumn(info, 1);
        root.Children.Add(info);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        if (q.Status == "active")
        {
            var complete = new Button { Content = L["complete"], Style = (Style)FindResource("BtnPrimary"), Margin = new Thickness(0, 0, 8, 0) };
            complete.Click += (_, __) => OpenProofModal(q);
            actions.Children.Add(complete);
        }
        var del = new Button { Content = "🗑", Style = (Style)FindResource("BtnGhost"), Padding = new Thickness(12, 8, 12, 8) };
        del.Click += (_, __) => DeleteQuest(q);
        actions.Children.Add(del);

        Grid.SetColumn(actions, 2);
        root.Children.Add(actions);

        card.Child = root;
        return card;
    }


    private void Filter_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tag) { _filter = tag; if (IsLoaded) _ = RenderAsync(); }
    }


    private void Add_Click(object sender, RoutedEventArgs e)
    {
        MTitle.Text = ""; MDesc.Text = ""; DEasy.IsChecked = true; _newDiff = "easy";
        QuestModal.Visibility = Visibility.Visible;
    }

    private void AddSuggestion(string title)
    {
        MTitle.Text = title; MDesc.Text = ""; DEasy.IsChecked = true; _newDiff = "easy";
        QuestModal.Visibility = Visibility.Visible;
    }

    private void Diff_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tag) _newDiff = tag;
    }

    private void ModalCancel_Click(object sender, RoutedEventArgs e) => QuestModal.Visibility = Visibility.Collapsed;

    private async void ModalSave_Click(object sender, RoutedEventArgs e)
    {
        string title = MTitle.Text.Trim();
        if (string.IsNullOrEmpty(title)) { _main.Toast(L["title"] + " — " + L["errorTitle"], "error"); return; }

        string day = DateTime.Now.ToString("yyyy-MM-dd");
        if (Me.QuestDay != day) { Me.QuestDay = day; Me.QuestDayCount = 0; }
        if (Me.QuestDayCount >= GameLogic.DailyQuestLimit)
        {
            _main.Toast(L["dailyLimitReached"], "error");
            return;
        }

        await App.Quests.CreateAsync(Me.Id, title, MDesc.Text.Trim(), _newDiff, day);
        Me.QuestDayCount++;
        await App.Users.UpdateAsync(Me);

        QuestModal.Visibility = Visibility.Collapsed;
        _main.Toast(L["questAdded"]);
        _ = RenderAsync();
    }


    private async void DeleteQuest(Quest q)
    {
        var res = MessageBox.Show(L["confirmDelete"], L["delete"], MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (res != MessageBoxResult.Yes) return;

        if (!string.IsNullOrEmpty(q.ProofPath) && File.Exists(q.ProofPath))
            try { File.Delete(q.ProofPath); } catch { }

        await App.Quests.DeleteAsync(q.Id);
        _main.Toast(L["questDeleted"]);
        _ = RenderAsync();
    }


    private void OpenProofModal(Quest q)
    {
        _proofQuestId = q.Id;
        _proofFilePath = null;
        PFileName.Text = "";
        int full = GameLogic.ComputeXp(q.Difficulty, true, Me.Profile.Multiplier);
        PFull.Text = $"{L["proofFull"]} {full} XP";
        ProofModal.Visibility = Visibility.Visible;
    }

    private void ProofClose_Click(object sender, RoutedEventArgs e) => ProofModal.Visibility = Visibility.Collapsed;

    private void ProofPick_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Зображення/відео|*.png;*.jpg;*.jpeg;*.gif;*.webp;*.mp4;*.webm|Усі файли|*.*"
        };
        if (dlg.ShowDialog() == true)
        {
            _proofFilePath = dlg.FileName;
            PFileName.Text = Path.GetFileName(dlg.FileName);
        }
    }

    private async void ProofSubmit_Click(object sender, RoutedEventArgs e)
    {
        var q = await App.Quests.GetAsync(_proofQuestId);
        if (q == null || q.Status != "active") { ProofModal.Visibility = Visibility.Collapsed; return; }
        if (string.IsNullOrEmpty(_proofFilePath)) { _main.Toast(L["pickFile"], "error"); return; }

        try
        {
            string proofsDir = Path.Combine(AppContext.BaseDirectory, "data", "proofs");
            Directory.CreateDirectory(proofsDir);
            string ext = Path.GetExtension(_proofFilePath);
            string dest = Path.Combine(proofsDir, $"u{Me.Id}_q{q.Id}{ext}");
            File.Copy(_proofFilePath, dest, true);
            q.ProofPath = dest;
        }
        catch (Exception ex) { _main.Toast(ex.Message, "error"); return; }

        q.HasProof = true;
        q.Status = "pending";
        await App.Quests.UpdateAsync(q);

        ProofModal.Visibility = Visibility.Collapsed;
        _main.Toast(L["proofSent"]);
        _ = RenderAsync();
    }

    private async void ProofSkip_Click(object sender, RoutedEventArgs e)
    {
        var q = await App.Quests.GetAsync(_proofQuestId);
        if (q == null || q.Status != "active") { ProofModal.Visibility = Visibility.Collapsed; return; }

        int xp = GameLogic.ComputeXp(q.Difficulty, false, Me.Profile.Multiplier);
        q.Status = "done"; q.HasProof = false; q.AwardedXp = xp;
        await App.Quests.UpdateAsync(q);

        var lvl = GameLogic.AddXp(Me.Profile, xp);
        Me.Profile.QuestsDone++;
        await App.Users.UpdateAsync(Me);

        ProofModal.Visibility = Visibility.Collapsed;
        _main.RefreshHero();
        _main.Toast($"{L["xpGained"]}: +{xp} XP (×⅓)", "xp");
        if (lvl.LeveledUp) _main.ShowLevelUp(lvl.ToLevel, lvl.RankChanged);
        _ = RenderAsync();
    }


    private async void BriefRefresh_Click(object sender, RoutedEventArgs e)
    {
        string city = CityBox.Text.Trim();
        if (Me.City != city)
        {
            Me.City = string.IsNullOrEmpty(city) ? null : city;
            await App.Users.UpdateAsync(Me);
        }
        await LoadBriefAsync(force: true);
    }

    private async Task LoadBriefAsync(bool force)
    {
        if (_briefLoading) return;
        if (!force && _brief != null) { ApplyBrief(); return; }

        _briefLoading = true;
        RefreshBtn.IsEnabled = false;
        try
        {
            _brief = await DailyBriefService.GetBriefAsync(CityBox.Text);
            _briefFailed = false;
            ApplyBrief();
        }
        catch
        {
            _briefFailed = true;
            ApplyBrief();
        }
        finally
        {
            _briefLoading = false;
            RefreshBtn.IsEnabled = true;
        }
    }

    private void ApplyBrief()
    {
        if (_brief == null)
        {
            QuoteText.Text = _briefFailed ? L["briefFail"] : "";
            QuoteAuthorText.Text = "";
            WeatherIcon.Text = "";
            WeatherTemp.Text = "";
            WeatherDesc.Text = "";
            WeatherWind.Text = "";
            SuggRow.Children.Clear();
            return;
        }

        var b = _brief;
        QuoteText.Text = b.Quote == null ? L["briefFail"] : $"“{b.Quote.Text}”";
        QuoteAuthorText.Text = b.Quote == null ? "" : "— " + b.Quote.Author;

        var w = b.Weather;
        string cat = b.Category;
        WeatherIcon.Text = WeatherEmoji(cat);
        WeatherTemp.Text = w == null ? "" : $"{w.TempC:0}°C";
        WeatherDesc.Text = w == null ? "" : $"{L["wcode_" + cat]} · {w.City}, {w.Country}".TrimEnd(' ', ',');
        WeatherWind.Text = w == null ? "" : $"{L["wind"]}: {w.WindMs:0.0} m/s";

        SuggRow.Children.Clear();
        for (int i = 1; i <= 3; i++)
        {
            string text = L[$"sugg_{cat}{i}"];
            var chip = new Button
            {
                Content = "+ " + text,
                Style = (Style)FindResource("BtnGhost"),
                FontSize = 12,
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 8, 8)
            };
            string captured = text;
            chip.Click += (_, __) => AddSuggestion(captured);
            SuggRow.Children.Add(chip);
        }
    }

    private static string WeatherEmoji(string cat) => cat switch
    {
        "clear" => "☀️",
        "clouds" => "⛅",
        "fog" => "🌫️",
        "rain" => "🌧️",
        "snow" => "❄️",
        "storm" => "⛈️",
        _ => "🌍"
    };
}
