# -RequireBoth: bat buoc build du ca ZWCAD lan AutoCAD (release_ohta.ps1 dung, tranh phat hanh DLL cu)
param([switch]$RequireBoth)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$OhtaRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$PluginProj = Join-Path $OhtaRoot "_system\OhtaTool.Plugin\OhtaTool.Plugin.csproj"
$TestProj   = Join-Path $OhtaRoot "_system\OhtaTool.Tests\OhtaTool.Tests.csproj"
# 03_Guro: DLL rieng GROTOOL.dll cua lenh GRO (nguon cu laptrinhlisp\ZwCadPlugin)
$GroProj     = Join-Path $OhtaRoot "03_Guro\plugin\GroTool.Plugin\GroTool.Plugin.csproj"
$GroTestProj = Join-Path $OhtaRoot "03_Guro\plugin\GroTool.Tests\GroTool.Tests.csproj"
# 05_AI: DLL rieng AITOOL.dll cua tab AI (ADR 0006) - chi build khi thu muc co tren may
$AiProj     = Join-Path $OhtaRoot "05_AI\plugin\AiTool.Plugin\AiTool.Plugin.csproj"
$AiTestProj = Join-Path $OhtaRoot "05_AI\plugin\AiTool.Tests\AiTool.Tests.csproj"
$HasAi = Test-Path $AiProj
# 06_Teams: DLL rieng TEAMSTOOL.dll cua tab Teams (ADR 0008)
$TeamsProj     = Join-Path $OhtaRoot "06_Teams\plugin\TeamsTool.Plugin\TeamsTool.Plugin.csproj"
$TeamsTestProj = Join-Path $OhtaRoot "06_Teams\plugin\TeamsTool.Tests\TeamsTool.Tests.csproj"
$HasTeams = Test-Path $TeamsProj
# _apps/OhtaTeams: ung dung rieng OhtaTeams.exe (net48, khong can SDK CAD) - ra thang _apps\OhtaTeams\bin
$TeamsAppProj = Join-Path $OhtaRoot "_apps\OhtaTeams\OhtaTeams.csproj"
$HasTeamsApp = Test-Path $TeamsAppProj

# --- Tu do SDK CAD dang cai tren may (moi may co the cai phien ban khac nhau) ---
# ZWCAD co the cai o o khac (vd D:\ZWCAD) -> lay InstallLocation tu registry, fallback duong dan mac dinh
$ZwCadSdkDir = Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*" -ErrorAction SilentlyContinue |
    Where-Object { $_.DisplayName -eq "ZWCAD 2026" -and $_.InstallLocation -and (Test-Path (Join-Path $_.InstallLocation "ZwManaged.dll")) } |
    Select-Object -First 1 -ExpandProperty InstallLocation
if (-not $ZwCadSdkDir) { $ZwCadSdkDir = "C:\Program Files\ZWSOFT\ZWCAD 2026" }
$ZwCadSdkDir = $ZwCadSdkDir.TrimEnd('\')
$HasZwCad = Test-Path (Join-Path $ZwCadSdkDir "ZwManaged.dll")

# Chon AutoCAD moi nhat co acmgd.dll. 2027+ chay .NET 10, 2025-2026 chay .NET 8.
$AutoCadSdkDir = $null
$AutoCadTfm = $null
$AutoCadYear = 0
Get-ChildItem "C:\Program Files\Autodesk" -Directory -Filter "AutoCAD 20*" -ErrorAction SilentlyContinue | ForEach-Object {
    if ($_.Name -match '^AutoCAD (\d{4})$' -and (Test-Path (Join-Path $_.FullName "acmgd.dll"))) {
        $year = [int]$Matches[1]
        if ($year -ge 2025 -and $year -gt $AutoCadYear) {
            $AutoCadYear = $year
            $AutoCadSdkDir = $_.FullName
        }
    }
}
if ($AutoCadSdkDir) {
    $AutoCadTfm = if ($AutoCadYear -ge 2027) { "net10.0-windows" } else { "net8.0-windows" }
}

if (-not $HasZwCad -and -not $AutoCadSdkDir) {
    throw "Khong tim thay SDK ZWCAD 2026 ($ZwCadSdkDir) hay AutoCAD 2025+ (C:\Program Files\Autodesk\AutoCAD 20xx) tren may nay!"
}

if ($RequireBoth -and (-not $HasZwCad -or -not $AutoCadSdkDir)) {
    throw "Phat hanh can build du ca 2 ban: may nay thieu SDK $(if (-not $HasZwCad) { 'ZWCAD 2026' } else { 'AutoCAD 2025+' })!"
}

$Targets = @()
if ($HasZwCad) { $Targets += "ZWCAD 2026 net48" } else { Write-Host "  [!] Khong co ZWCAD 2026 -> BO QUA ban ZWCAD (giu nguyen DLL cu trong bin\ZwCAD)" -ForegroundColor Yellow }
if ($AutoCadSdkDir) { $Targets += "AutoCAD $AutoCadYear $AutoCadTfm" } else { Write-Host "  [!] Khong co AutoCAD 2025+ -> BO QUA ban AutoCAD (giu nguyen DLL cu trong bin\AutoCAD)" -ForegroundColor Yellow }

Write-Host "`n[1/3] Bien dich OhtaTool.Plugin + GroTool.Plugin (+ AiTool.Plugin) ($($Targets -join ' & '))..." -ForegroundColor Cyan
$BuildArgs = @("-c", "Release", "-p:SkipPostBuildCopy=true", "--nologo", "-p:ZwCadSdkDir=$ZwCadSdkDir")
if ($AutoCadSdkDir) { $BuildArgs += @("-p:AutoCadSdkDir=$AutoCadSdkDir", "-p:AutoCadTfm=$AutoCadTfm") }
else { $BuildArgs += "-p:AutoCadSdkDir=__KHONG_CO_AUTOCAD__" }
dotnet build $PluginProj @BuildArgs
if ($LASTEXITCODE -ne 0) {
    throw "Bien dich OhtaTool.Plugin THAT BAI!"
}
dotnet build $GroProj @BuildArgs
if ($LASTEXITCODE -ne 0) {
    throw "Bien dich GroTool.Plugin (03_Guro) THAT BAI!"
}
if ($HasAi) {
    dotnet build $AiProj @BuildArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Bien dich AiTool.Plugin (05_AI) THAT BAI!"
    }
}
if ($HasTeams) {
    dotnet build $TeamsProj @BuildArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Bien dich TeamsTool.Plugin (06_Teams) THAT BAI!"
    }
}
if ($HasTeamsApp) {
    $TeamsBinDir = Join-Path (Split-Path -Parent $TeamsAppProj) "bin"
    if (Test-Path $TeamsBinDir) {
        Get-ChildItem -Path $TeamsBinDir -Filter "*.old_*" -ErrorAction SilentlyContinue | ForEach-Object {
            Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
        }
        $stampExe = [DateTime]::Now.ToString("yyyyMMdd_HHmmss")
        foreach ($ext in @("exe", "pdb")) {
            $fExe = Join-Path $TeamsBinDir "OhtaTeams.$ext"
            if (Test-Path $fExe) {
                try {
                    $fs = [IO.File]::Open($fExe, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
                    $fs.Close()
                } catch {
                    Move-Item -Path $fExe -Destination (Join-Path $TeamsBinDir "OhtaTeams.old_${stampExe}.$ext") -Force -ErrorAction SilentlyContinue
                }
            }
        }
    }
    dotnet build $TeamsAppProj -c Release --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Bien dich OhtaTeams.exe (_apps) THAT BAI! (Dang mo OhtaTeams thi tat di roi build lai)"
    }
}

function Publish-CadDll($srcDir, $destDir, $label, $name = "OHTATOOL") {
    if (-not (Test-Path (Join-Path $srcDir "$name.dll"))) { return }
    if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }

    # Don dep cac ban .old_* cu tu phien truoc neu CAD da dong
    Get-ChildItem -Path $destDir -Filter "*.old_*" -ErrorAction SilentlyContinue | ForEach-Object {
        Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
    }

    $stamp = [DateTime]::Now.ToString("yyyyMMdd_HHmmss")
    $locked = $false
    $copyList = @("$name.dll", "$name.pdb")
    # OHTATOOL kem DLL phu thuoc (PdfSharp, Microsoft.Extensions.* ...) cho tab PRINT - ADR 0005
    if ($name -eq "OHTATOOL") {
        $copyList += Get-ChildItem -Path $srcDir -Filter "*.dll" -File |
            Where-Object { $_.Name -ne "$name.dll" } | ForEach-Object { $_.Name }
    }
    foreach ($fileName in $copyList) {
        $srcFile = Join-Path $srcDir $fileName
        $dstFile = Join-Path $destDir $fileName
        if (-not (Test-Path $srcFile)) { continue }

        try {
            Copy-Item -Path $srcFile -Destination $dstFile -Force -ErrorAction Stop
        } catch {
            # File dang bi AutoCAD / ZWCAD giu khoa -> doi ten sang .old tren NTFS roi chep ban moi vao!
            $locked = $true
            $base = [IO.Path]::GetFileNameWithoutExtension($fileName)
            $ext = [IO.Path]::GetExtension($fileName)
            $oldName = Join-Path $destDir "$base.old_${stamp}$ext"
            Move-Item -Path $dstFile -Destination $oldName -Force -ErrorAction Stop
            Copy-Item -Path $srcFile -Destination $dstFile -Force -ErrorAction Stop
        }
    }

    $finalDll = Join-Path $destDir "$name.dll"
    if ($locked) {
        Write-Host "  [OK] Da cap nhat DLL $label (CAD dang mo -> khoi dong lai $label 1 lan de nhan giao dien C# moi): $finalDll" -ForegroundColor Yellow
    } else {
        Write-Host "  [OK] Da xuat DLL $label : $finalDll" -ForegroundColor Green
    }
}

# Chi xuat ban vua build (tranh chep nham DLL cu con sot trong bin\Release tu may khac)
if ($HasZwCad) {
    Publish-CadDll (Join-Path $OhtaRoot "_system\OhtaTool.Plugin\bin\Release\net48") (Join-Path $OhtaRoot "_system\bin\ZwCAD") "ZWCAD"
    Publish-CadDll (Join-Path $OhtaRoot "03_Guro\plugin\GroTool.Plugin\bin\Release\net48") (Join-Path $OhtaRoot "03_Guro\bin\ZwCAD") "ZWCAD" "GROTOOL"
    if ($HasAi) { Publish-CadDll (Join-Path $OhtaRoot "05_AI\plugin\AiTool.Plugin\bin\Release\net48") (Join-Path $OhtaRoot "05_AI\bin\ZwCAD") "ZWCAD" "AITOOL" }
    if ($HasTeams) { Publish-CadDll (Join-Path $OhtaRoot "06_Teams\plugin\TeamsTool.Plugin\bin\Release\net48") (Join-Path $OhtaRoot "06_Teams\bin\ZwCAD") "ZWCAD" "TEAMSTOOL" }
}
if ($AutoCadSdkDir) {
    Publish-CadDll (Join-Path $OhtaRoot "_system\OhtaTool.Plugin\bin\Release\$AutoCadTfm") (Join-Path $OhtaRoot "_system\bin\AutoCAD") "AutoCAD $AutoCadYear"
    Publish-CadDll (Join-Path $OhtaRoot "03_Guro\plugin\GroTool.Plugin\bin\Release\$AutoCadTfm") (Join-Path $OhtaRoot "03_Guro\bin\AutoCAD") "AutoCAD $AutoCadYear" "GROTOOL"
    if ($HasAi) { Publish-CadDll (Join-Path $OhtaRoot "05_AI\plugin\AiTool.Plugin\bin\Release\$AutoCadTfm") (Join-Path $OhtaRoot "05_AI\bin\AutoCAD") "AutoCAD $AutoCadYear" "AITOOL" }
    if ($HasTeams) { Publish-CadDll (Join-Path $OhtaRoot "06_Teams\plugin\TeamsTool.Plugin\bin\Release\$AutoCadTfm") (Join-Path $OhtaRoot "06_Teams\bin\AutoCAD") "AutoCAD $AutoCadYear" "TEAMSTOOL" }
}

Write-Host "`n[2/3] Chay bo Unit Tests OhtaTool.Tests + GroTool.Tests..." -ForegroundColor Cyan
dotnet run --project $TestProj -c Release --nologo
if ($LASTEXITCODE -ne 0) {
    throw "Unit Tests OhtaTool.Tests THAT BAI!"
}
dotnet run --project $GroTestProj -c Release --nologo
if ($LASTEXITCODE -ne 0) {
    throw "Unit Tests GroTool.Tests (03_Guro) THAT BAI!"
}
if ($HasAi) {
    dotnet run --project $AiTestProj -c Release --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Unit Tests AiTool.Tests (05_AI) THAT BAI!"
    }
}
if ($HasTeams) {
    dotnet run --project $TeamsTestProj -c Release --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Unit Tests TeamsTool.Tests (06_Teams) THAT BAI!"
    }
}

Write-Host "`n[3/3] Kiem tra can bang ngoac () va chuan ASCII cho tat ca file .lsp trong OHTA_TOOL..." -ForegroundColor Cyan
$LspFiles = Get-ChildItem -Path $OhtaRoot -Recurse -Filter "*.lsp"
foreach ($f in $LspFiles) {
    $bytes = [System.IO.File]::ReadAllBytes($f.FullName)
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
        throw "File LISP co chua UTF-8 BOM (vi pham rule 04): $($f.FullName)"
    }
    for ($i = 0; $i -lt $bytes.Length; $i++) {
        if ($bytes[$i] -gt 127) {
            throw "File LISP chua ky tu ngoai ASCII tai byte $i (vi pham rule 04): $($f.FullName)"
        }
    }

    $text = [System.Text.Encoding]::ASCII.GetString($bytes)
    $depth = 0
    $inStr = $false
    $inComment = $false
    $escaped = $false
    for ($i = 0; $i -lt $text.Length; $i++) {
        $ch = $text[$i]
        if ($inComment) {
            if ($ch -eq "`n") { $inComment = $false }
            continue
        }
        if ($inStr) {
            if ($escaped) {
                $escaped = $false
            } elseif ($ch -eq '\') {
                $escaped = $true
            } elseif ($ch -eq '"') {
                $inStr = $false
            }
            continue
        }
        if ($ch -eq ';') {
            $inComment = $true
        } elseif ($ch -eq '"') {
            $inStr = $true
        } elseif ($ch -eq '(') {
            $depth++
        } elseif ($ch -eq ')') {
            $depth--
            if ($depth -lt 0) {
                throw "Dong ngoac ')' du thua trong file: $($f.FullName)"
            }
        }
    }
    if ($inStr) {
        throw "Chuoi ngoac kep chua dong trong file: $($f.FullName)"
    }
    if ($depth -ne 0) {
        throw "Lech dau ngoac (depth = $depth) trong file: $($f.FullName)"
    }
    Write-Host "  [OK] LISP chuan ASCII & can bang ngoac: $($f.Name)" -ForegroundColor Green
}

Write-Host "`n=== HOAN TAT: DU AN OHTA_TOOL SAN SANG TREN CA ZWCAD VA AUTOCAD ===" -ForegroundColor Green

