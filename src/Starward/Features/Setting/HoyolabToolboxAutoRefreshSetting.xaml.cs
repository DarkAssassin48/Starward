using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Core;
using Starward.Features.GameRecord;
using Starward.Features.ViewHost;
using Starward.Frameworks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Starward.Features.Setting;

public sealed partial class HoyolabToolboxAutoRefreshSetting : PageBase
{
    private readonly GameRecordAutoRefreshService _service = AppConfig.GetService<GameRecordAutoRefreshService>();
    private bool _initializing = true;

    private static readonly IReadOnlyList<string> IntervalNames =
    [
        "Отключено",
        "При каждом запуске клиента",
        "Раз в день",
        "Раз в неделю",
        "Раз в месяц",
    ];

    public HoyolabToolboxAutoRefreshSetting()
    {
        InitializeComponent();
        InitializeInterval(GenshinInterval, GameBiz.hk4e);
        InitializeInterval(StarRailInterval, GameBiz.hkrpg);
        InitializeInterval(ZzzInterval, GameBiz.nap);
        UpdateLastRefreshLabels();
        _initializing = false;
    }

    private static void InitializeInterval(ComboBox comboBox, GameBiz game)
    {
        comboBox.ItemsSource = IntervalNames;
        comboBox.Tag = game.Value;
        comboBox.SelectedIndex = (int)AppConfig.GetGameRecordAutoRefreshInterval(game);
    }

    private void Interval_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || sender is not ComboBox comboBox || comboBox.Tag is not string game || comboBox.SelectedIndex < 0)
        {
            return;
        }

        AppConfig.SetGameRecordAutoRefreshInterval(new GameBiz(game), (GameRecordAutoRefreshInterval)comboBox.SelectedIndex);
        InAppToast.MainWindow?.Success(null, "Настройки автообновления сохранены");
    }

    private async void RefreshNow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string game)
        {
            return;
        }

        button.IsEnabled = false;
        try
        {
            GameRecordAutoRefreshResult result = await _service.RefreshGameNowAsync(new GameBiz(game));
            UpdateLastRefreshLabels();

            if (result.TotalRoles == 0)
            {
                InAppToast.MainWindow?.Warning(null, "Для этой игры нет сохранённых аккаунтов HoYoLAB");
            }
            else if (result.HasAnySuccess)
            {
                InAppToast.MainWindow?.Success(null, $"Обновление завершено: аккаунтов {result.RefreshedRoles}/{result.TotalRoles}, операций {result.SuccessfulOperations}");
            }
            else
            {
                InAppToast.MainWindow?.Warning(null, $"Не удалось обновить данные. Ошибок: {result.FailedOperations}");
            }
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex);
        }
        finally
        {
            button.IsEnabled = true;
        }
    }

    private void UpdateLastRefreshLabels()
    {
        GenshinLastRefresh.Text = FormatLastRefresh(GameBiz.hk4e);
        StarRailLastRefresh.Text = FormatLastRefresh(GameBiz.hkrpg);
        ZzzLastRefresh.Text = FormatLastRefresh(GameBiz.nap);
    }

    private string FormatLastRefresh(GameBiz game)
    {
        DateTimeOffset time = _service.GetLastSuccessfulRefreshTime(game);
        return time == default ? "Последнее успешное обновление: ещё не выполнялось" : $"Последнее успешное обновление: {time.LocalDateTime:g}";
    }
}
