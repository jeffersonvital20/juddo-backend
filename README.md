> **Nota de Arquitetura**: Este projeto segue as diretrizes e padrões da **Linha B**.

# 🥋 Judoo Backend API — Sistema Corporativo de Gestão de Torneios de Judô (FIJ)

API de alta performance e baixa latência desenvolvida em **.NET 8** para o gerenciamento em tempo real de campeonatos de Judô segundo o regulamento oficial da **Federação Internacional de Judô (FIJ)**.

Este projeto integra o ecossistema **Judoo**, servindo como núcleo de regras de negócio, persistência e comunicação em tempo real (WebSockets / SignalR) para:

- **Judoo Web (`juddo-web`)**: Portal administrativo, secretaria de pesagem, chaves e telão da arena.
- **Judoo Desktop (`juddo-desktop`)**: Aplicação nativa para a mesa de operação dos mesários nos tatames.

---

## 🚀 Tecnologias Utilizadas

- **Runtime & Framework**: [.NET 8](https://dotnet.microsoft.com/) (`ASP.NET Core Web API`)
- **Arquitetura**: **Clean Architecture** + **DDD (Domain-Driven Design)** + **CQRS** (`MediatR`)
- **Validação**: `FluentValidation`
- **Banco de Dados**: **PostgreSQL 16**
- **Acesso a Dados (Híbrido)**:
  - **Entity Framework Core 8 (`Npgsql`)**: Modelagem, mapeamento e execução automática de _Migrations_.
  - **Dapper**: Consultas e comandos SQL de ultra-baixa latência para placares e eventos de luta.
- **Comunicação em Tempo Real**: **ASP.NET Core SignalR** (`/hubs/tatame`) para atualização instantânea de placares, cronômetros, _Osaekomi_, _Golden Score_ e telões.
- **Segurança & Autenticação**: **JWT Bearer Authentication** + **RBAC** (perfis `Admin`, `Secretaria`, `Mesario`, `Publico`) + hash de senhas PBKDF2.
- **Documentação da API**: **Swagger / OpenAPI** (`Swashbuckle.AspNetCore`)
- **Testes Automatizados**: `xUnit`, `Microsoft.AspNetCore.Mvc.Testing` (Testes de Integração) e `Coverlet` (cobertura mínima de 90%).
- **Containerização & CI/CD**: **Docker**, **Docker Compose** e **GitHub Actions** (pipeline com estágios de Build, Testes Unitários, Testes de Integração e Deploy por ambiente `dev`, `homolog` e `main`).

---

## 📂 Estrutura da Solução

```text
juddo-backend/
├── src/
│   ├── Judoo.Domain/          # Entidades, Enums, Regras FIJ (Placar, Waza-ari, Ippon, Shido, Osaekomi), Interfaces
│   ├── Judoo.Application/     # Casos de Uso CQRS (Commands, Queries, Handlers, DTOs, Validators)
│   ├── Judoo.Infrastructure/  # EF Core DbContext, Migrations, Repositórios Dapper, JWT, SignalR Hub
│   └── Judoo.Api/             # Controllers REST, Configuração de Pipeline, Middlewares, Program.cs
├── tests/
│   ├── Judoo.Domain.UnitTests/    # Testes unitários das regras de domínio e placar FIJ
│   └── Judoo.IntegrationTests/    # Testes integrados de endpoints, autenticação, CQRS e repositórios
├── sql/
│   └── init.sql               # Script SQL de referência da modelagem PostgreSQL
├── .github/workflows/
│   └── ci-cd.yml              # Pipeline CI/CD do GitHub Actions
├── Dockerfile                 # Build multi-stage para produção (.NET 8 SDK -> ASP.NET Runtime)
└── docker-compose.yml         # Orquestração da API + PostgreSQL 16
```

---

## ⚙️ Pré-requisitos

Você pode rodar o projeto de duas maneiras:

1. **Via Docker (Recomendado)**:
   - [Docker](https://www.docker.com/) e **Docker Compose** instalados.
2. **Localmente sem Docker (na máquina)**:
   - [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
   - [PostgreSQL 16+](https://www.postgresql.org/)

---

## 🐳 Como Rodar com Docker (API + PostgreSQL)

Na raiz do projeto `juddo-backend`, execute:

```bash
docker compose up -d --build
```

O Docker Compose irá:

1. Subir o container **`judoo-postgres`** (`postgres:16-alpine`) exposto na porta `5433` do host (`localhost:5433`) com o banco `judoo_db_dev`.
2. Compilar e subir o container **`judoo-api`** na porta `8080`.
3. Executar automaticamente as **Migrations do Entity Framework Core** e criar os **usuários padrão de demonstração** na inicialização.

### Endereços Disponíveis

- **Swagger UI / Documentação Interativa**: [http://localhost:8080](http://localhost:8080)
- **Health Check**: [http://localhost:8080/health](http://localhost:8080/health)
- **SignalR Hub (Tempo Real)**: `ws://localhost:8080/hubs/tatame`
- **Banco PostgreSQL (Host)**: `localhost:5433`
  - **Database**: `judoo_db_dev`
  - **Username**: `judoo_user`
  - **Password**: `judoo_secret`

Para parar os containers:

```bash
docker compose down
```

---

## 💻 Como Rodar Localmente (Desenvolvimento .NET CLI)

1. Suba apenas o banco PostgreSQL via Docker (ou use uma instância local configurada em `src/Judoo.Api/appsettings.Development.json`):
   ```bash
   docker compose up -d postgres
   ```
2. Restaure os pacotes e execute a API:
   ```bash
   dotnet restore Judoo.sln
   dotnet run --project src/Judoo.Api/Judoo.Api.csproj
   ```
   A API aplicará as migrations automaticamente ao iniciar.

---

## 🏛️ Padrões de Projeto (Design Patterns) Justificados (M1)

1. **CQRS + Mediator (`MediatR`)**:
   - **Justificativa**: Separação estrita entre operações de escrita e leitura. Commands utilizam o Entity Framework Core para garantir consistência transacional e regras de negócio da FIJ. Queries utilizam o Dapper para consultas de placar e chaves em tempo real em microssegundos.
2. **Hybrid Repository & Unit of Work**:
   - **Justificativa**: Abstração da camada de persistência permitindo migrações automáticas via EF Core e queries de alto throughput via Dapper sem acoplar a camada de domínio.
3. **Observer Pattern (`ASP.NET Core SignalR`)**:
   - **Justificativa**: Difusão assíncrona bidirecional de eventos do tatame (pontos, faltas, cronômetro) para todos os clientes conectados sem polling HTTP.

---

## 🧪 Como Executar os Testes e Cobertura Mínima (M2)

O projeto possui **cobertura mínima de 90% declarada** no arquivo `coverlet.runsettings`:

```bash
# Executar todos os testes (unitários + integração)
dotnet test Judoo.sln --configuration Release

# Executar com relatório de cobertura Coverlet
dotnet test Judoo.sln --collect:"XPlat Code Coverage" --settings coverlet.runsettings

# Apenas Testes Unitários de Domínio
dotnet test tests/Judoo.Domain.UnitTests/Judoo.Domain.UnitTests.csproj

# Apenas Testes de Integração
dotnet test tests/Judoo.IntegrationTests/Judoo.IntegrationTests.csproj
```

---

## 📊 Observabilidade, Métricas Prometheus e SLO (M3)

- **Logs Estruturados**: Logs no padrão JSON via `ILogger` com correlação de requests e severidade.
- **Endpoint de Métricas**: `http://localhost:8080/metrics` expondo métricas no formato Prometheus:
  - `judoo_uptime_seconds`: Tempo de atividade da aplicação.
  - `judoo_memory_allocated_bytes`: Consumo de memória gerenciada do runtime.
- **SLO (Service Level Objective) Oficial**:
  - **Latência $p95$**: $99\%$ das requisições REST da API devem responder em menos de **$200\text{ms}$** (`p95 < 200ms`).
  - **Disponibilidade**: Disponibilidade da API e Hub SignalR $\ge 99.5\%$.

---

## 📑 Registros de Decisão Arquitetural (ADR) (M4)

Decisões de arquitetura documentadas no padrão internacional **MADR**:
- **[ADR-0001: Adoção de Clean Architecture, CQRS e SignalR para Torneios em Tempo Real](docs/adr/0001-adocao-clean-architecture-cqrs-signalr.md)**

---

## ⚡ OpenAPI & Smoke Test de Performance com k6 (M4)

- **OpenAPI 3.0**: Especificação formal do endpoint principal disponível em [`openapi/openapi.json`](openapi/openapi.json).
- **Smoke Test k6 com p95 medido**: Script disponível em [`k6/smoke-test-campeonatos.js`](k6/smoke-test-campeonatos.js).
  ```bash
  k6 run k6/smoke-test-campeonatos.js
  ```

---
