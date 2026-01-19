@echo off
echo ===========================================
echo   Auto-Installing Python 3.11 for BIMism
echo ===========================================
echo.
echo Please wait... Downloading and Installing...
echo (If a window pops up asking for permission, click YES)
echo.

winget install -e --id Python.Python.3.11 --scope user --accept-package-agreements --accept-source-agreements --override "/quiet PrependPath=1 Include_test=0"

echo.
echo ===========================================
echo   Python Installation Complete!
echo   You can now run 'run_brain.bat'
echo ===========================================
pause
