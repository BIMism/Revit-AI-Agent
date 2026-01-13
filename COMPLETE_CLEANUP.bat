@echo off
echo ========================================
echo BIMism AI Agent - COMPLETE CLEANUP
echo ========================================
echo.
echo This will remove ALL traces of old installations
echo.
pause

echo.
echo [1/4] Closing Revit (if running)...
taskkill /F /IM Revit.exe 2>nul
timeout /t 2 /nobreak >nul

echo [2/4] Removing old installation files...
set ADDIN_PATH=%APPDATA%\Autodesk\Revit\Addins\2025
if exist "%ADDIN_PATH%\BIMism" (
    echo Found BIMism folder - deleting...
    rd /s /q "%ADDIN_PATH%\BIMism"
)
if exist "%ADDIN_PATH%\BIMism.addin" (
    echo Found BIMism.addin file - deleting...
    del /f /q "%ADDIN_PATH%\BIMism.addin"
)

echo [3/4] Clearing Windows assembly cache...
if exist "%TEMP%\RevitAIAgent*" (
    del /f /q "%TEMP%\RevitAIAgent*"
)

echo [4/4] Cleanup complete!
echo.
echo ========================================
echo SUCCESS!
echo ========================================
echo.
echo Now run: BIMism_AI_Agent_Setup_v2.0.0.exe
echo.
pause
