# Estratégia de entrega e deploy

A API é entregue como imagem Docker no GitHub Container Registry (GHCR) e implantada localmente com Docker Compose, que baixa essa imagem.

```
 código ──► build ──► teste ──► entrega (GHCR) ──► implantação (Compose)
           └──── GitHub Actions (ci.yml) ────┘     └── máquina de destino ──┘
```

## Etapas

### 1. Build (compilar)

Feito pelo job `build-test` do [ci.yml](../.github/workflows/ci.yml), em todo pull request, push no `main` e tag `v*`:

```
dotnet restore
dotnet build --no-restore -c Release
```

Se o código não compila, o pipeline para aqui.

### 2. Teste (xUnit)

No mesmo job, logo depois do build:

```
dotnet test --no-build -c Release
```

Roda os testes unitários de `TaskRules` e os testes de integração dos endpoints com `WebApplicationFactory<Program>`. Se algum teste falha, o job fica vermelho e nenhuma imagem é publicada.

O job ainda roda `dotnet publish` e guarda o resultado como artefato `taskapi-publish`.

### 3. Entrega (imagem publicada no GHCR)

O job `docker` só roda quando o `build-test` passa, e apenas em push no `main` ou em tag `v*`. Ele constrói a imagem com o [Dockerfile](../Dockerfile) e publica em:

```
ghcr.io/juliabrpereira/task-project
```

| Tag da imagem | Quando é gerada | Para que serve |
|---|---|---|
| `sha-<commit>` | Em todo push no `main` e em toda tag | Identifica exatamente o commit que gerou a imagem |
| `v1.0.0` (igual à tag Git) | Quando uma tag `v*` é enviada | Versão de release, com rastreabilidade código ↔ imagem |
| `latest` | Só em push no `main` | Último build do `main` |

Entregar não é implantar: a imagem fica disponível no registry, mas nada roda até a etapa de implantação.

### 4. Implantação (Compose subindo a imagem)

Feita na máquina de destino com o [docker-compose.yml](../docker-compose.yml), que usa a imagem do GHCR. A tag é escolhida pela variável `IMAGE_TAG` (padrão: `latest`).

| Variável | Padrão | Função |
|---|---|---|
| `IMAGE_TAG` | `latest` | Qual versão da imagem implantar |
| `HOST_PORT` | `8080` | Porta da máquina onde a API fica acessível |
| `APP_ENV` | `Production` | Valor de `ASPNETCORE_ENVIRONMENT` |
| `LOG_LEVEL` | `Information` | Nível de log (`Logging__LogLevel__Default`) |

Os valores podem ficar num arquivo `.env` ao lado do `docker-compose.yml`, criado a partir do [.env.example](../.env.example). Veja o [README](../README.md#configuração-de-ambiente).

O Compose tem um healthcheck que chama `GET /health` a cada 30 segundos. Para isso, o Dockerfile instala o `curl` na imagem, porque a imagem `aspnet:10.0` não traz curl nem wget.

## Operação

Os exemplos abaixo usam Bash (Git Bash, Linux, macOS). No PowerShell, defina a variável antes do comando, por exemplo `$env:IMAGE_TAG="v1.0.0"; docker compose up -d`.

### Deploy

```
IMAGE_TAG=v1.0.0 docker compose up -d
docker compose ps
curl http://localhost:8080/health
```

A coluna `STATUS` do `docker compose ps` deve mostrar `(healthy)`.

### Atualização

O pipeline publica uma nova tag de imagem (por exemplo `sha-1a2b3c4` ou `v1.1.0`). Para implantá-la:

```
IMAGE_TAG=v1.1.0 docker compose pull
IMAGE_TAG=v1.1.0 docker compose up -d
```

O Compose recria o container só se a imagem mudou.

### Rollback

Basta subir de novo a tag anterior, que continua no GHCR:

```
IMAGE_TAG=v1.0.0 docker compose up -d
```

Como cada versão é uma imagem imutável, o rollback não exige recompilar nada.

### Parar

```
docker compose down
```

## Rastreabilidade com tags Git

Para gerar uma versão de release:

```
git tag v1.0.0
git push --tags
```

O push da tag dispara o pipeline, que publica a imagem `ghcr.io/juliabrpereira/task-project:v1.0.0`. Assim, a versão em produção aponta para um commit exato do repositório.

## Observações

- **Imagem privada:** na primeira publicação, o GHCR cria o pacote como privado. Para baixar a imagem, torne o pacote público (*Packages → Package settings → Change visibility*) ou faça login com um token que tenha a permissão `read:packages`: `docker login ghcr.io -u <usuario>`.
- **Porta ocupada:** se a porta 8080 já estiver em uso, rode por exemplo `HOST_PORT=8090 docker compose up -d`.
- **Dados em memória:** a API não tem banco de dados, então as tarefas se perdem a cada atualização ou rollback.
