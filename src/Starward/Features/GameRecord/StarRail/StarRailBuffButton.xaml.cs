using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Language;

namespace Starward.Features.GameRecord.StarRail;

public sealed partial class StarRailBuffButton : UserControl
{

    private bool _mechanicTitleLoaded;


    public StarRailBuffButton()
    {
        InitializeComponent();
        MechanicTitle = Lang.ResourceManager.GetString("PureFictionPage_GritMechanic", Lang.Culture) ?? "Grit Mechanic";
        Loaded += StarRailBuffButton_Loaded;
    }


    private async void StarRailBuffButton_Loaded(object sender, RoutedEventArgs e)
    {
        if (_mechanicTitleLoaded || string.IsNullOrWhiteSpace(MechanicDescription))
        {
            return;
        }

        _mechanicTitleLoaded = true;
        string? title = await HoYoLabMi18nService.GetStringAsync("mechanism_buff", Lang.Culture);
        if (!string.IsNullOrWhiteSpace(title))
        {
            MechanicTitle = title.Trim();
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
        new PropertyMetadata(null));


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
