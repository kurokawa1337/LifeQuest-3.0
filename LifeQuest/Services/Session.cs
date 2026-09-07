using LifeQuest.Models;

namespace LifeQuest.Services;

public sealed class Session
{
    public static Session Instance { get; } = new();

    public User? CurrentUser { get; set; }
    public AppSettings Settings { get; } = AppSettings.Load();

    public bool IsLoggedIn => CurrentUser != null;
    public bool IsAdmin => CurrentUser?.IsAdmin ?? false;

    public void Logout() => CurrentUser = null;
}
