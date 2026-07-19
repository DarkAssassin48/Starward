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

function Upsert-ResxValue([string]$Content, [string]$Name, [string]$Value) {
    $pattern = '<data name="' + [regex]::Escape($Name) + '" xml:space="preserve">\s*<value>.*?</value>\s*</data>'
    $replacement = "  <data name=\"$Name\" xml:space=\"preserve\">`r`n    <value>$Value</value>`r`n  </data>"
    $regex = [regex]::new($pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    $matches = $regex.Matches($Content)
    if ($matches.Count -gt 1) {
        throw "Resource ${Name}: expected at most one entry, found $($matches.Count)."
    }
    if ($matches.Count -eq 1) {
        return $regex.Replace($Content, $replacement, 1)
    }
    return Replace-Once $Content '</root>' ($replacement + "`r`n</root>") "insert resource $Name"
}

function Remove-ResxValue([string]$Content, [string]$Name) {
    $pattern = '\s*<data name="' + [regex]::Escape($Name) + '" xml:space="preserve">\s*<value>.*?</value>\s*</data>'
    return [regex]::Replace($Content, $pattern, '', [System.Text.RegularExpressions.RegexOptions]::Singleline)
}

function Get-SecondsFormat([string]$Culture) {
    switch -Regex ($Culture) {
        '^ru' { return '{0} с' }
        '^uk' { return '{0} с' }
        '^be' { return '{0} с' }
        '^zh' { return '{0} 秒' }
        '^ja' { return '{0} 秒' }
        '^ko' { return '{0} 초' }
        '^th' { return '{0} วิ' }
        '^vi' { return '{0} giây' }
        '^id' { return '{0} dtk' }
        '^tr' { return '{0} sn' }
        default { return '{0} s' }
    }
}

function Get-MinutesSecondsFormat([string]$Culture) {
    switch -Regex ($Culture) {
        '^ru' { return '{0} мин {1} с' }
        '^uk' { return '{0} хв {1} с' }
        '^be' { return '{0} хв {1} с' }
        '^zh' { return '{0} 分 {1} 秒' }
        '^ja' { return '{0} 分 {1} 秒' }
        '^ko' { return '{0} 분 {1} 초' }
        '^de' { return '{0} Min. {1} Sek.' }
        '^es' { return '{0} min {1} s' }
        '^it' { return '{0} min {1} s' }
        '^fr' { return '{0} min {1} s' }
        '^pt' { return '{0} min {1} s' }
        '^th' { return '{0} นาที {1} วินาที' }
        '^vi' { return '{0} phút {1} giây' }
        '^id' { return '{0} mnt {1} dtk' }
        '^tr' { return '{0} dk {1} sn' }
        default { return '{0} m {1} s' }
    }
}

# Shared duration resources for all current language files.
$languageFiles = @(Get-ChildItem 'src/Starward.Language' -Filter 'Lang*.resx' -File)
if ($languageFiles.Count -eq 0) {
    throw 'No Starward language resource files were found.'
}
foreach ($file in $languageFiles) {
    $culture = if ($file.BaseName -eq 'Lang') { '' } else { $file.BaseName.Substring(5) }
    $content = Read-TextFile $file.FullName
    $content = Remove-ResxValue $content 'StygianOnslaughtPage_SecondsFormat'
    $content = Upsert-ResxValue $content 'Common_SecondsFormat' (Get-SecondsFormat $culture)
    $content = Upsert-ResxValue $content 'ImaginariumTheaterPage_TotalPerformanceDurationFormat' (Get-MinutesSecondsFormat $culture)
    Write-TextFile $file.FullName $content
    Write-Host "Duration resources updated: $($file.Name)"
}

# Stygian Onslaught uses the shared seconds resource.
$stygianCodePath = 'src/Starward/Features/GameRecord/Genshin/StygianOnslaughtPage.xaml.cs'
$stygianCode = Read-TextFile $stygianCodePath
$stygianReplacement = @'
"Common_SecondsFormat",
            Lang.Culture) ?? "{0} s"
'@
$stygianCode = Replace-Once `
    $stygianCode `
    '"StygianOnslaughtPage_SecondsFormat",\s*Lang\.Culture\) \?\? "\{0\}s"' `
    $stygianReplacement `
    'Stygian shared seconds resource'
Write-TextFile $stygianCodePath $stygianCode

# Imaginarium Theater uses a localized minutes-and-seconds format.
$theaterCodePath = 'src/Starward/Features/GameRecord/Genshin/ImaginariumTheaterPage.xaml.cs'
$theaterCode = Read-TextFile $theaterCodePath
$performanceMethodPattern = 'public static string PerformancesTime\(int second\)\s*\{\s*var ts = TimeSpan\.FromSeconds\(second\);\s*return \$"\{ts\.Minutes\}m \{ts\.Seconds\}s";\s*\}'
$performanceMethodReplacement = @'
public static string PerformancesTime(int second)
    {
        var ts = TimeSpan.FromSeconds(second);
        var format = Lang.ResourceManager.GetString(
            "ImaginariumTheaterPage_TotalPerformanceDurationFormat",
            Lang.Culture) ?? "{0} m {1} s";
        return string.Format(format, (int)ts.TotalMinutes, ts.Seconds);
    }
'@
$theaterCode = Replace-Once $theaterCode $performanceMethodPattern $performanceMethodReplacement 'Imaginarium localized performance duration'
Write-TextFile $theaterCodePath $theaterCode

# Keep damage labels, icons and values on one line in every language.
$theaterXamlPath = 'src/Starward/Features/GameRecord/Genshin/ImaginariumTheaterPage.xaml'
$theaterXaml = Read-TextFile $theaterXamlPath
$fightGridPattern = '(<Grid Margin="0,8,0,0"\s+Padding="24,0,24,0"\s+Background="\{ThemeResource CustomOverlayAcrylicBrush\}"\s+CornerRadius="8"\s+Shadow="\{ThemeResource ThemeShadow\}"\s+Translation="0,0,16"\s+Visibility="\{x:Bind local:ImaginariumTheaterPage\.FightStatisicVisibility\(CurrentTheater\.Detail\.FightStatisic\.TotalUseTime\)\}">\s*)<Grid\.RowDefinitions>\s*<RowDefinition MinHeight="32" />\s*<RowDefinition MinHeight="32" />\s*<RowDefinition MinHeight="32" />\s*<RowDefinition MinHeight="32" />\s*<RowDefinition MinHeight="32" />\s*</Grid\.RowDefinitions>\s*<Grid\.ColumnDefinitions>\s*<ColumnDefinition Width="4\*" />\s*<ColumnDefinition Width="3\*" />\s*</Grid\.ColumnDefinitions>'
$fightGridReplacement = @'
$1<Grid.RowDefinitions>
                        <RowDefinition MinHeight="32" />
                        <RowDefinition MinHeight="40" />
                        <RowDefinition MinHeight="40" />
                        <RowDefinition MinHeight="40" />
                        <RowDefinition MinHeight="40" />
                    </Grid.RowDefinitions>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition />
                        <ColumnDefinition Width="Auto" />
                    </Grid.ColumnDefinitions>
'@
$theaterXaml = Replace-Once $theaterXaml $fightGridPattern $fightGridReplacement 'Imaginarium fight statistic grid'

$labelBindings = @(
    'lang:Lang.ImaginariumTheaterPage_HighestDamageDealt',
    'lang:Lang.ImaginariumTheaterPage_MostOpponentsDefeated',
    'lang:Lang.SpiralAbyssPage_MostDamageTaken'
)
foreach ($binding in $labelBindings) {
    $labelPattern = '<TextBlock Grid.Row="([123])"\s+VerticalAlignment="Center"\s+Text="\{x:Bind ' + [regex]::Escape($binding) + '\}" />'
    $labelTemplate = @'
<TextBlock Grid.Row="__ROW__"
                                MinWidth="0"
                                Margin="0,0,16,0"
                                VerticalAlignment="Center"
                                MaxLines="1"
                                Text="{x:Bind __BINDING__}"
                                TextTrimming="CharacterEllipsis"
                                TextWrapping="NoWrap" />
'@
    $match = [regex]::Match($theaterXaml, $labelPattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if (-not $match.Success) {
        throw "Imaginarium one-line label $binding was not found."
    }
    $replacement = $labelTemplate.Replace('__ROW__', $match.Groups[1].Value).Replace('__BINDING__', $binding)
    $theaterXaml = Replace-Once $theaterXaml $labelPattern $replacement "Imaginarium one-line label $binding"
}

$valueRows = @(
    @{ Row = '1'; Avatar = 'MaxDamageAvatar' },
    @{ Row = '2'; Avatar = 'MaxDefeatAvatar' },
    @{ Row = '3'; Avatar = 'MaxTakeDamageAvatar' }
)
foreach ($item in $valueRows) {
    $row = $item.Row
    $avatar = $item.Avatar
    $valuePattern = '<StackPanel Grid.Row="' + $row + '"\s+Grid.Column="1"\s+Orientation="Horizontal">\s*<sc:CachedImage Width="44"\s+Height="36"\s+Margin="-12,-4,0,4"\s+Source="\{x:Bind CurrentTheater\.Detail\.FightStatisic\.' + $avatar + '\.AvatarIcon\}" />\s*<TextBlock VerticalAlignment="Center" Text="\{x:Bind CurrentTheater\.Detail\.FightStatisic\.' + $avatar + '\.Value\}" />\s*</StackPanel>'
    $valueTemplate = @'
<Grid Grid.Row="__ROW__"
                          Grid.Column="1"
                          Margin="8,0,0,0"
                          VerticalAlignment="Center"
                          ColumnSpacing="8">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="36" />
                            <ColumnDefinition Width="Auto" />
                        </Grid.ColumnDefinitions>
                        <sc:CachedImage Width="36"
                                        Height="36"
                                        VerticalAlignment="Center"
                                        Source="{x:Bind CurrentTheater.Detail.FightStatisic.__AVATAR__.AvatarIcon}" />
                        <TextBlock Grid.Column="1"
                                   VerticalAlignment="Center"
                                   Text="{x:Bind CurrentTheater.Detail.FightStatisic.__AVATAR__.Value}"
                                   TextWrapping="NoWrap" />
                    </Grid>
'@
    $replacement = $valueTemplate.Replace('__ROW__', $row).Replace('__AVATAR__', $avatar)
    $theaterXaml = Replace-Once $theaterXaml $valuePattern $replacement "Imaginarium one-line value $avatar"
}
Write-TextFile $theaterXamlPath $theaterXaml

# Restore the original settings pane width and wrap only the long custom item.
$settingXamlPath = 'src/Starward/Features/Setting/SettingPage.xaml'
$settingXaml = Read-TextFile $settingXamlPath
$settingXaml = Replace-Once $settingXaml 'OpenPaneLength="\d+"' 'OpenPaneLength="260"' 'original settings pane width'
$navPattern = '<NavigationViewItem Content="\{x:Bind HoyolabToolboxAutoRefreshTitle\}"\s+Tag="HoyolabToolboxAutoRefreshSetting"\s+ToolTipService\.ToolTip="\{x:Bind HoyolabToolboxAutoRefreshTitle\}">\s*<NavigationViewItem\.Icon>\s*<FontIcon Glyph="&#xE895;" />\s*</NavigationViewItem\.Icon>\s*</NavigationViewItem>'
$navReplacement = @'
<NavigationViewItem MinHeight="40"
                                    Tag="HoyolabToolboxAutoRefreshSetting"
                                    ToolTipService.ToolTip="{x:Bind HoyolabToolboxAutoRefreshTitle}">
                    <NavigationViewItem.Content>
                        <TextBlock MaxWidth="174"
                                   MaxLines="2"
                                   Text="{x:Bind HoyolabToolboxAutoRefreshTitle}"
                                   TextTrimming="CharacterEllipsis"
                                   TextWrapping="WrapWholeWords" />
                    </NavigationViewItem.Content>
                    <NavigationViewItem.Icon>
                        <FontIcon Glyph="&#xE895;" />
                    </NavigationViewItem.Icon>
                </NavigationViewItem>
'@
$settingXaml = Replace-Once $settingXaml $navPattern $navReplacement 'wrapped HoYoLAB Toolbox Auto Refresh item'
Write-TextFile $settingXamlPath $settingXaml

$settingCodePath = 'src/Starward/Features/Setting/SettingPage.xaml.cs'
$settingCode = Read-TextFile $settingCodePath
$settingTitleReplacement = @'
public string HoyolabToolboxAutoRefreshTitle => Localized(
        "Автообновление HoYoLAB Toolbox",
        "HoYoLAB Toolbox Auto Refresh");
'@
$settingCode = Replace-Once `
    $settingCode `
    'public string HoyolabToolboxAutoRefreshTitle => Localized\(\s*"Автообновление HoYoLAB",\s*"HoYoLAB Auto Refresh"\);' `
    $settingTitleReplacement `
    'HoYoLAB Toolbox Auto Refresh navigation title'
Write-TextFile $settingCodePath $settingCode

# Restore the original fixed Anomaly Arbitration card height.
$challengePath = 'src/Starward/Features/GameRecord/StarRail/ChallengePeakPage.xaml'
$challenge = Read-TextFile $challengePath
$challenge = Replace-Once $challenge '<Grid MinHeight="192"' '<Grid Height="192"' 'Anomaly Arbitration original card height'
$challengeDescriptionReplacement = @'
MaxLines="4"
                                        Text="{x:Bind CurrentChallengePeakRecord.BossRecord.Buff.DescMi18n}"
'@
$challenge = Replace-Once `
    $challenge `
    'MaxLines="5"\s+Text="\{x:Bind CurrentChallengePeakRecord\.BossRecord\.Buff\.DescMi18n\}"' `
    $challengeDescriptionReplacement `
    'Anomaly Arbitration compact buff description'
Write-TextFile $challengePath $challenge

Write-Host 'Duration localization, Imaginarium layout, settings width, and Anomaly height fixes applied.'
