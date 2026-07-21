using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Starward.Features.GameRecord.StarRail;

public sealed partial class StarRailBuffButton : UserControl
{

    private readonly DispatcherTimer _languageRefreshTimer;
    private string? _displayedMechanicTitle;


    public StarRailBuffButton()
    {
        InitializeComponent();

        _languageRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250),
        };
        _languageRefreshTimer.Tick += LanguageRefreshTimer_Tick;

        Loaded += StarRailBuffButton_Loaded;
        Unloaded += StarRailBuffButton_Unloaded;
    }


    private void StarRailBuffButton_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateMechanicSection();
    }


    private void StarRailBuffButton_Unloaded(object sender, RoutedEventArgs e)
    {
        _languageRefreshTimer.Stop();
    }


    private void BuffButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateMechanicSection();
    }


    private void BuffFlyout_Opened(object sender, object e)
    {
        UpdateMechanicSection();
        _languageRefreshTimer.Start();
    }


    private void BuffFlyout_Closed(object sender, object e)
    {
        _languageRefreshTimer.Stop();
    }


    private void LanguageRefreshTimer_Tick(object? sender, object e)
    {
        UpdateMechanicTitle();
    }


    private static void OnMechanicDescriptionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is StarRailBuffButton control)
        {
            control.UpdateMechanicSection();
        }
    }


    private void UpdateMechanicSection()
    {
        string? description = string.IsNullOrWhiteSpace(MechanicDescription)
            ? null
            : MechanicDescription.Trim();

        if (MechanicDescriptionTextBlock is not null)
        {
            MechanicDescriptionTextBlock.Text = description ?? string.Empty;
        }

        if (MechanicSection is not null)
        {
            MechanicSection.Visibility = description is null ? Visibility.Collapsed : Visibility.Visible;
        }

        if (description is null)
        {
            _displayedMechanicTitle = null;
            if (MechanicTitleTextBlock is not null)
            {
                MechanicTitleTextBlock.Text = string.Empty;
            }
            return;
        }

        UpdateMechanicTitle();
    }


    private void UpdateMechanicTitle()
    {
        if (string.IsNullOrWhiteSpace(MechanicDescription))
        {
            return;
        }

        string title = HoYoLabMechanismBuffLabels.GetCurrent(Language);
        if (string.Equals(title, _displayedMechanicTitle, StringComparison.Ordinal))
        {
            return;
        }

        _displayedMechanicTitle = title;
        if (MechanicTitleTextBlock is not null)
        {
            MechanicTitleTextBlock.Text = title;
        }
    }


    public string? BuffIcon
    {
        get => (string?)GetValue(BuffIconProperty);
        set => SetValue(BuffIconProperty, value);
    }

    public static readonly DependencyProperty BuffIconProperty = DependencyProperty.Register(
        nameof(BuffIcon),
        typeof(string),
        typeof(StarRailBuffButton),
        new PropertyMetadata(null));


    public string? BuffName
    {
        get => (string?)GetValue(BuffNameProperty);
        set => SetValue(BuffNameProperty, value);
    }

    public static readonly DependencyProperty BuffNameProperty = DependencyProperty.Register(
        nameof(BuffName),
        typeof(string),
        typeof(StarRailBuffButton),
        new PropertyMetadata(null));


    public string? BuffDescription
    {
        get => (string?)GetValue(BuffDescriptionProperty);
        set => SetValue(BuffDescriptionProperty, value);
    }

    public static readonly DependencyProperty BuffDescriptionProperty = DependencyProperty.Register(
        nameof(BuffDescription),
        typeof(string),
        typeof(StarRailBuffButton),
        new PropertyMetadata(null));


    public string? MechanicDescription
    {
        get => (string?)GetValue(MechanicDescriptionProperty);
        set => SetValue(MechanicDescriptionProperty, value);
    }

    public static readonly DependencyProperty MechanicDescriptionProperty = DependencyProperty.Register(
        nameof(MechanicDescription),
        typeof(string),
        typeof(StarRailBuffButton),
        new PropertyMetadata(null, OnMechanicDescriptionChanged));

}
