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

function Upsert-ResxValue(
    [string]$Content,
    [string]$Name,
    [string]$Value
) {
    $escapedName = [regex]::Escape($Name)
    $pattern = '<data name="' + $escapedName + '" xml:space="preserve">\s*<value>.*?</value>\s*</data>'
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

function Remove-ResxValue(
    [string]$Content,
    [string]$Name
) {
    $escapedName = [regex]::Escape($Name)
    $pattern = '\s*<data name="' + $escapedName + '" xml:space="preserve">\s*<value>.*?</value>\s*</data>'
    return [regex]::Replace(
        $Content,
        $pattern,
        '',
        [System.Text.RegularExpressions.RegexOptions]::Singleline)
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

# -----------------------------------------------------------------------------
# Shared localized duration resources for every existing language file.
# -----------------------------------------------------------------------------
$languageFiles = Get-ChildItem 'src/Starward.Language' -Filter 'Lang*.resx' -File
if ($languageFiles.Count -eq 0) {
    throw 'No Starward language resource files were found.'
}

foreach ($file in $languageFiles) {
    $culture = if ($file.BaseName -eq 'Lang') { '' } else { $file.BaseName.Substring(5) }
    $content = Read-TextFile $file.FullName

    # Remove the temporary page-specific seconds key from the previous build.
    $content = Remove-ResxValue $content 'StygianOnslaughtPage_SecondsFormat'
    $content = Upsert-ResxValue $content 'Common_SecondsFormat' (Get-SecondsFormat $culture)
    $content = Upsert-ResxValue $content 'ImaginariumTheaterPage_TotalPerformanceDurationFormat' (Get-MinutesSecondsFormat $culture)

    Write-TextFile $file.FullName $content
    Write-Host "Duration resources updated: $($file.Name)"
}

# -----------------------------------------------------------------------------
# Stygian Onslaught: use the shared seconds resource and keep a visible space.
# -----------------------------------------------------------------------------
$stygianCodePath = 'src/Starward/Features/GameRecord/Genshin/StygianOnslaughtPage.xaml.cs'
$stygianCode = Read-TextFile $stygianCodePath
$stygianCode = Replace-Once `
    $stygianCode `
    '"StygianOnslaughtPage_SecondsFormat",\s*Lang\.Culture\) \?\? "\{0\}s"' `
    '"Common_SecondsFormat",`r`n            Lang.Culture) ?? "{0} s"' `
    'Stygian shared seconds resource'
Write-TextFile $stygianCodePath $stygianCode

# -----------------------------------------------------------------------------
# Imaginarium Theater: localized performance duration.
# -----------------------------------------------------------------------------
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

# Keep damage values, icons, and localized labels on one row.
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
    $escapedBinding = [regex]::Escape($binding)
    $labelPattern = '<TextBlock Grid.Row="([123])"\s+VerticalAlignment="Center"\s+Text="\{x:Bind ' + $escapedBinding + '\}" />'
    $labelReplacement = '<TextBlock Grid.Row="$1"`r`n                                MinWidth="0"`r`n                                Margin="0,0,16,0"`r`n                                VerticalAlignment="Center"`r`n                                MaxLines="1"`r`n                                Text="{x:Bind ' + $binding + '}"`r`n                                TextTrimming="CharacterEllipsis"`r`n                                TextWrapping="NoWrap" />'
    $theaterXaml = Replace-Once $theaterXaml $labelPattern $labelReplacement "Imaginarium one-line label $binding"
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
    $valueReplacement = '<Grid Grid.Row="' + $row + '"`r`n                          Grid.Column="1"`r`n                          Margin="8,0,0,0"`r`n                          VerticalAlignment="Center"`r`n                          ColumnSpacing="8">`r`n                        <Grid.ColumnDefinitions>`r`n                            <ColumnDefinition Width="36" />`r`n                            <ColumnDefinition Width="Auto" />`r`n                        </Grid.ColumnDefinitions>`r`n                        <sc:CachedImage Width="36"`r`n                                        Height="36"`r`n                                        VerticalAlignment="Center"`r`n                                        Source="{x:Bind CurrentTheater.Detail.FightStatisic.' + $avatar + '.AvatarIcon}" />`r`n                        <TextBlock Grid.Column="1"`r`n                                   VerticalAlignment="Center"`r`n                                   Text="{x:Bind CurrentTheater.Detail.FightStatisic.' + $avatar + '.Value}"`r`n                                   TextWrapping="NoWrap" />`r`n                    </Grid>'
    $theaterXaml = Replace-Once $theaterXaml $valuePattern $valueReplacement "Imaginarium one-line value $avatar"
}
Write-TextFile $theaterXamlPath $theaterXaml

# -----------------------------------------------------------------------------
# Settings: restore the original pane width and wrap only the long custom item.
# -----------------------------------------------------------------------------
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
$settingCode = Replace-Once `
    $settingCode `
    'public string HoyolabToolboxAutoRefreshTitle => Localized\(\s*"Автообновление HoYoLAB",\s*"HoYoLAB Auto Refresh"\);' `
    'public string HoyolabToolboxAutoRefreshTitle => Localized(`r`n        "Автообновление HoYoLAB Toolbox",`r`n        "HoYoLAB Toolbox Auto Refresh");' `
    'HoYoLAB Toolbox Auto Refresh navigation title'
Write-TextFile $settingCodePath $settingCode

# -----------------------------------------------------------------------------
# Anomaly Arbitration: restore the original fixed card height.
# -----------------------------------------------------------------------------
$challengePath = 'src/Starward/Features/GameRecord/StarRail/ChallengePeakPage.xaml'
$challenge = Read-TextFile $challengePath
$challenge = Replace-Once $challenge '<Grid MinHeight="192"' '<Grid Height="192"' 'Anomaly Arbitration original card height'
$challenge = Replace-Once $challenge 'MaxLines="5"\s+Text="\{x:Bind CurrentChallengePeakRecord\.BossRecord\.Buff\.DescMi18n\}"' 'MaxLines="4"`r`n                                        Text="{x:Bind CurrentChallengePeakRecord.BossRecord.Buff.DescMi18n}"' 'Anomaly Arbitration compact buff description'
Write-TextFile $challengePath $challenge

Write-Host 'Duration localization, Imaginarium layout, settings width, and Anomaly height fixes applied.'
