@echo off
chcp 65001 >nul
cls

echo ╔══════════════════════════════════════════════════════════════╗
echo ║     MoneyP OAuth 2.0 - Menu de Execução                      ║
echo ╚══════════════════════════════════════════════════════════════╝
echo.
echo Escolha uma opção:
echo.
echo   [1] Executar Aplicação Principal (MoneyPAuthClient)
echo   [2] Executar Ferramenta de Validação (ValidationTool)
echo   [3] Executar Testes Unitários
echo   [4] Compilar Toda a Solução
echo   [5] Abrir no Visual Studio
echo   [0] Sair
echo.

set /p opcao="Digite a opção desejada: "

if "%opcao%"=="1" goto app
if "%opcao%"=="2" goto validation
if "%opcao%"=="3" goto tests
if "%opcao%"=="4" goto build
if "%opcao%"=="5" goto vs
if "%opcao%"=="0" goto fim

echo Opção inválida!
pause
goto :eof

:app
echo.
echo Executando MoneyPAuthClient...
echo.
dotnet run --project src/MoneyPAuthClient/MoneyPAuthClient.csproj
pause
goto :eof

:validation
echo.
echo Executando ValidationTool...
echo.
dotnet run --project src/ValidationTool/ValidationTool.csproj
pause
goto :eof

:tests
echo.
echo Executando Testes Unitários...
echo.
dotnet test --verbosity normal
pause
goto :eof

:build
echo.
echo Compilando toda a solução...
echo.
dotnet build
pause
goto :eof

:vs
echo.
echo Abrindo no Visual Studio...
start MoneyPAuthClient.sln
goto :eof

:fim
echo Até logo!
