@echo off
setlocal
cd /d "%~dp0"
echo Deteniendo restos de compilacion...
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj
echo.
echo Restaurando y compilando el proyecto...
dotnet restore
if errorlevel 1 goto error
dotnet build --no-restore
if errorlevel 1 goto error
echo.
echo Compilacion terminada correctamente.
pause
exit /b 0
:error
echo.
echo La compilacion termino con errores. Revise el mensaje anterior.
pause
exit /b 1
