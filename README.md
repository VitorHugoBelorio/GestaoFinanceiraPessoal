# Contorno

Aplicação local de controle financeiro pessoal. Dê nome ao que entra e sai, acompanhe seus saldos e entenda o que fica.

## Estrutura

```text
server/  API ASP.NET Core e Entity Framework Core
app/     aplicação React, TypeScript e Vite
```

O projeto usa o banco SQL Server `GestaoFinanceiraPessoalV2Db`.

## Requisitos

- .NET SDK 10
- Node.js 22 ou superior
- SQL Server disponível em `localhost:1433`

## Configuração do servidor

1. Copie `server/appsettings.Development.example.json` para `appsettings.Development.json` na mesma pasta.
2. Substitua `SUA_SENHA_LOCAL` pela senha do SQL Server local.
3. Crie/atualize o banco:

```powershell
dotnet ef database update --project .\server
```

4. Inicie a API:

```powershell
dotnet run --project .\server
```

A API será exposta em `http://localhost:5133`. O endpoint `GET /health` pode ser usado para verificar a inicialização.

## Configuração da aplicação

```powershell
cd app
npm install
npm run dev
```

A aplicação será exposta em `http://localhost:5173` e encaminhará chamadas `/api` para a API local.

## Fluxo atual

1. Tela pública de boas-vindas.
2. Cadastro ou login.
3. Onboarding com capital inicial, perfil CLT/PJ e configuração de renda.
4. Dashboard com visão geral, lançamentos e espaço reservado para gráficos.
5. Cadastro de receitas, despesas únicas, despesas mensais e compras parceladas com término definido.

As decisões funcionais desta versão estão documentadas em [PRODUCT_DIRECTION.md](PRODUCT_DIRECTION.md).

As responsabilidades das pastas e as convenções de desenvolvimento estão documentadas em [ARCHITECTURE.md](ARCHITECTURE.md).
