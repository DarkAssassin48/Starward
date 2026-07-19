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

# Apocalyptic Shadow: keep long stage names inside the title column instead of
# allowing StackPanel's infinite-width measurement to draw under the star icons.
$xamlPath = 'src/Starward/Features/GameRecord/StarRail/ApocalypticShadowPage.xaml'
$xaml = Read-TextFile $xamlPath

$titlePattern = '<StackPanel\s+VerticalAlignment="Center"\s+Spacing="2">\s*<TextBlock\s+FontSize="18"\s+FontWeight="Bold"\s+Text="\{x:Bind Name\}"\s+TextTrimming="CharacterEllipsis"\s*/>\s*</StackPanel>'
$titleReplacement = @'
<TextBlock Grid.Column="0"
                                               MinWidth="0"
                                               Margin="0,0,12,0"
                                               VerticalAlignment="Center"
                                               FontSize="18"
                                               FontWeight="Bold"
                                               MaxLines="2"
                                               Text="{x:Bind Name}"
                                               TextTrimming="CharacterEllipsis"
                                               TextWrapping="WrapWholeWords"
                                               ToolTipService.ToolTip="{x:Bind Name}" />
'@
$xaml = Replace-Once $xaml $titlePattern $titleReplacement 'Apocalyptic Shadow floor title'

# Constrain each boss name independently, so translated names wrap/trim and never
# overlap the neighbouring boss icon or column.
$bosses = @(
    @{ Key = 'UpperBoss'; Description = 'upper boss text' },
    @{ Key = 'LowerBoss'; Description = 'lower boss text' },
    @{ Key = 'TierceBoss'; Description = 'third boss text' }
)

foreach ($boss in $bosses) {
    $key = $boss.Key
    $pattern = '(<sc:CachedImage\s+Width="40"\s+Height="40"\s+VerticalAlignment="Center"\s+CornerRadius="20"\s+Source="\{x:Bind CurrentApocalypticShadow\.Meta\.' + $key + '\.Icon\}"\s*/>\s*)<TextBlock\s+VerticalAlignment="Center"\s+FontSize="12">'
    $replacement = '$1<TextBlock MinWidth="0"' + "`r`n" +
        '                                       MaxWidth="132"' + "`r`n" +
        '                                       VerticalAlignment="Center"' + "`r`n" +
        '                                       FontSize="12"' + "`r`n" +
        '                                       MaxLines="2"' + "`r`n" +
        '                                       TextTrimming="CharacterEllipsis"' + "`r`n" +
        '                                       TextWrapping="WrapWholeWords"' + "`r`n" +
        '                                       ToolTipService.ToolTip="{x:Bind CurrentApocalypticShadow.Meta.' + $key + '.Name}">'
    $xaml = Replace-Once $xaml $pattern $replacement "Apocalyptic Shadow $($boss.Description)"
}

Write-TextFile $xamlPath $xaml

# Playtime: load the entire duration format from localization resources.
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

Write-Host 'Apocalyptic Shadow layout and localized playtime format fixes applied.'
