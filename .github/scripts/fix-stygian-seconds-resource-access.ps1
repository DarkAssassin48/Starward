$ErrorActionPreference = 'Stop'

$path = 'src/Starward/Features/GameRecord/Genshin/StygianOnslaughtPage.xaml.cs'
$content = [System.IO.File]::ReadAllText((Resolve-Path $path).Path)
$old = '        return string.Format(Lang.StygianOnslaughtPage_SecondsFormat, seconds);'
$new = @'
        var format = Lang.ResourceManager.GetString("StygianOnslaughtPage_SecondsFormat", Lang.Culture) ?? "{0}s";
        return string.Format(format, seconds);
'@

$count = ([regex]::Matches($content, [regex]::Escape($old))).Count
if ($count -ne 1) {
    throw "Stygian seconds resource access: expected exactly one match, found $count."
}

$content = $content.Replace($old, $new.TrimEnd("`r", "`n"))
[System.IO.File]::WriteAllText((Resolve-Path $path).Path, $content, [System.Text.UTF8Encoding]::new($false))
Write-Host 'Stygian seconds format now reads directly from the resx ResourceManager.'
