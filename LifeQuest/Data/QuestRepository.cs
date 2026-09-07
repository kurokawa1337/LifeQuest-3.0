using Dapper;
using LifeQuest.Models;

namespace LifeQuest.Data;

public sealed class QuestRepository
{
    private readonly Database _db;
    public QuestRepository(Database db) => _db = db;

    private const string Cols = @"Id, UserId, Title, Descr AS [Description], Difficulty, Status,
        HasProof, ProofPath, AwardedXp, CreatedAt, CreatedDay";

    public async Task<List<Quest>> ForUserAsync(int userId)
    {
        await using var conn = _db.Create();
        var rows = await conn.QueryAsync<Quest>(
            $"SELECT {Cols} FROM dbo.Quests WHERE UserId = @uid ORDER BY CreatedAt DESC", new { uid = userId });
        return rows.ToList();
    }

    public async Task<Quest?> GetAsync(int id)
    {
        await using var conn = _db.Create();
        return await conn.QuerySingleOrDefaultAsync<Quest>(
            $"SELECT {Cols} FROM dbo.Quests WHERE Id = @id", new { id });
    }

    public async Task<Quest> CreateAsync(int userId, string title, string desc, string difficulty, string day)
    {
        await using var conn = _db.Create();
        int id = await conn.ExecuteScalarAsync<int>(@"
INSERT INTO dbo.Quests (UserId, Title, Descr, Difficulty, Status, CreatedDay)
OUTPUT INSERTED.Id
VALUES (@uid, @title, @desc, @diff, 'active', @day);",
            new
            {
                uid = userId,
                title = title.Length > 80 ? title[..80] : title,
                desc = desc.Length > 240 ? desc[..240] : desc,
                diff = difficulty,
                day
            });

        return new Quest
        {
            Id = id,
            UserId = userId,
            Title = title,
            Description = desc,
            Difficulty = difficulty,
            Status = "active",
            CreatedDay = day
        };
    }

    public async Task UpdateAsync(Quest q)
    {
        await using var conn = _db.Create();
        await conn.ExecuteAsync(@"
UPDATE dbo.Quests SET
    Title = @title, Descr = @desc, Difficulty = @diff, Status = @status,
    HasProof = @hasProof, ProofPath = @proof, AwardedXp = @xp
WHERE Id = @id;", new
        {
            title = q.Title,
            desc = q.Description,
            diff = q.Difficulty,
            status = q.Status,
            hasProof = q.HasProof,
            proof = q.ProofPath,
            xp = q.AwardedXp,
            id = q.Id
        });
    }

    public async Task DeleteAsync(int id)
    {
        await using var conn = _db.Create();
        await conn.ExecuteAsync("DELETE FROM dbo.Quests WHERE Id = @id", new { id });
    }

    public class QueueItem
    {
        public int QuestId { get; set; }
        public int OwnerId { get; set; }
        public string OwnerName { get; set; } = "";
        public string Title { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public string? ProofPath { get; set; }
        public double OwnerMultiplier { get; set; }
    }

    public async Task<List<QueueItem>> PendingQueueAsync()
    {
        await using var conn = _db.Create();
        var rows = await conn.QueryAsync<QueueItem>(@"
SELECT q.Id AS QuestId, q.UserId AS OwnerId, u.Username AS OwnerName, q.Title, q.Difficulty,
       q.ProofPath, u.Multiplier AS OwnerMultiplier
FROM dbo.Quests q
JOIN dbo.Users u ON u.Id = q.UserId
WHERE q.Status = 'pending'
ORDER BY q.CreatedAt ASC;");
        return rows.ToList();
    }
}
