using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LifeQuest.Models;
using LifeQuest.Services;

namespace LifeQuest.Views;

public partial class StatsView : UserControl
{
    private readonly MainWindow _main;
    private readonly I18n L = I18n.Instance;
    private int _renderSeq;

    public StatsView(MainWindow main)
    {
        InitializeComponent();
        _main = main;
        L.LangChanged += Render;
    }

    private User Me => Session.Instance.CurrentUser!;

    public void Reload()
    {
        if (Session.Instance.CurrentUser == null) return;
        _ = RenderAsync();
    }

    private async void Render()
    {
        if (Session.Instance.CurrentUser == null) return;
        await RenderAsync();
    }

    private async Task RenderAsync()
    {
        int seq = ++_renderSeq;
        TitleText.Text = L["stats"];
        ActivityTitle.Text = L["activityTitle"];
        DiffTitle.Text = L["diffTitle"];
        var p = Me.Profile;
        int need = p.Level >= GameLogic.MaxLevel ? 0 : GameLogic.XpForLevel(p.Level);

        var cards = new (string label, string value, string color)[]
        {
            (L["statLevel"], p.Level.ToString(), "GoldBrush"),
            (L["statRank"], GameLogic.RankForLevel(p.Level), "GoldBrush"),
            (L["statTotalXp"], p.TotalXp.ToString(), "GreenBrush"),
            (L["statQuests"], p.QuestsDone.ToString(), "TextBrush"),
            (L["statProofs"], p.ProofsApproved.ToString(), "TextBrush"),
            (L["statPrestige"], "P" + p.Prestige, "WineBrush"),
            (L["statMult"], "×" + p.Multiplier.ToString("0.00"), "GoldBrush"),
            (L["statToNext"], p.Level >= GameLogic.MaxLevel ? L["max"] : (need - p.Xp) + " XP", "TextBrush"),
        };

        StatsGrid.Items.Clear();
        foreach (var c in cards)
            StatsGrid.Items.Add(BuildCard(c.label, c.value, c.color));

        bool atMax = p.Level >= GameLogic.MaxLevel;
        PrestigeBtn.Visibility = atMax ? Visibility.Visible : Visibility.Collapsed;
        PrestigeBtn.Content = L["prestigeBtn"];

        var quests = await App.Quests.ForUserAsync(Me.Id);
        if (seq != _renderSeq || Session.Instance.CurrentUser == null) return;

        BuildActivityChart(quests);
        BuildDifficultyChart(quests);
    }

    private void BuildActivityChart(List<Quest> quests)
    {
        var days = Enumerable.Range(0, 14)
            .Select(i => DateTime.Today.AddDays(i - 13))
            .ToArray();

        int[] counts = days.AsParallel()
            .Select(d => quests.Count(q => LocalDate(q.CreatedAt) == d))
            .ToArray();

        ActivityBars.Children.Clear();
        ActivityBars.ColumnDefinitions.Clear();
        int max = Math.Max(1, counts.Max());

        for (int i = 0; i < days.Length; i++)
        {
            ActivityBars.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var col = new Grid { Margin = new Thickness(2, 0, 2, 0) };

            var baseline = new Border
            {
                Height = 2, VerticalAlignment = VerticalAlignment.Bottom,
                Background = (Brush)FindResource("BorderBrush")
            };
            col.Children.Add(baseline);

            if (counts[i] > 0)
            {
                double h = 14 + 114.0 * counts[i] / max;
                var bar = new Border
                {
                    Height = h, VerticalAlignment = VerticalAlignment.Bottom,
                    CornerRadius = new CornerRadius(3, 3, 0, 0),
                    Background = (Brush)FindResource("AccentBrush"),
                    ToolTip = $"{days[i]:dd.MM} — {counts[i]}"
                };
                col.Children.Add(bar);
            }

            Grid.SetColumn(col, i);
            ActivityBars.Children.Add(col);
        }

        ActivityRange.Text = $"{days[0]:dd.MM} — {days[^1]:dd.MM}";
    }

    private void BuildDifficultyChart(List<Quest> quests)
    {
        var counts = new[] { "easy", "medium", "hard" }
            .AsParallel()
            .Select(d => (key: d, n: quests.Count(q => q.Difficulty == d)))
            .ToArray();

        int total = Math.Max(1, counts.Sum(c => c.n));

        DiffBars.Children.Clear();
        DiffBars.RowDefinitions.Clear();
        var colors = new[] { "GreenBrush", "GoldBrush", "DangerBrush" };

        for (int i = 0; i < counts.Length; i++)
        {
            DiffBars.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var row = new Grid { Margin = new Thickness(0, i == 0 ? 0 : 12, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var bar = new Grid { Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(counts[i].n, GridUnitType.Star) });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(total - counts[i].n, GridUnitType.Star) });
            bar.Background = (Brush)FindResource("SurfaceAltBrush");
            bar.Height = 22;

            var fill = new Border
            {
                CornerRadius = new CornerRadius(6, 0, 0, 6),
                Background = (Brush)FindResource(colors[i])
            };
            if (counts[i].n == 0) fill.Visibility = Visibility.Hidden;
            Grid.SetColumn(fill, 0);
            bar.Children.Add(fill);

            Grid.SetColumn(bar, 0);
            row.Children.Add(bar);

            var label = new TextBlock
            {
                Text = $"{L[counts[i].key]} · {counts[i].n}",
                FontSize = 12, Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)FindResource("TextBrush")
            };
            Grid.SetColumn(label, 1);
            row.Children.Add(label);

            Grid.SetRow(row, i);
            DiffBars.Children.Add(row);
        }

        if (quests.Count == 0)
        {
            DiffBars.Children.Add(new TextBlock
            {
                Text = L["noData"], Style = (Style)FindResource("Muted"), Margin = new Thickness(0, 4, 0, 0)
            });
        }
    }

    private static DateTime LocalDate(DateTime storedUtc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(storedUtc, DateTimeKind.Utc), TimeZoneInfo.Local).Date;

    private Border BuildCard(string label, string value, string colorKey)
    {
        var card = new Border { Style = (Style)FindResource("Card"), Margin = new Thickness(0, 0, 12, 12) };
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = label, Style = (Style)FindResource("Muted"), TextWrapping = TextWrapping.Wrap
        });
        sp.Children.Add(new TextBlock
        {
            Text = value, FontSize = 26, FontWeight = FontWeights.Bold,
            Foreground = (Brush)FindResource(colorKey), Margin = new Thickness(0, 6, 0, 0)
        });
        card.Child = sp;
        return card;
    }

    private async void Prestige_Click(object sender, RoutedEventArgs e)
    {
        if (!GameLogic.Prestige(Me.Profile)) return;
        await App.Users.UpdateAsync(Me);
        _main.RefreshHero();
        _ = RenderAsync();
        _main.Toast($"{L["prestigeDone"]} ×{Me.Profile.Multiplier:0.00}", "xp");
        _main.ShowLevelUp(1, true);
    }
}
