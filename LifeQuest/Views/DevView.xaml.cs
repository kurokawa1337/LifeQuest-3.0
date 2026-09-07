using System.Windows;
using System.Windows.Controls;
using LifeQuest.Models;
using LifeQuest.Services;

namespace LifeQuest.Views;

public partial class DevView : UserControl
{
    private readonly MainWindow _main;
    private readonly I18n L = I18n.Instance;

    public DevView(MainWindow main)
    {
        InitializeComponent();
        _main = main;
        L.LangChanged += ApplyTexts;
        ApplyTexts();
    }

    private void ApplyTexts()
    {
        TitleText.Text = L["devTitle"];
        UserLbl.Text = L["devUser"];
        Xp100.Content = $"+100 XP";
        Xp500.Content = $"+500 XP";
        Lvl1.Content = $"+1 {L["statLevel"]}";
        Lvl10.Content = $"+10 {L["statLevel"]}";
        MaxLvl.Content = $"{L["setLevel"]} 100";
        ResetBtn.Content = L["resetProfile"];
    }

    public async void Reload()
    {
        ApplyTexts();
        UserSelect.Items.Clear();
        var users = await App.Users.AllAsync();
        foreach (var u in users)
            UserSelect.Items.Add(new ComboBoxItem { Content = u.Username + (u.IsAdmin ? " (admin)" : ""), Tag = u.Id });

        int myId = Session.Instance.CurrentUser!.Id;
        foreach (ComboBoxItem item in UserSelect.Items)
            if ((int)item.Tag == myId) { UserSelect.SelectedItem = item; break; }
        if (UserSelect.SelectedItem == null && UserSelect.Items.Count > 0)
            UserSelect.SelectedIndex = 0;
    }

    private async Task<User?> SelectedUser()
    {
        if (UserSelect.SelectedItem is ComboBoxItem item && item.Tag is int id)
            return await App.Users.GetByIdAsync(id);
        return null;
    }

    private async Task AfterChange(User target)
    {
        await App.Users.UpdateAsync(target);
        if (target.Id == Session.Instance.CurrentUser!.Id) _main.RefreshMe();
    }

    private async void GrantXp_Click(object sender, RoutedEventArgs e)
    {
        var u = await SelectedUser(); if (u == null) return;
        int amt = int.Parse((string)((Button)sender).Tag);
        var lvl = GameLogic.AddXp(u.Profile, amt);
        await AfterChange(u);
        _main.Toast($"{u.Username}: +{amt} XP", "xp");
        if (u.Id == Session.Instance.CurrentUser!.Id && lvl.LeveledUp)
            _main.ShowLevelUp(lvl.ToLevel, lvl.RankChanged);
    }

    private async void GrantLevel_Click(object sender, RoutedEventArgs e)
    {
        var u = await SelectedUser(); if (u == null) return;
        int levels = int.Parse((string)((Button)sender).Tag);
        for (int i = 0; i < levels && u.Profile.Level < GameLogic.MaxLevel; i++)
        {
            int need = GameLogic.XpForLevel(u.Profile.Level);
            GameLogic.AddXp(u.Profile, need - u.Profile.Xp);
        }
        await AfterChange(u);
        _main.Toast($"{u.Username}: +{levels} {L["statLevel"]}", "xp");
    }

    private async void SetMax_Click(object sender, RoutedEventArgs e)
    {
        var u = await SelectedUser(); if (u == null) return;
        while (u.Profile.Level < GameLogic.MaxLevel)
        {
            int need = GameLogic.XpForLevel(u.Profile.Level);
            GameLogic.AddXp(u.Profile, need - u.Profile.Xp);
        }
        await AfterChange(u);
        _main.Toast($"{u.Username}: {L["setLevel"]} 100", "xp");
    }

    private async void Reset_Click(object sender, RoutedEventArgs e)
    {
        var u = await SelectedUser(); if (u == null) return;
        var res = MessageBox.Show($"{L["resetProfile"]} — {u.Username}?", L["resetProfile"],
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (res != MessageBoxResult.Yes) return;

        u.Profile = new Profile();
        u.QuestDay = ""; u.QuestDayCount = 0;
        await AfterChange(u);
        _main.Toast($"{u.Username}: {L["resetProfile"]}");
    }
}
