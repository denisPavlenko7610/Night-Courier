$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$templateRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot '../UnityTemplates/Assets/UnityTemplates'))
$linkRoot = Join-Path $projectRoot 'Assets/TemplateSources'

foreach ($name in @('Foundation', 'Modules')) {
    $target = Join-Path $templateRoot $name
    $link = Join-Path $linkRoot $name
    if (-not (Test-Path -LiteralPath $target -PathType Container)) {
        throw "UnityTemplates folder not found: $target"
    }
    if (Test-Path -LiteralPath $link) {
        $existing = Get-Item -LiteralPath $link -Force
        if (($existing.Attributes -band [IO.FileAttributes]::ReparsePoint) -and
            ([IO.Path]::GetFullPath($existing.Target) -eq [IO.Path]::GetFullPath($target))) {
            continue
        }
        throw "The destination already exists and is not the expected link: $link"
    }
    New-Item -ItemType Directory -Force -Path $linkRoot | Out-Null
    New-Item -ItemType Junction -Path $link -Target $target | Out-Null
}
Write-Host 'UnityTemplates source links are ready.'
