# MoneyP OAuth 2.0 - Cliente de Autenticação

Cliente de autenticação OAuth 2.0 para a API MoneyP, implementado em **C# .NET 8**, seguindo a documentação oficial.

## Início Rápido

### Pré-requisitos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [OpenSSL](https://www.openssl.org/) (para gerar as chaves)

### Instalação

1. **Clone ou extraia** o projeto
2. **Abra o terminal** na pasta raiz do projeto
3. **Restaure as dependências:**

```bash
dotnet restore
```

### Como Abrir no Visual Studio / Rider

Basta abrir o arquivo `MoneyPAuthClient.sln` na raiz do projeto. Todos os 3 projetos serão carregados automaticamente:

- **MoneyPAuthClient** - Biblioteca principal
- **ValidationTool** - Ferramenta de validação
- **MoneyPAuthClient.Tests** - Testes unitários

## Estrutura do Projeto

```
MoneyPAuthClient_Solution/
│
├── MoneyPAuthClient.sln          # ← ABRA ESTE ARQUIVO NO VISUAL STUDIO
│
├── src/
│   ├── MoneyPAuthClient/         # Biblioteca principal
│   │   ├── Models/               # AuthConfig, TokenResponse
│   │   ├── Services/             # JwtGenerator, MoneyPAuthService
│   │   ├── appsettings.json      # Configurações
│   │   └── Program.cs            # Exemplo de uso
│   │
│   └── ValidationTool/           # Ferramenta de validação interativa
│       └── Program.cs
│
├── tests/
│   └── MoneyPAuthClient.Tests/   # Testes unitários (21 testes)
│
├── run.bat                       # Script de execução (Windows)
├── run.sh                        # Script de execução (Linux/Mac)
└── README.md
```

## Como Executar

### Opção 1: Usando os Scripts (Recomendado)

**Windows:**
```cmd
run.bat
```

**Linux/Mac:**
```bash
chmod +x run.sh
./run.sh
```

O menu interativo permite escolher qual aplicação executar.

### Opção 2: Comandos Diretos

**Compilar toda a solução:**
```bash
dotnet build
```

**Executar a aplicação principal:**
```bash
dotnet run --project src/MoneyPAuthClient/MoneyPAuthClient.csproj
```

**Executar a ferramenta de validação:**
```bash
dotnet run --project src/ValidationTool/ValidationTool.csproj
```

**Executar os testes:**
```bash
dotnet test
```

### Opção 3: Via Visual Studio / Rider

1. Abra `MoneyPAuthClient.sln`
2. No **Solution Explorer**, clique com botão direito no projeto desejado
3. Selecione **"Set as Startup Project"**
4. Pressione **F5** para executar

## Configuração

### 1. Gerar as Chaves RSA

```bash
# Gerar chave privada (2048 bits)
openssl genrsa -out private.pem 2048

# Extrair chave pública
openssl rsa -in private.pem -pubout > public.pem
```

### 2. Configurar o appsettings.json

Edite o arquivo `src/MoneyPAuthClient/appsettings.json`:

```json
{
  "AuthConfig": {
    "ClientId": "SEU_CLIENT_ID_AQUI",
    "TokenEndpoint": "https://auth.moneyp.dev.br/connect/token",
    "PrivateKeyPath": "private.pem",
    "Scopes": "scope1 scope2 scope3",
    "JwtExpirationSeconds": 60
  }
}
```

### 3. Posicionar as Chaves

Copie os arquivos `private.pem` e `public.pem` para:
- `src/MoneyPAuthClient/` (para a aplicação principal)
- `src/ValidationTool/` (para a ferramenta de validação)

Ou especifique o caminho completo no `appsettings.json`.

## Tabela de Configuração

| Parâmetro | Descrição | Exemplo |
|-----------|-----------|---------|
| `ClientId` | ID fornecido pela MoneyP | `"meu_client_id"` |
| `TokenEndpoint` | URL de autenticação | `"https://auth.moneyp.dev.br/connect/token"` |
| `PrivateKeyPath` | Caminho da chave privada | `"private.pem"` ou `"C:/keys/private.pem"` |
| `Scopes` | Escopos separados por espaço | `"pix.read pix.write"` |
| `JwtExpirationSeconds` | Validade do JWT em segundos | `60` |

## Validação da Implementação

### Testes Unitários (21 testes)

```bash
dotnet test
```

Resultado esperado:
```
Passed!  - Failed: 0, Passed: 21, Skipped: 0, Total: 21
```

### Ferramenta de Validação Interativa

```bash
dotnet run --project src/ValidationTool/ValidationTool.csproj
```

A ferramenta valida:
- ✅ Item 1: Chaves RSA (formato, tamanho, assinatura)
- ✅ Item 2: Formato da chave pública para envio
- ✅ Item 3: Configuração do client_id
- ✅ Item 4: Geração do JWT (todas as claims)
- ✅ Item 5: Estrutura da requisição de token

## Uso da Biblioteca

### Exemplo Básico

```csharp
using MoneyPAuthClient.Models;
using MoneyPAuthClient.Services;

var config = new AuthConfig
{
    ClientId = "seu_client_id",
    PrivateKeyPath = "private.pem",
    Scopes = "scope1 scope2"
};

using var authService = new MoneyPAuthService(config);

// Obtém o token (renova automaticamente se expirado)
string token = await authService.GetAccessTokenAsync();

// Use o token nas suas requisições
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);
```

### Com Injeção de Dependência (ASP.NET Core)

```csharp
// Program.cs
builder.Services.AddMoneyPAuthentication(builder.Configuration);

// Em qualquer serviço ou controller
public class MeuServico
{
    private readonly MoneyPAuthService _authService;

    public MeuServico(MoneyPAuthService authService)
    {
        _authService = authService;
    }

    public async Task FazerRequisicao()
    {
        var token = await _authService.GetAccessTokenAsync();
        // ...
    }
}
```

## Troubleshooting

| Erro | Causa | Solução |
|------|-------|---------|
| `FileNotFoundException: private.pem` | Chave não encontrada | Copie `private.pem` para o diretório do projeto |
| `invalid_client` (HTTP 400/401) | client_id incorreto ou chave não corresponde | Verifique o client_id e se a chave pública enviada corresponde à privada |
| `Scopes excedem 300 caracteres` | Muitos scopes | Reduza a quantidade de scopes |
| Projeto não compila | Referências quebradas | Execute `dotnet restore` na raiz |

## Suporte

Se encontrar problemas:
1. Execute `dotnet test` para verificar se a implementação está correta
2. Use a ferramenta de validação para diagnóstico detalhado
3. Verifique se as chaves estão no local correto
