from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

replacements = {
    "src/Starward/Features/Setting/SettingPage.xaml": {
        "lang:Lang.SettingPage_HoyolabAutoRefresh": "lang:HoYoLabAutoRefreshText.SettingPageTitle",
    },
    "src/Starward/Features/Setting/HoyolabToolboxAutoRefreshSetting.xaml": {
        "lang:Lang.HoyolabAutoRefresh_Title": "lang:HoYoLabAutoRefreshText.Title",
        "lang:Lang.HoyolabAutoRefresh_Description": "lang:HoYoLabAutoRefreshText.Description",
        "lang:Lang.HoyolabAutoRefresh_GenshinDescription": "lang:HoYoLabAutoRefreshText.GenshinDescription",
        "lang:Lang.HoyolabAutoRefresh_StarRailDescription": "lang:HoYoLabAutoRefreshText.StarRailDescription",
        "lang:Lang.HoyolabAutoRefresh_ZZZDescription": "lang:HoYoLabAutoRefreshText.ZZZDescription",
        "lang:Lang.HoyolabAutoRefresh_RefreshNow": "lang:HoYoLabAutoRefreshText.RefreshNow",
        "lang:Lang.HoyolabAutoRefresh_Save": "lang:HoYoLabAutoRefreshText.Save",
        "lang:Lang.HoyolabAutoRefresh_RefreshAll": "lang:HoYoLabAutoRefreshText.RefreshAll",
        "lang:Lang.HoyolabAutoRefresh_ScheduleNote": "lang:HoYoLabAutoRefreshText.ScheduleNote",
    },
    "src/Starward/Features/Setting/HoyolabToolboxAutoRefreshSetting.xaml.cs": {
        "Lang.HoyolabAutoRefresh_IntervalDisabled": "HoYoLabAutoRefreshText.IntervalDisabled",
        "Lang.HoyolabAutoRefresh_IntervalOnStartup": "HoYoLabAutoRefreshText.IntervalOnStartup",
        "Lang.HoyolabAutoRefresh_IntervalDaily": "HoYoLabAutoRefreshText.IntervalDaily",
        "Lang.HoyolabAutoRefresh_IntervalWeekly": "HoYoLabAutoRefreshText.IntervalWeekly",
        "Lang.HoyolabAutoRefresh_IntervalMonthly": "HoYoLabAutoRefreshText.IntervalMonthly",
        "Lang.HoyolabAutoRefresh_SettingsSaved": "HoYoLabAutoRefreshText.SettingsSaved",
        "Lang.HoyolabAutoRefresh_AllGamesCompleted": "HoYoLabAutoRefreshText.AllGamesCompleted",
        "Lang.HoyolabAutoRefresh_ResultFormat": "HoYoLabAutoRefreshText.ResultFormat",
        "Lang.HoyolabAutoRefresh_RefreshCompleted": "HoYoLabAutoRefreshText.RefreshCompleted",
        "Lang.HoyolabAutoRefresh_NothingRefreshed": "HoYoLabAutoRefreshText.NothingRefreshed",
        "Lang.HoyolabAutoRefresh_CheckAccountCookie": "HoYoLabAutoRefreshText.CheckAccountCookie",
        "Lang.HoyolabAutoRefresh_NotPerformedYet": "HoYoLabAutoRefreshText.NotPerformedYet",
        "Lang.HoyolabAutoRefresh_NotScheduled": "HoYoLabAutoRefreshText.NotScheduled",
        "Lang.HoyolabAutoRefresh_NextApplicationStartup": "HoYoLabAutoRefreshText.NextApplicationStartup",
        "Lang.HoyolabAutoRefresh_LastSuccessfulRefresh": "HoYoLabAutoRefreshText.LastSuccessfulRefresh",
        "Lang.HoyolabAutoRefresh_NextScheduledRefresh": "HoYoLabAutoRefreshText.NextScheduledRefresh",
    },
}

for relative, mapping in replacements.items():
    path = ROOT / relative
    text = path.read_text(encoding="utf-8-sig")
    for old, new in mapping.items():
        if new in text:
            continue
        count = text.count(old)
        if count == 0:
            raise RuntimeError(f"Missing {old!r} in {relative}")
        text = text.replace(old, new)
    path.write_text(text, encoding="utf-8")

print("Auto refresh resource references now use HoYoLabAutoRefreshText.")
