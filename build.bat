@echo off
chcp 65001 >nul
echo ========================================================
echo  Сборка проекта LAN Share Manager (.NET 8 Windows)
echo ========================================================

dotnet --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo [ОШИБКА] .NET 8 SDK не найден на компьютере!
    echo Установите .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo.
echo [1/3] Восстановление зависимостей (dotnet restore)...
dotnet restore LANShareManager.sln
if %ERRORLEVEL% neq 0 (
    echo [ОШИБКА] Не удалось восстановить зависимости.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [2/3] Сборка решения (dotnet build)...
dotnet build LANShareManager.sln -c Release
if %ERRORLEVEL% neq 0 (
    echo [ОШИБКА] Ошибка при компиляции решения.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [3/3] Выполнение модульных тестов (dotnet test)...
dotnet test tests\LANShareManager.Tests\LANShareManager.Tests.csproj -c Release --no-build
if %ERRORLEVEL% neq 0 (
    echo [ПРЕДУПРЕЖДЕНИЕ] Некоторые тесты не пройдены.
)

echo.
echo ========================================================
echo  Сборка успешно завершена!
echo ========================================================
pause
