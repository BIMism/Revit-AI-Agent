@echo off
echo =================================
echo   BIMism AI Brain (Python V3)
echo =================================

cd /d "%~dp0"
echo Installing Dependencies (First Run Only)...
pip install -r requirements.txt > nul 2>&1

echo.
echo Launching Neural Engine...
python main.py

pause
