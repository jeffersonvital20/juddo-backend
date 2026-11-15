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
  - **Entity Framework Core 8 (`Npgsql`)**: Modelagem, mapeamento e execução automática de *Migrations*.
  - **Dapper**: Consultas e comandos SQL de ultra-baixa latência para placares e eventos de luta.
- **Comunicação em Tempo Real**: **ASP.NET Core SignalR** (`/hubs/tatame`) para atualização instantânea de placares, cronômetros, *Osaekomi*, *Golden Score* e telões.
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

## 🧪 Como Executar os Testes e Cobertura

Para rodar todos os testes (unitários + integração):
```bash
dotnet test Judoo.sln --configuration Release
```

Para rodar separadamente por estágio:
```bash
# Apenas Testes Unitários
dotnet test tests/Judoo.Domain.UnitTests/Judoo.Domain.UnitTests.csproj

# Apenas Testes de Integração
dotnet test tests/Judoo.IntegrationTests/Judoo.IntegrationTests.csproj
```

---

## 🔐 Credenciais Padrão (Seed de Desenvolvimento / Homologação)

Quando `Security:SeedDemoUsers` está ativo (`Development` e `Staging`), os seguintes usuários são criados automaticamente com a senha **`123456`**:

| Perfil (Role) | E-mail | Senha | Permissões Principais |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@judoo.com` | `123456` | Gestão completa de torneios, tatames, chaves, usuários e banco |
| **Secretaria** | `secretaria@judoo.com` | `123456` | Cadastro de atletas, categorias e aferição oficial de pesagem |
| **Mesário** | `mesario1@judoo.com` | `123456` | Operação de placar e cronômetro do tatame em tempo real |
| **Público** | `publico@judoo.com` | `123456` | Visualização de chaves, lutas e placares públicos |

---

## 🔄 Ambientes e CI/CD (GitHub Actions)

O projeto possui arquivos de configuração para 3 ambientes (`.env.development`, `.env.staging`, `.env.production`) e pipeline em `.github/workflows/ci-cd.yml` com 4 estágios:
1. **Build (.NET 8 & Docker)**
2. **Testes Unitários**
3. **Testes de Integração**
4. **Deploy Automático por Branch**:
   - Branch `dev` → Ambiente de **Desenvolvimento**
   - Branch `homolog` → Ambiente de **Homologação (`Staging`)**
   - Branch `main` → Ambiente de **Produção (`Production`)** (executado exclusivamente em commits/merges na `main`)

👉 Para o passo a passo completo de como criar as Secrets no GitHub e publicar em uma VM no Google Cloud (Compute Engine), consulte o guia **[DEPLOY_GCP_GITHUB.md](DEPLOY_GCP_GITHUB.md)**.
