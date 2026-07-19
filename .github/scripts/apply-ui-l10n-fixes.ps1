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
# Apocalyptic Shadow
# -----------------------------------------------------------------------------
$xamlPath = 'src/Starward/Features/GameRecord/StarRail/ApocalypticShadowPage.xaml'
$xaml = Read-TextFile $xamlPath

# Let long stage names use the available title area without drawing under stars.
$xaml = Replace-Once $xaml '<Grid Height="360" Padding="20,0,20,0">' '<Grid MinHeight="360" Padding="20,0,20,0">' 'Apocalyptic Shadow floor card height'
$xaml = Replace-Once $xaml '<RowDefinition Height="56" />' '<RowDefinition Height="Auto" MinHeight="56" />' 'Apocalyptic Shadow title row'

$titlePattern = '<StackPanel\s+VerticalAlignment="Center"\s+Spacing="2">\s*<TextBlock\s+FontSize="18"\s+FontWeight="Bold"\s+Text="\{x:Bind Name\}"\s+TextTrimming="CharacterEllipsis"\s*/>\s*</StackPanel>'
$titleReplacement = @'
<TextBlock Grid.Column="0"
                                               MinWidth="0"
                                               Margin="0,8,12,8"
                                               VerticalAlignment="Center"
                                               FontSize="18"
                                               FontWeight="Bold"
                                               Text="{x:Bind Name}"
                                               TextWrapping="WrapWholeWords"
                                               ToolTipService.ToolTip="{x:Bind Name}" />
'@
$xaml = Replace-Once $xaml $titlePattern $titleReplacement 'Apocalyptic Shadow floor title'

# Keep the original two-team layout. Use a dedicated adaptive layout only when
# the third team exists, so all three boss icons and names remain readable.
$bossPanelPattern = '<Grid x:Name="BossPanelGrid" Grid.Row="3" Grid.ColumnSpan="3">.*?</Grid>'
$bossPanelReplacement = @'
<Grid x:Name="BossPanelGrid" Grid.Row="3" Grid.ColumnSpan="3">
                        <!-- Original layout for records with two teams. -->
                        <Grid Visibility="{x:Bind CurrentApocalypticShadow.Meta.TierceBoss, Converter={StaticResource ObjectToVisibilityReversedConverter}}">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition />
                                <ColumnDefinition />
                                <ColumnDefinition />
                            </Grid.ColumnDefinitions>
                            <StackPanel Grid.Column="0"
                                        HorizontalAlignment="Center"
                                        Orientation="Horizontal"
                                        Spacing="12">
                                <sc:CachedImage Width="40"
                                                Height="40"
                                                VerticalAlignment="Center"
                                                CornerRadius="20"
                                                Source="{x:Bind CurrentApocalypticShadow.Meta.UpperBoss.Icon}" />
                                <TextBlock VerticalAlignment="Center" FontSize="12">
                                    <Run Text="{x:Bind lang:Lang.ForgottenHallPage_TeamSetup}" />
                                    <Run Text="1" />
                                    <LineBreak />
                                    <Run Foreground="{ThemeResource TextFillColorSecondaryBrush}" Text="{x:Bind CurrentApocalypticShadow.Meta.UpperBoss.Name}" />
                                </TextBlock>
                            </StackPanel>
                            <StackPanel Grid.Column="1"
                                        HorizontalAlignment="Center"
                                        Orientation="Horizontal"
                                        Spacing="12">
                                <sc:CachedImage Width="40"
                                                Height="40"
                                                VerticalAlignment="Center"
                                                CornerRadius="20"
                                                Source="{x:Bind CurrentApocalypticShadow.Meta.LowerBoss.Icon}" />
                                <TextBlock VerticalAlignment="Center" FontSize="12">
                                    <Run Text="{x:Bind lang:Lang.ForgottenHallPage_TeamSetup}" />
                                    <Run Text="2" />
                                    <LineBreak />
                                    <Run Foreground="{ThemeResource TextFillColorSecondaryBrush}" Text="{x:Bind CurrentApocalypticShadow.Meta.LowerBoss.Name}" />
                                </TextBlock>
                            </StackPanel>
                        </Grid>

                        <!-- Wider wrapping layout for records with three teams. -->
                        <Grid x:Name="ThreeBossPanel"
                              ColumnSpacing="12"
                              x:Load="{x:Bind CurrentApocalypticShadow.Meta.TierceBoss, Converter={StaticResource ObjectToBoolConverter}}">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition />
                                <ColumnDefinition />
                                <ColumnDefinition />
                            </Grid.ColumnDefinitions>

                            <Grid Grid.Column="0" Margin="6,0">
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="Auto" />
                                    <ColumnDefinition />
                                </Grid.ColumnDefinitions>
                                <sc:CachedImage Width="40"
                                                Height="40"
                                                VerticalAlignment="Center"
                                                CornerRadius="20"
                                                Source="{x:Bind CurrentApocalypticShadow.Meta.UpperBoss.Icon}" />
                                <StackPanel Grid.Column="1" Margin="10,0,0,0" VerticalAlignment="Center" Spacing="2">
                                    <TextBlock FontSize="12">
                                        <Run Text="{x:Bind lang:Lang.ForgottenHallPage_TeamSetup}" />
                                        <Run Text="1" />
                                    </TextBlock>
                                    <TextBlock FontSize="11"
                                               Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                                               Text="{x:Bind CurrentApocalypticShadow.Meta.UpperBoss.Name}"
                                               TextWrapping="WrapWholeWords"
                                               ToolTipService.ToolTip="{x:Bind CurrentApocalypticShadow.Meta.UpperBoss.Name}" />
                                </StackPanel>
                            </Grid>

                            <Grid Grid.Column="1" Margin="6,0">
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="Auto" />
                                    <ColumnDefinition />
                                </Grid.ColumnDefinitions>
                                <sc:CachedImage Width="40"
                                                Height="40"
                                                VerticalAlignment="Center"
                                                CornerRadius="20"
                                                Source="{x:Bind CurrentApocalypticShadow.Meta.LowerBoss.Icon}" />
                                <StackPanel Grid.Column="1" Margin="10,0,0,0" VerticalAlignment="Center" Spacing="2">
                                    <TextBlock FontSize="12">
                                        <Run Text="{x:Bind lang:Lang.ForgottenHallPage_TeamSetup}" />
                                        <Run Text="2" />
                                    </TextBlock>
                                    <TextBlock FontSize="11"
                                               Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                                               Text="{x:Bind CurrentApocalypticShadow.Meta.LowerBoss.Name}"
                                               TextWrapping="WrapWholeWords"
                                               ToolTipService.ToolTip="{x:Bind CurrentApocalypticShadow.Meta.LowerBoss.Name}" />
                                </StackPanel>
                            </Grid>

                            <Grid Grid.Column="2" Margin="6,0">
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="Auto" />
                                    <ColumnDefinition />
                                </Grid.ColumnDefinitions>
                                <sc:CachedImage Width="40"
                                                Height="40"
                                                VerticalAlignment="Center"
                                                CornerRadius="20"
                                                Source="{x:Bind CurrentApocalypticShadow.Meta.TierceBoss.Icon}" />
                                <StackPanel Grid.Column="1" Margin="10,0,0,0" VerticalAlignment="Center" Spacing="2">
                                    <TextBlock FontSize="12">
                                        <Run Text="{x:Bind lang:Lang.ForgottenHallPage_TeamSetup}" />
                                        <Run Text="3" />
                                    </TextBlock>
                                    <TextBlock FontSize="11"
                                               Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                                               Text="{x:Bind CurrentApocalypticShadow.Meta.TierceBoss.Name}"
                                               TextWrapping="WrapWholeWords"
                                               ToolTipService.ToolTip="{x:Bind CurrentApocalypticShadow.Meta.TierceBoss.Name}" />
                                </StackPanel>
                            </Grid>
                        </Grid>
                    </Grid>
'@
$xaml = Replace-Once $xaml $bossPanelPattern $bossPanelReplacement 'Apocalyptic Shadow boss summary panel'
Write-TextFile $xamlPath $xaml

# -----------------------------------------------------------------------------
# Settings navigation and auto-refresh page layout
# -----------------------------------------------------------------------------
$settingPageXamlPath = 'src/Starward/Features/Setting/SettingPage.xaml'
$settingPageXaml = Read-TextFile $settingPageXamlPath
$navPattern = '<NavigationViewItem MinHeight="52"\s+Tag="HoyolabToolboxAutoRefreshSetting"\s+ToolTipService\.ToolTip="\{x:Bind HoyolabToolboxAutoRefreshTitle\}">\s*<NavigationViewItem\.Content>\s*<TextBlock MaxWidth="250"\s+Text="\{x:Bind HoyolabToolboxAutoRefreshTitle\}"\s+TextWrapping="Wrap" />\s*</NavigationViewItem\.Content>\s*<NavigationViewItem\.Icon>\s*<FontIcon Glyph="&#xE895;" />\s*</NavigationViewItem\.Icon>\s*</NavigationViewItem>'
$navReplacement = @'
<NavigationViewItem Content="{x:Bind HoyolabToolboxAutoRefreshTitle}"
                                    Tag="HoyolabToolboxAutoRefreshSetting"
                                    ToolTipService.ToolTip="{x:Bind HoyolabToolboxAutoRefreshTitle}">
                    <NavigationViewItem.Icon>
                        <FontIcon Glyph="&#xE895;" />
                    </NavigationViewItem.Icon>
                </NavigationViewItem>
'@
$settingPageXaml = Replace-Once $settingPageXaml $navPattern $navReplacement 'HoYoLAB auto refresh navigation item'
Write-TextFile $settingPageXamlPath $settingPageXaml

$autoRefreshXamlPath = 'src/Starward/Features/Setting/HoyolabToolboxAutoRefreshSetting.xaml'
$autoRefreshXaml = Read-TextFile $autoRefreshXamlPath

$gameRows = @(
    @{ Name = 'Genshin'; Tag = 'hk4e' },
    @{ Name = 'StarRail'; Tag = 'hkrpg' },
    @{ Name = 'ZZZ'; Tag = 'nap' }
)
foreach ($row in $gameRows) {
    $name = $row.Name
    $tag = $row.Tag
    $pattern = '<Grid ColumnSpacing="12">\s*<Grid\.ColumnDefinitions>\s*<ColumnDefinition />\s*<ColumnDefinition Width="Auto" />\s*</Grid\.ColumnDefinitions>\s*<ComboBox x:Name="ComboBox_' + $name + '"\s+MinWidth="220"\s+MaxWidth="420"\s+HorizontalAlignment="Stretch"\s+DisplayMemberPath="Name"\s+ItemsSource="\{x:Bind ScheduleOptions\}" />\s*<Button Grid\.Column="1"\s+MinWidth="140"\s+Tag="' + $tag + '"\s+Click="RefreshGame_Click"\s+Content="\{x:Bind RefreshNowText\}" />\s*</Grid>'
    $replacement = '<Grid ColumnSpacing="12">' + "`r`n" +
        '                        <Grid.ColumnDefinitions>' + "`r`n" +
        '                            <ColumnDefinition Width="Auto" />' + "`r`n" +
        '                            <ColumnDefinition />' + "`r`n" +
        '                            <ColumnDefinition Width="Auto" />' + "`r`n" +
        '                        </Grid.ColumnDefinitions>' + "`r`n" +
        '                        <ComboBox x:Name="ComboBox_' + $name + '"' + "`r`n" +
        '                                  Width="320"' + "`r`n" +
        '                                  HorizontalAlignment="Left"' + "`r`n" +
        '                                  DisplayMemberPath="Name"' + "`r`n" +
        '                                  ItemsSource="{x:Bind ScheduleOptions}" />' + "`r`n" +
        '                        <Button Grid.Column="2"' + "`r`n" +
        '                                MinWidth="140"' + "`r`n" +
        '                                Tag="' + $tag + '"' + "`r`n" +
        '                                Click="RefreshGame_Click"' + "`r`n" +
        '                                Content="{x:Bind RefreshNowText}" />' + "`r`n" +
        '                    </Grid>'
    $autoRefreshXaml = Replace-Once $autoRefreshXaml $pattern $replacement "schedule selector row $name"
}
Write-TextFile $autoRefreshXamlPath $autoRefreshXaml

$settingPageCsPath = 'src/Starward/Features/Setting/SettingPage.xaml.cs'
$settingPageCs = Read-TextFile $settingPageCsPath
$settingTitlePattern = 'public string HoyolabToolboxAutoRefreshTitle => Localized\(\s*"Настройки автообновления HoYoLAB Toolbox",\s*"HoYoLAB Toolbox automatic refresh settings"\);'
$settingTitleReplacement = @'
public string HoyolabToolboxAutoRefreshTitle => Localized(
        "Автообновление HoYoLAB",
        "HoYoLAB Auto Refresh");
'@
$settingPageCs = Replace-Once $settingPageCs $settingTitlePattern $settingTitleReplacement 'settings navigation title'
Write-TextFile $settingPageCsPath $settingPageCs

$autoRefreshCsPath = 'src/Starward/Features/Setting/HoyolabToolboxAutoRefreshSetting.xaml.cs'
$autoRefreshCs = Read-TextFile $autoRefreshCsPath
$pageTitlePattern = 'public string PageTitle => Localized\(\s*"Настройки автообновления HoYoLAB Toolbox",\s*"HoYoLAB Toolbox automatic refresh settings"\);'
$pageTitleReplacement = @'
public string PageTitle => Localized(
        "Автообновление данных HoYoLAB",
        "HoYoLAB Data Auto Refresh");
'@
$autoRefreshCs = Replace-Once $autoRefreshCs $pageTitlePattern $pageTitleReplacement 'auto refresh page title'

$pageDescriptionPattern = 'public string PageDescription => Localized\(\s*"Настройте расписание обновления сохранённых данных HoYoLAB отдельно для каждой игры\. Для ежемесячных отчётов автоматически загружаются подробности всех месяцев, доступных в меню Get Details\.",\s*"Configure the saved HoYoLAB data refresh schedule separately for each game\. Monthly reports also download details for every month available in the Get Details menu\."\);'
$pageDescriptionReplacement = @'
public string PageDescription => Localized(
        "Выберите, как часто Starward будет обновлять данные HoYoLAB для каждой игры. Для ежемесячных отчётов также загружаются сведения за все доступные месяцы.",
        "Choose how often Starward refreshes HoYoLAB data for each game. Monthly reports also include details for every available month.");
'@
$autoRefreshCs = Replace-Once $autoRefreshCs $pageDescriptionPattern $pageDescriptionReplacement 'auto refresh page description'

$scheduleNotePattern = 'public string ScheduleNote => Localized\(\s*"Проверка каждые 30 минут отключена\. Starward запускает обновление только при наступлении выбранного периода\. Если клиент был закрыт, пропущенное обновление выполняется при следующем запуске\.",\s*"The 30-minute polling check is disabled\. Starward refreshes only when the selected interval becomes due\. A missed refresh runs on the next launch\."\);'
$scheduleNoteReplacement = @'
public string ScheduleNote => Localized(
        "Обновление запускается только по выбранному расписанию. Если Starward был закрыт, пропущенное обновление выполнится при следующем запуске.",
        "Refresh runs only on the selected schedule. A missed refresh runs the next time Starward starts.");
'@
$autoRefreshCs = Replace-Once $autoRefreshCs $scheduleNotePattern $scheduleNoteReplacement 'auto refresh schedule note'
Write-TextFile $autoRefreshCsPath $autoRefreshCs

# -----------------------------------------------------------------------------
# Localized playtime duration
# -----------------------------------------------------------------------------
$playTimePath = 'src/Starward/Features/PlayTime/PlayTimeButton.xaml.cs'
$playTime = Read-TextFile $playTimePath

if ($playTime -notmatch 'using Starward\.Language;') {
    $playTime = Replace-Once $playTime '(using Starward\.Features\.Database;\r?\n)' ('$1' + "using Starward.Language;`r`n") 'Starward.Language using'
}
if ($playTime -notmatch 'using System\.Globalization;') {
    $playTime = Replace-Once $playTime '(using System;\r?\n)' ('$1' + "using System.Globalization;`r`n") 'System.Globalization using'
}

$durationPattern = 'return\s+\$"\{Math\.Floor\(timeSpan\.TotalHours\)\}h \{timeSpan\.Minutes\}m";'
$durationReplacement = @'
string format = Lang.ResourceManager.GetString("PlayTimeButton_DurationFormat", Lang.Culture)
            ?? "{0}h {1}m";
        return string.Format(CultureInfo.CurrentUICulture, format, Math.Floor(timeSpan.TotalHours), timeSpan.Minutes);
'@
$playTime = Replace-Once $playTime $durationPattern $durationReplacement 'localized playtime duration formatter'
Write-TextFile $playTimePath $playTime

function Add-ResxEntry([string]$Path, [string]$Name, [string]$Value) {
    $content = Read-TextFile $Path
    if ($content.Contains(('name="{0}"' -f $Name))) {
        return
    }

    $entry = '  <data name="{0}" xml:space="preserve">{1}    <value>{2}</value>{1}  </data>{1}' -f $Name, "`r`n", $Value
    $pattern = '\r?\n</root>\s*$'
    $regex = [regex]::new($pattern)
    if (-not $regex.IsMatch($content)) {
        throw "Cannot find </root> in $Path."
    }
    $content = $regex.Replace($content, "`r`n$entry</root>`r`n", 1)
    Write-TextFile $Path $content
}

Add-ResxEntry 'src/Starward.Language/Lang.resx' 'PlayTimeButton_DurationFormat' '{0}h {1}m'
Add-ResxEntry 'src/Starward.Language/Lang.ru-RU.resx' 'PlayTimeButton_DurationFormat' '{0} ч {1} м'

Write-Host 'Settings layout, Apocalyptic Shadow layouts, and localized playtime fixes applied.'