using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Core;
using Starward.Features.Database;
using Starward.Helpers;
using System;
using System.Globalization;


namespace Starward.Features.PlayTime;

[INotifyPropertyChanged]
public sealed partial class PlayTimeButton : UserControl
{

    public GameBiz CurrentGameBiz { get; set; }


    private readonly ILogger<PlayTimeButton> _logger = AppConfig.GetLogger<PlayTimeButton>();

    private readonly PlayTimeService _playTimeService = AppConfig.GetService<PlayTimeService>();


    public PlayTimeButton()
    {
        this.InitializeComponent();
    }


    public TimeSpan PlayTimeTotal { get; set => SetProperty(ref field, value); }

    public TimeSpan PlayTimeMonth { get; set => SetProperty(ref field, value); }

    public TimeSpan PlayTimeWeek { get; set => SetProperty(ref field, value); }

    public TimeSpan PlayTimeDay { get; set => SetProperty(ref field, value); }

    public TimeSpan PlayTimeLast { get; set => SetProperty(ref field, value); }

    public string LastPlayTimeText { get; set => SetProperty(ref field, value); }

    public int StartUpCount { get; set => SetProperty(ref field, value); }


    public static string PlayTimeTitle => GetLabel(0);

    public static string StartupCountTitle => GetLabel(1);

    public static string ThisDayTitle => GetLabel(2);

    public static string ThisWeekTitle => GetLabel(3);

    public static string ThisMonthTitle => GetLabel(4);

    public static string LastTimeTitle => GetLabel(5);


    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        InitializePlayTime();
    }


    private void InitializePlayTime()
    {
        try
        {
            PlayTimeTotal = DatabaseService.GetValue<TimeSpan>($"playtime_total_{CurrentGameBiz}", out _);
            PlayTimeMonth = DatabaseService.GetValue<TimeSpan>($"playtime_month_{CurrentGameBiz}", out _);
            PlayTimeWeek = DatabaseService.GetValue<TimeSpan>($"playtime_week_{CurrentGameBiz}", out _);
            PlayTimeDay = DatabaseService.GetValue<TimeSpan>($"playtime_day_{CurrentGameBiz}", out _);
            StartUpCount = DatabaseService.GetValue<int>($"startup_count_{CurrentGameBiz}", out _);
            (var time, PlayTimeLast) = _playTimeService.GetLastPlayTime(CurrentGameBiz);
            LastPlayTimeText = FormatLastPlayTime(time);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize play time");
        }
    }


    [RelayCommand]
    private void UpdatePlayTime()
    {
        try
        {
            PlayTimeTotal = _playTimeService.GetPlayTimeTotal(CurrentGameBiz);
            PlayTimeMonth = _playTimeService.GetPlayCurrentMonth(CurrentGameBiz);
            PlayTimeWeek = _playTimeService.GetPlayCurrentWeek(CurrentGameBiz);
            PlayTimeDay = _playTimeService.GetPlayCurrentDay(CurrentGameBiz);
            StartUpCount = _playTimeService.GetStartUpCount(CurrentGameBiz);
            (var time, PlayTimeLast) = _playTimeService.GetLastPlayTime(CurrentGameBiz);
            LastPlayTimeText = FormatLastPlayTime(time);
            DatabaseService.SetValue($"playtime_total_{CurrentGameBiz}", PlayTimeTotal);
            DatabaseService.SetValue($"playtime_month_{CurrentGameBiz}", PlayTimeMonth);
            DatabaseService.SetValue($"playtime_week_{CurrentGameBiz}", PlayTimeWeek);
            DatabaseService.SetValue($"playtime_day_{CurrentGameBiz}", PlayTimeDay);
            DatabaseService.SetValue($"startup_count_{CurrentGameBiz}", StartUpCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update play time");
        }
    }


    public static string TimeSpanToString(TimeSpan timeSpan)
    {
        return LocalizedTimeFormatter.FormatHoursMinutes(timeSpan);
    }


    private static string FormatLastPlayTime(DateTimeOffset time)
    {
        return time > DateTimeOffset.MinValue
            ? time.LocalDateTime.ToString("g", CultureInfo.CurrentUICulture)
            : string.Empty;
    }


    private static string GetLabel(int index)
    {
        string culture = CultureInfo.CurrentUICulture.Name;
        string[] labels;

        if (culture.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
            labels = ["Игровое время", "Количество запусков", "Сегодня", "На этой неделе", "В этом месяце", "Последний запуск"];
        else if (culture.StartsWith("de", StringComparison.OrdinalIgnoreCase))
            labels = ["Spielzeit", "Startanzahl", "Heute", "Diese Woche", "Dieser Monat", "Letzter Start"];
        else if (culture.StartsWith("es", StringComparison.OrdinalIgnoreCase))
            labels = ["Tiempo de juego", "Número de inicios", "Hoy", "Esta semana", "Este mes", "Último inicio"];
        else if (culture.StartsWith("it", StringComparison.OrdinalIgnoreCase))
            labels = ["Tempo di gioco", "Numero di avvii", "Oggi", "Questa settimana", "Questo mese", "Ultimo avvio"];
        else if (culture.StartsWith("ja", StringComparison.OrdinalIgnoreCase))
            labels = ["プレイ時間", "起動回数", "今日", "今週", "今月", "前回の起動"];
        else if (culture.StartsWith("ko", StringComparison.OrdinalIgnoreCase))
            labels = ["플레이 시간", "실행 횟수", "오늘", "이번 주", "이번 달", "마지막 실행"];
        else if (culture.StartsWith("th", StringComparison.OrdinalIgnoreCase))
            labels = ["เวลาเล่น", "จำนวนครั้งที่เปิด", "วันนี้", "สัปดาห์นี้", "เดือนนี้", "เปิดครั้งล่าสุด"];
        else if (culture.StartsWith("vi", StringComparison.OrdinalIgnoreCase))
            labels = ["Thời gian chơi", "Số lần khởi động", "Hôm nay", "Tuần này", "Tháng này", "Lần khởi động trước"];
        else if (culture.StartsWith("zh-TW", StringComparison.OrdinalIgnoreCase)
              || culture.StartsWith("zh-HK", StringComparison.OrdinalIgnoreCase)
              || culture.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase))
            labels = ["遊戲時間", "啟動次數", "今日", "本週", "本月", "上次啟動"];
        else if (culture.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            labels = ["游戏时间", "启动次数", "今日", "本周", "本月", "上次启动"];
        else
            labels = ["Play Time", "Startup Count", "This Day", "This Week", "This Month", "Last Time"];

        return labels[index];
    }

}
