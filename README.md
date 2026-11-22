# Task API

API REST de tarefas feita com ASP.NET Core Minimal API (.NET 10), com armazenamento em memória, testes automatizados, imagem Docker e pipeline de CI/CD no GitHub Actions.

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| GET | `/health` | Verificação de saúde |
| GET | `/tasks` | Lista as tarefas |
| GET | `/tasks/{id}` | Busca uma tarefa |
| POST | `/tasks` | Cria uma tarefa |
| PUT | `/tasks/{id}` | Atualiza uma tarefa |
| DELETE | `/tasks/{id}` | Remove uma tarefa |

Regras de negócio: o título é obrigatório (até 100 caracteres) e uma tarefa já concluída não pode ser concluída de novo.

## Pré-requisitos

| Ferramenta | Versão | Quando é necessária |
|---|---|---|
| [Git](https://git-scm.com/) | qualquer versão recente | Sempre |
| [Docker](https://docs.docker.com/get-docker/) com Docker Compose | 24+ | Para rodar com containers |
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | 10.0 | Só para rodar ou testar sem Docker |

O projeto usa .NET 10 (`net10.0`), então o SDK do .NET 8 não serve.

## Rodar com Docker Compose (recomendado)

Funciona igual em qualquer máquina com Docker, sem precisar do .NET instalado:

```
git clone https://github.com/juliabrpereira/task-project.git
cd task-project
cp .env.example .env
docker compose up -d
```

Depois confira:

```
docker compose ps
curl http://localhost:8080/health
```

A resposta esperada é `{"status":"healthy"}`. Para parar, use `docker compose down`.

No PowerShell, troque `cp` por `Copy-Item .env.example .env`.

## Configuração de ambiente

Toda a configuração vem de variáveis de ambiente. O Docker Compose lê o arquivo `.env` da pasta do projeto automaticamente. O [.env.example](.env.example) traz os valores padrão e não tem nenhum dado sensível. O `.env` fica fora do Git (está no `.gitignore`), então cada máquina pode ter seus próprios valores.

| Variável no `.env` | Padrão | Vira, no container | Função |
|---|---|---|---|
| `IMAGE_TAG` | `latest` | tag da imagem | Versão da imagem a implantar (`latest`, `v1.0.0`, `sha-<commit>`) |
| `HOST_PORT` | `8080` | porta publicada | Porta da máquina onde a API fica acessível |
| `APP_ENV` | `Production` | `ASPNETCORE_ENVIRONMENT` | Ambiente do ASP.NET Core (`Development`, `Staging`, `Production`) |
| `LOG_LEVEL` | `Information` | `Logging__LogLevel__Default` | Nível de log (`Trace`, `Debug`, `Information`, `Warning`, `Error`) |

Dentro do container, a API sempre escuta na porta 8080 (`ASPNETCORE_URLS`, definida no Dockerfile). Para mudar a porta de acesso, altere só `HOST_PORT`.

Exemplo de ambiente de desenvolvimento, editando o `.env`:

```
APP_ENV=Development
LOG_LEVEL=Debug
HOST_PORT=8090
```

Rode `docker compose up -d` de novo para aplicar.

## Rodar sem Docker

```
dotnet run --project src/TaskApi.Api
```

A API sobe em `http://localhost:5169` com `ASPNETCORE_ENVIRONMENT=Development` (definido em `launchSettings.json`). As mesmas variáveis do ASP.NET Core funcionam aqui, por exemplo:

```
ASPNETCORE_ENVIRONMENT=Production Logging__LogLevel__Default=Warning dotnet run --project src/TaskApi.Api --no-launch-profile --urls http://localhost:5000
```

## Testes

```
dotnet test
```

Roda os testes unitários da regra de negócio e os testes de integração dos endpoints.

## Build da imagem local

```
docker build -t taskapi:local .
docker run -p 8080:8080 taskapi:local
```

## CI/CD e deploy

O pipeline em [.github/workflows/ci.yml](.github/workflows/ci.yml) compila e testa em todo pull request, e publica a imagem no GHCR (`ghcr.io/juliabrpereira/task-project`) a cada push no `main` e a cada tag `v*`.

Deploy, atualização e rollback estão descritos em [docs/DEPLOY.md](docs/DEPLOY.md).
