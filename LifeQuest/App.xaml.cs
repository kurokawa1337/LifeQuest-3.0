using System.Windows;
using LifeQuest.Data;
using LifeQuest.Services;

namespace LifeQuest;

public partial class App : Application
{
    public static Database Db { get; private set; } = null!;
    public static UserRepository Users { get; private set; } = null!;
    public static QuestRepository Quests { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settings = Session.Instance.Settings;
        Db = new Database(settings.ConnectionString);

        try
        {
            await Db.InitializeAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Не вдалося підключитися до SQL Server та створити базу даних.\n\n" +
                $"Рядок підключення:\n{settings.ConnectionString}\n\nПомилка:\n{ex.Message}\n\n" +
                $"Перевір, що SQL Server (напр. .\\SQLEXPRESS) запущено, і виправ рядок підключення в appsettings.json.",
                "LifeQuest — помилка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        Users = new UserRepository(Db);
        Quests = new QuestRepository(Db);

        var win = new MainWindow();
        win.Show();
    }
}
