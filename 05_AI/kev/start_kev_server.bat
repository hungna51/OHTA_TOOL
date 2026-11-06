@echo off
chcp 65001 >nul
title OHTA AI - Kev-0.8B Server (Port 8008)
echo ============================================================
echo   OHTA AI - KEV-0.8B DECISION SERVER (LOCAL / LAN :8008)
echo ============================================================
set PYTHONUTF8=1
set HF_HUB_OFFLINE=1
set HF_HUB_DISABLE_SYMLINKS_WARNING=1
set KEV_DTYPE=fp16
set KEV_CUDA_GRAPHS=1
set KEV_FUSED=0
set KEV_TEMPERATURE=1.0

set SCRIPT_DIR=%~dp0
for %%I in ("%SCRIPT_DIR%..\data") do set OHTA_AI_DATA_DIR=%%~fI
set SERVER_SCRIPT=%SCRIPT_DIR%ohta_kev_server.py

set KEV_DIR=%LOCALAPPDATA%\OHTA_TOOL\kev
set KEV_PYTHON=%KEV_DIR%\.venv\Scripts\python.exe
if not exist "%KEV_PYTHON%" (
    echo [LOI] Chua tim thay moi truong Kev tai: %KEV_PYTHON%
    pause
    exit /b 1
)

cd /d "%KEV_DIR%"
set PYTHONPATH=%KEV_DIR%

echo Dang khoi dong model jaredpalmer/kev-0.8b tren cong 8008...
echo Thu muc luat .md dong bo LAN: %OHTA_AI_DATA_DIR%
echo Cac may con trong mang LAN ket noi qua: http://%COMPUTERNAME%:8008
echo.
if exist "%SERVER_SCRIPT%" (
    "%KEV_PYTHON%" "%SERVER_SCRIPT%" --run jaredpalmer/kev-0.8b --host 0.0.0.0 --port 8008
) else (
    "%KEV_PYTHON%" -m kev.serve --run jaredpalmer/kev-0.8b --host 0.0.0.0 --port 8008
)
pause
