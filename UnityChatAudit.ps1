# UnityChatAudit.ps1
# Run from the Unity project root. Creates one compact AI-readable report.
$ErrorActionPreference = "SilentlyContinue"
$Root = (Get-Location).Path
$Out = Join-Path $Root "AUDIT_FOR_AI.txt"
$SkipDirs = @("Library","Temp","Obj","Logs","Build","Builds",".git",".vs","UserSettings","node_modules")
$TextExt = @(".cs",".asmdef",".asmref",".json",".txt",".yml",".yaml",".shader",".compute",".hlsl",".cginc",".uxml",".uss")
$MaxBytes = 150000

$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine("UNITY PROJECT QUICK AUDIT")
[void]$sb.AppendLine("Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
[void]$sb.AppendLine("Project root: $Root")
[void]$sb.AppendLine("NOTE: Generated/cache folders excluded. Binary assets are listed, not dumped.")
[void]$sb.AppendLine("")

$versionFile = Join-Path $Root "ProjectSettings/ProjectVersion.txt"
if (Test-Path $versionFile) {
    [void]$sb.AppendLine("=== UNITY VERSION ===")
    [void]$sb.AppendLine((Get-Content $versionFile -Raw))
}
$manifest = Join-Path $Root "Packages/manifest.json"
if (Test-Path $manifest) {
    [void]$sb.AppendLine("=== PACKAGES ===")
    [void]$sb.AppendLine((Get-Content $manifest -Raw))
}

$files = Get-ChildItem -LiteralPath $Root -File -Recurse | Where-Object {
    $rel = $_.FullName.Substring($Root.Length).TrimStart('\','/')
    $parts = $rel -split '[\\/]'
    -not ($parts | Where-Object { $SkipDirs -contains $_ }) -and
    $_.Name -ne "AUDIT_FOR_AI.txt" -and
    $_.FullName -notmatch '[\\/]\.env(\.|$)|secret|credential|\.pem$|\.key$|\.p12$|\.keystore$'
} | Sort-Object FullName

[void]$sb.AppendLine("=== COMPLETE FILE PATH INVENTORY ===")
foreach ($f in $files) {
    $rel = $f.FullName.Substring($Root.Length).TrimStart('\','/')
    [void]$sb.AppendLine(("{0} ({1:N0} bytes)" -f $rel, $f.Length))
}

[void]$sb.AppendLine("")
[void]$sb.AppendLine("=== SOURCE / CONFIG CONTENT ===")
foreach ($f in $files) {
    if ($TextExt -notcontains $f.Extension.ToLower()) { continue }
    if ($f.Length -gt $MaxBytes) {
        $rel = $f.FullName.Substring($Root.Length).TrimStart('\','/')
        [void]$sb.AppendLine("`n--- SKIPPED (file too large): $rel ---")
        continue
    }
    $rel = $f.FullName.Substring($Root.Length).TrimStart('\','/')
    [void]$sb.AppendLine("`n--- FILE: $rel ---")
    try {
        $content = Get-Content -LiteralPath $f.FullName -Raw
        [void]$sb.AppendLine($content)
    } catch {
        [void]$sb.AppendLine("[Could not read file]")
    }
}

[void]$sb.AppendLine("")
[void]$sb.AppendLine("=== END AUDIT ===")
[System.IO.File]::WriteAllText($Out, $sb.ToString(), [System.Text.UTF8Encoding]::new($false))
Write-Host "Audit selesai: $Out" -ForegroundColor Green
Write-Host "Upload AUDIT_FOR_AI.txt ke chat AI. Untuk error, copy juga pesan dari Unity Console." -ForegroundColor Cyan
