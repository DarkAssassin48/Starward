using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Core;
using Starward.Features.GameRecord;
using Starward.Frameworks;
using Starward.Helpers;
using Starward.Language;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Starward.Features.Setting;


public sealed partial class HoyolabToolboxAutoRefreshSetting : PageBase
{

    private readonly ILogger<HoyolabToolboxAutoRefreshSetting> _logger =
        AppConfig.GetLogger<HoyolabToolboxAutoRefreshSetting>();

    private readonly GameRecordAutoRefreshService _autoRefreshService =
        AppConfig.GetService<GameRecordAutoRefreshService>();


    public HoyolabToolboxAutoRefreshSetting()
    {
        InitializeComponent();
        ReloadScheduleOptions();
        UpdateScheduleStatus();

        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, (_, _) =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                Bindings.Update();
                ReloadScheduleOptions(preserveSelection: true);
                UpdateScheduleStatus();
            });
        });
    }


    private IReadOnlyList<AutoRefreshScheduleOption> BuildScheduleOptions()
    {
        return
        [
            new(GameRecordAutoRefreshInterval.Disabled, HoYoLabAutoRefreshText.IntervalDisabled),
            new(GameRecordAutoRefreshInterval.OnStartup, HoYoLabAutoRefreshText.IntervalOnStartup),
            new(GameRecordAutoRefreshInterval.Daily, HoYoLabAutoRefreshText.IntervalDaily),
            new(GameRecordAutoRefreshInterval.Weekly, HoYoLabAutoRefreshText.IntervalWeekly),
            new(GameRecordAutoRefreshInterval.Monthly, HoYoLabAutoRefreshText.IntervalMonthly),
        ];
    }


    private void ReloadScheduleOptions(bool preserveSelection = false)
    {
        GameRecordAutoRefreshInterval genshin = preserveSelection
            ? GetSelectedInterval(ComboBox_Genshin)
            : AppConfig.GetGameRecordAutoRefreshInterval(GameBiz.hk4e);
        GameRecordAutoRefreshInterval starRail = preserveSelection
            ? GetSelectedInterval(ComboBox_StarRail)
            : AppConfig.GetGameRecordAutoRefreshInterval(GameBiz.hkrpg);
        GameRecordAutoRefreshInterval zzz = preserveSelection
            ? GetSelectedInterval(ComboBox_ZZZ)
            : AppConfig.GetGameRecordAutoRefreshInterval(GameBiz.nap);

        IReadOnlyList<AutoRefreshScheduleOption> options = BuildScheduleOptions();
        ComboBox_Genshin.ItemsSource = options;
        ComboBox_StarRail.ItemsSource = options;
        ComboBox_ZZZ.ItemsSource = options;

        ComboBox_Genshin.SelectedIndex = (int)genshin;
        ComboBox_StarRail.SelectedIndex = (int)starRail;
        ComboBox_ZZZ.SelectedIndex = (int)zzz;
    }


    private void SaveSchedules()
    {
        AppConfig.SetGameRecordAutoRefreshInterval(GameBiz.hk4e, GetSelectedInterval(ComboBox_Genshin));
        AppConfig.SetGameRecordAutoRefreshInterval(GameBiz.hkrpg, GetSelectedInterval(ComboBox_StarRail));
        AppConfig.SetGameRecordAutoRefreshInterval(GameBiz.nap, GetSelectedInterval(ComboBox_ZZZ));
        _autoRefreshService.NotifyScheduleChanged();
        UpdateScheduleStatus();
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
            InAppToast.MainWindow?.Success(HoYoLabAutoRefreshText.SettingsSaved);
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
            UpdateScheduleStatus();
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

            UpdateScheduleStatus();

            int refreshedRoles = genshin.RefreshedRoles + starRail.RefreshedRoles + zzz.RefreshedRoles;
            int successfulOperations = genshin.SuccessfulOperations + starRail.SuccessfulOperations + zzz.SuccessfulOperations;
            int failedOperations = genshin.FailedOperations + starRail.FailedOperations + zzz.FailedOperations;

            if (successfulOperations > 0)
            {
                InAppToast.MainWindow?.Success(
                    HoYoLabAutoRefreshText.AllGamesCompleted,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        HoYoLabAutoRefreshText.ResultFormat,
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


    private static void ShowRefreshResult(string gameName, GameRecordAutoRefreshResult result)
    {
        if (result.HasAnySuccess)
        {
            InAppToast.MainWindow?.Success(
                $"{HoYoLabAutoRefreshText.RefreshCompleted} — {gameName}",
                string.Format(
                    CultureInfo.CurrentCulture,
                    HoYoLabAutoRefreshText.ResultFormat,
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
            HoYoLabAutoRefreshText.NothingRefreshed,
            HoYoLabAutoRefreshText.CheckAccountCookie);
    }


    private void UpdateScheduleStatus()
    {
        TextBlock_GenshinStatus.Text = FormatScheduleStatus(GameBiz.hk4e);
        TextBlock_StarRailStatus.Text = FormatScheduleStatus(GameBiz.hkrpg);
        TextBlock_ZZZStatus.Text = FormatScheduleStatus(GameBiz.nap);
    }


    private string FormatScheduleStatus(GameBiz game)
    {
        DateTimeOffset last = _autoRefreshService.GetLastSuccessfulRefreshTime(game);
        string lastText = last == default
            ? HoYoLabAutoRefreshText.NotPerformedYet
            : last.LocalDateTime.ToString("g", CultureInfo.CurrentCulture);

        GameRecordAutoRefreshInterval interval = AppConfig.GetGameRecordAutoRefreshInterval(game);
        string nextText;
        if (interval is GameRecordAutoRefreshInterval.Disabled)
        {
            nextText = HoYoLabAutoRefreshText.NotScheduled;
        }
        else if (interval is GameRecordAutoRefreshInterval.OnStartup)
        {
            nextText = HoYoLabAutoRefreshText.NextApplicationStartup;
        }
        else
        {
            DateTimeOffset next = _autoRefreshService.GetNextScheduledRefreshTime(game);
            nextText = next == default
                ? HoYoLabAutoRefreshText.NotScheduled
                : next.LocalDateTime.ToString("g", CultureInfo.CurrentCulture);
        }

        return $"{HoYoLabAutoRefreshText.LastSuccessfulRefresh}: {lastText}{Environment.NewLine}" +
               $"{HoYoLabAutoRefreshText.NextScheduledRefresh}: {nextText}";
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


    protected override void OnUnloaded()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }


    public sealed record AutoRefreshScheduleOption(GameRecordAutoRefreshInterval Value, string Name);

}
