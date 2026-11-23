using System.Globalization;

namespace Starward.Language;


/// <summary>
/// Localized text used by the HoYoLAB Toolbox automatic-refresh interface.
/// Uses normal Starward resources when available and keeps built-in English/Russian
/// fallbacks so upstream Crowdin resource updates can be merged without conflicts.
/// </summary>
public static class HoYoLabAutoRefreshText
{

    private static CultureInfo CurrentCulture => Lang.Culture ?? CultureInfo.CurrentUICulture;


    public static string SettingPageTitle => Get("SettingPage_HoyolabAutoRefresh", "HoYoLAB Toolbox Auto Refresh");

    public static string SettingMenuTitle => Get("SettingPage_HoyolabAutoRefreshMenu", "HoYoLAB Auto Refresh");

    public static string Title => Get("HoyolabAutoRefresh_Title", "HoYoLAB Data Auto Refresh");

    public static string Description => Get("HoyolabAutoRefresh_Description", "Choose how often Starward refreshes saved HoYoLAB Toolbox data for each game. Monthly reports include every available month.");

    public static string GenshinDescription => Get("HoyolabAutoRefresh_GenshinDescription", "Spiral Abyss, Imaginarium Theater, Stygian Onslaught, and Traveler's Diary.");

    public static string StarRailDescription => Get("HoyolabAutoRefresh_StarRailDescription", "Trailblaze Monthly Calendar, Simulated Universe, Forgotten Hall, Pure Fiction, Apocalyptic Shadow, and Anomaly Arbitration.");

    public static string ZZZDescription => Get("HoyolabAutoRefresh_ZZZDescription", "Inter-Knot Monthly Report, Shiyu Defense, and Deadly Assault.");

    public static string RefreshNow => Get("HoyolabAutoRefresh_RefreshNow", "Refresh now");

    public static string RefreshAll => Get("HoyolabAutoRefresh_RefreshAll", "Refresh all games now");

    public static string Save => Get("HoyolabAutoRefresh_Save", "Save");

    public static string ScheduleNote => Get("HoyolabAutoRefresh_ScheduleNote", "Scheduled refreshes run while Starward is open. If a scheduled refresh was missed while the app was closed, it runs after the next launch.");

    public static string IntervalDisabled => Get("HoyolabAutoRefresh_IntervalDisabled", "Disabled");

    public static string IntervalOnStartup => Get("HoyolabAutoRefresh_IntervalOnStartup", "Every time the client starts");

    public static string IntervalDaily => Get("HoyolabAutoRefresh_IntervalDaily", "Once a day");

    public static string IntervalWeekly => Get("HoyolabAutoRefresh_IntervalWeekly", "Once a week");

    public static string IntervalMonthly => Get("HoyolabAutoRefresh_IntervalMonthly", "Once a month");

    public static string SettingsSaved => Get("HoyolabAutoRefresh_SettingsSaved", "Automatic refresh settings saved");

    public static string AllGamesCompleted => Get("HoyolabAutoRefresh_AllGamesCompleted", "All games refreshed");

    public static string RefreshCompleted => Get("HoyolabAutoRefresh_RefreshCompleted", "Refresh completed");

    public static string AutomaticRefreshTitle => Get("HoyolabAutoRefresh_AutomaticRefreshTitle", "HoYoLAB automatic refresh");

    public static string AccountUpdateSuccessFormat => Get("HoyolabAutoRefresh_AccountUpdateSuccessFormat", "successful: {0} operations");

    public static string AccountUpdatePartialFormat => Get("HoyolabAutoRefresh_AccountUpdatePartialFormat", "partially successful: {0} successful, {1} errors");

    public static string AccountUpdateFailedFormat => Get("HoyolabAutoRefresh_AccountUpdateFailedFormat", "failed: {0} errors");

    public static string ResultFormat => Get("HoyolabAutoRefresh_ResultFormat", "Roles refreshed: {0}. Successful operations: {1}, errors: {2}");

    public static string NothingRefreshed => Get("HoyolabAutoRefresh_NothingRefreshed", "Nothing was refreshed");

    public static string CheckAccountCookie => Get("HoyolabAutoRefresh_CheckAccountCookie", "Check that an account is added and its cookie is valid");

    public static string NotPerformedYet => Get("HoyolabAutoRefresh_NotPerformedYet", "Not performed yet");

    public static string NotScheduled => Get("HoyolabAutoRefresh_NotScheduled", "Not scheduled");

    public static string NextApplicationStartup => Get("HoyolabAutoRefresh_NextApplicationStartup", "Next application startup");

    public static string LastSuccessfulRefresh => Get("HoyolabAutoRefresh_LastSuccessfulRefresh", "Last successful refresh");

    public static string NextScheduledRefresh => Get("HoyolabAutoRefresh_NextScheduledRefresh", "Next scheduled refresh");


    private static string Get(string key, string fallback)
    {
        string? resource = Lang.ResourceManager.GetString(key, CurrentCulture);
        if (!string.IsNullOrWhiteSpace(resource))
        {
            return resource;
        }

        if (string.Equals(CurrentCulture.TwoLetterISOLanguageName, "ru", StringComparison.OrdinalIgnoreCase))
        {
            return key switch
            {
                "SettingPage_HoyolabAutoRefresh" => "Автообновление HoYoLAB Toolbox",
                "SettingPage_HoyolabAutoRefreshMenu" => "Автообновление HoYoLAB",
                "HoyolabAutoRefresh_Title" => "Автообновление данных HoYoLAB",
                "HoyolabAutoRefresh_Description" => "Выберите, как часто Starward будет обновлять сохранённые данные HoYoLAB Toolbox для каждой игры. Для ежемесячных отчётов загружаются все доступные месяцы.",
                "HoyolabAutoRefresh_GenshinDescription" => "Витая Бездна, Театр Воображариум, Мрачный натиск и Заметки Путешественника.",
                "HoyolabAutoRefresh_StarRailDescription" => "Календарь Освоения, Виртуальная вселенная, Зал забвения, Чистый вымысел, Иллюзия конца и Арбитраж аномалий.",
                "HoyolabAutoRefresh_ZZZDescription" => "Ежемесячный отчёт Интернота, Оборона Шиюй и Смертельный штурм.",
                "HoyolabAutoRefresh_RefreshNow" => "Обновить сейчас",
                "HoyolabAutoRefresh_RefreshAll" => "Обновить все игры сейчас",
                "HoyolabAutoRefresh_Save" => "Сохранить",
                "HoyolabAutoRefresh_ScheduleNote" => "Обновление по расписанию выполняется, пока Starward открыт. Если срок наступил при закрытом клиенте, пропущенное обновление выполнится после следующего запуска.",
                "HoyolabAutoRefresh_IntervalDisabled" => "Отключено",
                "HoyolabAutoRefresh_IntervalOnStartup" => "При каждом запуске клиента",
                "HoyolabAutoRefresh_IntervalDaily" => "Раз в день",
                "HoyolabAutoRefresh_IntervalWeekly" => "Раз в неделю",
                "HoyolabAutoRefresh_IntervalMonthly" => "Раз в месяц",
                "HoyolabAutoRefresh_SettingsSaved" => "Настройки автообновления сохранены",
                "HoyolabAutoRefresh_AllGamesCompleted" => "Обновление всех игр завершено",
                "HoyolabAutoRefresh_RefreshCompleted" => "Обновление завершено",
                "HoyolabAutoRefresh_AutomaticRefreshTitle" => "Автообновление HoYoLAB",
                "HoyolabAutoRefresh_AccountUpdateSuccessFormat" => "успешно: {0} операций",
                "HoyolabAutoRefresh_AccountUpdatePartialFormat" => "частично: {0} успешно, ошибок: {1}",
                "HoyolabAutoRefresh_AccountUpdateFailedFormat" => "ошибка: {0} ошибок",
                "HoyolabAutoRefresh_ResultFormat" => "Аккаунтов обновлено: {0}. Успешных операций: {1}, ошибок: {2}",
                "HoyolabAutoRefresh_NothingRefreshed" => "Нет данных для обновления",
                "HoyolabAutoRefresh_CheckAccountCookie" => "Проверьте, что аккаунт добавлен и Cookie действителен",
                "HoyolabAutoRefresh_NotPerformedYet" => "Ещё не выполнялось",
                "HoyolabAutoRefresh_NotScheduled" => "Не запланировано",
                "HoyolabAutoRefresh_NextApplicationStartup" => "При следующем запуске клиента",
                "HoyolabAutoRefresh_LastSuccessfulRefresh" => "Последнее успешное обновление",
                "HoyolabAutoRefresh_NextScheduledRefresh" => "Следующее обновление",
                _ => fallback,
            };
        }

        return fallback;
    }

}
