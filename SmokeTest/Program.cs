using LifeQuest.Data;
using LifeQuest.Models;
using LifeQuest.Services;

const string cs = @"Server=.\SQLEXPRESS;Database=LifeQuest;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;";

int failures = 0;
void Check(string name, bool ok)
{
    Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {name}");
    if (!ok) failures++;
}

var db = new Database(cs);
await db.InitializeAsync();
Check("Database.InitializeAsync", true);

var users = new UserRepository(db);
var quests = new QuestRepository(db);

string uname = "smoke_" + Guid.NewGuid().ToString("N")[..8];
var u = await users.CreateAsync(uname, "pass1234");
u = (await users.GetByIdAsync(u.Id))!;
Check("CreateAsync + GetByIdAsync", u != null && u.Username == uname);
Check("Password hashed (not plaintext)", PasswordHasher.Verify("pass1234", u!.PasswordHash) && u.PasswordHash != "pass1234");
Check("Wrong password rejected", !PasswordHasher.Verify("nope", u.PasswordHash));
Check("ExistsAsync", await users.ExistsAsync(uname));

var fetched = await users.GetByUsernameAsync(uname);
Check("GetByUsernameAsync", fetched != null && fetched!.Id == u.Id);

string day = DateTime.Now.ToString("yyyy-MM-dd");
var q = await quests.CreateAsync(u.Id, "Smoke quest", "desc", "medium", day);
Check("Quest create", await quests.GetAsync(q.Id) != null);

q = (await quests.GetAsync(q.Id))!;
q.Status = "pending"; q.HasProof = true; q.ProofPath = "x.png";
await quests.UpdateAsync(q);
var queue = await quests.PendingQueueAsync();
Check("PendingQueue has quest", queue.Exists(i => i.QuestId == q.Id));

int expected = GameLogic.ComputeXp("medium", true, u.Profile.Multiplier);
Check("ComputeXp medium+proof = 50", expected == 50);
var lvl = GameLogic.AddXp(u.Profile, expected);
u.Profile.QuestsDone++; u.Profile.ProofsApproved++;
q.Status = "done"; q.AwardedXp = expected;
await quests.UpdateAsync(q);
await users.UpdateAsync(u);

var reloaded = (await users.GetByIdAsync(u.Id))!;
Check("XP persisted", reloaded.Profile.TotalXp == expected);
Check("QuestsDone persisted", reloaded.Profile.QuestsDone == 1);
Check("ProofsApproved persisted", reloaded.Profile.ProofsApproved == 1);

var mine = await quests.ForUserAsync(u.Id);
Check("ForUserAsync returns quest", mine.Exists(x => x.Id == q.Id && x.Status == "done" && x.AwardedXp == expected));

int noProof = GameLogic.ComputeXp("easy", false, 1.0);
Check("ComputeXp easy no-proof = 8", noProof == 8);

Check("XpForLevel(1)=100", GameLogic.XpForLevel(1) == 100);
Check("Rank lvl1 Novice", GameLogic.RankForLevel(1) == "Novice");
Check("Rank lvl11 Apprentice", GameLogic.RankForLevel(11) == "Apprentice");

var p = new Profile { Level = 100 };
Check("Prestige at max", GameLogic.Prestige(p) && p.Prestige == 1 && p.Level == 1 && Math.Abs(p.Multiplier - 1.25) < 0.001);

Check("WeatherCategory rain", DailyBriefService.WeatherCategory(63) == "rain");
Check("WeatherCategory snow", DailyBriefService.WeatherCategory(73) == "snow");
Check("WeatherCategory storm", DailyBriefService.WeatherCategory(95) == "storm");
Check("WeatherCategory clear", DailyBriefService.WeatherCategory(0) == "clear");

var lb = await users.LeaderboardAsync();
Check("Leaderboard returns rows", lb.Count > 0);

await quests.DeleteAsync(q.Id);
Check("Quest delete", await quests.GetAsync(q.Id) == null);

Console.WriteLine();
Console.WriteLine(failures == 0 ? "ALL PASS" : $"{failures} FAILURE(S)");
Environment.Exit(failures == 0 ? 0 : 1);
