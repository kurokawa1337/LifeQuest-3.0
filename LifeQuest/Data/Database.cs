using Microsoft.Data.SqlClient;

namespace LifeQuest.Data;

public sealed class Database
{
    private readonly string _connectionString;
    private readonly string _masterConnectionString;
    private readonly string _databaseName;

    public Database(string connectionString)
    {
        _connectionString = connectionString;

        var b = new SqlConnectionStringBuilder(connectionString);
        _databaseName = string.IsNullOrWhiteSpace(b.InitialCatalog) ? "LifeQuest" : b.InitialCatalog;

        var master = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
        _masterConnectionString = master.ConnectionString;
    }

    public SqlConnection Create() => new(_connectionString);

    public async Task TestConnectionAsync()
    {
        await using var conn = new SqlConnection(_masterConnectionString);
        await conn.OpenAsync();
    }

    public async Task InitializeAsync()
    {
        await using (var master = new SqlConnection(_masterConnectionString))
        {
            await master.OpenAsync();
            await using var cmd = master.CreateCommand();
            cmd.CommandText = $@"
IF DB_ID(@name) IS NULL
BEGIN
    DECLARE @sql NVARCHAR(MAX) = N'CREATE DATABASE ' + QUOTENAME(@name);
    EXEC sp_executesql @sql;
END";
            cmd.Parameters.AddWithValue("@name", _databaseName);
            await cmd.ExecuteNonQueryAsync();
        }

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var t = conn.CreateCommand();
        t.CommandText = @"
IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        Username        NVARCHAR(50)  NOT NULL,
        UsernameKey     NVARCHAR(50)  NOT NULL UNIQUE,
        PasswordHash    NVARCHAR(300) NOT NULL,
        IsAdmin         BIT           NOT NULL DEFAULT 0,
        Lang            NVARCHAR(5)   NOT NULL DEFAULT 'uk',
        Theme           NVARCHAR(10)  NOT NULL DEFAULT 'light',
        AvatarPath      NVARCHAR(500) NULL,
        City            NVARCHAR(60)  NULL,
        Xp              INT           NOT NULL DEFAULT 0,
        Lvl             INT           NOT NULL DEFAULT 1,
        TotalXp         BIGINT        NOT NULL DEFAULT 0,
        Prestige        INT           NOT NULL DEFAULT 0,
        Multiplier      FLOAT         NOT NULL DEFAULT 1,
        QuestsDone      INT           NOT NULL DEFAULT 0,
        ProofsApproved  INT           NOT NULL DEFAULT 0,
        QuestDay        NVARCHAR(10)  NOT NULL DEFAULT '',
        QuestDayCount   INT           NOT NULL DEFAULT 0,
        CreatedAt       DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;

IF COL_LENGTH('dbo.Users', 'City') IS NULL
    ALTER TABLE dbo.Users ADD City NVARCHAR(60) NULL;

IF OBJECT_ID('dbo.Quests', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Quests (
        Id           INT IDENTITY(1,1) PRIMARY KEY,
        UserId       INT           NOT NULL,
        Title        NVARCHAR(80)  NOT NULL,
        Descr        NVARCHAR(240) NOT NULL DEFAULT '',
        Difficulty   NVARCHAR(10)  NOT NULL DEFAULT 'easy',
        Status       NVARCHAR(10)  NOT NULL DEFAULT 'active',
        HasProof     BIT           NOT NULL DEFAULT 0,
        ProofPath    NVARCHAR(500) NULL,
        AwardedXp    INT           NOT NULL DEFAULT 0,
        CreatedAt    DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedDay   NVARCHAR(10)  NOT NULL DEFAULT '',
        CONSTRAINT FK_Quests_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_Quests_UserId ON dbo.Quests(UserId);
END;";
        await t.ExecuteNonQueryAsync();
    }
}
