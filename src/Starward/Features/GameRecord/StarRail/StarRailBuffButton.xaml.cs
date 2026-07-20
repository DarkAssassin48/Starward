using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Starward.Features.GameRecord.StarRail;

public sealed partial class StarRailBuffButton : UserControl
{

    public StarRailBuffButton()
    {
        InitializeComponent();
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

}
