from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def load(relative: str) -> tuple[Path, str]:
    path = ROOT / relative
    return path, path.read_text(encoding="utf-8-sig")


def save(path: Path, text: str) -> None:
    path.write_text(text, encoding="utf-8")


def replace_once(relative: str, old: str, new: str) -> None:
    path, text = load(relative)
    if new in text:
        return
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"Expected one replacement in {relative}, found {count}: {old[:80]!r}")
    save(path, text.replace(old, new, 1))


def replace_all_required(relative: str, replacements: dict[str, str]) -> None:
    path, text = load(relative)
    changed = False
    for old, new in replacements.items():
        if new in text:
            continue
        count = text.count(old)
        if count == 0:
            raise RuntimeError(f"Missing required expression in {relative}: {old}")
        text = text.replace(old, new)
        changed = True
    if changed:
        save(path, text)


# Dependency injection.
replace_once(
    "src/Starward/AppConfig.ServiceProvider.cs",
    "            sc.AddSingleton<GameRecordService>();\n",
    "            sc.AddSingleton<GameRecordService>();\n            sc.AddSingleton<GameRecordAutoRefreshService>();\n",
)

# Persistent schedule and per-role timestamps.
replace_once(
    "src/Starward/AppConfig.Setting.cs",
    "using Starward.Features.GameLauncher;\n",
    "using Starward.Features.GameLauncher;\nusing Starward.Features.GameRecord;\n",
)

setting_methods = '''    /// <summary>\n    /// Automatic HoYoLAB game-record refresh interval, stored independently for each game.\n    /// </summary>\n    public static GameRecordAutoRefreshInterval GetGameRecordAutoRefreshInterval(GameBiz game)\n    {\n        return GetValue(\n            GameRecordAutoRefreshInterval.Disabled,\n            $"game_record_auto_refresh_interval_{game.Game}");\n    }\n\n\n    public static void SetGameRecordAutoRefreshInterval(\n        GameBiz game,\n        GameRecordAutoRefreshInterval value)\n    {\n        SetValue(value, $"game_record_auto_refresh_interval_{game.Game}");\n    }\n\n\n    public static DateTimeOffset GetGameRecordLastAutoRefreshTime(GameBiz game, long uid)\n    {\n        return GetValue<DateTimeOffset>(\n            default,\n            $"game_record_auto_refresh_last_time_{game.Game}_{uid}");\n    }\n\n\n    public static void SetGameRecordLastAutoRefreshTime(\n        GameBiz game,\n        long uid,\n        DateTimeOffset value)\n    {\n        SetValue(value, $"game_record_auto_refresh_last_time_{game.Game}_{uid}");\n    }\n\n\n    public static DateTimeOffset GetGameRecordLastAutoRefreshAttemptTime(GameBiz game, long uid)\n    {\n        return GetValue<DateTimeOffset>(\n            default,\n            $"game_record_auto_refresh_last_attempt_{game.Game}_{uid}");\n    }\n\n\n    public static void SetGameRecordLastAutoRefreshAttemptTime(\n        GameBiz game,\n        long uid,\n        DateTimeOffset value)\n    {\n        SetValue(value, $"game_record_auto_refresh_last_attempt_{game.Game}_{uid}");\n    }\n\n\n'''
replace_once(
    "src/Starward/AppConfig.Setting.cs",
    "    #endregion\n\n\n\n    #region Setting Method",
    setting_methods + "    #endregion\n\n\n\n    #region Setting Method",
)

# Application lifetime.
replace_once(
    "src/Starward/App.xaml.cs",
    "using Starward.Features.GamepadControl;\n",
    "using Starward.Features.GamepadControl;\nusing Starward.Features.GameRecord;\n",
)
replace_once(
    "src/Starward/App.xaml.cs",
    "        else\n        {\n            m_MainWindow = new MainWindow();\n            m_MainWindow.Activate();\n        }\n    }",
    "        else\n        {\n            m_MainWindow = new MainWindow();\n            m_MainWindow.Activate();\n        }\n\n        AppConfig.GetService<GameRecordAutoRefreshService>().Start();\n    }",
)
replace_once(
    "src/Starward/App.xaml.cs",
    "    public new void Exit()\n    {\n        GamepadController.RestoreGamepadGuideButtonForGameBar();",
    "    public new void Exit()\n    {\n        AppConfig.GetService<GameRecordAutoRefreshService>().Stop();\n        GamepadController.RestoreGamepadGuideButtonForGameBar();",
)

# Settings navigation item.
settings_item = '''                <!--  HoYoLAB Toolbox auto refresh  -->\n                <NavigationViewItem MinHeight="40"\n                                    Tag="HoyolabToolboxAutoRefreshSetting"\n                                    ToolTipService.ToolTip="{x:Bind lang:Lang.SettingPage_HoyolabAutoRefresh}">\n                    <NavigationViewItem.Content>\n                        <TextBlock MaxWidth="174"\n                                   MaxLines="2"\n                                   Text="{x:Bind lang:Lang.SettingPage_HoyolabAutoRefresh}"\n                                   TextTrimming="CharacterEllipsis"\n                                   TextWrapping="WrapWholeWords" />\n                    </NavigationViewItem.Content>\n                    <NavigationViewItem.Icon>\n                        <FontIcon Glyph="&#xE895;" />\n                    </NavigationViewItem.Icon>\n                </NavigationViewItem>\n\n'''
replace_once(
    "src/Starward/Features/Setting/SettingPage.xaml",
    "                <!--  工具箱  -->\n",
    settings_item + "                <!--  工具箱  -->\n",
)
replace_once(
    "src/Starward/Features/Setting/SettingPage.xaml.cs",
    "                nameof(GamepadControlSetting) => typeof(GamepadControlSetting),\n",
    "                nameof(GamepadControlSetting) => typeof(GamepadControlSetting),\n                nameof(HoyolabToolboxAutoRefreshSetting) => typeof(HoyolabToolboxAutoRefreshSetting),\n",
)

# Reload the currently opened record page when background refresh succeeds.
message_registration = '''        WeakReferenceMessenger.Default.Register<GameRecordAutoRefreshCompletedMessage>(this, (r, m) =>\n        {\n            if (CurrentGameBiz.Game == m.Game.Game &&\n                frame.SourcePageType is Type pageType &&\n                pageType != typeof(LoginPage))\n            {\n                DispatcherQueue.TryEnqueue(() => NavigateTo(pageType, force_navigate: true));\n            }\n        });\n'''
replace_once(
    "src/Starward/Features/GameRecord/GameRecordPage.xaml.cs",
    "        WeakReferenceMessenger.Default.Register<GameRecordVerifyAccountMessage>(this, (r, m) =>\n        {\n            ShowBattleChronicleWindow();\n        });\n",
    "        WeakReferenceMessenger.Default.Register<GameRecordVerifyAccountMessage>(this, (r, m) =>\n        {\n            ShowBattleChronicleWindow();\n        });\n" + message_registration,
)

# GameRecordService: always select the API client from the role itself so a
# background refresh cannot change the client used by an opened page.
client_method = '''\n\n    /// <summary>\n    /// Selects the correct API client from the role without changing the client\n    /// selected by the currently opened HoYoLAB Toolbox page.\n    /// </summary>\n    private GameRecordClient GetClient(GameRecordRole role)\n    {\n        return new GameBiz(role.GameBiz).IsGlobalServer() ? _hoyolabClient : _hyperionClient;\n    }\n'''
replace_once(
    "src/Starward/Features/GameRecord/GameRecordService.cs",
    "        _memoryCache = memoryCache;\n    }\n",
    "        _memoryCache = memoryCache;\n    }\n" + client_method,
)

old_device_method = '''    public async Task UpdateDeviceFpAsync(bool forceUpdate = false, CancellationToken cancellationToken = default)\n    {\n        if (IsHoyolab)\n        {\n            return;\n        }\n        string? id = AppConfig.HyperionDeviceId;\n        string? fp = AppConfig.HyperionDeviceFp;\n        DateTimeOffset lastUpdateTime = AppConfig.HyperionDeviceFpLastUpdateTime;\n        if (!forceUpdate && !string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(fp))\n        {\n            _gameRecordClient.DeviceId = id;\n            _gameRecordClient.DeviceFp = fp;\n        }\n        if (forceUpdate || DateTimeOffset.Now - lastUpdateTime > TimeSpan.FromDays(3))\n        {\n            await _gameRecordClient.GetDeviceFpAsync(cancellationToken);\n            AppConfig.HyperionDeviceId = _gameRecordClient.DeviceId;\n            AppConfig.HyperionDeviceFp = _gameRecordClient.DeviceFp;\n            AppConfig.HyperionDeviceFpLastUpdateTime = DateTimeOffset.Now;\n        }\n    }\n'''
new_device_method = '''    public async Task UpdateDeviceFpAsync(bool forceUpdate = false, CancellationToken cancellationToken = default)\n    {\n        if (IsHoyolab)\n        {\n            return;\n        }\n        await UpdateHyperionDeviceFpAsync(forceUpdate, cancellationToken);\n    }\n\n\n    /// <summary>\n    /// Updates the China-server device fingerprint without changing the client\n    /// selected by the currently opened HoYoLAB Toolbox page.\n    /// </summary>\n    public async Task UpdateHyperionDeviceFpAsync(\n        bool forceUpdate = false,\n        CancellationToken cancellationToken = default)\n    {\n        string? id = AppConfig.HyperionDeviceId;\n        string? fp = AppConfig.HyperionDeviceFp;\n        DateTimeOffset lastUpdateTime = AppConfig.HyperionDeviceFpLastUpdateTime;\n        if (!forceUpdate && !string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(fp))\n        {\n            _hyperionClient.DeviceId = id;\n            _hyperionClient.DeviceFp = fp;\n        }\n        if (forceUpdate || DateTimeOffset.Now - lastUpdateTime > TimeSpan.FromDays(3))\n        {\n            await _hyperionClient.GetDeviceFpAsync(cancellationToken);\n            AppConfig.HyperionDeviceId = _hyperionClient.DeviceId;\n            AppConfig.HyperionDeviceFp = _hyperionClient.DeviceFp;\n            AppConfig.HyperionDeviceFpLastUpdateTime = DateTimeOffset.Now;\n        }\n    }\n'''
replace_once(
    "src/Starward/Features/GameRecord/GameRecordService.cs",
    old_device_method,
    new_device_method,
)

roles_method = '''\n\n    /// <summary>\n    /// Returns saved roles from every server of the selected game.\n    /// </summary>\n    public List<GameRecordRole> GetGameRolesOfGame(GameBiz game)\n    {\n        using var dapper = DatabaseService.CreateConnection();\n        string prefix = $"{game.Game}_";\n        return dapper.Query<GameRecordRole>(\n            "SELECT * FROM GameRecordRole WHERE substr(GameBiz, 1, @length) = @prefix ORDER BY GameBiz, Uid;",\n            new { prefix, length = prefix.Length }).ToList();\n    }\n'''
replace_once(
    "src/Starward/Features/GameRecord/GameRecordService.cs",
    "        return list.ToList();\n    }\n\n\n\n    public GameRecordRole? GetLastSelectGameRecordRoleOrTheFirstOne(GameBiz gameBiz)",
    "        return list.ToList();\n    }\n" + roles_method + "\n\n    public GameRecordRole? GetLastSelectGameRecordRoleOrTheFirstOne(GameBiz gameBiz)",
)

client_replacements = {
    "_gameRecordClient.UpdateGameRoleHeadIconAsync(role)": "GetClient(role).UpdateGameRoleHeadIconAsync(role)",
    "_gameRecordClient.GetSpiralAbyssInfoAsync(role, schedule)": "GetClient(role).GetSpiralAbyssInfoAsync(role, schedule)",
    "_gameRecordClient.GetTravelsDiarySummaryAsync(role, month)": "GetClient(role).GetTravelsDiarySummaryAsync(role, month)",
    "_gameRecordClient.GetTravelsDiaryDetailAsync(role, month, type, limit)": "GetClient(role).GetTravelsDiaryDetailAsync(role, month, type, limit)",
    "_gameRecordClient.GetImaginariumTheaterInfosAsync(role, cancellationToken)": "GetClient(role).GetImaginariumTheaterInfosAsync(role, cancellationToken)",
    "_gameRecordClient.GetSimulatedUniverseInfoAsync(role, detail)": "GetClient(role).GetSimulatedUniverseInfoAsync(role, detail)",
    "_gameRecordClient.GetForgottenHallInfoAsync(role, schedule)": "GetClient(role).GetForgottenHallInfoAsync(role, schedule)",
    "_gameRecordClient.GetPureFictionInfoAsync(role, schedule)": "GetClient(role).GetPureFictionInfoAsync(role, schedule)",
    "_gameRecordClient.GetApocalypticShadowInfoAsync(role, schedule)": "GetClient(role).GetApocalypticShadowInfoAsync(role, schedule)",
    "_gameRecordClient.GetTrailblazeCalendarSummaryAsync(role, month)": "GetClient(role).GetTrailblazeCalendarSummaryAsync(role, month)",
    "_gameRecordClient.GetTrailblazeCalendarDetailByPageAsync(role, month, type, 1, 1)": "GetClient(role).GetTrailblazeCalendarDetailByPageAsync(role, month, type, 1, 1)",
    "_gameRecordClient.GetTrailblazeCalendarDetailAsync(role, month, type)": "GetClient(role).GetTrailblazeCalendarDetailAsync(role, month, type)",
    "_gameRecordClient.GetInterKnotReportSummaryAsync(role, month)": "GetClient(role).GetInterKnotReportSummaryAsync(role, month)",
    "_gameRecordClient.GetInterKnotReportDetailByPageAsync(role, month, type, 1, 1)": "GetClient(role).GetInterKnotReportDetailByPageAsync(role, month, type, 1, 1)",
    "_gameRecordClient.GetInterKnotReportDetailAsync(role, month, type)": "GetClient(role).GetInterKnotReportDetailAsync(role, month, type)",
    "_gameRecordClient.GetZZZGachaRecordAsync(role, gachaType, endId, language, cancellationToken)": "GetClient(role).GetZZZGachaRecordAsync(role, gachaType, endId, language, cancellationToken)",
    "_gameRecordClient.GetShiyuDefenseInfoAsync(role, schedule)": "GetClient(role).GetShiyuDefenseInfoAsync(role, schedule)",
    "_gameRecordClient.GetDeadlyAssaultInfoAsync(role, schedule)": "GetClient(role).GetDeadlyAssaultInfoAsync(role, schedule)",
    "_gameRecordClient.GetBH3DailyNoteAsync(role, cancellationToken)": "GetClient(role).GetBH3DailyNoteAsync(role, cancellationToken)",
    "_gameRecordClient.GetGenshinDailyNoteAsync(role, cancellationToken)": "GetClient(role).GetGenshinDailyNoteAsync(role, cancellationToken)",
    "_gameRecordClient.GetStarRailDailyNoteAsync(role, cancellationToken)": "GetClient(role).GetStarRailDailyNoteAsync(role, cancellationToken)",
    "_gameRecordClient.GetZZZDailyNoteAsync(role, cancellationToken)": "GetClient(role).GetZZZDailyNoteAsync(role, cancellationToken)",
    "_gameRecordClient.GetStygianOnslaughtInfosAsync(role, cancellationToken)": "GetClient(role).GetStygianOnslaughtInfosAsync(role, cancellationToken)",
    "_gameRecordClient.GetStarRailChallengePeakDataAsync(role, 1, cancellationToken)": "GetClient(role).GetStarRailChallengePeakDataAsync(role, 1, cancellationToken)",
    "_gameRecordClient.GetStarRailChallengePeakDataAsync(role, 3, cancellationToken)": "GetClient(role).GetStarRailChallengePeakDataAsync(role, 3, cancellationToken)",
}
replace_all_required(
    "src/Starward/Features/GameRecord/GameRecordService.cs",
    client_replacements,
)

# Add neutral English resources and Russian translations. Other locales fall back
# to the neutral resource file through ResourceManager.
resources = {
    "Lang.resx": {
        "SettingPage_HoyolabAutoRefresh": "HoYoLAB Toolbox Auto Refresh",
        "HoyolabAutoRefresh_Title": "HoYoLAB Data Auto Refresh",
        "HoyolabAutoRefresh_Description": "Choose how often Starward refreshes saved HoYoLAB Toolbox data for each game. Monthly reports include every available month.",
        "HoyolabAutoRefresh_GenshinDescription": "Spiral Abyss, Imaginarium Theater, Stygian Onslaught, and Traveler's Diary.",
        "HoyolabAutoRefresh_StarRailDescription": "Trailblaze Monthly Calendar, Simulated Universe, Forgotten Hall, Pure Fiction, Apocalyptic Shadow, and Anomaly Arbitration.",
        "HoyolabAutoRefresh_ZZZDescription": "Inter-Knot Monthly Report, Shiyu Defense, and Deadly Assault.",
        "HoyolabAutoRefresh_RefreshNow": "Refresh now",
        "HoyolabAutoRefresh_RefreshAll": "Refresh all games now",
        "HoyolabAutoRefresh_Save": "Save",
        "HoyolabAutoRefresh_ScheduleNote": "Scheduled refreshes run while Starward is open. If a scheduled refresh was missed while the app was closed, it runs after the next launch.",
        "HoyolabAutoRefresh_IntervalDisabled": "Disabled",
        "HoyolabAutoRefresh_IntervalOnStartup": "Every time the client starts",
        "HoyolabAutoRefresh_IntervalDaily": "Once a day",
        "HoyolabAutoRefresh_IntervalWeekly": "Once a week",
        "HoyolabAutoRefresh_IntervalMonthly": "Once a month",
        "HoyolabAutoRefresh_SettingsSaved": "Automatic refresh settings saved",
        "HoyolabAutoRefresh_AllGamesCompleted": "All games refreshed",
        "HoyolabAutoRefresh_RefreshCompleted": "Refresh completed",
        "HoyolabAutoRefresh_ResultFormat": "Roles refreshed: {0}. Successful operations: {1}, errors: {2}",
        "HoyolabAutoRefresh_NothingRefreshed": "Nothing was refreshed",
        "HoyolabAutoRefresh_CheckAccountCookie": "Check that an account is added and its cookie is valid",
        "HoyolabAutoRefresh_NotPerformedYet": "Not performed yet",
        "HoyolabAutoRefresh_NotScheduled": "Not scheduled",
        "HoyolabAutoRefresh_NextApplicationStartup": "Next application startup",
        "HoyolabAutoRefresh_LastSuccessfulRefresh": "Last successful refresh",
        "HoyolabAutoRefresh_NextScheduledRefresh": "Next scheduled refresh",
    },
    "Lang.ru-RU.resx": {
        "SettingPage_HoyolabAutoRefresh": "Автообновление HoYoLAB Toolbox",
        "HoyolabAutoRefresh_Title": "Автообновление данных HoYoLAB",
        "HoyolabAutoRefresh_Description": "Выберите, как часто Starward будет обновлять сохранённые данные HoYoLAB Toolbox для каждой игры. Для ежемесячных отчётов загружаются все доступные месяцы.",
        "HoyolabAutoRefresh_GenshinDescription": "Витая Бездна, Театр Воображариум, Мрачный натиск и Заметки Путешественника.",
        "HoyolabAutoRefresh_StarRailDescription": "Календарь Освоения, Виртуальная вселенная, Зал забвения, Чистый вымысел, Иллюзия конца и Арбитраж аномалий.",
        "HoyolabAutoRefresh_ZZZDescription": "Ежемесячный отчёт Интернота, Оборона Шиюй и Смертельный штурм.",
        "HoyolabAutoRefresh_RefreshNow": "Обновить сейчас",
        "HoyolabAutoRefresh_RefreshAll": "Обновить все игры сейчас",
        "HoyolabAutoRefresh_Save": "Сохранить",
        "HoyolabAutoRefresh_ScheduleNote": "Обновление по расписанию выполняется, пока Starward открыт. Если срок наступил при закрытом клиенте, пропущенное обновление выполнится после следующего запуска.",
        "HoyolabAutoRefresh_IntervalDisabled": "Отключено",
        "HoyolabAutoRefresh_IntervalOnStartup": "При каждом запуске клиента",
        "HoyolabAutoRefresh_IntervalDaily": "Раз в день",
        "HoyolabAutoRefresh_IntervalWeekly": "Раз в неделю",
        "HoyolabAutoRefresh_IntervalMonthly": "Раз в месяц",
        "HoyolabAutoRefresh_SettingsSaved": "Настройки автообновления сохранены",
        "HoyolabAutoRefresh_AllGamesCompleted": "Обновление всех игр завершено",
        "HoyolabAutoRefresh_RefreshCompleted": "Обновление завершено",
        "HoyolabAutoRefresh_ResultFormat": "Аккаунтов обновлено: {0}. Успешных операций: {1}, ошибок: {2}",
        "HoyolabAutoRefresh_NothingRefreshed": "Нет данных для обновления",
        "HoyolabAutoRefresh_CheckAccountCookie": "Проверьте, что аккаунт добавлен и Cookie действителен",
        "HoyolabAutoRefresh_NotPerformedYet": "Ещё не выполнялось",
        "HoyolabAutoRefresh_NotScheduled": "Не запланировано",
        "HoyolabAutoRefresh_NextApplicationStartup": "При следующем запуске клиента",
        "HoyolabAutoRefresh_LastSuccessfulRefresh": "Последнее успешное обновление",
        "HoyolabAutoRefresh_NextScheduledRefresh": "Следующее обновление",
    },
}

for filename, entries in resources.items():
    path, text = load(f"src/Starward.Language/{filename}")
    additions = []
    for key, value in entries.items():
        if f'name="{key}"' not in text:
            additions.append(
                f'  <data name="{key}" xml:space="preserve">\n'
                f'    <value>{value}</value>\n'
                f'  </data>\n'
            )
    if additions:
        if "</root>" not in text:
            raise RuntimeError(f"Missing </root> in {filename}")
        save(path, text.replace("</root>", "".join(additions) + "</root>", 1))

print("HoYoLAB auto refresh port applied successfully.")
