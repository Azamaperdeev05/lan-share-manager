@echo off
chcp 65001 >nul
echo ========================================================
echo  Публикация LAN Share Manager для Windows x64
echo ========================================================

echo.
echo [1/2] Публикация графического интерфейса (GUI)...
dotnet publish src\LANShareManager.App\LANShareManager.App.csproj -c Release -r win-x64 --self-contained false -o publish\win-x64\
if %ERRORLEVEL% neq 0 (
    echo [ОШИБКА] Не удалось опубликовать GUI.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [2/2] Публикация консольной утилиты (CLI)...
dotnet publish src\LANShareManager.CLI\LANShareManager.CLI.csproj -c Release -r win-x64 --self-contained false -o publish\win-x64\
if %ERRORLEVEL% neq 0 (
    echo [ОШИБКА] Не удалось опубликовать CLI.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ========================================================
echo  Публикация завершена успешно!
echo  Исполняемые файлы находятся в папке: publish\win-x64\
echo    - LANShareManagerGUI.exe  (Графическое приложение)
echo    - LANShareManager.exe     (Консольная утилита)
echo ========================================================
pause
