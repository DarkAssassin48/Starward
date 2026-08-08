using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Starward.Features.GameRecord.StarRail;

public sealed partial class StarRailBuffButton : UserControl
{

    public StarRailBuffButton()
    {
        InitializeComponent();
        Loaded += StarRailBuffButton_Loaded;
    }


    private void StarRailBuffButton_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateMechanicSection();
    }


    private void BuffButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateMechanicSection();
    }


    private void BuffFlyout_Opened(object sender, object e)
    {
        UpdateMechanicSection();
    }


    private static void OnMechanicContentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is StarRailBuffButton control)
        {
            control.UpdateMechanicSection();
        }
    }


    private void UpdateMechanicSection()
    {
        string? title = string.IsNullOrWhiteSpace(MechanicTitle)
            ? null
            : MechanicTitle.Trim();
        string? description = string.IsNullOrWhiteSpace(MechanicDescription)
            ? null
            : MechanicDescription.Trim();

        if (MechanicTitleTextBlock is not null)
        {
            MechanicTitleTextBlock.Text = title ?? string.Empty;
            MechanicTitleTextBlock.Visibility = title is null ? Visibility.Collapsed : Visibility.Visible;
        }

        if (MechanicDescriptionTextBlock is not null)
        {
            MechanicDescriptionTextBlock.Text = description ?? string.Empty;
        }

        if (MechanicSection is not null)
        {
            MechanicSection.Visibility = description is null ? Visibility.Collapsed : Visibility.Visible;
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


    public string? MechanicTitle
    {
        get => (string?)GetValue(MechanicTitleProperty);
        set => SetValue(MechanicTitleProperty, value);
    }

    public static readonly DependencyProperty MechanicTitleProperty = DependencyProperty.Register(
        nameof(MechanicTitle),
        typeof(string),
        typeof(StarRailBuffButton),
        new PropertyMetadata(null, OnMechanicContentChanged));


    public string? MechanicDescription
    {
        get => (string?)GetValue(MechanicDescriptionProperty);
        set => SetValue(MechanicDescriptionProperty, value);
    }

    public static readonly DependencyProperty MechanicDescriptionProperty = DependencyProperty.Register(
        nameof(MechanicDescription),
        typeof(string),
        typeof(StarRailBuffButton),
        new PropertyMetadata(null, OnMechanicContentChanged));

}
