using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LifeQuest.Data;
using LifeQuest.Services;

namespace LifeQuest.Views;

public partial class LeaderboardView : UserControl
{
    private readonly I18n L = I18n.Instance;

    public LeaderboardView()
    {
        InitializeComponent();
        L.LangChanged += Render;
    }

    public void Reload() => Render();

    private async void Render()
    {
        TitleText.Text = L["leaderboard"];
        Board.Items.Clear();

        var rows = await App.Users.LeaderboardAsync();
        string? meName = Session.Instance.CurrentUser?.Username;
        int i = 0;
        foreach (var row in rows)
        {
            i++;
            Board.Items.Add(BuildRow(i, row, string.Equals(row.Username, meName, StringComparison.OrdinalIgnoreCase)));
        }
    }

    private Border BuildRow(int place, UserRepository.LeaderRow row, bool isMe)
    {
        var card = new Border
        {
            Style = (Style)FindResource("Card"),
            Margin = new Thickness(0, 0, 0, 8),
            Padding = new Thickness(16, 12, 16, 12)
        };
        if (isMe) card.BorderBrush = (Brush)FindResource("AccentBrush");

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        string medal = place switch { 1 => "🥇", 2 => "🥈", 3 => "🥉", _ => "#" + place };
        var placeText = new TextBlock
        {
            Text = medal, FontSize = place <= 3 ? 22 : 16, FontWeight = FontWeights.Bold,
            Foreground = (Brush)FindResource(place == 1 ? "GoldBrush" : "TextBrush"),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(placeText, 0);
        grid.Children.Add(placeText);

        var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var nameRow = new StackPanel { Orientation = Orientation.Horizontal };
        nameRow.Children.Add(new TextBlock
        {
            Text = row.Username, FontSize = 15, FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("TextBrush")
        });
        if (row.Prestige > 0)
            nameRow.Children.Add(new TextBlock
            {
                Text = $"  P{row.Prestige}", FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("GoldBrush")
            });
        if (row.IsAdmin)
            nameRow.Children.Add(new TextBlock
            {
                Text = "  DEV", FontSize = 10, FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("WineBrush"), VerticalAlignment = VerticalAlignment.Center
            });
        info.Children.Add(nameRow);
        info.Children.Add(new TextBlock
        {
            Text = $"{L["statLevel"]} {row.Level} · {row.Rank}",
            Style = (Style)FindResource("Muted"), Margin = new Thickness(0, 2, 0, 0)
        });
        Grid.SetColumn(info, 1);
        grid.Children.Add(info);

        var xp = new TextBlock
        {
            Text = $"{row.TotalXp} XP", FontSize = 15, FontWeight = FontWeights.Bold,
            Foreground = (Brush)FindResource("GreenBrush"), VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(xp, 2);
        grid.Children.Add(xp);

        card.Child = grid;
        return card;
    }
}
