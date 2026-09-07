using Dapper;
using LifeQuest.Models;
using LifeQuest.Services;

namespace LifeQuest.Data;

public sealed class UserRepository
{
    private readonly Database _db;
    private const string AdminUsername = "hlib";

    public UserRepository(Database db) => _db = db;

    private static string Key(string username) => username.Trim().ToLowerInvariant();

    private const string SelectCols = @"Id, Username, PasswordHash, IsAdmin, Lang, Theme, AvatarPath, City,
        Xp, Lvl, TotalXp, Prestige, Multiplier, QuestsDone, ProofsApproved, QuestDay, QuestDayCount, CreatedAt";

    private sealed class UserRow
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public bool IsAdmin { get; set; }
        public string Lang { get; set; } = "";
        public string Theme { get; set; } = "";
        public string? AvatarPath { get; set; }
        public string? City { get; set; }
        public int Xp { get; set; }
        public int Lvl { get; set; }
        public long TotalXp { get; set; }
        public int Prestige { get; set; }
        public double Multiplier { get; set; }
        public int QuestsDone { get; set; }
        public int ProofsApproved { get; set; }
        public string QuestDay { get; set; } = "";
        public int QuestDayCount { get; set; }
        public DateTime CreatedAt { get; set; }

        public User ToUser() => new()
        {
            Id = Id,
            Username = Username,
            PasswordHash = PasswordHash,
            IsAdmin = IsAdmin,
            Lang = Lang,
            Theme = Theme,
            AvatarPath = AvatarPath,
            City = City,
            Profile = new Profile
            {
                Xp = Xp,
                Level = Lvl,
                TotalXp = TotalXp,
                Prestige = Prestige,
                Multiplier = Multiplier,
                QuestsDone = QuestsDone,
                ProofsApproved = ProofsApproved
            },
            QuestDay = QuestDay,
            QuestDayCount = QuestDayCount,
            CreatedAt = CreatedAt
        };
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        await using var conn = _db.Create();
        var row = await conn.QuerySingleOrDefaultAsync<UserRow>(
            $"SELECT {SelectCols} FROM dbo.Users WHERE UsernameKey = @k", new { k = Key(username) });
        return row?.ToUser();
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        await using var conn = _db.Create();
        var row = await conn.QuerySingleOrDefaultAsync<UserRow>(
            $"SELECT {SelectCols} FROM dbo.Users WHERE Id = @id", new { id });
        return row?.ToUser();
    }

    public async Task<List<User>> AllAsync()
    {
        await using var conn = _db.Create();
        var rows = await conn.QueryAsync<UserRow>($"SELECT {SelectCols} FROM dbo.Users ORDER BY Id");
        return rows.Select(r => r.ToUser()).ToList();
    }

    public async Task<User> CreateAsync(string username, string password, string lang = "uk", string theme = "light")
    {
        bool isAdmin = Key(username) == AdminUsername;
        await using var conn = _db.Create();
        int id = await conn.ExecuteScalarAsync<int>(@"
INSERT INTO dbo.Users (Username, UsernameKey, PasswordHash, IsAdmin, Lang, Theme)
OUTPUT INSERTED.Id
VALUES (@u, @k, @p, @admin, @lang, @theme);",
            new
            {
                u = username.Trim(),
                k = Key(username),
                p = PasswordHasher.Hash(password),
                admin = isAdmin,
                lang,
                theme
            });

        return new User
        {
            Id = id,
            Username = username.Trim(),
            PasswordHash = "",
            IsAdmin = isAdmin,
            Lang = lang,
            Theme = theme,
            Profile = new Profile()
        };
    }

    public async Task<bool> ExistsAsync(string username)
    {
        await using var conn = _db.Create();
        int n = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM dbo.Users WHERE UsernameKey = @k", new { k = Key(username) });
        return n > 0;
    }

    public async Task UpdateAsync(User u)
    {
        await using var conn = _db.Create();
        await conn.ExecuteAsync(@"
UPDATE dbo.Users SET
    Lang = @lang, Theme = @theme, AvatarPath = @avatar, City = @city,
    Xp = @xp, Lvl = @lvl, TotalXp = @total, Prestige = @prestige, Multiplier = @mult,
    QuestsDone = @qdone, ProofsApproved = @proofs,
    QuestDay = @qday, QuestDayCount = @qcount
WHERE Id = @id;", new
        {
            lang = u.Lang,
            theme = u.Theme,
            avatar = u.AvatarPath,
            city = u.City,
            xp = u.Profile.Xp,
            lvl = u.Profile.Level,
            total = u.Profile.TotalXp,
            prestige = u.Profile.Prestige,
            mult = u.Profile.Multiplier,
            qdone = u.Profile.QuestsDone,
            proofs = u.Profile.ProofsApproved,
            qday = u.QuestDay,
            qcount = u.QuestDayCount,
            id = u.Id
        });
    }

    public class LeaderRow
    {
        public string Username { get; set; } = "";
        public int Level { get; set; }
        public int Prestige { get; set; }
        public long TotalXp { get; set; }
        public string Rank { get; set; } = "";
        public bool IsAdmin { get; set; }
    }

    public async Task<List<LeaderRow>> LeaderboardAsync()
    {
        await using var conn = _db.Create();
        var rows = await conn.QueryAsync<(string Username, int Level, int Prestige, long TotalXp, bool IsAdmin)>(@"
SELECT TOP 100 Username, Lvl, Prestige, TotalXp, IsAdmin
FROM dbo.Users
ORDER BY TotalXp DESC, Lvl DESC;");
        return rows.Select(r => new LeaderRow
        {
            Username = r.Username,
            Level = r.Level,
            Prestige = r.Prestige,
            TotalXp = r.TotalXp,
            Rank = GameLogic.RankForLevel(r.Level),
            IsAdmin = r.IsAdmin
        }).ToList();
    }
}
