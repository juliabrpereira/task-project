# Observabilidade

## Health check

A API usa o mecanismo nativo de health checks do ASP.NET Core:

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<MemoryHealthCheck>("memoria");

app.MapHealthChecks("/health");
```

| Situação | Status HTTP | Corpo |
|---|---|---|
| Saudável | 200 | `Healthy` |
| Com problema | 503 | `Unhealthy` |

O [MemoryHealthCheck](../src/TaskApi.Api/Health/MemoryHealthCheck.cs) compara a memória gerenciada em uso com um limite configurável (`HealthChecks:MemoryLimitMB`, padrão 512 MB). No Compose, o limite vem da variável `HEALTH_MEMORY_LIMIT_MB`.

O `docker-compose.yml` chama o `/health` a cada 30 segundos. Depois de 3 falhas seguidas, o container aparece como `unhealthy` no `docker compose ps`.

## Logs

Os endpoints registram os eventos importantes com `ILogger`, em logs estruturados (os valores entre chaves viram campos pesquisáveis):

| Evento | Nível | Mensagem |
|---|---|---|
| Tarefa criada | Information | `Tarefa {TaskId} criada com título {Title}` |
| Tarefa atualizada | Information | `Tarefa {TaskId} atualizada (concluída: {IsCompleted})` |
| Tarefa removida | Information | `Tarefa {TaskId} removida` |
| Erro de validação | Warning | `Validação falhou ao criar tarefa: {Errors}` |
| Tarefa não encontrada | Warning | `Tarefa {TaskId} não encontrada` |
| Health check falhou | Error | `Uso de memória {UsedMB} MB atingiu o limite de {LimitMB} MB` |

O nível mínimo é controlado por `LOG_LEVEL` no `.env` (padrão `Information`).

### Ver os logs

```
docker compose logs -f
```

Ou, pelo nome do container:

```
docker ps
docker logs <container>
docker logs -f <container>
```

Exemplo de saída depois de criar uma tarefa, enviar um título vazio e buscar um id inexistente:

```
api-1  | info: Program[0]
api-1  |       Tarefa 1 criada com título Estudar observabilidade
api-1  | warn: Program[0]
api-1  |       Validação falhou ao criar tarefa: O título é obrigatório.
api-1  | warn: Program[0]
api-1  |       Tarefa 42 não encontrada
```

No console, `info`, `warn` e `fail` correspondem a Information, Warning e Error.

## Demonstração de falha

### Opção 1: forçar o health check a falhar

Suba a API com um limite de memória que ela certamente ultrapassa:

```
HEALTH_MEMORY_LIMIT_MB=1 docker compose up -d
curl -i http://localhost:8080/health
docker compose logs api
```

No PowerShell: `$env:HEALTH_MEMORY_LIMIT_MB="1"; docker compose up -d`.

Resultado esperado:

```
HTTP/1.1 503 Service Unavailable

Unhealthy
```

```
api-1  | fail: TaskApi.Api.Health.MemoryHealthCheck[0]
api-1  |       Uso de memória 1 MB atingiu o limite de 1 MB
api-1  | fail: Microsoft.Extensions.Diagnostics.HealthChecks.DefaultHealthCheckService[103]
api-1  |       Health check memoria with status Unhealthy completed after 8.5745ms with message 'Memória acima do limite (1/1 MB)'
```

Depois de cerca de 1 minuto e meio (3 verificações de 30 segundos), o `docker compose ps` mostra o container como `unhealthy`.

Para voltar ao normal, suba de novo sem a variável (ou com `HEALTH_MEMORY_LIMIT_MB=512`):

```
docker compose up -d
```

### Opção 2: derrubar o container

```
docker compose stop api
curl http://localhost:8080/health
docker compose ps -a
```

O `curl` falha com conexão recusada e o container aparece como `Exited`. Para subir de novo: `docker compose start api`.
