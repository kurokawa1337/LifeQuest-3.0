using LifeQuest.Models;

namespace LifeQuest.Services;

public static class GameLogic
{
    public static readonly Dictionary<string, int> XpByDiff = new()
    {
        ["easy"] = 25,
        ["medium"] = 50,
        ["hard"] = 100
    };

    public const double ProofPenalty = 1.0 / 3.0;
    public const int MaxLevel = 100;
    public const int DailyQuestLimit = 10;

    public static readonly string[] Ranks =
    {
        "Novice", "Apprentice", "Adventurer", "Fighter", "Veteran",
        "Champion", "Hero", "Master", "Grandmaster", "Legend"
    };

    public static int XpForLevel(int level) => (int)Math.Floor(100 * Math.Pow(level, 1.35));

    public static string RankForLevel(int level)
    {
        int idx = Math.Min(Ranks.Length - 1, (level - 1) / 10);
        return Ranks[Math.Max(0, idx)];
    }

    public static int BaseXp(string difficulty) =>
        XpByDiff.TryGetValue(difficulty, out var v) ? v : XpByDiff["easy"];

    public static double ClampMultiplier(double m)
    {
        if (double.IsNaN(m) || double.IsInfinity(m)) return 1;
        return Math.Min(10, Math.Max(1, m));
    }

    public static int ComputeXp(string difficulty, bool hasProof, double multiplier)
    {
        double xp = BaseXp(difficulty);
        if (!hasProof) xp = Math.Round(xp * ProofPenalty);
        double m = ClampMultiplier(multiplier);
        return Math.Max(0, (int)Math.Round(xp * m));
    }

    public class LevelUpResult
    {
        public bool LeveledUp;
        public int NewLevels;
        public int FromLevel;
        public int ToLevel;
        public bool RankChanged;
        public bool CanPrestige;
    }

    public static LevelUpResult AddXp(Profile p, int amount)
    {
        amount = Math.Max(0, amount);
        int beforeLevel = p.Level;
        string beforeRank = RankForLevel(p.Level);
        p.Xp += amount;
        p.TotalXp += amount;

        int leveled = 0;
        while (p.Level < MaxLevel)
        {
            int need = XpForLevel(p.Level);
            if (p.Xp >= need)
            {
                p.Xp -= need;
                p.Level++;
                leveled++;
            }
            else break;
        }
        if (p.Level >= MaxLevel) p.Level = MaxLevel;

        return new LevelUpResult
        {
            LeveledUp = leveled > 0,
            NewLevels = leveled,
            FromLevel = beforeLevel,
            ToLevel = p.Level,
            RankChanged = RankForLevel(p.Level) != beforeRank,
            CanPrestige = p.Level >= MaxLevel
        };
    }

    public static bool Prestige(Profile p)
    {
        if (p.Level < MaxLevel) return false;
        p.Prestige += 1;
        p.Multiplier = ClampMultiplier(Math.Round(1 + p.Prestige * 0.25, 2));
        p.Level = 1;
        p.Xp = 0;
        return true;
    }

    public static (int need, int cur, double pct) Progress(Profile p)
    {
        if (p.Level >= MaxLevel) return (p.Xp, p.Xp, 100);
        int need = XpForLevel(p.Level);
        return (need, p.Xp, Math.Min(100, (double)p.Xp / need * 100));
    }
}
