$ErrorActionPreference = 'Stop'

$target = Join-Path $PSScriptRoot 'apply-duration-imaginarium-settings-fixes.ps1'
$content = [System.IO.File]::ReadAllText($target)

$bad = @'
    $replacement = "  <data name=\"$Name\" xml:space=\"preserve\">`r`n    <value>$Value</value>`r`n  </data>"
'@
$good = @'
    $replacement = ('  <data name="{0}" xml:space="preserve">{1}    <value>{2}</value>{1}  </data>' -f $Name, "`r`n", $Value)
'@

if (-not $content.Contains($bad)) {
    throw 'The expected invalid PowerShell XML replacement line was not found.'
}

$content = $content.Replace($bad, $good)
[System.IO.File]::WriteAllText($target, $content, [System.Text.UTF8Encoding]::new($false))

& $target
if (-not $?) {
    exit 1
}
