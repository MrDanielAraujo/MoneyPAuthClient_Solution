#!/bin/bash

clear

echo "╔══════════════════════════════════════════════════════════════╗"
echo "║     MoneyP OAuth 2.0 - Menu de Execução                      ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""
echo "Escolha uma opção:"
echo ""
echo "  [1] Executar Aplicação Principal (MoneyPAuthClient)"
echo "  [2] Executar Ferramenta de Validação (ValidationTool)"
echo "  [3] Executar Testes Unitários"
echo "  [4] Compilar Toda a Solução"
echo "  [0] Sair"
echo ""

read -p "Digite a opção desejada: " opcao

case $opcao in
    1)
        echo ""
        echo "Executando MoneyPAuthClient..."
        echo ""
        dotnet run --project src/MoneyPAuthClient/MoneyPAuthClient.csproj
        ;;
    2)
        echo ""
        echo "Executando ValidationTool..."
        echo ""
        dotnet run --project src/ValidationTool/ValidationTool.csproj
        ;;
    3)
        echo ""
        echo "Executando Testes Unitários..."
        echo ""
        dotnet test --verbosity normal
        ;;
    4)
        echo ""
        echo "Compilando toda a solução..."
        echo ""
        dotnet build
        ;;
    0)
        echo "Até logo!"
        exit 0
        ;;
    *)
        echo "Opção inválida!"
        ;;
esac
