using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Language;
using System.Threading.Tasks;

namespace Starward.Features.GameRecord.StarRail;

public sealed partial class StarRailBuffButton : UserControl
{

    private bool _mechanicTitleLoaded;
    private bool _mechanicTitleLoading;


    public StarRailBuffButton()
    {
        InitializeComponent();
        Loaded += StarRailBuffButton_Loaded;
    }


    private async void StarRailBuffButton_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateMechanicSection();
        await EnsureMechanicTitleAsync();
    }


    private async void BuffButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateMechanicSection();
        await EnsureMechanicTitleAsync();
    }


    private static void OnMechanicDescriptionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is StarRailBuffButton control)
        {
            control.UpdateMechanicSection();
            _ = control.EnsureMechanicTitleAsync();
        }
    }


    private void UpdateMechanicSection()
    {
        bool hasDescription = !string.IsNullOrWhiteSpace(MechanicDescription);

        if (MechanicSection is not null)
        {
            MechanicSection.Visibility = hasDescription ? Visibility.Visible : Visibility.Collapsed;
        }

        if (!hasDescription)
        {
            MechanicTitle = null;
            _mechanicTitleLoaded = false;
        }
    }


    private async Task EnsureMechanicTitleAsync()
    {
        if (_mechanicTitleLoaded || _mechanicTitleLoading || string.IsNullOrWhiteSpace(MechanicDescription))
        {
            return;
        }

        _mechanicTitleLoading = true;
        try
        {
            string? title = await HoYoLabMi18nService.GetStringAsync("mechanism_buff", Lang.Culture);
            if (!string.IsNullOrWhiteSpace(title))
            {
                MechanicTitle = title.Trim();
                _mechanicTitleLoaded = true;
            }
        }
        finally
        {
            _mechanicTitleLoading = false;
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


    public string? MechanicTitle
    {
        get => (string?)GetValue(MechanicTitleProperty);
        private set => SetValue(MechanicTitleProperty, value);
    }

    public static readonly DependencyProperty MechanicTitleProperty = DependencyProperty.Register(
        nameof(MechanicTitle),
        typeof(string),
        typeof(StarRailBuffButton),
        new PropertyMetadata(null));

}
