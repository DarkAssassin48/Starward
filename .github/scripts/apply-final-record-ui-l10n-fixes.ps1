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

function Add-Using([string]$Content, [string]$AfterPattern, [string]$UsingLine, [string]$Description) {
    if ($Content.Contains($UsingLine)) {
        return $Content
    }
    return Replace-Once $Content $AfterPattern ('$1' + $UsingLine + "`r`n") $Description
}

function Add-StageNameFormatter([string]$Path, [string]$Anchor, [string]$Description) {
    $content = Read-TextFile $Path
    $content = Add-Using $content '(using System\.Net\.Http;\r?\n)' 'using System.Text.RegularExpressions;' "$Description regex using"

    if ($content -notmatch 'FormatStageName\(') {
        $method = @'


    public static string FormatStageName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return name ?? string.Empty;
        }

        return Regex.Replace(
            name,
            @"(?<=[\p{L}\p{N}\)])(?=(?:Starward Mode|Режим(?:\s+Starward)?\b))",
            Environment.NewLine,
            RegexOptions.CultureInvariant);
    }
'@
        $content = Replace-Once $content $Anchor ('$1' + $method) "$Description formatter method"
    }
    Write-TextFile $Path $content
}

# -----------------------------------------------------------------------------
# Remove full-name hover tooltips from stage titles
# -----------------------------------------------------------------------------
foreach ($path in @(
    'src/Starward/Features/GameRecord/StarRail/ApocalypticShadowPage.xaml',
    'src/Starward/Features/GameRecord/StarRail/PureFictionPage.xaml',
    'src/Starward/Features/GameRecord/Genshin/StygianOnslaughtPage.xaml'
)) {
    $xaml = Read-TextFile $path
    $xaml = Replace-Once `
        $xaml `
        '(TextWrapping="WrapWholeWords")\s+ToolTipService\.ToolTip="\{x:Bind Name\}"' `
        '$1' `
        "remove stage title tooltip from $path"
    Write-TextFile $path $xaml
}

# -----------------------------------------------------------------------------
# Forgotten Hall: adaptive title, separated Starward Mode, no hover tooltip
# -----------------------------------------------------------------------------
$forgottenPath = 'src/Starward/Features/GameRecord/StarRail/ForgottenHallPage.xaml'
$forgotten = Read-TextFile $forgottenPath
$forgotten = Replace-Once $forgotten '<Grid Height="300" Padding="20,0,20,0">' '<Grid MinHeight="300" Padding="20,0,20,0">' 'Forgotten Hall card height'
$forgotten = Replace-Once $forgotten '<RowDefinition Height="52" />' '<RowDefinition Height="Auto" MinHeight="52" />' 'Forgotten Hall title row'
$forgottenTitlePattern = '<StackPanel VerticalAlignment="Center" Spacing="2">\s*<TextBlock FontWeight="Bold"\s+Text="\{x:Bind Name\}"\s+TextTrimming="CharacterEllipsis" />\s*<TextBlock FontSize="12" Foreground="\{ThemeResource TextFillColorSecondaryBrush\}">\s*<Run Text="\{x:Bind lang:Lang\.ForgottenHallPage_CyclesUsed\}" />\s*<Run Text="\{x:Bind RoundNum\}" />\s*</TextBlock>\s*</StackPanel>'
$forgottenTitleReplacement = @'
<StackPanel Grid.Column="0"
                                                 MinWidth="0"
                                                 Margin="0,6,16,6"
                                                 VerticalAlignment="Center"
                                                 Spacing="2">
                                        <TextBlock FontWeight="Bold"
                                                   MaxLines="3"
                                                   Text="{x:Bind local:ForgottenHallPage.FormatStageName(Name)}"
                                                   TextTrimming="CharacterEllipsis"
                                                   TextWrapping="WrapWholeWords" />
                                        <TextBlock FontSize="12" Foreground="{ThemeResource TextFillColorSecondaryBrush}">
                                            <Run Text="{x:Bind lang:Lang.ForgottenHallPage_CyclesUsed}" />
                                            <Run Text="{x:Bind RoundNum}" />
                                        </TextBlock>
                                    </StackPanel>
'@
$forgotten = Replace-Once $forgotten $forgottenTitlePattern $forgottenTitleReplacement 'Forgotten Hall stage title'
Write-TextFile $forgottenPath $forgotten
Add-StageNameFormatter `
    'src/Starward/Features/GameRecord/StarRail/ForgottenHallPage.xaml.cs' `
    '(public static bool FloorHasExtraStar\(int starNum\) => starNum > 3;)' `
    'Forgotten Hall'

# -----------------------------------------------------------------------------
# Anomaly Arbitration: compact transparent buff area and wrapped boss title
# -----------------------------------------------------------------------------
$challengePath = 'src/Starward/Features/GameRecord/StarRail/ChallengePeakPage.xaml'
$challenge = Read-TextFile $challengePath
$challenge = Replace-Once $challenge '<Grid MinHeight="220"' '<Grid MinHeight="192"' 'Anomaly Arbitration compact card height'
$challenge = Replace-Once $challenge '<ColumnDefinition Width="240" />' '<ColumnDefinition Width="220" />' 'Anomaly Arbitration buff column width'

$bossTitlePattern = '<TextBlock FontSize="16"\s+FontWeight="Bold"\s+Text="\{x:Bind CurrentChallengePeakRecord\.BossInfo\.NameMi18n\}" />'
$bossTitleReplacement = @'
<TextBlock MinWidth="0"
                                   Margin="0,0,16,0"
                                   FontSize="16"
                                   FontWeight="Bold"
                                   MaxLines="2"
                                   Text="{x:Bind CurrentChallengePeakRecord.BossInfo.NameMi18n}"
                                   TextTrimming="CharacterEllipsis"
                                   TextWrapping="WrapWholeWords" />
'@
$challenge = Replace-Once $challenge $bossTitlePattern $bossTitleReplacement 'Anomaly Arbitration boss title wrapping'

$buffPattern = '<Border Grid.RowSpan="2"\s+Grid.Column="1"\s+Margin="8,10,12,10"\s+Padding="12,10"\s+VerticalAlignment="Stretch"\s+Background="\{ThemeResource ControlOnImageFillColorDefaultBrush\}"\s+CornerRadius="8"\s+Visibility="\{x:Bind CurrentChallengePeakRecord\.BossRecord, Converter=\{StaticResource ObjectToVisibilityConverter\}\}">.*?</Border>'
$buffReplacement = @'
<Border Grid.RowSpan="2"
                            Grid.Column="1"
                            Margin="8,8,12,8"
                            Padding="10,8"
                            VerticalAlignment="Center"
                            Background="Transparent"
                            Visibility="{x:Bind CurrentChallengePeakRecord.BossRecord, Converter={StaticResource ObjectToVisibilityConverter}}">
                        <StackPanel VerticalAlignment="Center" Spacing="6">
                            <!--  Buff  -->
                            <StackPanel Orientation="Horizontal" Spacing="8">
                                <sc:CachedImage Width="30"
                                                Height="30"
                                                VerticalAlignment="Center"
                                                Source="{x:Bind CurrentChallengePeakRecord.BossRecord.Buff.Icon}" />
                                <TextBlock MinWidth="0"
                                           VerticalAlignment="Center"
                                           FontWeight="SemiBold"
                                           MaxLines="2"
                                           Text="{x:Bind CurrentChallengePeakRecord.BossRecord.Buff.NameMi18n}"
                                           TextTrimming="CharacterEllipsis"
                                           TextWrapping="WrapWholeWords" />
                            </StackPanel>
                            <TextBlock Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                                       MaxLines="5"
                                       Text="{x:Bind CurrentChallengePeakRecord.BossRecord.Buff.DescMi18n}"
                                       TextTrimming="CharacterEllipsis"
                                       TextWrapping="WrapWholeWords" />
                        </StackPanel>
                    </Border>
'@
$challenge = Replace-Once $challenge $buffPattern $buffReplacement 'Anomaly Arbitration transparent buff panel'
Write-TextFile $challengePath $challenge

# -----------------------------------------------------------------------------
# Stygian Onslaught: aligned labels and localized seconds
# -----------------------------------------------------------------------------
$stygianPath = 'src/Starward/Features/GameRecord/Genshin/StygianOnslaughtPage.xaml'
$stygian = Read-TextFile $stygianPath

$listSecondsPattern = '<TextBlock Grid.Row="1"\s+Margin="36,0,0,2"\s+VerticalAlignment="Center"\s+Foreground="\{ThemeResource TextFillColorSecondaryBrush\}">\s*<Run Text="\{x:Bind Second\}" /><Run Text="s" />\s*</TextBlock>'
$listSecondsReplacement = @'
<TextBlock Grid.Row="1"
                                   Margin="36,0,0,2"
                                   VerticalAlignment="Center"
                                   Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                                   Text="{x:Bind local:StygianOnslaughtPage.FormatSeconds(Second)}" />
'@
$stygian = Replace-Once $stygian $listSecondsPattern $listSecondsReplacement 'Stygian list seconds'

$bestSecondsPattern = '<TextBlock Margin="4,0,0,0" FontSize="16">\s*<Run Text="\{x:Bind CurrentSelectedBattle\.Best\.Seconds\}" /><Run Text="s" />\s*</TextBlock>'
$bestSecondsReplacement = @'
<TextBlock Margin="4,0,0,0"
                                   VerticalAlignment="Center"
                                   FontSize="16"
                                   Text="{x:Bind local:StygianOnslaughtPage.FormatSeconds(CurrentSelectedBattle.Best.Seconds)}" />
'@
$stygian = Replace-Once $stygian $bestSecondsPattern $bestSecondsReplacement 'Stygian best seconds'

$battleTimePattern = '<StackPanel Grid.Row="1"\s+HorizontalAlignment="Left"\s+VerticalAlignment="Center"\s+Orientation="Horizontal"\s+Spacing="12">\s*<TextBlock Foreground="\{ThemeResource TextFillColorSecondaryBrush\}" Text="\{x:Bind lang:Lang\.StygianOnslaughtPage_BattleTime\}" />\s*<TextBlock Grid.Row="1" Foreground="\{ThemeResource TextFillColorSecondaryBrush\}">\s*<Run Text="\{x:Bind Second\}" /><Run Text="s" />\s*</TextBlock>\s*</StackPanel>'
$battleTimeReplacement = @'
<Grid Grid.Row="1"
                                      HorizontalAlignment="Left"
                                      VerticalAlignment="Center"
                                      ColumnSpacing="12">
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="Auto" />
                                        <ColumnDefinition Width="Auto" />
                                    </Grid.ColumnDefinitions>
                                    <TextBlock VerticalAlignment="Center"
                                               Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                                               Text="{x:Bind lang:Lang.StygianOnslaughtPage_BattleTime}" />
                                    <TextBlock Grid.Column="1"
                                               VerticalAlignment="Center"
                                               Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                                               Text="{x:Bind local:StygianOnslaughtPage.FormatSeconds(Second)}" />
                                </Grid>
'@
$stygian = Replace-Once $stygian $battleTimePattern $battleTimeReplacement 'Stygian battle time row'

$bestStatsPattern = '<ItemsControl Grid.Row="3"\s+Grid.ColumnSpan="2"\s+ItemsSource="\{x:Bind BestAvatar\}">.*?</ItemsControl>'
$bestStatsReplacement = @'
<ItemsControl Grid.Row="3"
                                              Grid.ColumnSpan="2"
                                              HorizontalAlignment="Stretch"
                                              ItemsSource="{x:Bind BestAvatar}">
                                    <ItemsControl.ItemsPanel>
                                        <ItemsPanelTemplate>
                                            <StackPanel Orientation="Horizontal" Spacing="16" />
                                        </ItemsPanelTemplate>
                                    </ItemsControl.ItemsPanel>
                                    <ItemsControl.ItemTemplate>
                                        <DataTemplate x:DataType="so:StygianOnslaughtBestAvatar">
                                            <Grid Width="318" Height="40">
                                                <Grid.ColumnDefinitions>
                                                    <ColumnDefinition Width="40" />
                                                    <ColumnDefinition />
                                                    <ColumnDefinition Width="Auto" />
                                                </Grid.ColumnDefinitions>
                                                <sc:CachedImage Width="40"
                                                                Height="40"
                                                                VerticalAlignment="Center"
                                                                Source="{x:Bind SideIcon}" />
                                                <TextBlock Grid.Column="1"
                                                           Margin="8,0"
                                                           VerticalAlignment="Center"
                                                           Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                                                           Text="{x:Bind local:StygianOnslaughtPage.BestTypeToString(Type)}"
                                                           TextTrimming="CharacterEllipsis" />
                                                <TextBlock Grid.Column="2"
                                                           VerticalAlignment="Center"
                                                           Text="{x:Bind DPS}" />
                                            </Grid>
                                        </DataTemplate>
                                    </ItemsControl.ItemTemplate>
                                </ItemsControl>
'@
$stygian = Replace-Once $stygian $bestStatsPattern $bestStatsReplacement 'Stygian aligned best stats'
Write-TextFile $stygianPath $stygian

$stygianCodePath = 'src/Starward/Features/GameRecord/Genshin/StygianOnslaughtPage.xaml.cs'
$stygianCode = Read-TextFile $stygianCodePath
if ($stygianCode -notmatch 'FormatSeconds\(') {
    $secondsMethod = @'


    public static string FormatSeconds(int seconds)
    {
        return string.Format(Lang.StygianOnslaughtPage_SecondsFormat, seconds);
    }
'@
    $stygianCode = Replace-Once `
        $stygianCode `
        '(public static string BestTypeToString\(int type\))' `
        ($secondsMethod + "`r`n`r`n    " + '$1') `
        'Stygian localized seconds formatter'
}
Write-TextFile $stygianCodePath $stygianCode

# Add the localized format to the default and Russian resource files.
$defaultLangPath = 'src/Starward.Language/Lang.resx'
$defaultLang = Read-TextFile $defaultLangPath
if ($defaultLang -notmatch 'StygianOnslaughtPage_SecondsFormat') {
    $defaultResource = @'
  <data name="StygianOnslaughtPage_SecondsFormat" xml:space="preserve">
    <value>{0}s</value>
  </data>
'@
    $defaultLang = Replace-Once `
        $defaultLang `
        '(<data name="StygianOnslaughtPage_BattleTime" xml:space="preserve">\s*<value>Battle Time</value>\s*</data>\s*)' `
        ('$1' + $defaultResource) `
        'default seconds localization resource'
}
Write-TextFile $defaultLangPath $defaultLang

$russianLangPath = 'src/Starward.Language/Lang.ru-RU.resx'
$russianLang = Read-TextFile $russianLangPath
if ($russianLang -notmatch 'StygianOnslaughtPage_SecondsFormat') {
    $russianResource = @'
  <data name="StygianOnslaughtPage_SecondsFormat" xml:space="preserve">
    <value>{0} с</value>
  </data>
'@
    $russianLang = Replace-Once `
        $russianLang `
        '(<data name="StygianOnslaughtPage_BattleTime" xml:space="preserve">\s*<value>Время боя</value>\s*</data>\s*)' `
        ('$1' + $russianResource) `
        'Russian seconds localization resource'
}
Write-TextFile $russianLangPath $russianLang

Write-Host 'Final record UI and Stygian localization fixes applied.'
