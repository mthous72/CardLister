@echo off
echo Converting USER-GUIDE.md to Word document...
echo.

REM Try to find and use pandoc
where pandoc >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo Found pandoc in PATH
    pandoc "Docs\USER-GUIDE.md" -o "Docs\CardLister-User-Guide.docx" --toc --toc-depth=2 -s
    if %ERRORLEVEL% EQU 0 (
        echo.
        echo ✅ Success! Created: Docs\CardLister-User-Guide.docx
        echo.
        pause
        exit /b 0
    ) else (
        echo ❌ Error during conversion
        pause
        exit /b 1
    )
) else (
    echo Pandoc not found in PATH yet.
    echo Please close this window and restart your command prompt/terminal.
    echo After restarting, run this batch file again.
    echo.
    pause
    exit /b 1
)
