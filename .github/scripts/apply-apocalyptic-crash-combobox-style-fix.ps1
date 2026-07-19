$ErrorActionPreference = 'Stop'

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function Read-TextFile([string]$Path) {
    return [System.IO.File]::ReadAllText((Resolve-Path $Path).Path)
}

function Write-TextFile([string]$Path, [string]$Content) {
    [System.IO.File]::WriteAllText((Resolve-Path $Path).Path, $Content, $utf8NoBom)
}

function Replace-Once(
    [string]$Content,
    [string]$Pattern,
    [string]$Replacement,
    [string]$Description
) {
    $regex = [regex]::new($Pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    $matches = $regex.Matches($Content)
    if ($matches.Count -ne 1) {
        throw "${Description}: expected exactly one match, found $($matches.Count)."
    }
    return $regex.Replace($Content, $Replacement, 1)
}

# -----------------------------------------------------------------------------
# Apocalyptic Shadow crash
# -----------------------------------------------------------------------------
# The refined XAML no longer stores three columns directly on BossPanelGrid.
# The original observable-property callback still tried to change column index 2,
# which caused a NullReferenceException both on selection and when unloading.
$codePath = 'src/Starward/Features/GameRecord/StarRail/ApocalypticShadowPage.xaml.cs'
$code = Read-TextFile $codePath
$callbackPattern = 'partial void OnCurrentApocalypticShadowChanged\(ApocalypticShadowInfo\? value\)\s*\{\s*if \(BossPanelGrid is null\)\s*\{\s*return;\s*\}\s*if \(value\?\.Meta\?\.TierceBoss != null\)\s*\{\s*BossPanelGrid\.ColumnDefinitions\[2\]\.Width = new GridLength\(1, GridUnitType\.Star\);\s*\}\s*else\s*\{\s*BossPanelGrid\.ColumnDefinitions\[2\]\.Width = new GridLength\(0\);\s*\}\s*\}'
$callbackReplacement = @'
partial void OnCurrentApocalypticShadowChanged(ApocalypticShadowInfo? value)
    {
        // Two-team and three-team layouts are selected by XAML visibility bindings.
        // No column mutation is required here.
    }
'@
$code = Replace-Once $code $callbackPattern $callbackReplacement 'Apocalyptic Shadow observable callback'
Write-TextFile $codePath $code

$xamlPath = 'src/Starward/Features/GameRecord/StarRail/ApocalypticShadowPage.xaml'
$xaml = Read-TextFile $xamlPath

# Avoid x:Load lifecycle changes for the three-team summary. Keeping the element
# loaded and toggling Visibility prevents generated fields/bindings from becoming
# unavailable while the selected record or page is being unloaded.
$threeTeamPattern = '<Grid x:Name="ThreeBossPanel"\s+ColumnSpacing="12"\s+x:Load="\{x:Bind CurrentApocalypticShadow\.Meta\.TierceBoss, Converter=\{StaticResource ObjectToBoolConverter\}\}">'
$threeTeamReplacement = @'
<Grid ColumnSpacing="12"
                              Visibility="{x:Bind CurrentApocalypticShadow.Meta.TierceBoss, Converter={StaticResource ObjectToVisibilityConverter}}">
'@
$xaml = Replace-Once $xaml $threeTeamPattern $threeTeamReplacement 'Apocalyptic Shadow three-team visibility layout'

# The two-team layout must use the whole row. The previous three-column grid left
# an empty third column, compressed both existing teams and allowed long boss names
# to push the first icon outside the visible area. This replacement affects only
# the reversed-visibility two-team panel; the separate three-team panel is untouched.
$twoTeamPattern = '<!-- Original layout for records with two teams\. -->\s*<Grid Visibility="\{x:Bind CurrentApocalypticShadow\.Meta\.TierceBoss, Converter=\{StaticResource ObjectToVisibilityReversedConverter\}\}">.*?</Grid>\s*<!-- Wider wrapping layout for records with three teams\. -->'
$twoTeamReplacement = @'
<!-- Dedicated full-width layout for records with two teams. -->
                        <Grid ColumnSpacing="28"
                              Visibility="{x:Bind CurrentApocalypticShadow.Meta.TierceBoss, Converter={StaticResource ObjectToVisibilityReversedConverter}}">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition />
                                <ColumnDefinition />
                            </Grid.ColumnDefinitions>

                            <Grid Grid.Column="0" Margin="12,0,6,0">
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="Auto" />
                                    <ColumnDefinition />
                                </Grid.ColumnDefinitions>
                                <sc:CachedImage Width="40"
                                                Height="40"
                                                VerticalAlignment="Center"
                                                CornerRadius="20"
                                                Source="{x:Bind CurrentApocalypticShadow.Meta.UpperBoss.Icon}" />
                                <StackPanel Grid.Column="1"
                                            MinWidth="0"
                                            Margin="12,0,0,0"
                                            VerticalAlignment="Center"
                                            Spacing="2">
                                    <TextBlock FontSize="12">
                                        <Run Text="{x:Bind lang:Lang.ForgottenHallPage_TeamSetup}" />
                                        <Run Text="1" />
                                    </TextBlock>
                                    <TextBlock FontSize="11"
                                               Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                                               MaxLines="2"
                                               Text="{x:Bind CurrentApocalypticShadow.Meta.UpperBoss.Name}"
                                               TextTrimming="CharacterEllipsis"
                                               TextWrapping="WrapWholeWords"
                                               ToolTipService.ToolTip="{x:Bind CurrentApocalypticShadow.Meta.UpperBoss.Name}" />
                                </StackPanel>
                            </Grid>

                            <Grid Grid.Column="1" Margin="6,0,12,0">
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="Auto" />
                                    <ColumnDefinition />
                                </Grid.ColumnDefinitions>
                                <sc:CachedImage Width="40"
                                                Height="40"
                                                VerticalAlignment="Center"
                                                CornerRadius="20"
                                                Source="{x:Bind CurrentApocalypticShadow.Meta.LowerBoss.Icon}" />
                                <StackPanel Grid.Column="1"
                                            MinWidth="0"
                                            Margin="12,0,0,0"
                                            VerticalAlignment="Center"
                                            Spacing="2">
                                    <TextBlock FontSize="12">
                                        <Run Text="{x:Bind lang:Lang.ForgottenHallPage_TeamSetup}" />
                                        <Run Text="2" />
                                    </TextBlock>
                                    <TextBlock FontSize="11"
                                               Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                                               MaxLines="2"
                                               Text="{x:Bind CurrentApocalypticShadow.Meta.LowerBoss.Name}"
                                               TextTrimming="CharacterEllipsis"
                                               TextWrapping="WrapWholeWords"
                                               ToolTipService.ToolTip="{x:Bind CurrentApocalypticShadow.Meta.LowerBoss.Name}" />
                                </StackPanel>
                            </Grid>
                        </Grid>

                        <!-- Wider wrapping layout for records with three teams. -->
'@
$xaml = Replace-Once $xaml $twoTeamPattern $twoTeamReplacement 'Apocalyptic Shadow two-team full-width layout'
Write-TextFile $xamlPath $xaml

# -----------------------------------------------------------------------------
# HoYoLAB schedule ComboBox style
# -----------------------------------------------------------------------------
# Match the language selector: same height, inner padding, border and asymmetric
# rounded corners. Width remains 320 so the schedule text fits comfortably.
$settingsPath = 'src/Starward/Features/Setting/HoyolabToolboxAutoRefreshSetting.xaml'
$settings = Read-TextFile $settingsPath
foreach ($name in @('Genshin', 'StarRail', 'ZZZ')) {
    $pattern = '<ComboBox x:Name="ComboBox_' + $name + '"\s+Width="320"\s+HorizontalAlignment="Left"\s+DisplayMemberPath="Name"\s+ItemsSource="\{x:Bind ScheduleOptions\}" />'
    $replacement = '<ComboBox x:Name="ComboBox_' + $name + '"' + "`r`n" +
        '                                  Height="36"' + "`r`n" +
        '                                  Width="320"' + "`r`n" +
        '                                  HorizontalAlignment="Left"' + "`r`n" +
        '                                  Padding="18.5,0,0,0"' + "`r`n" +
        '                                  BorderThickness="0"' + "`r`n" +
        '                                  CornerRadius="8,18,18,8"' + "`r`n" +
        '                                  DisplayMemberPath="Name"' + "`r`n" +
        '                                  ItemsSource="{x:Bind ScheduleOptions}" />'
    $settings = Replace-Once $settings $pattern $replacement "schedule ComboBox style $name"
}
Write-TextFile $settingsPath $settings

Write-Host 'Apocalyptic Shadow crash guard, separate two-team layout, and HoYoLAB ComboBox styling applied.'
