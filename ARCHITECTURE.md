# Arquitetura e convenções

Este documento define o padrão de organização usado no projeto. A estrutura deve crescer conforme as funcionalidades reais, sem criar camadas vazias ou abstrações antes de existir uma necessidade concreta.

## Frontend (`app`)

```text
src/
  api/                    cliente HTTP e infraestrutura de comunicação
  auth/                   estado e regras de autenticação no cliente
  components/
    layout/               estruturas compartilhadas de página
    ui/                   componentes visuais reutilizáveis e sem regra de negócio
  features/
    <feature>/
      components/         componentes exclusivos da funcionalidade
      <feature>.types.ts  tipos internos da funcionalidade
      <feature>.*.ts      formatadores, serviços ou utilitários específicos
  motion/                 configuração e variantes globais de animação
  pages/                  composição das rotas e coordenação de estado
  theme/                  tema e preferências visuais
  types/                  contratos compartilhados com a API
  utils/                  utilitários genéricos e puros
```

### Responsabilidades

- `pages` coordenam carregamento, estado da rota e composição. Elementos grandes ou reutilizáveis devem ser extraídos.
- `components/ui` não conhece regras financeiras nem chama a API.
- `components/layout` define estruturas compartilhadas entre páginas.
- `features` reúne tudo que pertence exclusivamente a um domínio da interface.
- Chamadas HTTP usam o cliente central em `api/client.ts`; não devem duplicar configuração de `fetch`.
- Tipos compartilhados com o backend ficam em `types/api.ts`; tipos internos permanecem dentro da feature.

## Backend (`server`)

```text
server/
  Contracts/<Feature>/   requests e responses expostos pela API
  Controllers/           entrada HTTP, autorização e tradução da resposta
  Data/                  DbContext, configurações e migrations do EF Core
  Domain/
    Entities/            entidades do domínio
    Enums/               enumerações do domínio
  Extensions/            extensões pequenas e reutilizáveis
  Services/              regras e processos de negócio reutilizáveis
```

### Responsabilidades

- Controllers recebem e validam a requisição, identificam o usuário e devolvem códigos HTTP adequados.
- Regras com reutilização, efeitos em múltiplas entidades ou processamento recorrente pertencem a serviços.
- DTOs de entrada e saída não devem expor diretamente entidades do Entity Framework.
- Consultas somente leitura devem preferir `AsNoTracking`.
- Operações assíncronas recebem `CancellationToken` e usam o sufixo `Async` quando não são ações HTTP.
- Migrations ficam somente em `Data/Migrations` e representam a evolução versionada do banco.
- Segredos e connection strings locais nunca devem ser versionados.

## Convenções gerais

- Componentes React, classes e tipos: `PascalCase`.
- Funções, variáveis e hooks: `camelCase`; hooks começam com `use`.
- Um arquivo deve ter uma responsabilidade principal clara.
- Evitar arquivos `index.ts` apenas para reexportação quando não trouxerem benefício real.
- Evitar dependências circulares e componentes que chamem APIs fora da sua feature.
- Novas abstrações devem surgir por necessidade observada, não por antecipação.
- Toda mudança deve compilar frontend e backend antes de ser considerada concluída.
