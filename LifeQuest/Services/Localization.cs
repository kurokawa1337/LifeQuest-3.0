using System.ComponentModel;

namespace LifeQuest.Services;

public sealed class I18n : INotifyPropertyChanged
{
    public static I18n Instance { get; } = new();

    private string _lang = "uk";
    public string Lang => _lang;

    public event PropertyChangedEventHandler? PropertyChanged;

    private static readonly Dictionary<string, Dictionary<string, string>> Dict = new()
    {
        ["en"] = new()
        {
            ["tagline"] = "Turn your routine into an adventure",
            ["appName"] = "LifeQuest",
            ["login"] = "Login", ["register"] = "Register", ["signIn"] = "Sign in", ["createHero"] = "Create hero",
            ["username"] = "Username", ["password"] = "Password", ["repeatPassword"] = "Repeat password",
            ["quests"] = "Quests", ["stats"] = "Stats", ["leaderboard"] = "Leaderboard", ["settings"] = "Settings",
            ["admin"] = "Review Queue", ["dev"] = "Dev Mode", ["logout"] = "Log out",
            ["questBoard"] = "Quest Board", ["questBoardSub"] = "Complete quests, submit proof, earn XP",
            ["newQuest"] = "New Quest", ["all"] = "All", ["active"] = "Active", ["pending"] = "In review", ["done"] = "Completed",
            ["noQuests"] = "No quests yet. Create your first and begin the journey!",
            ["dailyLeft"] = "Quests left today", ["dailyLimitReached"] = "Daily limit reached (10 quests/day)",
            ["title"] = "Title", ["description"] = "Description", ["difficulty"] = "Difficulty",
            ["easy"] = "Easy", ["medium"] = "Medium", ["hard"] = "Hard",
            ["titlePh"] = "e.g. Do morning workout", ["descPh"] = "Optional details",
            ["cancel"] = "Cancel", ["create"] = "Create", ["save"] = "Save",
            ["complete"] = "Complete", ["delete"] = "Delete",
            ["reward"] = "Reward", ["xpGained"] = "XP gained",
            ["proofTitle"] = "Quest Proof", ["proofFull"] = "Attach a photo or video and earn",
            ["proofWarn"] = "Without proof, XP is cut to a third", ["xp"] = "XP",
            ["pickFile"] = "Choose a file…", ["submitProof"] = "Submit proof", ["skipProof"] = "Skip (×⅓ XP)",
            ["proofSent"] = "Proof submitted for review",
            ["questAdded"] = "Quest added!", ["questDeleted"] = "Quest deleted",
            ["statLevel"] = "Level", ["statRank"] = "Rank", ["statTotalXp"] = "Total XP earned",
            ["statQuests"] = "Quests completed", ["statProofs"] = "Proofs approved", ["statPrestige"] = "Prestige",
            ["statMult"] = "XP Multiplier", ["statToNext"] = "To next level", ["max"] = "MAX",
            ["prestigeBtn"] = "REBIRTH (Prestige) — reset level, +0.25 multiplier forever",
            ["prestigeDone"] = "Rebirth complete! Multiplier is now",
            ["reviewTitle"] = "Review Queue", ["reviewSub"] = "Approve players' proofs to grant full XP",
            ["queueEmpty"] = "Queue is empty — all proofs reviewed!",
            ["player"] = "Player", ["approve"] = "Approve", ["reject"] = "Reject",
            ["approved"] = "Proof approved", ["rejected"] = "Proof rejected",
            ["devTitle"] = "Developer Mode", ["devUser"] = "Player", ["resetProfile"] = "Reset profile",
            ["grantXp"] = "Grant XP", ["grantLevel"] = "Grant levels", ["setLevel"] = "Set level",
            ["lvlUp"] = "LEVEL UP!", ["newRank"] = "New Rank!",
            ["settingsTitle"] = "Settings", ["language"] = "Language", ["avatar"] = "Avatar",
            ["theme"] = "Theme", ["themeLight"] = "Light", ["themeDark"] = "Dark",
            ["changeAvatar"] = "Change avatar", ["avatarHint"] = "PNG, JPG or GIF",
            ["avatarUpdated"] = "Avatar updated", ["langEn"] = "English", ["langUk"] = "Ukrainian",
            ["account"] = "Account", ["devBadge"] = "DEV", ["loggedAs"] = "Logged in as",
            ["database"] = "Database", ["connString"] = "Connection string", ["testConn"] = "Test connection",
            ["connOk"] = "Connection successful", ["connFail"] = "Connection failed",
            ["passwordsMismatch"] = "Passwords do not match",
            ["usernameShort"] = "Username too short (min 2)", ["passwordShort"] = "Password too short (min 4)",
            ["userTaken"] = "Username already taken", ["badCredentials"] = "Invalid username or password",
            ["errorTitle"] = "Error", ["confirmDelete"] = "Delete this quest?",
            ["welcome"] = "Welcome",
            ["briefTitle"] = "Daily Brief", ["city"] = "City", ["refresh"] = "Refresh",
            ["briefFail"] = "Brief unavailable — check the internet connection",
            ["ideasToday"] = "Quest ideas for today's weather", ["wind"] = "Wind", ["feels"] = "Feels like",
            ["activityTitle"] = "Quests created · last 14 days", ["diffTitle"] = "Quests by difficulty",
            ["noData"] = "No data yet",
            ["wcode_clear"] = "Clear sky", ["wcode_clouds"] = "Cloudy", ["wcode_fog"] = "Fog",
            ["wcode_rain"] = "Rain", ["wcode_snow"] = "Snow", ["wcode_storm"] = "Thunderstorm", ["wcode_any"] = "Weather",
            ["sugg_clear1"] = "Take a 25-minute walk in the sun",
            ["sugg_clear2"] = "Outdoor workout or a run",
            ["sugg_clear3"] = "Read a book in the park for 30 minutes",
            ["sugg_clouds1"] = "Bike ride around the neighborhood",
            ["sugg_clouds2"] = "Photo walk: capture 10 good shots",
            ["sugg_clouds3"] = "Tidy up the balcony or yard",
            ["sugg_fog1"] = "10 minutes of breathing or meditation",
            ["sugg_fog2"] = "Plan the coming week on paper",
            ["sugg_fog3"] = "Sort out one shelf or drawer",
            ["sugg_rain1"] = "Deep-clean your room for 20 minutes",
            ["sugg_rain2"] = "Learn 10 new foreign words",
            ["sugg_rain3"] = "Cook a dish you never made before",
            ["sugg_snow1"] = "Morning warm-up: 3 sets of squats",
            ["sugg_snow2"] = "Hot cocoa + plan the day in 15 minutes",
            ["sugg_snow3"] = "Call a friend you haven't talked to for long",
            ["sugg_storm1"] = "Write 300 words in your journal",
            ["sugg_storm2"] = "Home movie night: exactly one episode",
            ["sugg_storm3"] = "Do a 15-minute stretch session",
            ["sugg_any1"] = "Drink a glass of water every 2 hours",
            ["sugg_any2"] = "Walk 6000 steps today",
            ["sugg_any3"] = "10 minutes of stretching before bed"
        },
        ["uk"] = new()
        {
            ["tagline"] = "Перетвори рутину на пригоду",
            ["appName"] = "LifeQuest",
            ["login"] = "Вхід", ["register"] = "Реєстрація", ["signIn"] = "Увійти", ["createHero"] = "Створити героя",
            ["username"] = "Нікнейм", ["password"] = "Пароль", ["repeatPassword"] = "Повтори пароль",
            ["quests"] = "Квести", ["stats"] = "Статистика", ["leaderboard"] = "Таблиця лідерів", ["settings"] = "Налаштування",
            ["admin"] = "Перевірка", ["dev"] = "Реж. розробника", ["logout"] = "Вийти",
            ["questBoard"] = "Дошка квестів", ["questBoardSub"] = "Виконуй квести, надсилай пруф, отримуй XP",
            ["newQuest"] = "Новий квест", ["all"] = "Усі", ["active"] = "Активні", ["pending"] = "На перевірці", ["done"] = "Завершені",
            ["noQuests"] = "Поки немає квестів. Створи перший і вирушай у подорож!",
            ["dailyLeft"] = "Залишилось квестів сьогодні", ["dailyLimitReached"] = "Денний ліміт досягнуто (10 квестів/день)",
            ["title"] = "Назва", ["description"] = "Опис", ["difficulty"] = "Складність",
            ["easy"] = "Легкий", ["medium"] = "Середній", ["hard"] = "Складний",
            ["titlePh"] = "напр. Зробити зарядку", ["descPh"] = "Деталі (необов'язково)",
            ["cancel"] = "Скасувати", ["create"] = "Створити", ["save"] = "Зберегти",
            ["complete"] = "Виконати", ["delete"] = "Видалити",
            ["reward"] = "Нагорода", ["xpGained"] = "XP отримано",
            ["proofTitle"] = "Підтвердження квесту", ["proofFull"] = "Прикріпи фото або відео й отримай",
            ["proofWarn"] = "Без пруфу XP ріжеться втричі", ["xp"] = "XP",
            ["pickFile"] = "Вибрати файл…", ["submitProof"] = "Надіслати пруф", ["skipProof"] = "Пропустити (×⅓ XP)",
            ["proofSent"] = "Пруф надіслано на перевірку",
            ["questAdded"] = "Квест додано!", ["questDeleted"] = "Квест видалено",
            ["statLevel"] = "Рівень", ["statRank"] = "Ранг", ["statTotalXp"] = "Усього XP зароблено",
            ["statQuests"] = "Квестів виконано", ["statProofs"] = "Пруфів прийнято", ["statPrestige"] = "Престиж",
            ["statMult"] = "Множник XP", ["statToNext"] = "До наступного рівня", ["max"] = "МАКС",
            ["prestigeBtn"] = "ПЕРЕРОДЖЕННЯ (Prestige) — скинути рівень, +0.25 до множника назавжди",
            ["prestigeDone"] = "Переродження завершено! Множник тепер",
            ["reviewTitle"] = "Черга перевірки", ["reviewSub"] = "Підтверджуй пруфи гравців, щоб нарахувати повний XP",
            ["queueEmpty"] = "Черга порожня — усі пруфи перевірені!",
            ["player"] = "Гравець", ["approve"] = "Підтвердити", ["reject"] = "Відхилити",
            ["approved"] = "Пруф підтверджено", ["rejected"] = "Пруф відхилено",
            ["devTitle"] = "Режим розробника", ["devUser"] = "Гравець", ["resetProfile"] = "Скинути профіль",
            ["grantXp"] = "Видати XP", ["grantLevel"] = "Видати рівні", ["setLevel"] = "Встановити рівень",
            ["lvlUp"] = "НОВИЙ РІВЕНЬ!", ["newRank"] = "Новий ранг!",
            ["settingsTitle"] = "Налаштування", ["language"] = "Мова", ["avatar"] = "Аватар",
            ["theme"] = "Тема", ["themeLight"] = "Світла", ["themeDark"] = "Темна",
            ["changeAvatar"] = "Змінити аватар", ["avatarHint"] = "PNG, JPG або GIF",
            ["avatarUpdated"] = "Аватар оновлено", ["langEn"] = "English", ["langUk"] = "Українська",
            ["account"] = "Акаунт", ["devBadge"] = "DEV", ["loggedAs"] = "Ви увійшли як",
            ["database"] = "База даних", ["connString"] = "Рядок підключення", ["testConn"] = "Перевірити підключення",
            ["connOk"] = "Підключення успішне", ["connFail"] = "Помилка підключення",
            ["passwordsMismatch"] = "Паролі не збігаються",
            ["usernameShort"] = "Нікнейм закороткий (мін. 2)", ["passwordShort"] = "Пароль закороткий (мін. 4)",
            ["userTaken"] = "Такий нікнейм вже зайнятий", ["badCredentials"] = "Невірний нікнейм або пароль",
            ["errorTitle"] = "Помилка", ["confirmDelete"] = "Видалити цей квест?",
            ["welcome"] = "Вітаємо",
            ["briefTitle"] = "Бріф дня", ["city"] = "Місто", ["refresh"] = "Оновити",
            ["briefFail"] = "Бріф недоступний — перевір інтернет",
            ["ideasToday"] = "Ідеї квестів під погоду сьогодні", ["wind"] = "Вітер", ["feels"] = "Відчувається",
            ["activityTitle"] = "Створено квестів · 14 днів", ["diffTitle"] = "Квести за складністю",
            ["noData"] = "Даних поки немає",
            ["wcode_clear"] = "Ясно", ["wcode_clouds"] = "Хмарно", ["wcode_fog"] = "Туман",
            ["wcode_rain"] = "Дощ", ["wcode_snow"] = "Сніг", ["wcode_storm"] = "Гроза", ["wcode_any"] = "Погода",
            ["sugg_clear1"] = "25 хвилин прогулянки на сонці",
            ["sugg_clear2"] = "Тренування на вулиці або пробіжка",
            ["sugg_clear3"] = "Півгодини з книгою у парку",
            ["sugg_clouds1"] = "Велопрогулянка по району",
            ["sugg_clouds2"] = "Фотопрогулянка: 10 вдалих кадрів",
            ["sugg_clouds3"] = "Прибрати балкон або подвір'я",
            ["sugg_fog1"] = "10 хвилин дихання або медитації",
            ["sugg_fog2"] = "Спланувати тиждень на папері",
            ["sugg_fog3"] = "Розібрати одну полицю або шухляду",
            ["sugg_rain1"] = "20 хвилин прибирання в кімнаті",
            ["sugg_rain2"] = "Вивчити 10 нових слів англійської",
            ["sugg_rain3"] = "Приготувати страву, яку ніколи не готував",
            ["sugg_snow1"] = "Ранкова розминка: 3 підходи присідань",
            ["sugg_snow2"] = "Гаряче какао + план дня за 15 хвилин",
            ["sugg_snow3"] = "Подзвонити другові, з яким давно не спілкувався",
            ["sugg_storm1"] = "Написати 300 слів у щоденник",
            ["sugg_storm2"] = "Домашній кіно-вечір: рівно одна серія",
            ["sugg_storm3"] = "15 хвилин розтяжки",
            ["sugg_any1"] = "Пити склянку води кожні 2 години",
            ["sugg_any2"] = "Пройти 6000 кроків за день",
            ["sugg_any3"] = "10 хвилин розтяжки перед сном"
        }
    };

    public string this[string key] => T(key);

    public string T(string key)
    {
        if (Dict.TryGetValue(_lang, out var d) && d.TryGetValue(key, out var v)) return v;
        if (Dict["en"].TryGetValue(key, out var e)) return e;
        return key;
    }

    public void SetLang(string lang)
    {
        if (!Dict.ContainsKey(lang) || lang == _lang) return;
        _lang = lang;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Lang)));
        LangChanged?.Invoke();
    }

    public event Action? LangChanged;
}
