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

        // HoYoLAB sometimes joins the stage name and the Starward-mode suffix
        // without a separator, for example "Difficulty 4Starward Mode".
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
# Apocalyptic Shadow stage title
# -----------------------------------------------------------------------------
$apocalypticXamlPath = 'src/Starward/Features/GameRecord/StarRail/ApocalypticShadowPage.xaml'
$apocalypticXaml = Read-TextFile $apocalypticXamlPath
$apocalypticTitlePattern = '<TextBlock Grid.Column="0"\s+MinWidth="0"\s+Margin="0,8,12,8"\s+VerticalAlignment="Center"\s+FontSize="18"\s+FontWeight="Bold"\s+Text="\{x:Bind Name\}"\s+TextWrapping="WrapWholeWords"\s+ToolTipService.ToolTip="\{x:Bind Name\}" />'
$apocalypticTitleReplacement = @'
<TextBlock Grid.Column="0"
                                                MinWidth="0"
                                                Margin="0,8,16,8"
                                                VerticalAlignment="Center"
                                                FontSize="18"
                                                FontWeight="Bold"
                                                MaxLines="3"
                                                Text="{x:Bind local:ApocalypticShadowPage.FormatStageName(Name)}"
                                                TextTrimming="CharacterEllipsis"
                                                TextWrapping="WrapWholeWords"
                                                ToolTipService.ToolTip="{x:Bind Name}" />
'@
$apocalypticXaml = Replace-Once $apocalypticXaml $apocalypticTitlePattern $apocalypticTitleReplacement 'Apocalyptic Shadow stage title'
Write-TextFile $apocalypticXamlPath $apocalypticXaml
Add-StageNameFormatter 'src/Starward/Features/GameRecord/StarRail/ApocalypticShadowPage.xaml.cs' '(public static bool FloorHasExtraStar\(int starNum\) => starNum > 3;)' 'Apocalyptic Shadow'

# -----------------------------------------------------------------------------
# Pure Fiction stage title
# -----------------------------------------------------------------------------
$pureFictionXamlPath = 'src/Starward/Features/GameRecord/StarRail/PureFictionPage.xaml'
$pureFictionXaml = Read-TextFile $pureFictionXamlPath
$pureFictionXaml = Replace-Once $pureFictionXaml '<Grid Height="304" Padding="20,0,20,0">' '<Grid MinHeight="304" Padding="20,0,20,0">' 'Pure Fiction card height'
$pureFictionXaml = Replace-Once $pureFictionXaml '<RowDefinition Height="56" />' '<RowDefinition Height="Auto" MinHeight="56" />' 'Pure Fiction title row'
$pureTitlePattern = '<StackPanel VerticalAlignment="Center" Spacing="2">\s*<TextBlock FontWeight="Bold"\s+Text="\{x:Bind Name\}"\s+TextTrimming="CharacterEllipsis" />\s*<TextBlock FontSize="12" Foreground="\{ThemeResource TextFillColorSecondaryBrush\}">\s*<Run Text="\{x:Bind lang:Lang\.ForgottenHallPage_CyclesUsed\}" />\s*<Run Text="\{x:Bind RoundNum\}" />\s*</TextBlock>\s*</StackPanel>'
$pureTitleReplacement = @'
<StackPanel Grid.Column="0"
                                                MinWidth="0"
                                                Margin="0,6,16,6"
                                                VerticalAlignment="Center"
                                                Spacing="2">
                                        <TextBlock FontWeight="Bold"
                                                   MaxLines="3"
                                                   Text="{x:Bind local:PureFictionPage.FormatStageName(Name)}"
                                                   TextTrimming="CharacterEllipsis"
                                                   TextWrapping="WrapWholeWords"
                                                   ToolTipService.ToolTip="{x:Bind Name}" />
                                        <TextBlock FontSize="12" Foreground="{ThemeResource TextFillColorSecondaryBrush}">
                                            <Run Text="{x:Bind lang:Lang.ForgottenHallPage_CyclesUsed}" />
                                            <Run Text="{x:Bind RoundNum}" />
                                        </TextBlock>
                                    </StackPanel>
'@
$pureFictionXaml = Replace-Once $pureFictionXaml $pureTitlePattern $pureTitleReplacement 'Pure Fiction stage title'
Write-TextFile $pureFictionXamlPath $pureFictionXaml
Add-StageNameFormatter 'src/Starward/Features/GameRecord/StarRail/PureFictionPage.xaml.cs' '(public static bool FloorHasExtraStar\(int starNum\) => starNum > 3;)' 'Pure Fiction'

# -----------------------------------------------------------------------------
# Stygian Onslaught boss/stage names
# -----------------------------------------------------------------------------
$stygianPath = 'src/Starward/Features/GameRecord/Genshin/StygianOnslaughtPage.xaml'
$stygian = Read-TextFile $stygianPath
$stygianTitlePattern = '<TextBlock FontSize="16" Text="\{x:Bind Name\}" />'
$stygianTitleReplacement = @'
<TextBlock Grid.Column="0"
                                           MinWidth="0"
                                           Margin="0,0,16,0"
                                           FontSize="16"
                                           MaxLines="3"
                                           Text="{x:Bind Name}"
                                           TextTrimming="CharacterEllipsis"
                                           TextWrapping="WrapWholeWords"
                                           ToolTipService.ToolTip="{x:Bind Name}" />
'@
$stygian = Replace-Once $stygian $stygianTitlePattern $stygianTitleReplacement 'Stygian Onslaught challenge name'
Write-TextFile $stygianPath $stygian

# -----------------------------------------------------------------------------
# Anomaly Arbitration (Challenge Peak) buff panel
# -----------------------------------------------------------------------------
$challengePeakPath = 'src/Starward/Features/GameRecord/StarRail/ChallengePeakPage.xaml'
$challengePeak = Read-TextFile $challengePeakPath
$challengeHeightReplacement = @'
<Grid MinHeight="220"
                       Background="{ThemeResource CustomOverlayAcrylicBrush}"
'@
$challengePeak = Replace-Once $challengePeak '<Grid Height="192"\s+Background="\{ThemeResource CustomOverlayAcrylicBrush\}"' $challengeHeightReplacement 'Challenge Peak boss card height'

$challengeColumnsReplacement = @'
<Grid.ColumnDefinitions>
                        <ColumnDefinition />
                        <ColumnDefinition Width="240" />
                    </Grid.ColumnDefinitions>

                    <!--  BOSS 图  -->
                    <sc:CachedImage Grid.RowSpan="2"
                                    Grid.Column="1"
'@
$challengePeak = Replace-Once $challengePeak '<Grid\.ColumnDefinitions>\s*<ColumnDefinition Width="Auto" />\s*<ColumnDefinition />\s*</Grid\.ColumnDefinitions>\s*\r?\n\s*<!--  BOSS 图  -->\s*<sc:CachedImage Grid.RowSpan="2"\s+Grid.ColumnSpan="2"' $challengeColumnsReplacement 'Challenge Peak boss columns and image'

$buffPattern = '<StackPanel Grid.Row="1"\s+Grid.Column="1"\s+Margin="12,0,12,0"\s+Visibility="\{x:Bind CurrentChallengePeakRecord\.BossRecord, Converter=\{StaticResource ObjectToVisibilityConverter\}\}">\s*<!--  Buff  -->\s*<StackPanel Orientation="Horizontal">\s*<sc:CachedImage Width="28"\s+Height="28"\s+Source="\{x:Bind CurrentChallengePeakRecord\.BossRecord\.Buff\.Icon\}" />\s*<TextBlock VerticalAlignment="Center" Text="\{x:Bind CurrentChallengePeakRecord\.BossRecord\.Buff\.NameMi18n\}" />\s*</StackPanel>\s*<TextBlock Foreground="\{ThemeResource TextFillColorSecondaryBrush\}"\s+Text="\{x:Bind CurrentChallengePeakRecord\.BossRecord\.Buff\.DescMi18n\}"\s+TextWrapping="Wrap" />\s*</StackPanel>'
$buffReplacement = @'
<Border Grid.RowSpan="2"
                            Grid.Column="1"
                            Margin="8,10,12,10"
                            Padding="12,10"
                            VerticalAlignment="Stretch"
                            Background="{ThemeResource ControlOnImageFillColorDefaultBrush}"
                            CornerRadius="8"
                            Visibility="{x:Bind CurrentChallengePeakRecord.BossRecord, Converter={StaticResource ObjectToVisibilityConverter}}">
                        <StackPanel VerticalAlignment="Center" Spacing="8">
                            <!--  Buff  -->
                            <StackPanel Orientation="Horizontal" Spacing="8">
                                <sc:CachedImage Width="32"
                                                Height="32"
                                                VerticalAlignment="Center"
                                                Source="{x:Bind CurrentChallengePeakRecord.BossRecord.Buff.Icon}" />
                                <TextBlock MinWidth="0"
                                           VerticalAlignment="Center"
                                           FontWeight="SemiBold"
                                           MaxLines="2"
                                           Text="{x:Bind CurrentChallengePeakRecord.BossRecord.Buff.NameMi18n}"
                                           TextTrimming="CharacterEllipsis"
                                           TextWrapping="WrapWholeWords"
                                           ToolTipService.ToolTip="{x:Bind CurrentChallengePeakRecord.BossRecord.Buff.NameMi18n}" />
                            </StackPanel>
                            <TextBlock Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                                       MaxLines="7"
                                       Text="{x:Bind CurrentChallengePeakRecord.BossRecord.Buff.DescMi18n}"
                                       TextTrimming="CharacterEllipsis"
                                       TextWrapping="WrapWholeWords"
                                       ToolTipService.ToolTip="{x:Bind CurrentChallengePeakRecord.BossRecord.Buff.DescMi18n}" />
                        </StackPanel>
                    </Border>
'@
$challengePeak = Replace-Once $challengePeak $buffPattern $buffReplacement 'Challenge Peak buff panel'
Write-TextFile $challengePeakPath $challengePeak

Write-Host 'Stage title, Stygian name, and Anomaly Arbitration buff layout fixes applied.'
