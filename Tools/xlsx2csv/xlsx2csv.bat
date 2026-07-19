@echo off
chcp 65001 >nul 2>&1
cd /d "%~dp0"

echo =====================================
echo   xlsx -^> CSV Batch Converter
echo =====================================
echo.

python xlsx2csv.py %*

if %errorlevel% neq 0 (
    echo.
    echo [Hint] If openpyxl is missing, run: pip install openpyxl
)

pause
