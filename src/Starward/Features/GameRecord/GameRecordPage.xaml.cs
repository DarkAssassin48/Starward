using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Features.GameLauncher;
using Starward.Features.GameRecord.Genshin;
using Starward.Features.GameRecord.StarRail;
using Starward.Features.GameRecord.ZZZ;
using Starward.Features.ViewHost;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;


namespace Starward.Features.GameRecord;

public sealed partial class GameRecordPage : PageBase
{


    private readonly ILogger<GameRecordPage> _logger = AppConfig.GetLogger<GameRecordPage>();


    private readonly GameRecordService _gameRecordService = AppConfig.GetService<GameRecordService>();

    private readonly GameRecordAutoRefreshService _autoRefreshService = AppConfig.GetService<GameRecordAutoRefreshService>();

    private NavigationViewItem? _autoRefreshSettingsItem;



    public GameRecordPage()
    {
        this.InitializeComponent();
    }




    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        CurrentGameBiz = CurrentGameBiz.Value switch
        {
            GameBiz.hk4e_bilibili => GameBiz.hk4e_cn,
            GameBiz.hkrpg_bilibili => GameBiz.hkrpg_cn,
            GameBiz.nap_bilibili => GameBiz.nap_cn,
            _ => CurrentGameBiz,
        };
        _gameRecordService.IsHoyolab = CurrentGameBiz.IsGlobalServer();
        NavigationViewItem_UpdateDeviceInfo.Visibility = CurrentGameBiz.IsGlobalServer()
            ? Visibility.Collapsed
            : Visibility.Visible;
        _gameRecordService.Language = CultureInfo.CurrentUICulture.Name;
        if (_autoRefreshSettingsItem is not null)
        {
            _autoRefreshSettingsItem.Visibility = CurrentGameBiz.Game is GameBiz.hk4e or GameBiz.hkrpg or GameBiz.nap
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        InitializeNavigationViewItemVisibility();
    }




    protected override async void OnLoaded()
    {
        if (AppConfig.HoyolabToolboxPaneOpen)
        {
            OpenNavigationViewPane();
        }
        else
        {
            CloseNavigationViewPane();
        }
        WeakReferenceMessenger.Default.Register<GameRecordRoleChangedMessage>(this, (r, m) =>
        {
            LoadGameRoles(m.GameRole);
        });
        WeakReferenceMessenger.Default.Register<GameRecordVerifyAccountMessage>(this, (r, m) =>
        {
            ShowBattleChronicleWindow();
        });
        WeakReferenceMessenger.Default.Register<GameRecordAutoRefreshCompletedMessage>(this, (r, m) =>
        {
            if (CurrentGameBiz.Game == m.Game.Game && frame.SourcePageType is Type pageType && pageType != typeof(LoginPage))
            {
                DispatcherQueue.TryEnqueue(() => NavigateTo(pageType, force_navigate: true));
            }
        });
        await Task.Delay(16);
        NavigateTo(typeof(BlankPage));
        if (await CheckAgreementAsync())
        {
            LoadGameRoles();
            await UpdateDeviceInfoAsync();
            await RefreshGameRoleHeadIconSilentlyAsync();
        }
    }



    protected override void OnUnloaded()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        NavigationViewItem_BattleChronicle.Tapped -= NavigationViewItem_BattleChronicle_Tapped;
        NavigationViewItem_UpdateDeviceInfo.Tapped -= NavigationViewItem_UpdateDeviceInfo_Tapped;
        if (_autoRefreshSettingsItem is not null)
        {
            _autoRefreshSettingsItem.Tapped -= NavigationViewItem_AutoRefreshSettings_Tapped;
        }
        CurrentRole = null;
        GameRoleList = null!;
        _battleChronicleWindow = null;
    }




    private async Task<bool> CheckAgreementAsync()
    {
        try
        {
            if (!AppConfig.AcceptHoyolabToolboxAgreement)
            {
                var dialog = new ContentDialog
                {
                    Title = Lang.Common_Disclaimer,
                    Content = Lang.HoyolabToolboxPage_DisclaimerContent,
                    PrimaryButtonText = Lang.Common_Accept + " (5s)",
                    SecondaryButtonText = Lang.Common_Reject,
                    IsPrimaryButtonEnabled = false,
                    DefaultButton = ContentDialogButton.Secondary,
                    XamlRoot = this.XamlRoot,
                };
                var resultTask = dialog.ShowAsync();
                bool cancel = false;
                for (int i = 0; i < 5; i++)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        await Task.Delay(100);
                        if (resultTask.Status is Windows.Foundation.AsyncStatus.Completed)
                        {
                            cancel = true;
                            break;
                        }
                    }
                    if (cancel)
                    {
                        break;
                    }
                    dialog.PrimaryButtonText = Lang.Common_Accept + $" ({4 - i}s)";
                }
                dialog.PrimaryButtonText = Lang.Common_Accept;
                dialog.IsPrimaryButtonEnabled = true;
                var result = await resultTask;
                if (result is ContentDialogResult.Primary)
                {
                    AppConfig.AcceptHoyolabToolboxAgreement = true;
                }
                else
                {
                    WeakReferenceMessenger.Default.Send(new MainViewNavigateMessage(typeof(GameLauncherPage)));
                    return false;
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check agreement.");
            return false;
        }
    }




    #region Navigation Style


    public Thickness NavigationViewItemContentMargin { get; set => SetProperty(ref field, value); } = new Thickness(-2, 0, 0, 0);


    // Close pane
    private void Grid_Avatar_1_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        CloseNavigationViewPane();
    }


    // Open pane
    private void Border_Avatar_2_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        OpenNavigationViewPane();
    }


    private void OpenNavigationViewPane()
    {
        NavigationViewItemContentMargin = new Thickness(-2, 0, 0, 0);
        NavigationView_Toolbox.IsPaneOpen = true;
        Grid_Avatar_1.Visibility = Visibility.Visible;
        Border_Avatar_2.Visibility = Visibility.Collapsed;
        AppConfig.HoyolabToolboxPaneOpen = true;
    }


    private void CloseNavigationViewPane()
    {
        NavigationViewItemContentMargin = new Thickness(2, 0, 0, 0);
        NavigationView_Toolbox.IsPaneOpen = false;
        Grid_Avatar_1.Visibility = Visibility.Collapsed;
        Border_Avatar_2.Visibility = Visibility.Visible;
        AppConfig.HoyolabToolboxPaneOpen = false;
    }


    private void InitializeNavigationViewItemVisibility()
    {
        if (CurrentGameBiz.Game is GameBiz.bh3)
        {
            NavigationViewItem_BattleChronicle.Visibility = Visibility.Visible;
            // 崩坏3战绩图片
            Image_BattleChronicle.Source = new BitmapImage(new("ms-appx:///Assets/Image/4d94fbd5ff63c8b4344876ce21e04d10_2581928258151711511.png"));
        }
        else if (CurrentGameBiz.Game is GameBiz.hk4e)
        {
            NavigationViewItem_BattleChronicle.Visibility = Visibility.Visible;
            NavigationViewItem_TravelersDiary.Visibility = Visibility.Visible;
            NavigationViewItem_SpiralAbyss.Visibility = Visibility.Visible;
            NavigationViewItem_ImaginariumTheater.Visibility = Visibility.Visible;
            NavigationViewItem_StygianOnslaught.Visibility = Visibility.Visible;
            // 原神战绩图片
            Image_BattleChronicle.Source = new BitmapImage(new("ms-appx:///Assets/Image/ced4deac2162690105bbc8baad2b51a3_4109616186965788891.png"));
        }
        else if (CurrentGameBiz.Game is GameBiz.hkrpg)
        {
            NavigationViewItem_BattleChronicle.Visibility = Visibility.Visible;
            NavigationViewItem_TrailblazeMonthlyCalendar.Visibility = Visibility.Visible;
            NavigationViewItem_SimulatedUniverse.Visibility = Visibility.Visible;
            NavigationViewItem_ForgottenHall.Visibility = Visibility.Visible;
            NavigationViewItem_PureFiction.Visibility = Visibility.Visible;
            NavigationViewItem_ApocalypticShadow.Visibility = Visibility.Visible;
            NavigationViewItem_ChallengePeak.Visibility = Visibility.Visible;
            // 铁道战绩图片
            Image_BattleChronicle.Source = new BitmapImage(new("ms-appx:///Assets/Image/ade9545750299456a3fcbc8c3b63521d_2941971308029698042.png"));
        }
        else if (CurrentGameBiz.Game is GameBiz.nap)
        {
            NavigationViewItem_BattleChronicle.Visibility = Visibility.Visible;
            NavigationViewItem_InterKnotMonthlyReport.Visibility = Visibility.Visible;
            NavigationViewItem_ShiyuDefense.Visibility = Visibility.Visible;
            NavigationViewItem_DeadlyAssault.Visibility = Visibility.Visible;
            // 绝区零战绩图片
            Image_BattleChronicle.Source = new BitmapImage(new("ms-appx:///Assets/Image/bc8f0b7384b306c80f2a1fcca9f3d14b_8590605504999484795.png"));
        }
    }




    #endregion




    #region Game Role Info



    public GameRecordRole? CurrentRole
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(AvatarUrl));
            }
        }
    }


    public List<GameRecordRole> GameRoleList { get; set => SetProperty(ref field, value); }


    public string AvatarUrl => !string.IsNullOrWhiteSpace(CurrentRole?.HeadIcon) ? CurrentRole.HeadIcon : $"ms-appx:///Assets/Image/icon_{(CurrentGameBiz.IsGlobalServer() ? "hoyolab" : "hyperion")}.png";



    private void LoadGameRoles(GameRecordRole? role = null)
    {
        try
        {
            if (role != null)
            {
                _gameRecordService.SetLastSelectGameRecordRole(CurrentGameBiz, role);
            }
            role ??= _gameRecordService.GetLastSelectGameRecordRoleOrTheFirstOne(CurrentGameBiz);
            var list = _gameRecordService.GetGameRoles(CurrentGameBiz);
            CurrentRole = role ?? list.FirstOrDefault();
            GameRoleList = list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load game roles ({gameBiz}).", CurrentGameBiz);
        }
    }




    [RelayCommand]
    private void WebLogin()
    {
        NavigateTo(typeof(LoginPage), CurrentGameBiz);
    }




    [RelayCommand]
    private async Task RefreshGameRoleInfoAsync()
    {
        try
        {
            if (CurrentRole is null)
            {
                await _gameRecordService.RefreshAllGameRolesInfoAsync();
            }
            else
            {
                await _gameRecordService.RefreshGameRoleInfoAsync(CurrentRole);
            }
            LoadGameRoles();
        }
        catch (miHoYoApiException ex)
        {
            _logger.LogError(ex, "Refresh game role info ({gameBiz}, {uid}).", CurrentRole?.GameBiz, CurrentRole?.Uid);
            InAppToast.MainWindow?.Warning(Lang.Common_AccountError, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Refresh game role info ({gameBiz}, {uid}).", CurrentRole?.GameBiz, CurrentRole?.Uid);
            InAppToast.MainWindow?.Warning(Lang.Common_NetworkError, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh game role info ({gameBiz}, {uid}).", CurrentRole?.GameBiz, CurrentRole?.Uid);
            InAppToast.MainWindow?.Error(ex);
        }
    }



    private void ListView_GameRoles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is GameRecordRole role)
        {
            CurrentRole = role;
            _gameRecordService.SetLastSelectGameRecordRole(CurrentGameBiz, role);
            if (frame.SourcePageType?.Name is not nameof(LoginPage))
            {
                NavigateTo(frame.SourcePageType, force_navigate: true);
            }
        }
    }




    private void MenuFlyoutItem_CopyCookie_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement { Tag: GameRecordRole role })
            {

                ClipboardHelper.SetText(role.Cookie);
            }
        }
        catch { }
    }



    private void MenuFlyoutItem_DeleteGameRole_Click(object sender, RoutedEventArgs e)
    {
        GameRecordRole? gameRole = null;
        try
        {
            if (sender is FrameworkElement { Tag: GameRecordRole role })
            {
                gameRole = role;
                _gameRecordService.DeleteGameRole(role);
                LoadGameRoles();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete game role ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
        }
    }



    [RelayCommand]
    private async Task InputCookieAsync()
    {
        try
        {
            var textbox = new TextBox
            {
                IsSpellCheckEnabled = false,
            };
            var dialog = new ContentDialog
            {
                Title = Lang.HoyolabToolboxPage_InputCookie,
                Content = textbox,
                PrimaryButtonText = Lang.Common_Confirm,
                SecondaryButtonText = Lang.Common_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
            };
            var result = await dialog.ShowAsync();
            if (result is ContentDialogResult.Primary)
            {
                var cookie = textbox.Text;
                if (string.IsNullOrWhiteSpace(cookie))
                {
                    _logger.LogInformation("Input cookie is null or white space.");
                    return;
                }
                var user = await _gameRecordService.AddRecordUserAsync(cookie);
                var roles = await _gameRecordService.AddGameRolesAsync(cookie);
                InAppToast.MainWindow?.Success(null, string.Format(Lang.LoginPage_AlreadyAddedGameRoles, roles.Count, string.Join("\r\n", roles.Select(x => $"{x.Nickname}  {x.Uid}"))), 5000);
                LoadGameRoles(roles.FirstOrDefault(x => x.GameBiz == CurrentGameBiz.ToString()));
            }
        }
        catch (miHoYoApiException ex)
        {
            _logger.LogError(ex, "Input cookie");
            InAppToast.MainWindow?.Warning(Lang.Common_AccountError, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Input cookie");
            InAppToast.MainWindow?.Warning(Lang.Common_NetworkError, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Input cookie");
            InAppToast.MainWindow?.Error(ex);
        }
    }



    private async Task RefreshGameRoleHeadIconSilentlyAsync()
    {
        try
        {
            if (CurrentRole is not null)
            {
                await _gameRecordService.UpdateGameRoleHeadIconAsync(CurrentRole);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update game role head icon silently ({gameBiz}, {uid}).", CurrentRole?.GameBiz, CurrentRole?.Uid);
        }
    }




    #endregion




    #region Navigate



    private void NavigateTo(Type? page, object? parameter = null, bool force_navigate = false)
    {
        if (page is null)
        {
            return;
        }
        if (!force_navigate && frame.SourcePageType == page)
        {
            return;
        }
        frame.Navigate(page, parameter ?? CurrentRole);
    }



    private void NavigationView_Toolbox_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        try
        {
            var item = args.InvokedItemContainer as NavigationViewItem;
            if (item != null)
            {
                if (args.InvokedItemContainer?.IsSelected ?? false)
                {
                    return;
                }
                var type = item.Tag switch
                {
                    nameof(TravelersDiaryPage) => typeof(TravelersDiaryPage),
                    nameof(SpiralAbyssPage) => typeof(SpiralAbyssPage),
                    nameof(ImaginariumTheaterPage) => typeof(ImaginariumTheaterPage),
                    nameof(StygianOnslaughtPage) => typeof(StygianOnslaughtPage),
                    nameof(TrailblazeCalendarPage) => typeof(TrailblazeCalendarPage),
                    nameof(SimulatedUniversePage) => typeof(SimulatedUniversePage),
                    nameof(ForgottenHallPage) => typeof(ForgottenHallPage),
                    nameof(PureFictionPage) => typeof(PureFictionPage),
                    nameof(ApocalypticShadowPage) => typeof(ApocalypticShadowPage),
                    nameof(ChallengePeakPage) => typeof(ChallengePeakPage),
                    nameof(InterKnotMonthlyReportPage) => typeof(InterKnotMonthlyReportPage),
                    nameof(ShiyuDefensePage) => typeof(ShiyuDefensePage),
                    nameof(DeadlyAssaultPage) => typeof(DeadlyAssaultPage),
                    _ => null,
                };
                NavigateTo(type);
            }
        }
        catch { }
    }



    private void NavigationViewItem_BattleChronicle_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        ShowBattleChronicleWindow();
    }



    private BattleChronicleWindow? _battleChronicleWindow;



    private void ShowBattleChronicleWindow()
    {
        // 窗口关闭后 AppWindow is null
        if (_battleChronicleWindow?.AppWindow is null)
        {
            _battleChronicleWindow = new BattleChronicleWindow
            {
                CurrentRole = CurrentRole,
            };
        }
        else if (_battleChronicleWindow.CurrentRole != CurrentRole)
        {
            _battleChronicleWindow.CurrentRole = CurrentRole;
        }
        _battleChronicleWindow.Activate();
    }




    #endregion




    #region Game Record Auto Refresh


    private void InitializeAutoRefreshSettingsItem()
    {
        // PaneFooter currently contains the device fingerprint item. Re-wrap it in
        // a panel so the new automatic refresh settings are available beside it.
        NavigationView_Toolbox.PaneFooter = null;

        _autoRefreshSettingsItem = new NavigationViewItem
        {
            Margin = new Thickness(-2, 0, 0, -4),
            Content = Localized("Настройки автообновления", "Automatic refresh settings"),
            Icon = new FontIcon { Glyph = "\uE895" },
        };
        _autoRefreshSettingsItem.Tapped += NavigationViewItem_AutoRefreshSettings_Tapped;

        var footer = new StackPanel();
        footer.Children.Add(_autoRefreshSettingsItem);
        footer.Children.Add(NavigationViewItem_UpdateDeviceInfo);
        NavigationView_Toolbox.PaneFooter = footer;
    }


    private async void NavigationViewItem_AutoRefreshSettings_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        await ShowAutoRefreshSettingsAsync();
    }


    private async Task ShowAutoRefreshSettingsAsync()
    {
        try
        {
            GameBiz game = CurrentGameBiz.ToGame();
            GameRecordAutoRefreshInterval current = AppConfig.GetGameRecordAutoRefreshInterval(game);

            var scheduleComboBox = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                MinWidth = 300,
            };

            GameRecordAutoRefreshInterval[] values =
            [
                GameRecordAutoRefreshInterval.Disabled,
                GameRecordAutoRefreshInterval.OnStartup,
                GameRecordAutoRefreshInterval.Daily,
                GameRecordAutoRefreshInterval.Weekly,
                GameRecordAutoRefreshInterval.Monthly,
            ];

            foreach (GameRecordAutoRefreshInterval value in values)
            {
                scheduleComboBox.Items.Add(new ComboBoxItem
                {
                    Content = GetAutoRefreshIntervalText(value),
                    Tag = value,
                });
            }
            scheduleComboBox.SelectedIndex = Array.IndexOf(values, current);

            DateTimeOffset lastRefresh = _autoRefreshService.GetLastSuccessfulRefreshTime(game);
            string lastRefreshText = lastRefresh == default
                ? Localized("Ещё не выполнялось", "Not performed yet")
                : lastRefresh.LocalDateTime.ToString("g");

            var content = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock
                    {
                        Text = GetAutoRefreshDescription(game),
                        TextWrapping = TextWrapping.Wrap,
                        MaxWidth = 520,
                    },
                    new TextBlock
                    {
                        Text = Localized("Расписание", "Schedule"),
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    },
                    scheduleComboBox,
                    new TextBlock
                    {
                        Text = $"{Localized("Последнее успешное обновление", "Last successful refresh")}: {lastRefreshText}",
                        Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new TextBlock
                    {
                        Text = Localized(
                            "Расписание проверяется при запуске Starward и каждые 30 минут, пока клиент открыт. Если срок наступил при закрытом клиенте, данные обновятся при следующем запуске.",
                            "The schedule is checked when Starward starts and every 30 minutes while it is open. Missed refreshes run on the next launch."),
                        Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                        TextWrapping = TextWrapping.Wrap,
                        MaxWidth = 520,
                    },
                },
            };

            var dialog = new ContentDialog
            {
                Title = $"{Localized("Автообновление", "Automatic refresh")} — {game.ToGameName()}",
                Content = content,
                PrimaryButtonText = Localized("Сохранить", "Save"),
                SecondaryButtonText = Localized("Обновить сейчас", "Refresh now"),
                CloseButtonText = Localized("Отмена", "Cancel"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot,
            };

            ContentDialogResult result = await dialog.ShowAsync();
            if (result is ContentDialogResult.Primary or ContentDialogResult.Secondary &&
                scheduleComboBox.SelectedItem is ComboBoxItem { Tag: GameRecordAutoRefreshInterval interval })
            {
                AppConfig.SetGameRecordAutoRefreshInterval(game, interval);
            }

            if (result is ContentDialogResult.Primary)
            {
                InAppToast.MainWindow?.Success(Localized("Настройки автообновления сохранены", "Automatic refresh settings saved"));
            }
            else if (result is ContentDialogResult.Secondary)
            {
                GameRecordAutoRefreshResult refreshResult = await _autoRefreshService.RefreshGameNowAsync(game);
                if (refreshResult.HasAnySuccess)
                {
                    InAppToast.MainWindow?.Success(
                        Localized("Обновление завершено", "Refresh completed"),
                        string.Format(
                            Localized("Аккаунтов обновлено: {0}. Успешных операций: {1}, ошибок: {2}", "Roles refreshed: {0}. Successful operations: {1}, errors: {2}"),
                            refreshResult.RefreshedRoles,
                            refreshResult.SuccessfulOperations,
                            refreshResult.FailedOperations));
                }
                else
                {
                    InAppToast.MainWindow?.Warning(
                        Localized("Нет данных для обновления", "Nothing was refreshed"),
                        Localized("Проверьте, что аккаунт добавлен и Cookie действителен", "Check that an account is added and its cookie is valid"));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Show or execute game record auto refresh settings ({gameBiz}).", CurrentGameBiz);
            InAppToast.MainWindow?.Error(ex);
        }
    }


    private static string GetAutoRefreshIntervalText(GameRecordAutoRefreshInterval interval)
    {
        return interval switch
        {
            GameRecordAutoRefreshInterval.Disabled => Localized("Отключено", "Disabled"),
            GameRecordAutoRefreshInterval.OnStartup => Localized("При каждом запуске клиента", "Every time the client starts"),
            GameRecordAutoRefreshInterval.Daily => Localized("Раз в день", "Once a day"),
            GameRecordAutoRefreshInterval.Weekly => Localized("Раз в неделю", "Once a week"),
            GameRecordAutoRefreshInterval.Monthly => Localized("Раз в месяц", "Once a month"),
            _ => interval.ToString(),
        };
    }


    private static string GetAutoRefreshDescription(GameBiz game)
    {
        return game.Game switch
        {
            GameBiz.hk4e => Localized(
                "Будут обновляться: Витая Бездна, Театр Воображариум, Мрачный натиск и Дневник путешественника — для всех добавленных аккаунтов Genshin Impact.",
                "Refreshes Spiral Abyss, Imaginarium Theater, Stygian Onslaught and Traveler's Diary for every added Genshin Impact account."),
            GameBiz.hkrpg => Localized(
                "Будут обновляться: Календарь Освоения, Виртуальная вселенная, Зал забвения, Чистый вымысел, Иллюзия конца и Арбитраж аномалий — для всех добавленных аккаунтов Honkai: Star Rail.",
                "Refreshes Trailblaze Calendar, Simulated Universe, Forgotten Hall, Pure Fiction, Apocalyptic Shadow and Anomaly Arbitration for every added Honkai: Star Rail account."),
            GameBiz.nap => Localized(
                "Будут обновляться: Ежемесячный отчёт Интернота, Оборона шиюй и Смертельный штурм — для всех добавленных аккаунтов Zenless Zone Zero.",
                "Refreshes Inter-Knot Monthly Report, Shiyu Defense and Deadly Assault for every added Zenless Zone Zero account."),
            _ => string.Empty,
        };
    }


    private static string Localized(string russian, string english)
    {
        return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ru", StringComparison.OrdinalIgnoreCase)
            ? russian
            : english;
    }


    #endregion




    #region Device Info




    private async void NavigationViewItem_UpdateDeviceInfo_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        await UpdateDeviceInfoAsync(true);
    }



    private async Task UpdateDeviceInfoAsync(bool forceUpdate = false)
    {
        try
        {
            await _gameRecordService.UpdateDeviceFpAsync(forceUpdate);
            if (forceUpdate)
            {
                InAppToast.MainWindow?.Success(Lang.HoyolabToolboxPage_TheDeviceFingerprintIsAlreadyUpdated);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update device info");
            if (forceUpdate)
            {
                InAppToast.MainWindow?.Error(ex);
            }
        }
    }





    #endregion





    public static void HandleMiHoYoApiException(miHoYoApiException ex)
    {
        if (ex.ReturnCode is 1034 or 5003 or 10035 or 10041 or 10053)
        {
            InAppToast.MainWindow?.ShowWithButton(InfoBarSeverity.Warning, Lang.Common_AccountError, ex.Message, Lang.HoyolabToolboxPage_VerifyAccount, () =>
            {
                WeakReferenceMessenger.Default.Send(new GameRecordVerifyAccountMessage());
            });
        }
        else
        {
            InAppToast.MainWindow?.Warning(Lang.Common_AccountError, ex.Message);
        }
    }


}
