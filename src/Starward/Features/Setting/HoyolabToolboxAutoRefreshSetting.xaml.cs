using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Core;
using Starward.Features.GameRecord;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;


namespace Starward.Features.Setting;

public sealed partial class HoyolabToolboxAutoRefreshSetting : PageBase
{

    private readonly ILogger<HoyolabToolboxAutoRefreshSetting> _logger = AppConfig.GetLogger<HoyolabToolboxAutoRefreshSetting>();

    private readonly GameRecordAutoRefreshService _autoRefreshService = AppConfig.GetService<GameRecordAutoRefreshService>();


    public string PageTitle => Localized(
        "Автообновление данных HoYoLAB",
        "HoYoLAB Data Auto Refresh");

    public string PageDescription => Localized(
        "Выберите, как часто Starward будет обновлять данные HoYoLAB для каждой игры. Для ежемесячных отчётов также загружаются сведения за все доступные месяцы.",
        "Choose how often Starward refreshes HoYoLAB data for each game. Monthly reports also include details for every available month.");

    public string GenshinDescription => Localized(
        "Витая Бездна, Театр Воображариум, Мрачный натиск и Дневник путешественника.",
        "Spiral Abyss, Imaginarium Theater, Stygian Onslaught and Traveler's Diary.");

    public string StarRailDescription => Localized(
        "Календарь Освоения, Виртуальная вселенная, Зал забвения, Чистый вымысел, Иллюзия конца и Арбитраж аномалий.",
        "Trailblaze Calendar, Simulated Universe, Forgotten Hall, Pure Fiction, Apocalyptic Shadow and Anomaly Arbitration.");

    public string ZZZDescription => Localized(
        "Ежемесячный отчёт Интернота, Оборона шиюй и Смертельный штурм.",
        "Inter-Knot Monthly Report, Shiyu Defense and Deadly Assault.");

    public string RefreshNowText => Localized("Обновить сейчас", "Refresh now");

    public string RefreshAllText => Localized("Обновить все игры сейчас", "Refresh all games now");

    public string SaveText => Localized("Сохранить", "Save");

    public string ScheduleNote => Localized(
        "Обновление запускается только по выбранному расписанию. Если Starward был закрыт, пропущенное обновление выполнится при следующем запуске.",
        "Refresh runs only on the selected schedule. A missed refresh runs the next time Starward starts.");

    public IReadOnlyList<AutoRefreshScheduleOption> ScheduleOptions { get; } =
    [
        new(GameRecordAutoRefreshInterval.Disabled, Localized("Отключено", "Disabled")),
        new(GameRecordAutoRefreshInterval.OnStartup, Localized("При каждом запуске клиента", "Every time the client starts")),
        new(GameRecordAutoRefreshInterval.Daily, Localized("Раз в день", "Once a day")),
        new(GameRecordAutoRefreshInterval.Weekly, Localized("Раз в неделю", "Once a week")),
        new(GameRecordAutoRefreshInterval.Monthly, Localized("Раз в месяц", "Once a month")),
    ];


    public HoyolabToolboxAutoRefreshSetting()
    {
        this.InitializeComponent();
        LoadSchedules();
        UpdateLastRefreshText();
    }


    private void LoadSchedules()
    {
        ComboBox_Genshin.SelectedIndex = (int)AppConfig.GetGameRecordAutoRefreshInterval(GameBiz.hk4e);
        ComboBox_StarRail.SelectedIndex = (int)AppConfig.GetGameRecordAutoRefreshInterval(GameBiz.hkrpg);
        ComboBox_ZZZ.SelectedIndex = (int)AppConfig.GetGameRecordAutoRefreshInterval(GameBiz.nap);
    }


    private void SaveSchedules()
    {
        AppConfig.SetGameRecordAutoRefreshInterval(GameBiz.hk4e, GetSelectedInterval(ComboBox_Genshin));
        AppConfig.SetGameRecordAutoRefreshInterval(GameBiz.hkrpg, GetSelectedInterval(ComboBox_StarRail));
        AppConfig.SetGameRecordAutoRefreshInterval(GameBiz.nap, GetSelectedInterval(ComboBox_ZZZ));
        _autoRefreshService.NotifyScheduleChanged();
    }


    private static GameRecordAutoRefreshInterval GetSelectedInterval(ComboBox comboBox)
    {
        return comboBox.SelectedItem is AutoRefreshScheduleOption option
            ? option.Value
            : GameRecordAutoRefreshInterval.Disabled;
    }


    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveSchedules();
            InAppToast.MainWindow?.Success(Localized("Настройки автообновления сохранены", "Automatic refresh settings saved"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save HoYoLAB Toolbox auto refresh settings.");
            InAppToast.MainWindow?.Error(ex);
        }
    }


    private async void RefreshGame_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || GetGameFromTag(button.Tag) is not GameBiz game)
        {
            return;
        }

        await RefreshGameAsync(game, button);
    }


    private async Task RefreshGameAsync(GameBiz game, Button? button = null)
    {
        try
        {
            SaveSchedules();
            if (button is not null)
            {
                button.IsEnabled = false;
            }

            GameRecordAutoRefreshResult result = await _autoRefreshService.RefreshGameNowAsync(game);
            UpdateLastRefreshText();
            ShowRefreshResult(game.ToGameName(), result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh HoYoLAB Toolbox records for {game}.", game);
            InAppToast.MainWindow?.Error(ex);
        }
        finally
        {
            if (button is not null)
            {
                button.IsEnabled = true;
            }
        }
    }


    private async void RefreshAll_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        try
        {
            SaveSchedules();
            button.IsEnabled = false;

            GameRecordAutoRefreshResult genshin = await _autoRefreshService.RefreshGameNowAsync(GameBiz.hk4e);
            GameRecordAutoRefreshResult starRail = await _autoRefreshService.RefreshGameNowAsync(GameBiz.hkrpg);
            GameRecordAutoRefreshResult zzz = await _autoRefreshService.RefreshGameNowAsync(GameBiz.nap);

            UpdateLastRefreshText();

            int refreshedRoles = genshin.RefreshedRoles + starRail.RefreshedRoles + zzz.RefreshedRoles;
            int successfulOperations = genshin.SuccessfulOperations + starRail.SuccessfulOperations + zzz.SuccessfulOperations;
            int failedOperations = genshin.FailedOperations + starRail.FailedOperations + zzz.FailedOperations;

            if (successfulOperations > 0)
            {
                InAppToast.MainWindow?.Success(
                    Localized("Обновление всех игр завершено", "All games refreshed"),
                    string.Format(
                        Localized("Аккаунтов обновлено: {0}. Успешных операций: {1}, ошибок: {2}", "Roles refreshed: {0}. Successful operations: {1}, errors: {2}"),
                        refreshedRoles,
                        successfulOperations,
                        failedOperations));
            }
            else
            {
                ShowNothingRefreshed();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh all HoYoLAB Toolbox records.");
            InAppToast.MainWindow?.Error(ex);
        }
        finally
        {
            button.IsEnabled = true;
        }
    }


    private void ShowRefreshResult(string gameName, GameRecordAutoRefreshResult result)
    {
        if (result.HasAnySuccess)
        {
            InAppToast.MainWindow?.Success(
                Localized("Обновление завершено", "Refresh completed") + $" — {gameName}",
                string.Format(
                    Localized("Аккаунтов обновлено: {0}. Успешных операций: {1}, ошибок: {2}", "Roles refreshed: {0}. Successful operations: {1}, errors: {2}"),
                    result.RefreshedRoles,
                    result.SuccessfulOperations,
                    result.FailedOperations));
        }
        else
        {
            ShowNothingRefreshed();
        }
    }


    private static void ShowNothingRefreshed()
    {
        InAppToast.MainWindow?.Warning(
            Localized("Нет данных для обновления", "Nothing was refreshed"),
            Localized("Проверьте, что аккаунт добавлен и Cookie действителен", "Check that an account is added and its cookie is valid"));
    }


    private void UpdateLastRefreshText()
    {
        TextBlock_GenshinLastRefresh.Text = FormatLastRefresh(GameBiz.hk4e);
        TextBlock_StarRailLastRefresh.Text = FormatLastRefresh(GameBiz.hkrpg);
        TextBlock_ZZZLastRefresh.Text = FormatLastRefresh(GameBiz.nap);
    }


    private string FormatLastRefresh(GameBiz game)
    {
        DateTimeOffset value = _autoRefreshService.GetLastSuccessfulRefreshTime(game);
        string time = value == default
            ? Localized("Ещё не выполнялось", "Not performed yet")
            : value.LocalDateTime.ToString("g");

        return $"{Localized("Последнее успешное обновление", "Last successful refresh")}: {time}";
    }


    private static GameBiz? GetGameFromTag(object tag)
    {
        return tag?.ToString() switch
        {
            "hk4e" => GameBiz.hk4e,
            "hkrpg" => GameBiz.hkrpg,
            "nap" => GameBiz.nap,
            _ => null,
        };
    }


    private static string Localized(string russian, string english)
    {
        return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ru", StringComparison.OrdinalIgnoreCase)
            ? russian
            : english;
    }


    public sealed record AutoRefreshScheduleOption(GameRecordAutoRefreshInterval Value, string Name);

}
