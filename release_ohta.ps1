# Phat hanh 1 ban OHTA TOOL len GitHub Releases (ADR 0004) - may doi tu cap nhat tu day.
#   powershell -ExecutionPolicy Bypass -File OHTA_TOOL\release_ohta.ps1           (phat hanh that)
#   powershell -ExecutionPolicy Bypass -File OHTA_TOOL\release_ohta.ps1 -DryRun   (chi build + dong goi, khong tag / push / release)
# Truoc khi chay: sua VERSION (vd 1.2.0), them muc "## v1.2.0" vao dau CHANGELOG.md, commit het tren nhanh main.
# Can: GitHub CLI (winget install GitHub.cli, roi gh auth login 1 lan) va remote origin tro toi GitHub.
param([switch]$DryRun)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$Repo = "hungna51/OHTA_TOOL"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Invoke-Git { & git -C $Root @args; if ($LASTEXITCODE -ne 0) { throw "git $($args -join ' ') THAT BAI" } }

# --- 1. Phien ban + ghi chu ---
$Version = (Get-Content (Join-Path $Root "VERSION") -Raw).Trim().TrimStart('v', 'V')
if ($Version -notmatch '^\d+(\.\d+)*$') { throw "File VERSION sai dinh dang (vd 1.2.0): '$Version'" }
$Tag = "v$Version"

$Changelog = Get-Content (Join-Path $Root "CHANGELOG.md") -Raw -Encoding UTF8
$m = [regex]::Match($Changelog, "(?ms)^## $([regex]::Escape($Tag))\b[^\n]*\n(.*?)(?=^## |\z)")
if (-not $m.Success -or -not $m.Groups[1].Value.Trim()) { throw "CHANGELOG.md chua co muc '## $Tag' (kem ghi chu)." }
$Notes = $m.Groups[1].Value.Trim()
Write-Host "Phat hanh OHTA TOOL $Tag" -ForegroundColor Cyan

# --- 2. Kiem tra git ---
$Branch = (Invoke-Git rev-parse --abbrev-ref HEAD).Trim()
$Dirty = Invoke-Git status --porcelain
$TagExists = (& git -C $Root tag --list $Tag)
$Problems = @()
if ($Branch -ne "main") { $Problems += "Dang o nhanh '$Branch' - phai phat hanh tu nhanh main." }
if ($Dirty) { $Problems += "Con thay doi chua commit:`n$($Dirty -join "`n")" }
if ($TagExists) { $Problems += "Tag $Tag da ton tai - tang so trong VERSION." }
if (-not $DryRun) {
    $Origin = (& git -C $Root remote get-url origin 2>$null)
    if (-not $Origin) { $Problems += "Chua co remote origin. Chay: git -C `"$Root`" remote add origin https://github.com/$Repo.git" }
    elseif (& git -C $Root ls-remote --tags origin $Tag) { $Problems += "Tag $Tag da co tren GitHub." }
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { $Problems += "Chua cai GitHub CLI: winget install GitHub.cli roi gh auth login" }
}
if ($Problems.Count -gt 0) {
    if ($DryRun) { $Problems | ForEach-Object { Write-Host "  [!] $_" -ForegroundColor Yellow } }
    else { throw ("Chua phat hanh duoc:`n - " + ($Problems -join "`n - ")) }
}

# --- 3. Build du 2 ban + test ---
# -RequireBoth: thieu SDK 1 trong 2 CAD thi dung (build_ohta binh thuong se giu DLL cu cua ban do)
& (Join-Path $Root "build_ohta.ps1") -RequireBoth

$Dlls = @(
    "_system/bin/ZwCAD/OHTATOOL.dll", "_system/bin/AutoCAD/OHTATOOL.dll",
    "03_Guro/bin/ZwCAD/GROTOOL.dll",  "03_Guro/bin/AutoCAD/GROTOOL.dll",
    "05_AI/bin/ZwCAD/AITOOL.dll",     "05_AI/bin/AutoCAD/AITOOL.dll",
    "06_Teams/bin/ZwCAD/TEAMSTOOL.dll", "06_Teams/bin/AutoCAD/TEAMSTOOL.dll",
    "_apps/OhtaTeams/bin/OhtaTeams.exe", "_apps/OhtaTeams/bin/OhtaTeams.exe.config"
)
foreach ($d in $Dlls) {
    $f = Get-Item (Join-Path $Root $d) -ErrorAction SilentlyContinue
    if (-not $f) { throw "Thieu DLL sau khi build: $d" }
}
# DLL phu thuoc canh OHTATOOL.dll (PdfSharp ... cho tab PRINT - ADR 0005)
foreach ($sub in @("ZwCAD", "AutoCAD")) {
    $Dlls += Get-ChildItem -Path (Join-Path $Root "_system/bin/$sub") -Filter "*.dll" -File |
        Where-Object { $_.Name -ne "OHTATOOL.dll" -and $_.Name -notmatch '\.old_' } |
        ForEach-Object { "_system/bin/$sub/" + $_.Name }
}

# --- 4. Gom file vao thu muc tam ---
# Goi gom: file goc can cho may doi + moi thu muc tab (git dang theo doi) + DLL vua build.
# Bo: ma nguon C# (_system/OhtaTool.*, <tab>/plugin/), script Apps Script (.gs), khoa google_sheet.key, tai lieu dev.
$RootFiles = @("OHTA_Loader.lsp", "README.md", "GEMINI.md", "GLOSSARY.md", "VERSION", "CHANGELOG.md")
$Tracked = Invoke-Git -c core.quotepath=off ls-files
$Files = $Tracked | Where-Object {
    $p = $_
    $top = $p.Split('/')[0]
    if ($p -notmatch '/') { return $RootFiles -contains $p }
    if ($p -match '^[^/]+/plugin/' -or $p -match '\.gs$' -or $p -match 'google_sheet\.key$' -or $p -match 'ai\.key' -or $p -match '^05_AI/log/') { return $false }
    if ($top -eq '_Template_Module') { return $true }
    return -not ($top.StartsWith('_') -or $top.StartsWith('.') -or $top -eq 'rules')
}
$Files = @($Files) + $Dlls
foreach ($need in @("VERSION", "CHANGELOG.md", "OHTA_Loader.lsp")) {
    if ($Files -notcontains $need) { throw "Goi thieu $need - file nay da commit chua?" }
}

$Work = Join-Path ([IO.Path]::GetTempPath()) ("ohta_release_" + [guid]::NewGuid().ToString("N"))
$Stage = Join-Path $Work "OHTA_TOOL"
try {
    foreach ($p in $Files) {
        $dst = Join-Path $Stage $p
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dst) | Out-Null
        Copy-Item -LiteralPath (Join-Path $Root $p) -Destination $dst -Force
    }

    # --- 5. Manifest (sha256 tung file) ---
    $lines = foreach ($p in ($Files | Sort-Object)) {
        $h = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $Stage $p)).Hash.ToLowerInvariant()
        "$h  $p"
    }
    $manifest = Join-Path $Stage "_system/manifest.txt"
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $manifest) | Out-Null
    [IO.File]::WriteAllText($manifest, (($lines -join "`n") + "`n"), (New-Object System.Text.UTF8Encoding($false)))

    # --- 6. Zip ---
    $ReleaseDir = Join-Path $Root "_release"
    New-Item -ItemType Directory -Force -Path $ReleaseDir | Out-Null
    $Zip = Join-Path $ReleaseDir "OHTA_TOOL_$Tag.zip"
    if (Test-Path $Zip) { Remove-Item $Zip -Force }
    # Tu ghi tung entry voi dau / (CreateFromDirectory tren PowerShell 5.1 ghi dau \)
    Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
    $za = [IO.Compression.ZipFile]::Open($Zip, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($p in (@($Files) + "_system/manifest.txt")) {
            [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($za, (Join-Path $Stage $p), "OHTA_TOOL/$p", [IO.Compression.CompressionLevel]::Optimal)
        }
    }
    finally {
        $za.Dispose()
    }
    Write-Host "  [OK] Dong goi $($Files.Count) file -> $Zip" -ForegroundColor Green
}
finally {
    Remove-Item -Recurse -Force $Work -ErrorAction SilentlyContinue
}

if ($DryRun) {
    Write-Host "`n[DryRun] Khong tao tag / push / Release. Ghi chu se dung:`n$Notes" -ForegroundColor Yellow
    return
}

# --- 7. Tag + push + GitHub Release ---
$NotesFile = Join-Path ([IO.Path]::GetTempPath()) "ohta_release_notes_$Version.md"
[IO.File]::WriteAllText($NotesFile, $Notes, (New-Object System.Text.UTF8Encoding($false)))
try {
    Invoke-Git tag -a $Tag -m "OHTA TOOL $Tag"
    Invoke-Git push origin main
    Invoke-Git push origin $Tag
    & gh release create $Tag $Zip --repo $Repo --title "OHTA TOOL $Tag" --notes-file $NotesFile
    if ($LASTEXITCODE -ne 0) { throw "gh release create THAT BAI (tag $Tag da push; chay lai lenh gh release create bang tay)" }
}
finally {
    Remove-Item $NotesFile -Force -ErrorAction SilentlyContinue
}

Write-Host "`n=== DA PHAT HANH OHTA TOOL $Tag - may doi se duoc nhac cap nhat khi mo ban ve ===" -ForegroundColor Green
