namespace LifeQuest.Models;

public class Profile
{
    public int Xp { get; set; } = 0;
    public int Level { get; set; } = 1;
    public long TotalXp { get; set; } = 0;
    public int Prestige { get; set; } = 0;
    public double Multiplier { get; set; } = 1.0;
    public int QuestsDone { get; set; } = 0;
    public int ProofsApproved { get; set; } = 0;
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsAdmin { get; set; }
    public string Lang { get; set; } = "uk";
    public string Theme { get; set; } = "light";
    public string? AvatarPath { get; set; }
    public string? City { get; set; }
    public Profile Profile { get; set; } = new();

    public string QuestDay { get; set; } = "";
    public int QuestDayCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Quest
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Difficulty { get; set; } = "easy";
    public string Status { get; set; } = "active";
    public bool HasProof { get; set; }
    public string? ProofPath { get; set; }
    public int AwardedXp { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedDay { get; set; } = "";
}
