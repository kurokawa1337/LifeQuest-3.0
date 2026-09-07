using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LifeQuest.Data;
using LifeQuest.Services;

namespace LifeQuest.Views;

public partial class AdminView : UserControl
{
    private readonly MainWindow _main;
    private readonly I18n L = I18n.Instance;

    public AdminView(MainWindow main)
    {
        InitializeComponent();
        _main = main;
        L.LangChanged += Render;
    }

    public void Reload() => Render();

    private async void Render()
    {
        TitleText.Text = L["reviewTitle"];
        SubText.Text = L["reviewSub"];
        EmptyText.Text = L["queueEmpty"];

        QueueList.Items.Clear();
        var queue = await App.Quests.PendingQueueAsync();
        EmptyText.Visibility = queue.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        foreach (var item in queue)
            QueueList.Items.Add(BuildCard(item));
    }

    private Border BuildCard(QuestRepository.QueueItem it)
    {
        var card = new Border { Style = (Style)FindResource("Card"), Margin = new Thickness(0, 0, 0, 12) };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var thumb = new Border
        {
            Width = 120, Height = 90, CornerRadius = new CornerRadius(8),
            Background = (Brush)FindResource("SurfaceAltBrush"),
            Margin = new Thickness(0, 0, 16, 0), ClipToBounds = true
        };
        if (!string.IsNullOrEmpty(it.ProofPath) && File.Exists(it.ProofPath) && IsImage(it.ProofPath))
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(it.ProofPath);
                bmp.EndInit();
                thumb.Background = new ImageBrush(bmp) { Stretch = Stretch.UniformToFill };
                thumb.Cursor = System.Windows.Input.Cursors.Hand;
                string path = it.ProofPath;
                thumb.MouseLeftButtonUp += (_, __) => OpenFile(path);
            }
            catch { }
        }
        else
        {
            thumb.Child = new TextBlock
            {
                Text = "📄", FontSize = 30, HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            if (!string.IsNullOrEmpty(it.ProofPath))
            {
                thumb.Cursor = System.Windows.Input.Cursors.Hand;
                string path = it.ProofPath;
                thumb.MouseLeftButtonUp += (_, __) => OpenFile(path);
            }
        }
        Grid.SetColumn(thumb, 0);
        grid.Children.Add(thumb);

        int fullXp = GameLogic.ComputeXp(it.Difficulty, true, it.OwnerMultiplier);
        var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        info.Children.Add(new TextBlock
        {
            Text = it.Title, FontSize = 16, FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("TextBrush"), TextWrapping = TextWrapping.Wrap
        });
        info.Children.Add(new TextBlock
        {
            Text = $"{L["player"]}: {it.OwnerName} · {L[it.Difficulty]} · {L["reward"]}: {fullXp} XP",
            Style = (Style)FindResource("Muted"), Margin = new Thickness(0, 4, 0, 0)
        });
        Grid.SetColumn(info, 1);
        grid.Children.Add(info);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var approve = new Button { Content = L["approve"], Style = (Style)FindResource("BtnPrimary"), Margin = new Thickness(0, 0, 8, 0) };
        approve.Click += (_, __) => Review(it, true);
        var reject = new Button { Content = L["reject"], Style = (Style)FindResource("BtnDanger") };
        reject.Click += (_, __) => Review(it, false);
        actions.Children.Add(approve);
        actions.Children.Add(reject);
        Grid.SetColumn(actions, 2);
        grid.Children.Add(actions);

        card.Child = grid;
        return card;
    }

    private static bool IsImage(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".bmp";
    }

    private static void OpenFile(string path)
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true }); }
        catch { }
    }

    private async void Review(QuestRepository.QueueItem it, bool approve)
    {
        var owner = await App.Users.GetByIdAsync(it.OwnerId);
        var q = await App.Quests.GetAsync(it.QuestId);
        if (owner == null || q == null || q.Status != "pending") { Render(); return; }

        if (approve)
        {
            int xp = GameLogic.ComputeXp(q.Difficulty, true, owner.Profile.Multiplier);
            q.Status = "done"; q.AwardedXp = xp;
            await App.Quests.UpdateAsync(q);

            var lvl = GameLogic.AddXp(owner.Profile, xp);
            owner.Profile.QuestsDone++;
            owner.Profile.ProofsApproved++;
            await App.Users.UpdateAsync(owner);
            _main.Toast(L["approved"]);
            _ = lvl;
        }
        else
        {
            q.Status = "active"; q.HasProof = false;
            if (!string.IsNullOrEmpty(q.ProofPath) && File.Exists(q.ProofPath))
                try { File.Delete(q.ProofPath); } catch { }
            q.ProofPath = null;
            await App.Quests.UpdateAsync(q);
            _main.Toast(L["rejected"], "error");
        }

        if (owner.Id == Session.Instance.CurrentUser!.Id) _main.RefreshMe();
        Render();
    }
}
